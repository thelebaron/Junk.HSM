using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StateView : VisualElement
{
    private StateData stateData;
    private GraphView graphView;
    private Label stateLabel;
    private VisualElement connectionPoint;
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private bool isSelected = false;

    public StateData StateData => stateData;

    public StateView(StateData data, GraphView parent)
    {
        stateData = data;
        graphView = parent;

        // Safety check for state position
        if (float.IsNaN(stateData.Position.x) || float.IsNaN(stateData.Position.y))
        {
            UnityEngine.Debug.LogWarning($"StateView: {stateData.Name} has invalid position, resetting to (0,0)");
            stateData.Position = Vector2.zero;
        }

        AddToClassList("state");

        // Create connection point (visual indicator for connections)
        connectionPoint = new VisualElement();
        connectionPoint.AddToClassList("connection-point");
        Add(connectionPoint);

        // Create state label
        stateLabel = new Label(stateData.Name);
        stateLabel.AddToClassList("state-label");
        Add(stateLabel);

        // Set initial position
        UpdatePosition();

        // Register events
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseUpEvent>(OnMouseUp);

        // Context menu
        this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

        // Make label editable
        stateLabel.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);
    }

    public void UpdatePosition()
    {
        var panOffset = graphView.GetPanOffset();
        style.left = stateData.Position.x + panOffset.x;
        style.top = stateData.Position.y + panOffset.y;
    }

    private void UpdateParentNodeSize()
    {
        if (!string.IsNullOrEmpty(stateData.ParentNodeId))
        {
            var parentNodeView = graphView.GetNodeView(stateData.ParentNodeId);
            if (parentNodeView != null)
            {
                parentNodeView.UpdateSize();
            }
        }
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // Left mouse button
        {
            // Select this state
            graphView.SelectState(this);

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
            stateData.Position += delta;
            UpdatePosition();

            // Update parent node size in real-time during dragging
            UpdateParentNodeSize();

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

            // Final update of parent node size after dragging is complete
            UpdateParentNodeSize();

            evt.StopPropagation();
        }
    }

    private void OnLabelMouseDown(MouseDownEvent evt)
    {
        if (evt.clickCount == 2) // Double click to edit
        {
            var textField = new TextField();
            textField.value = stateData.Name;
            textField.style.position = Position.Absolute;
            textField.style.left = stateLabel.layout.x;
            textField.style.top = stateLabel.layout.y;
            textField.style.width = stateLabel.layout.width;
            
            Add(textField);
            textField.Focus();
            
            textField.RegisterCallback<BlurEvent>((e) => {
                stateData.Name = textField.value;
                stateLabel.text = stateData.Name;
                textField.RemoveFromHierarchy();
            });
            
            textField.RegisterCallback<KeyDownEvent>((e) => {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    stateData.Name = textField.value;
                    stateLabel.text = stateData.Name;
                    textField.RemoveFromHierarchy();
                }
            });
            
            evt.StopPropagation();
        }
    }

    private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Delete State", (a) => DeleteState());
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Connect to...", (a) => ShowConnectionDropdown());

        // Show existing connections for deletion
        if (stateData.OutgoingConnections.Count > 0)
        {
            evt.menu.AppendSeparator();
            foreach (var connection in stateData.OutgoingConnections)
            {
                var graphData = graphView.GetGraphData();
                if (graphData != null)
                {
                    string targetName = "Unknown";
                    if (connection.IsStateToState())
                    {
                        var targetState = graphData.GetStateById(connection.TargetStateId);
                        targetName = targetState?.Name ?? "Unknown State";
                    }
                    else if (connection.IsStateToNode())
                    {
                        var targetNode = graphData.GetNodeById(connection.TargetNodeId);
                        targetName = targetNode?.Name ?? "Unknown Node";
                    }

                    evt.menu.AppendAction($"Remove connection to {targetName}",
                        (a) => RemoveConnection(connection));
                }
            }
        }
    }

    private void DeleteState()
    {
        graphView.RemoveStateView(stateData.Id);
    }

    private void ShowConnectionDropdown()
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null) return;

        // Create dropdown choices
        var choices = new List<string>();
        var targetMap = new Dictionary<string, object>(); // Can be StateData or NodeData

        // Add nodes first
        choices.Add("--- Nodes ---");
        foreach (var node in graphData.Nodes)
        {
            if (node.Id != stateData.ParentNodeId) // Don't connect to own parent node
            {
                var displayName = $"  [Node] {node.Name}";
                choices.Add(displayName);
                targetMap[displayName] = node;
            }
        }

        // Add states from the same node
        if (!string.IsNullOrEmpty(stateData.ParentNodeId))
        {
            var parentNode = graphData.GetNodeById(stateData.ParentNodeId);
            if (parentNode != null)
            {
                choices.Add($"--- {parentNode.Name} (Same Node) ---");
                foreach (var state in parentNode.States)
                {
                    if (state.Id != stateData.Id) // Don't include self
                    {
                        var displayName = $"  {state.Name}";
                        choices.Add(displayName);
                        targetMap[displayName] = state;
                    }
                }
            }
        }

        // Add states from other nodes, grouped by node
        foreach (var node in graphData.Nodes)
        {
            if (node.Id == stateData.ParentNodeId) continue; // Skip same node

            if (node.States.Count > 0)
            {
                choices.Add($"--- {node.Name} ---");
                foreach (var state in node.States)
                {
                    var displayName = $"  {state.Name}";
                    choices.Add(displayName);
                    targetMap[displayName] = state;
                }
            }
        }

        // Add unassigned states
        var unassignedStates = graphData.States.FindAll(s => string.IsNullOrEmpty(s.ParentNodeId) && s.Id != stateData.Id);
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

        if (choices.Count <= 1) // Only header, no actual targets
        {
            Debug.Log("No available targets to connect to");
            return;
        }

        // Create and show dropdown with no pre-selection
        var worldBound = this.worldBound;
        var dropdown = new DropdownField("Connect to:", choices, -1);
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

            // Skip empty selections and header items
            if (string.IsNullOrEmpty(selectedChoice) || selectedChoice.StartsWith("---"))
            {
                return;
            }

            if (targetMap.TryGetValue(selectedChoice, out var target))
            {
                if (target is StateData targetState)
                {
                    CreateStateToStateConnection(targetState);
                }
                else if (target is NodeData targetNode)
                {
                    CreateStateToNodeConnection(targetNode);
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

    private void CreateStateToStateConnection(StateData targetState)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null || targetState == null) return;

        // Check if connection already exists
        foreach (var existingConnection in stateData.OutgoingConnections)
        {
            if (existingConnection.IsStateToState() && existingConnection.TargetStateId == targetState.Id)
            {
                Debug.Log($"State-to-state connection already exists from {stateData.Name} to {targetState.Name}");
                return;
            }
        }

        // Create new state-to-state connection
        var connection = new ConnectionData(
            stateData.Id,
            targetState.Id,
            stateData.ParentNodeId,
            targetState.ParentNodeId,
            false // State to state connection
        );

        // Add connection to source state
        stateData.AddConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        Debug.Log($"Created state-to-state connection from {stateData.Name} to {targetState.Name}");
    }

    private void CreateStateToNodeConnection(NodeData targetNode)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null || targetNode == null) return;

        // Check if connection already exists
        foreach (var existingConnection in stateData.OutgoingConnections)
        {
            if (existingConnection.IsStateToNode() && existingConnection.TargetNodeId == targetNode.Id)
            {
                Debug.Log($"State-to-node connection already exists from {stateData.Name} to {targetNode.Name}");
                return;
            }
        }

        // Create new state-to-node connection
        var connection = new ConnectionData(
            stateData.Id,
            string.Empty, // No target state
            stateData.ParentNodeId,
            targetNode.Id,
            false // Not a node-to-node connection
        );

        // Add connection to source state
        stateData.AddConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        Debug.Log($"Created state-to-node connection from {stateData.Name} to {targetNode.Name}");
    }

    private void RemoveConnection(ConnectionData connection)
    {
        if (connection == null) return;

        // Remove connection from source state
        stateData.RemoveConnection(connection);

        // Refresh connections in the graph view
        graphView.RefreshConnections();

        var graphData = graphView.GetGraphData();
        if (graphData != null)
        {
            string targetName = "Unknown";
            if (connection.IsStateToState())
            {
                var targetState = graphData.GetStateById(connection.TargetStateId);
                targetName = targetState?.Name ?? "Unknown State";
            }
            else if (connection.IsStateToNode())
            {
                var targetNode = graphData.GetNodeById(connection.TargetNodeId);
                targetName = targetNode?.Name ?? "Unknown Node";
            }

            Debug.Log($"Removed connection from {stateData.Name} to {targetName}");
        }
    }



    public Vector2 GetWorldPosition()
    {
        // Get the world position of this state for connection drawing
        return new Vector2(stateData.Position.x + layout.width * 0.5f, stateData.Position.y + layout.height * 0.5f);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (isSelected)
        {
            AddToClassList("state-selected");
        }
        else
        {
            RemoveFromClassList("state-selected");
        }
    }

    public void UpdateLabel()
    {
        if (stateLabel != null)
        {
            stateLabel.text = stateData.Name;
        }
    }
}
