using System;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Yard.Editor
{
    public class GraphView : VisualElement
    {
        private Dictionary<string, NodeView>  nodeViews       = new Dictionary<string, NodeView>();
        private Dictionary<string, StateView> stateViews      = new Dictionary<string, StateView>();
        private List<ConnectionView>          connectionViews = new List<ConnectionView>();
        private Vector2                       panOffset       = Vector2.zero;
        private bool                          isPanning       = false;
        private Vector2                       lastMousePosition;
        private StateView                     selectedStateView;
        private NodeView                      selectedNodeView;
        private Vector2                       lastContextMenuPosition;
        private StateData                     copiedStateData;
        private NodeData                      copiedNodeData;
        public  GraphData                     GraphData { get; private set; }

        // Connection creation state
        private StateData     connectionSourceState;
        private VisualElement connectionPreview;

        public event Action<StateData> OnStateSelected;
        public event Action<NodeData>  OnNodeSelected;


        public GraphView()
        {
            this.AddToClassList("graph-view");

            // Enable mouse events
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Make focusable for keyboard events
            focusable = true;

            // Set up context menu
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            style.overflow = Overflow.Hidden;

            // Ensure this element can receive mouse events
            pickingMode = PickingMode.Position;
        }

        public void LoadGraph(GraphData graph)
        {
            GraphData = graph;
            Clear();

            if (GraphData == null) return;

            // Fix any corrupted data before loading
            FixCorruptedData();

            // Create node views
            foreach (var nodeData in GraphData.Nodes)
            {
                CreateNodeView(nodeData);
            }

            // Create state views from all nodes' states
            foreach (var nodeData in GraphData.Nodes)
            foreach (var stateData in nodeData.States)
                CreateStateView(stateData);

            // Create connection views from all connections
            var allConnections = GraphData.GetAllConnections();
            foreach (var connectionData in allConnections)
                CreateConnectionView(connectionData);

            // Update all node sizes to ensure they encompass their states
            foreach (var nodeView in nodeViews.Values)
                nodeView.UpdateSize();

            // Frame the view to show all content (delay to ensure layout is ready)
            schedule.Execute(() => FrameAll()).ExecuteLater(100);
        }

        private void CreateNodeView(NodeData nodeData)
        {
            var nodeView = new NodeView(nodeData, this);
            nodeViews[nodeData.Id] = nodeView;
            Add(nodeView);
        }

        private void CreateStateView(StateData stateData)
        {
            var stateView = new StateView(stateData, this);
            stateViews[stateData.Id] = stateView;
            Add(stateView);
        }

        private void CreateConnectionView(ConnectionData connectionData)
        {
            var connectionView = new ConnectionView(connectionData, this);
            connectionViews.Add(connectionView);
            Add(connectionView);
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            lastContextMenuPosition = evt.localMousePosition;

            // Check if mouse is over a node (selected or unselected)
            var hoveredNodeView = GetNodeViewAtPosition(evt.localMousePosition);

            // When a node is selected, show "Add State" option
            if (selectedNodeView != null)
            {
                evt.menu.AppendAction("Add State", (a) => CreateStateAssignedTo(lastContextMenuPosition, selectedNodeView.NodeData.Id));

                // Add INode-derived struct types as prenamed node options with auto-assignment
                var nodeTypes = NodeTypeScanner.GetAllINodeStructTypes();
                if (nodeTypes.Count > 0)
                {
                    evt.menu.AppendSeparator();
                    foreach (var nodeType in nodeTypes)
                    {
                        var displayName = NodeTypeScanner.GetDisplayName(nodeType);
                        evt.menu.AppendAction($"Add {displayName} Node", (a) => CreateTypedNodeAssignedTo(lastContextMenuPosition, displayName, selectedNodeView.NodeData.Id));
                    }
                }
            }
            // When right-clicking on an unselected node, show "Add State" for that node
            else if (hoveredNodeView != null)
            {
                evt.menu.AppendAction("Add State", (a) => CreateStateAssignedTo(lastContextMenuPosition, hoveredNodeView.NodeData.Id));
            }
            else
            {
                // When right-clicking empty space, only show "Create Node"
                evt.menu.AppendAction("Create Node", (a) => CreateNode(lastContextMenuPosition));

                // Add INode-derived struct types as prenamed node options
                var nodeTypes = NodeTypeScanner.GetAllINodeStructTypes();
                if (nodeTypes.Count > 0)
                {
                    evt.menu.AppendSeparator();
                    foreach (var nodeType in nodeTypes)
                    {
                        var displayName = NodeTypeScanner.GetDisplayName(nodeType);
                        evt.menu.AppendAction($"Add {displayName} Node", (a) => CreateTypedNode(lastContextMenuPosition, displayName));
                    }
                }
            }

            evt.menu.AppendSeparator();

            if (selectedStateView != null)
            {
                evt.menu.AppendAction("Copy State", (a) => CopyState());
                evt.menu.AppendAction("Paste State", (a) => PasteState(lastContextMenuPosition));
            }
            else if (selectedNodeView != null)
            {
                evt.menu.AppendAction("Copy Node", (a) => CopyNode());
                evt.menu.AppendAction("Paste Node", (a) => PasteNode(lastContextMenuPosition));
            }

            if (copiedStateData != null || copiedNodeData != null)
            {
                if (copiedStateData != null)
                    evt.menu.AppendAction("Paste State", (a) => PasteState(lastContextMenuPosition));
                if (copiedNodeData != null)
                    evt.menu.AppendAction("Paste Node", (a) => PasteNode(lastContextMenuPosition));
            }
        }

        private void CreateNode(Vector2 position)
        {
            if (GraphData == null) return;

            var nodeData = new NodeData("New Node", position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);
        }

        private void CreateNodeAssignedTo(Vector2 position, string parentNodeId)
        {
            if (GraphData == null) return;

            var nodeData = new NodeData("New Node", position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);

            // Auto-assign the new node to the selected parent node by creating a connection
            var parentNode = GraphData.GetNodeById(parentNodeId);
            if (parentNode != null)
            {
                var connection = new ConnectionData(
                    string.Empty, // No source state (node to node connection)
                    string.Empty, // No target state (node to node connection)
                    parentNodeId, // Source node ID
                    nodeData.Id,  // Target node ID
                    true          // IsNodeToNodeConnection
                );

                parentNode.AddConnection(connection);
                RefreshConnections();
            }
        }

        private void CreateTypedNode(Vector2 position, string nodeTypeName)
        {
            if (GraphData == null) return;

            var nodeData = new NodeData(nodeTypeName, position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);
        }

        private void CreateTypedNodeAssignedTo(Vector2 position, string nodeTypeName, string parentNodeId)
        {
            if (GraphData == null) return;

            var nodeData = new NodeData(nodeTypeName, position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);

            // Auto-assign the new node to the selected parent node by creating a connection
            var parentNode = GraphData.GetNodeById(parentNodeId);
            if (parentNode != null)
            {
                var connection = new ConnectionData(
                    string.Empty, // No source state (node to node connection)
                    string.Empty, // No target state (node to node connection)
                    parentNodeId, // Source node ID
                    nodeData.Id,  // Target node ID
                    true          // IsNodeToNodeConnection
                );

                parentNode.AddConnection(connection);
                RefreshConnections();
            }
        }

        private void CreateStateAssignedTo(Vector2 position, string parentNodeId)
        {
            if (GraphData == null) return;

            // Create state with the parent node ID assigned
            var stateData = new StateData("New State", position - panOffset, parentNodeId);
            GraphData.AddState(stateData);
            CreateStateView(stateData);

            // Update the parent node's size to accommodate the new state
            var parentNode = GraphData.GetNodeById(parentNodeId);
            if (parentNode != null)
            {
                var parentNodeView = GetNodeView(parentNodeId);
                if (parentNodeView != null)
                {
                    parentNodeView.UpdateSize();
                }
            }
        }

        public NodeView GetNodeView(string nodeId)
        {
            nodeViews.TryGetValue(nodeId, out var nodeView);
            return nodeView;
        }

        /// <summary>
        /// Gets the NodeView that contains the specified position, if any.
        /// </summary>
        /// <param name="position">Position in local coordinates</param>
        /// <returns>NodeView at position, or null if none found</returns>
        private NodeView GetNodeViewAtPosition(Vector2 position)
        {
            foreach (var nodeView in nodeViews.Values)
            {
                if (nodeView != null && IsPositionInNodeView(position, nodeView))
                    return nodeView;
            }

            return null;
        }

        /// <summary>
        /// Checks if a position is within the bounds of a NodeView.
        /// </summary>
        /// <param name="position">Position in local coordinates</param>
        /// <param name="nodeView">NodeView to check</param>
        /// <returns>True if position is within the node bounds</returns>
        private bool IsPositionInNodeView(Vector2 position, NodeView nodeView)
        {
            var nodeData  = nodeView.NodeData;
            var panOffset = GetPanOffset();

            // Calculate node bounds in local coordinates (with pan offset applied)
            var nodeLeft   = nodeData.Position.x + panOffset.x;
            var nodeTop    = nodeData.Position.y + panOffset.y;
            var nodeRight  = nodeLeft            + nodeData.Size.x;
            var nodeBottom = nodeTop             + nodeData.Size.y;

            return position.x >= nodeLeft && position.x <= nodeRight &&
                position.y    >= nodeTop  && position.y <= nodeBottom;
        }

        public StateView GetStateView(string stateId)
        {
            stateViews.TryGetValue(stateId, out var stateView);
            return stateView;
        }

        public void RemoveNodeView(string nodeId)
        {
            if (nodeViews.TryGetValue(nodeId, out var nodeView))
            {
                // First, remove all state views that belong to this node
                if (GraphData != null)
                {
                    var statesForNode = GraphData.GetStatesForNode(nodeId);
                    foreach (var stateData in statesForNode)
                    {
                        if (stateViews.TryGetValue(stateData.Id, out var stateView))
                        {
                            stateViews.Remove(stateData.Id);
                            stateView.RemoveFromHierarchy();
                        }
                    }
                }

                // Remove the node view
                nodeViews.Remove(nodeId);
                nodeView.RemoveFromHierarchy();

                // Remove from graph data (this will also remove the state data)
                if (GraphData != null)
                {
                    var nodeData = GraphData.GetNodeById(nodeId);
                    if (nodeData != null)
                    {
                        GraphData.RemoveNode(nodeData);
                    }
                }

                RefreshConnections();
            }
        }

        public void RemoveStateView(string stateId)
        {
            Assert.IsNotNull(GraphData);

            if (!stateViews.TryGetValue(stateId, out var stateView))
                return;
            stateViews.Remove(stateId);
            stateView.RemoveFromHierarchy();

            var stateData = GraphData.GetStateById(stateId);
            if (stateData != null)
            {
                GraphData.RemoveState(stateData);
            }

            RefreshConnections();
        }

        public void RefreshConnections()
        {
            // Remove all connection views
            foreach (var connectionView in connectionViews)
            {
                connectionView.RemoveFromHierarchy();
            }

            connectionViews.Clear();

            // Recreate connection views
            var allConnections = GraphData?.GetAllConnections() ?? new List<ConnectionData>();
            foreach (var connectionData in allConnections)
            {
                CreateConnectionView(connectionData);
            }
        }

        public void UpdateConnections()
        {
            // Just update existing connections without recreating them
            foreach (var connectionView in connectionViews)
            {
                connectionView.UpdateConnection();
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 2) // Middle mouse button
            {
                isPanning         = true;
                lastMousePosition = evt.localMousePosition;
                this.CaptureMouse();
                evt.StopPropagation();
            }
            else if (evt.button == 1) // Right mouse button for panning (alternative)
            {
                if (evt.ctrlKey) // Ctrl + Right mouse for panning
                {
                    isPanning         = true;
                    lastMousePosition = evt.localMousePosition;
                    this.CaptureMouse();
                    evt.StopPropagation();
                }
            }
            else if (evt.button == 0) // Left mouse button on empty space
            {
                DeselectAll();
                this.Focus(); // Ensure GraphView has focus for keyboard events
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isPanning)
            {
                var delta = evt.localMousePosition - lastMousePosition;
                panOffset         += delta;
                lastMousePosition =  evt.localMousePosition;

                // Update all positions
                UpdateAllPositions();

                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if ((evt.button == 2 || (evt.button == 1 && evt.ctrlKey)) && isPanning)
            {
                isPanning = false;
                this.ReleaseMouse();
                evt.StopPropagation();
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            // Zoom functionality could be added here
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                if (selectedStateView != null)
                {
                    RemoveStateView(selectedStateView.StateData.Id);
                }
                else if (selectedNodeView != null)
                {
                    RemoveNodeView(selectedNodeView.NodeData.Id);
                }

                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.C)
            {
                if (selectedStateView != null)
                {
                    CopyState();
                }
                else if (selectedNodeView != null)
                {
                    CopyNode();
                }

                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.V)
            {
                var mousePos = lastContextMenuPosition;
                if (copiedStateData != null)
                {
                    PasteState(mousePos);
                }
                else if (copiedNodeData != null)
                {
                    PasteNode(mousePos);
                }

                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.F)
            {
                FrameAll();
                evt.StopPropagation();
            }
        }

        public Vector2 GetPanOffset()
        {
            // Safety check to prevent NaN values
            if (float.IsNaN(panOffset.x) || float.IsNaN(panOffset.y))
            {
                panOffset = Vector2.zero;
            }

            return panOffset;
        }


        public void SelectState(StateView stateView)
        {
            // Deselect previous selections
            if (selectedStateView != null)
            {
                selectedStateView.SetSelected(false);
            }

            if (selectedNodeView != null)
            {
                selectedNodeView.SetSelected(false);
            }

            // Select new state
            selectedStateView = stateView;
            selectedNodeView  = null;

            if (selectedStateView != null)
            {
                selectedStateView.SetSelected(true);
                OnStateSelected?.Invoke(selectedStateView.StateData);
            }
            else
            {
                OnStateSelected?.Invoke(null);
            }

            // Update connection opacity based on new selection
            UpdateConnectionOpacity();
        }

        public void SelectNode(NodeView nodeView)
        {
            // Deselect previous selections
            if (selectedStateView != null)
            {
                selectedStateView.SetSelected(false);
            }

            if (selectedNodeView != null)
            {
                selectedNodeView.SetSelected(false);
            }

            // Select new node
            selectedNodeView  = nodeView;
            selectedStateView = null;

            if (selectedNodeView != null)
            {
                selectedNodeView.SetSelected(true);
                OnNodeSelected?.Invoke(selectedNodeView.NodeData);
            }
            else
            {
                OnNodeSelected?.Invoke(null);
            }

            // Update connection opacity based on new selection
            UpdateConnectionOpacity();
        }

        public void DeselectAll()
        {
            SelectState(null);
            SelectNode(null);
            // UpdateConnectionOpacity is called by SelectState/SelectNode
        }

        /// <summary>
        /// Updates the opacity of all connections based on current selection state
        /// </summary>
        private void UpdateConnectionOpacity()
        {
            foreach (var connectionView in connectionViews)
            {
                connectionView.MarkDirtyRepaint();
            }
        }

        private void CopyState()
        {
            if (selectedStateView != null)
            {
                copiedStateData = selectedStateView.StateData;
                copiedNodeData  = null;
            }
        }

        private void CopyNode()
        {
            if (selectedNodeView != null)
            {
                copiedNodeData  = selectedNodeView.NodeData;
                copiedStateData = null;
            }
        }

        private void PasteState(Vector2 position)
        {
            if (copiedStateData == null || GraphData == null) return;

            var newState = new StateData(copiedStateData.Name + " Copy", position - panOffset, copiedStateData.ParentNodeId);

            // Copy connections
            foreach (var connection in copiedStateData.OutgoingConnections)
            {
                var newConnection = new ConnectionData(
                    newState.Id,
                    connection.TargetStateId,
                    newState.ParentNodeId,
                    connection.TargetNodeId,
                    connection.IsNodeToNodeConnection
                );
                newState.AddConnection(newConnection);
            }

            GraphData.AddState(newState);
            CreateStateView(newState);

            // Update parent node size if assigned
            if (!string.IsNullOrEmpty(newState.ParentNodeId))
            {
                var parentNodeView = GetNodeView(newState.ParentNodeId);
                if (parentNodeView != null)
                {
                    parentNodeView.UpdateSize();
                }
            }

            RefreshConnections();
        }

        private void PasteNode(Vector2 position)
        {
            if (copiedNodeData == null || GraphData == null) return;

            var newNode = new NodeData(copiedNodeData.Name + " Copy", position - panOffset);
            newNode.Size = copiedNodeData.Size;

            // Copy connections
            foreach (var connection in copiedNodeData.OutgoingConnections)
            {
                var newConnection = new ConnectionData(
                    connection.SourceStateId,
                    connection.TargetStateId,
                    newNode.Id,
                    connection.TargetNodeId,
                    connection.IsNodeToNodeConnection
                );
                newNode.AddConnection(newConnection);
            }

            GraphData.AddNode(newNode);
            CreateNodeView(newNode);

            // Copy states belonging to the node
            var originalStates = GraphData.GetStatesForNode(copiedNodeData.Id);
            foreach (var originalState in originalStates)
            {
                var offset   = originalState.Position - copiedNodeData.Position;
                var newState = new StateData(originalState.Name, newNode.Position + offset, newNode.Id);

                // Copy state connections
                foreach (var connection in originalState.OutgoingConnections)
                {
                    var newConnection = new ConnectionData(
                        newState.Id,
                        connection.TargetStateId,
                        newNode.Id,
                        connection.TargetNodeId,
                        connection.IsNodeToNodeConnection
                    );
                    newState.AddConnection(newConnection);
                }

                GraphData.AddState(newState);
                CreateStateView(newState);
            }

            RefreshConnections();
        }

        public void FrameAll()
        {
            if (GraphData == null) return;

            // Check if layout is valid
            if (layout.width <= 0 || layout.height <= 0)
            {
                // Layout not ready yet, just reset pan offset
                panOffset = Vector2.zero;
                return;
            }

            // Calculate bounds of all nodes and states
            var bounds = CalculateContentBounds();

            if (bounds.size == Vector2.zero)
            {
                // No content, center the view
                panOffset = Vector2.zero;
                return;
            }

            // Add padding around the content
            const float padding = 50f;
            bounds.xMin -= padding;
            bounds.yMin -= padding;
            bounds.xMax += padding;
            bounds.yMax += padding;

            // Calculate the center of the bounds
            var boundsCenter = bounds.center;

            // Calculate the center of the view
            var viewCenter = new Vector2(layout.width * 0.5f, layout.height * 0.5f);

            // Set pan offset to center the content
            panOffset = viewCenter - boundsCenter;

            // Update all positions
            UpdateAllPositions();
        }

        private void FixCorruptedData()
        {
            if (GraphData == null) return;

            // Fix corrupted node positions (including extremely large values)
            for (int i = 0; i < GraphData.Nodes.Count; i++)
            {
                var node = GraphData.Nodes[i];
                if (float.IsNaN(node.Position.x)       || float.IsNaN(node.Position.y) ||
                    Mathf.Abs(node.Position.x) > 10000 || Mathf.Abs(node.Position.y) > 10000)
                {
                    UnityEngine.Debug.LogWarning($"Fixed corrupted position for node: {node.Name}");
                    node.Position = new Vector2(100 + (i % 3) * 300, 100 + (i / 3) * 200);
                }

                if (float.IsNaN(node.Size.x) || float.IsNaN(node.Size.y) ||
                    node.Size.x <= 0         || node.Size.y <= 0)
                {
                    UnityEngine.Debug.LogWarning($"Fixed corrupted size for node: {node.Name}");
                    node.Size = new Vector2(200, 100);
                }
            }

            // Fix corrupted state positions (including extremely large values)
            int stateIndex = 0;
            foreach (var node in GraphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    if (float.IsNaN(state.Position.x)       || float.IsNaN(state.Position.y) ||
                        Mathf.Abs(state.Position.x) > 10000 || Mathf.Abs(state.Position.y) > 10000)
                    {
                        UnityEngine.Debug.LogWarning($"Fixed corrupted position for state: {state.Name}");
                        // Position states in a grid if they're corrupted
                        state.Position = new Vector2(150 + (stateIndex % 5) * 120, 150 + (stateIndex / 5) * 50);
                    }

                    stateIndex++;
                }
            }
        }

        private Rect CalculateContentBounds()
        {
            // Check if we have any nodes (states are now contained within nodes)
            if (GraphData == null || GraphData.Nodes.Count == 0)
                return new Rect(0, 0, 0, 0);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            // Include all nodes
            foreach (var node in GraphData.Nodes)
            {
                minX = Mathf.Min(minX, node.Position.x);
                minY = Mathf.Min(minY, node.Position.y);
                maxX = Mathf.Max(maxX, node.Position.x + node.Size.x);
                maxY = Mathf.Max(maxY, node.Position.y + node.Size.y);
            }

            // Include all states from all nodes
            foreach (var node in GraphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    const float stateWidth  = 100f; // Approximate state width
                    const float stateHeight = 25f;  // Approximate state height

                    minX = Mathf.Min(minX, state.Position.x);
                    minY = Mathf.Min(minY, state.Position.y);
                    maxX = Mathf.Max(maxX, state.Position.x + stateWidth);
                    maxY = Mathf.Max(maxY, state.Position.y + stateHeight);
                }
            }

            // If no valid bounds found, return zero rect
            if (minX == float.MaxValue)
            {
                return new Rect(0, 0, 0, 0);
            }

            var result = new Rect(minX, minY, maxX - minX, maxY - minY);
            return result;
        }

        private void UpdateAllPositions()
        {
            // Update all node positions
            foreach (var nodeView in nodeViews.Values) 
                nodeView.UpdatePosition();

            // Update all state positions
            foreach (var stateView in stateViews.Values) 
                stateView.UpdatePosition();

            // Update all connections
            UpdateConnections();
        }
    }
}