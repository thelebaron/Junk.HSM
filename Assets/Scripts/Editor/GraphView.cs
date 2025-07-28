using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphView : VisualElement
{
    private GraphData graphData;
    private Dictionary<string, NodeView> nodeViews = new Dictionary<string, NodeView>();
    private Dictionary<string, StateView> stateViews = new Dictionary<string, StateView>();
    private List<ConnectionView> connectionViews = new List<ConnectionView>();
    private Vector2 panOffset = Vector2.zero;
    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private StateView selectedStateView;
    private NodeView selectedNodeView;
    private Vector2 lastContextMenuPosition;
    private StateData copiedStateData;
    private NodeData copiedNodeData;

    public event Action<StateData> OnStateSelected;
    public event Action<NodeData> OnNodeSelected;



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
        graphData = graph;
        Clear();

        if (graphData == null) return;

        // Fix any corrupted data before loading
        FixCorruptedData();

        // Create node views
        foreach (var nodeData in graphData.Nodes)
        {
            CreateNodeView(nodeData);
        }

        // Create state views (states are now independent of nodes visually)
        foreach (var stateData in graphData.States)
        {
            CreateStateView(stateData);
        }

        // Create connection views from all connections
        var allConnections = graphData.GetAllConnections();
        foreach (var connectionData in allConnections)
        {
            CreateConnectionView(connectionData);
        }

        // Update all node sizes to ensure they encompass their states
        foreach (var nodeView in nodeViews.Values)
        {
            nodeView.UpdateSize();
        }

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
        evt.menu.AppendAction("Create Node", (a) => CreateNode(lastContextMenuPosition));
        evt.menu.AppendAction("Create State", (a) => CreateState(lastContextMenuPosition));
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
        if (graphData == null) return;

        var nodeData = new NodeData("New Node", position - panOffset);
        graphData.AddNode(nodeData);
        CreateNodeView(nodeData);
    }

    private void CreateState(Vector2 position)
    {
        if (graphData == null) return;

        var stateData = new StateData("New State", position - panOffset, string.Empty);
        graphData.AddState(stateData);
        CreateStateView(stateData);
    }

    public NodeView GetNodeView(string nodeId)
    {
        nodeViews.TryGetValue(nodeId, out var nodeView);
        return nodeView;
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
            nodeViews.Remove(nodeId);
            nodeView.RemoveFromHierarchy();

            // Remove from graph data
            if (graphData != null)
            {
                var nodeData = graphData.GetNodeById(nodeId);
                if (nodeData != null)
                {
                    graphData.RemoveNode(nodeData);
                }
            }

            RefreshConnections();
        }
    }

    public void RemoveStateView(string stateId)
    {
        if (stateViews.TryGetValue(stateId, out var stateView))
        {
            stateViews.Remove(stateId);
            stateView.RemoveFromHierarchy();

            // Remove from graph data
            if (graphData != null)
            {
                var stateData = graphData.GetStateById(stateId);
                if (stateData != null)
                {
                    graphData.RemoveState(stateData);
                }
            }

            RefreshConnections();
        }
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
        var allConnections = graphData?.GetAllConnections() ?? new List<ConnectionData>();
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
            isPanning = true;
            lastMousePosition = evt.localMousePosition;
            this.CaptureMouse();
            evt.StopPropagation();
        }
        else if (evt.button == 1) // Right mouse button for panning (alternative)
        {
            if (evt.ctrlKey) // Ctrl + Right mouse for panning
            {
                isPanning = true;
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
            panOffset += delta;
            lastMousePosition = evt.localMousePosition;

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

    public GraphData GetGraphData()
    {
        return graphData;
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
        selectedNodeView = null;

        if (selectedStateView != null)
        {
            selectedStateView.SetSelected(true);
            OnStateSelected?.Invoke(selectedStateView.StateData);
        }
        else
        {
            OnStateSelected?.Invoke(null);
        }
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
        selectedNodeView = nodeView;
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
    }

    public void DeselectAll()
    {
        SelectState(null);
        SelectNode(null);
    }

    private void CopyState()
    {
        if (selectedStateView != null)
        {
            copiedStateData = selectedStateView.StateData;
            copiedNodeData = null;
        }
    }

    private void CopyNode()
    {
        if (selectedNodeView != null)
        {
            copiedNodeData = selectedNodeView.NodeData;
            copiedStateData = null;
        }
    }

    private void PasteState(Vector2 position)
    {
        if (copiedStateData == null || graphData == null) return;

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

        graphData.AddState(newState);
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
        if (copiedNodeData == null || graphData == null) return;

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

        graphData.AddNode(newNode);
        CreateNodeView(newNode);

        // Copy states belonging to the node
        var originalStates = graphData.GetStatesForNode(copiedNodeData.Id);
        foreach (var originalState in originalStates)
        {
            var offset = originalState.Position - copiedNodeData.Position;
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

            graphData.AddState(newState);
            CreateStateView(newState);
        }

        RefreshConnections();
    }

    public void FrameAll()
    {
        if (graphData == null) return;

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
        if (graphData == null) return;

        // Fix corrupted node positions (including extremely large values)
        for (int i = 0; i < graphData.Nodes.Count; i++)
        {
            var node = graphData.Nodes[i];
            if (float.IsNaN(node.Position.x) || float.IsNaN(node.Position.y) ||
                Mathf.Abs(node.Position.x) > 10000 || Mathf.Abs(node.Position.y) > 10000)
            {
                UnityEngine.Debug.LogWarning($"Fixed corrupted position for node: {node.Name}");
                node.Position = new Vector2(100 + (i % 3) * 300, 100 + (i / 3) * 200);
            }

            if (float.IsNaN(node.Size.x) || float.IsNaN(node.Size.y) ||
                node.Size.x <= 0 || node.Size.y <= 0)
            {
                UnityEngine.Debug.LogWarning($"Fixed corrupted size for node: {node.Name}");
                node.Size = new Vector2(200, 100);
            }
        }

        // Fix corrupted state positions (including extremely large values)
        for (int i = 0; i < graphData.States.Count; i++)
        {
            var state = graphData.States[i];
            if (float.IsNaN(state.Position.x) || float.IsNaN(state.Position.y) ||
                Mathf.Abs(state.Position.x) > 10000 || Mathf.Abs(state.Position.y) > 10000)
            {
                UnityEngine.Debug.LogWarning($"Fixed corrupted position for state: {state.Name}");
                // Position states in a grid if they're corrupted
                state.Position = new Vector2(150 + (i % 5) * 120, 150 + (i / 5) * 50);
            }
        }
    }

    private Rect CalculateContentBounds()
    {
 
        if (graphData == null || (graphData.Nodes.Count == 0 && graphData.States.Count == 0))
        {
            return new Rect(0, 0, 0, 0);
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        // Include all nodes
        foreach (var node in graphData.Nodes)
        {
            minX = Mathf.Min(minX, node.Position.x);
            minY = Mathf.Min(minY, node.Position.y);
            maxX = Mathf.Max(maxX, node.Position.x + node.Size.x);
            maxY = Mathf.Max(maxY, node.Position.y + node.Size.y);
        }

        // Include all states
        foreach (var state in graphData.States)
        {
            const float stateWidth = 100f; // Approximate state width
            const float stateHeight = 25f; // Approximate state height

            minX = Mathf.Min(minX, state.Position.x);
            minY = Mathf.Min(minY, state.Position.y);
            maxX = Mathf.Max(maxX, state.Position.x + stateWidth);
            maxY = Mathf.Max(maxY, state.Position.y + stateHeight);
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
        {
            nodeView.UpdatePosition();
        }

        // Update all state positions
        foreach (var stateView in stateViews.Values)
        {
            stateView.UpdatePosition();
        }

        // Update all connections
        UpdateConnections();
    }
}
