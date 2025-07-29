using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeView : VisualElement
{
    private NodeData nodeData;
    private GraphView graphView;
    private Label titleLabel;
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private bool isSelected = false;

    public NodeData NodeData => nodeData;

    public NodeView(NodeData data, GraphView parent)
    {
        nodeData = data;
        graphView = parent;

        AddToClassList("node");

        // Create title bar
        var titleBar = new VisualElement();
        titleBar.AddToClassList("node-title-bar");

        titleLabel = new Label(nodeData.Name);
        titleLabel.AddToClassList("node-title");
        titleBar.Add(titleLabel);

        Add(titleBar);

        // Set initial position and size
        UpdatePosition();
        UpdateSize();

        // Register events
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseUpEvent>(OnMouseUp);

        // Context menu
        this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

        // Make title editable
        titleLabel.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
    }

    public void UpdatePosition()
    {
        var panOffset = graphView.GetPanOffset();
        style.left = nodeData.Position.x + panOffset.x;
        style.top = nodeData.Position.y + panOffset.y;
    }

    public void UpdateSize()
    {
        var oldPosition = nodeData.Position;

        // Pass GraphData to ensure we get the most up-to-date state information
        var graphData = graphView.GetGraphData();
        nodeData.RecalculateSize(graphData);

        // Update visual size
        style.width = nodeData.Size.x;
        style.height = nodeData.Size.y;

        // Update position since it may have changed during recalculation
        UpdatePosition();

        // If the node position changed, we need to update connections
        if (oldPosition != nodeData.Position)
        {
            graphView.UpdateConnections();
        }
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // Left mouse button
        {
            // Select this node
            graphView.SelectNode(this);

            isDragging = true;
            dragStartPosition = evt.localMousePosition;
            this.CaptureMouse();
            evt.StopPropagation();
        }
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (isDragging)
        {
            var delta = evt.localMousePosition - dragStartPosition;
            nodeData.Position += delta;

            // Move all child states with the node
            var graphData = graphView.GetGraphData();
            if (graphData != null)
            {
                var childStates = graphData.GetStatesForNode(nodeData.Id);
                foreach (var state in childStates)
                {
                    state.Position += delta;
                    var stateView = graphView.GetStateView(state.Id);
                    if (stateView != null)
                    {
                        stateView.UpdatePosition();
                    }
                }
            }

            UpdatePosition();

            // Update connections
            graphView.UpdateConnections();

            // Update drag start position for next frame
            dragStartPosition = evt.localMousePosition;

            evt.StopPropagation();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 0 && isDragging)
        {
            isDragging = false;
            this.ReleaseMouse();
            evt.StopPropagation();
        }
    }

    private void OnTitleMouseDown(MouseDownEvent evt)
    {
        if (evt.clickCount == 2) // Double click
        {
            var textField = new TextField();
            textField.value = nodeData.Name;
            textField.style.position = Position.Absolute;
            textField.style.left = titleLabel.layout.x;
            textField.style.top = titleLabel.layout.y;
            textField.style.width = titleLabel.layout.width;
            
            Add(textField);
            textField.Focus();
            
            textField.RegisterCallback<BlurEvent>((e) => {
                nodeData.Name = textField.value;
                titleLabel.text = nodeData.Name;
                textField.RemoveFromHierarchy();
            });
            
            textField.RegisterCallback<KeyDownEvent>((e) => {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    nodeData.Name = textField.value;
                    titleLabel.text = nodeData.Name;
                    textField.RemoveFromHierarchy();
                }
            });
            
            evt.StopPropagation();
        }
    }

    private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Resize", (a) => ResizeToFitStates());
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Connect to...", (a) => ShowConnectionDropdown());

        // Show existing connections for deletion
        if (nodeData.OutgoingConnections.Count > 0)
        {
            evt.menu.AppendSeparator();
            foreach (var connection in nodeData.OutgoingConnections)
            {
                var graphData = graphView.GetGraphData();
                if (graphData != null)
                {
                    string targetName = "Unknown";
                    if (connection.IsNodeToNode())
                    {
                        var targetNode = graphData.GetNodeById(connection.TargetNodeId);
                        targetName = targetNode?.Name ?? "Unknown Node";
                    }
                    else if (connection.IsNodeToState())
                    {
                        var targetState = graphData.GetStateById(connection.TargetStateId);
                        targetName = targetState?.Name ?? "Unknown State";
                    }

                    evt.menu.AppendAction($"Remove connection to {targetName}",
                        (a) => RemoveConnection(connection));
                }
            }
        }

        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Delete Node", (a) => DeleteNode());
    }

    private void ResizeToFitStates()
    {
        // Force recalculation of size based on current states
        var graphData = graphView.GetGraphData();
        nodeData.RecalculateSize(graphData);

        // Update the visual representation
        UpdateSize();

        // Update connections since the node size changed
        graphView.UpdateConnections();
    }

    private void DeleteNode()
    {
        graphView.RemoveNodeView(nodeData.Id);
    }

    private void ShowConnectionDropdown()
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null) return;

        var choices = new List<string>();
        var targetMap = new Dictionary<string, object>(); // Can be NodeData or StateData

        // Add other nodes section
        var otherNodes = graphData.Nodes.Where(n => n.Id != nodeData.Id).ToList();
        if (otherNodes.Count > 0)
        {
            choices.Add("--- Other Nodes ---");
            foreach (var node in otherNodes)
            {
                var displayName = $"  {node.Name}";
                choices.Add(displayName);
                targetMap[displayName] = node;
            }
        }

        // Add states grouped by their parent nodes
        var nodeGroups = graphData.States
            .Where(s => !string.IsNullOrEmpty(s.ParentNodeId))
            .GroupBy(s => s.ParentNodeId)
            .ToList();

        foreach (var group in nodeGroups)
        {
            var parentNode = graphData.GetNodeById(group.Key);
            if (parentNode != null)
            {
                choices.Add($"--- {parentNode.Name} ---");
                foreach (var state in group)
                {
                    var displayName = $"  {state.Name}";
                    choices.Add(displayName);
                    targetMap[displayName] = state;
                }
            }
        }

        // Add unassigned states
        var unassignedStates = graphData.States.Where(s => string.IsNullOrEmpty(s.ParentNodeId)).ToList();
        if (unassignedStates.Count > 0)
        {
            choices.Add("--- Unassigned States ---");
            foreach (var state in unassignedStates)
            {
                var displayName = $"  {state.Name}";
                choices.Add(displayName);
                targetMap[displayName] = state;
            }
        }

        if (choices.Count == 0 || targetMap.Count == 0)
        {
            Debug.Log("No available targets to connect to");
            return;
        }

        // Create and show dropdown
        var worldBound = this.worldBound;
        var dropdown = new DropdownField("Connect to:", choices, -1); // Start with no selection
        dropdown.style.position = Position.Absolute;
        dropdown.style.left = worldBound.x;
        dropdown.style.top = worldBound.yMax + 5;
        dropdown.style.width = 300;

        // Add to parent (graph view)
        graphView.Add(dropdown);

        // Handle selection
        dropdown.RegisterValueChangedCallback(evt =>
        {
            var selectedChoice = evt.newValue;

            // Skip header items (those that start with "---") and empty selections
            if (string.IsNullOrEmpty(selectedChoice) || selectedChoice.StartsWith("---"))
            {
                return;
            }

            if (targetMap.TryGetValue(selectedChoice, out var target))
            {
                if (target is NodeData targetNode)
                {
                    CreateNodeToNodeConnection(targetNode);
                }
                else if (target is StateData targetState)
                {
                    CreateNodeToStateConnection(targetState);
                }
            }
            dropdown.RemoveFromHierarchy();
        });

        // Remove dropdown when clicking elsewhere
        dropdown.RegisterCallback<BlurEvent>(evt =>
        {
            dropdown.RemoveFromHierarchy();
        });

        dropdown.Focus();
    }

    private void CreateNodeToNodeConnection(NodeData targetNode)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null || targetNode == null) return;

        // Check if connection already exists
        foreach (var existingConnection in nodeData.OutgoingConnections)
        {
            if (existingConnection.IsNodeToNode() && existingConnection.TargetNodeId == targetNode.Id)
            {
                Debug.Log($"Node-to-node connection already exists from {nodeData.Name} to {targetNode.Name}");
                return;
            }
        }

        // Create new node-to-node connection
        var connection = new ConnectionData(
            string.Empty, // No source state
            string.Empty, // No target state
            nodeData.Id,
            targetNode.Id,
            true // Node to node connection
        );

        // Add connection to source node
        nodeData.AddConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        Debug.Log($"Created node-to-node connection from {nodeData.Name} to {targetNode.Name}");
    }

    private void CreateNodeToStateConnection(StateData targetState)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null || targetState == null) return;

        // Check if connection already exists
        foreach (var existingConnection in nodeData.OutgoingConnections)
        {
            if (existingConnection.IsNodeToState() && existingConnection.TargetStateId == targetState.Id)
            {
                Debug.Log($"Node-to-state connection already exists from {nodeData.Name} to {targetState.Name}");
                return;
            }
        }

        // Create new node-to-state connection
        var connection = new ConnectionData(
            string.Empty, // No source state
            targetState.Id,
            nodeData.Id,
            targetState.ParentNodeId,
            false // Not a node-to-node connection
        );

        // Add connection to source node
        nodeData.AddConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        Debug.Log($"Created node-to-state connection from {nodeData.Name} to {targetState.Name}");
    }

    private void RemoveConnection(ConnectionData connection)
    {
        if (connection == null) return;

        // Remove connection from source node
        nodeData.RemoveConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        var graphData = graphView.GetGraphData();
        if (graphData != null)
        {
            string targetName = "Unknown";
            if (connection.IsNodeToNode())
            {
                var targetNode = graphData.GetNodeById(connection.TargetNodeId);
                targetName = targetNode?.Name ?? "Unknown Node";
            }
            else if (connection.IsNodeToState())
            {
                var targetState = graphData.GetStateById(connection.TargetStateId);
                targetName = targetState?.Name ?? "Unknown State";
            }

            Debug.Log($"Removed connection from {nodeData.Name} to {targetName}");
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (isSelected)
        {
            AddToClassList("node-selected");
        }
        else
        {
            RemoveFromClassList("node-selected");
        }
    }

    public void UpdateLabel()
    {
        if (titleLabel != null)
        {
            titleLabel.text = nodeData.Name;
        }
    }
}
