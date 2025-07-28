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

    public event Action<StateData> OnStateSelected;
    public event Action<NodeData> OnNodeSelected;

    public new class UxmlFactory : UxmlFactory<GraphView, UxmlTraits> { }

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
    }

    public void LoadGraph(GraphData graph)
    {
        graphData = graph;
        Clear();

        if (graphData == null) return;

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
        evt.menu.AppendAction("Create Node", (a) => CreateNode(evt.localMousePosition));
        evt.menu.AppendAction("Create State", (a) => CreateState(evt.localMousePosition));
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

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 2) // Middle mouse button
        {
            isPanning = true;
            lastMousePosition = evt.localMousePosition;
            this.CaptureMouse();
            evt.StopPropagation();
        }
        else if (evt.button == 0) // Left mouse button on empty space
        {
            DeselectAll();
        }
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (isPanning)
        {
            var delta = evt.localMousePosition - lastMousePosition;
            panOffset += delta;
            lastMousePosition = evt.localMousePosition;
            
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
            foreach (var connectionView in connectionViews)
            {
                connectionView.UpdateConnection();
            }
            
            evt.StopPropagation();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 2 && isPanning)
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
            // Delete selected nodes
            evt.StopPropagation();
        }
    }

    public Vector2 GetPanOffset()
    {
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
}
