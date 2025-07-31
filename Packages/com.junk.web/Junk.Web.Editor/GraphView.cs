using System;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public partial class GraphView : VisualElement
    {
        private Dictionary<string, NodeView>  nodeViews       = new();
        private Dictionary<string, StateView> stateViews      = new();
        private List<ConnectionView>          connectionViews = new();
        private Vector2                       panOffset       = Vector2.zero;
        private float                         zoomLevel       = 1.0f;
        private const float                   MinZoom         = 0.25f;
        private const float                   MaxZoom         = 3.0f;
        private const float                   ZoomSpeed       = 0.1f;
        private bool                          isPanning;
        private Vector2                       lastMousePosition;
        private StateView                     selectedStateView;
        private NodeView                      selectedNodeView;
        private Vector2                       lastContextMenuPosition;
        private StateData                     copiedStateData;
        private NodeData                      copiedNodeData;
        private VisualElement                 contentContainer;
        public  GraphData                     GraphData { get; private set; }

        // Connection creation state
        private StateData     connectionSourceState;
        private VisualElement connectionPreview;

        public event Action<StateData> OnStateSelected;
        public event Action<NodeData>  OnNodeSelected;
        
        public GraphView()
        {
            this.AddToClassList("graph-view");

            // Create content container that will hold all graph elements
            contentContainer = new VisualElement();
            contentContainer.AddToClassList("graph-content");
            contentContainer.style.position = Position.Absolute;
            contentContainer.style.left = 0;
            contentContainer.style.top = 0;
            contentContainer.style.width = Length.Percent(100);
            contentContainer.style.height = Length.Percent(100);
            contentContainer.style.overflow = Overflow.Visible;
            contentContainer.pickingMode = PickingMode.Ignore; // Let events pass through to GraphView
            Add(contentContainer);

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
            ClearGraphVisuals();
            
            if (graph == null) 
                return;
            // Fix any corrupted data before loading
            EnsureValidData();

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

            // Update colors for all nodes and states
            foreach (var nodeView in nodeViews.Values)
                nodeView.UpdateColors();
            
            // Apply initial zoom
            ApplyZoom();
            
            // Defer size calculation and framing until after the layout pass
            schedule.Execute(UpdateAllNodeSizesAndFrame).ExecuteLater(1000);
        }

        private void ClearGraphVisuals()
        {
            // Clear visual elements from the content container only
            contentContainer?.Clear();

            // Clear dictionaries and lists
            nodeViews.Clear();
            stateViews.Clear();
            connectionViews.Clear();
        }

        private void UpdateAllNodeSizesAndFrame()
        {
            // Update all node sizes to ensure they encompass their states
            foreach (var nodeView in nodeViews.Values)
            {
                nodeView.UpdateSize();
            }

            // Frame the view to show all content
            FrameAll();
        }

        private void CreateNodeView(NodeData nodeData)
        {
            var nodeView = new NodeView(nodeData, this);
            nodeViews[nodeData.Id] = nodeView;
            contentContainer.Add(nodeView);

            // Ensure colors are applied after creation
            nodeView.UpdateColors();
        }

        private void CreateStateView(StateData stateData, bool autoSelect = false)
        {
            var stateView = new StateView(stateData, this);
            stateViews[stateData.Id] = stateView;
            contentContainer.Add(stateView);

            // Ensure colors are applied after creation
            stateView.UpdateColors();

            // Auto-select the state if requested
            if (autoSelect)
            {
                SelectState(stateView);
            }
        }

        private void CreateConnectionView(ConnectionData connectionData)
        {
            var connectionView = new ConnectionView(connectionData, this);
            connectionViews.Add(connectionView);
            contentContainer.Add(connectionView);
        }

        

        private void CreateNode(Vector2 position)
        {
            var nodeData = new NodeData("New Node", position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);
        }
        
        private void CreateTypedNode(Vector2 position, string nodeTypeName)
        {
            var nodeData = new NodeData(nodeTypeName, position - panOffset);
            GraphData.AddNode(nodeData);
            CreateNodeView(nodeData);
        }

        private void CreateTypedNodeAssignedTo(Vector2 position, string nodeTypeName, string parentNodeId)
        {
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
            // Create state with the parent node ID assigned
            var stateData = new StateData("New State", position - panOffset, parentNodeId);
            GraphData.AddState(stateData);
            CreateStateView(stateData, autoSelect: true);

            // Update the parent node's size to accommodate the new state
            var parentNode = GraphData.GetNodeById(parentNodeId);
            if (parentNode == null)
            {
                Debug.LogError($"Parent node not found for ID: {parentNodeId}");
                return;
            }

            // Defer the size update until after the layout pass to ensure StateView dimensions are calculated
            var parentNodeView = GetNodeView(parentNodeId);
            if (parentNodeView != null) 
                schedule.Execute(() => parentNodeView.UpdateSize()).ExecuteLater(50);
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
                if (IsPositionInNodeView(position, nodeView))
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
            var nodeRight  = nodeLeft            + nodeView.layout.width;
            var nodeBottom = nodeTop             + nodeView.layout.height;

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
                var statesForNode = GraphData.GetStatesForNode(nodeId);
                foreach (var stateData in statesForNode)
                {
                    if (stateViews.TryGetValue(stateData.Id, out var stateView))
                    {
                        stateViews.Remove(stateData.Id);
                        stateView.RemoveFromHierarchy();
                    }
                }

                // Remove the node view
                nodeViews.Remove(nodeId);
                nodeView.RemoveFromHierarchy();

                // Remove from graph data (this will also remove the state data)
                var nodeData = GraphData.GetNodeById(nodeId);
                if (nodeData != null)
                {
                    GraphData.RemoveNode(nodeData);
                }

                RefreshConnections();
            }
        }

        public void RemoveStateView(string stateId)
        {
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
            if (copiedStateData == null) 
                return;

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
            CreateStateView(newState, autoSelect: true);

            // Update parent node size if assigned
            if (!string.IsNullOrEmpty(newState.ParentNodeId))
            {
                var parentNodeView = GetNodeView(newState.ParentNodeId);
                if (parentNodeView != null)
                {
                    // Defer the size update until after the layout pass to ensure StateView dimensions are calculated
                    schedule.Execute(() => parentNodeView.UpdateSize()).ExecuteLater(50);
                }
            }

            RefreshConnections();
        }

        private void PasteNode(Vector2 position)
        {
            if (copiedNodeData == null) return;

            var newNode = new NodeData(copiedNodeData.Name + " Copy", position - panOffset);

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
                CreateStateView(newState, autoSelect: true);
            }

            RefreshConnections();
        }

        public void FrameAll()
        {
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
                var nodeView = GetNodeView(node.Id);
                if (nodeView == null) continue;

                minX = Mathf.Min(minX, node.Position.x);
                minY = Mathf.Min(minY, node.Position.y);
                maxX = Mathf.Max(maxX, node.Position.x + nodeView.layout.width);
                maxY = Mathf.Max(maxY, node.Position.y + nodeView.layout.height);
            }

            // Include all states from all nodes
            foreach (var node in GraphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    var stateView = GetStateView(state.Id);
                    if (stateView == null) continue;

                    minX = Mathf.Min(minX, state.Position.x);
                    minY = Mathf.Min(minY, state.Position.y);
                    maxX = Mathf.Max(maxX, state.Position.x + stateView.layout.width);
                    maxY = Mathf.Max(maxY, state.Position.y + stateView.layout.height);
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

        private void ApplyZoom()
        {
            // Apply zoom scaling to the content container only
            // This maintains element relationships while keeping GraphView at full size
            contentContainer.style.scale = new StyleScale(new Scale(new Vector3(zoomLevel, zoomLevel, 1f)));
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Convert screen position to world position considering zoom and pan
            return (screenPosition - panOffset) / zoomLevel;
        }

        private Vector2 WorldToScreen(Vector2 worldPosition)
        {
            // Convert world position to screen position considering zoom and pan
            return worldPosition * zoomLevel + panOffset;
        }

        public float GetZoomLevel()
        {
            return zoomLevel;
        }
    }
}