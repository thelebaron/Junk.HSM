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
        stateLabel = new Label(GetDisplayText());
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
            textField.value = stateData.Name; // Edit only the state name, not the display text
            textField.style.position = Position.Absolute;
            textField.style.left = stateLabel.layout.x;
            textField.style.top = stateLabel.layout.y;
            textField.style.width = stateLabel.layout.width;

            Add(textField);
            textField.Focus();

            textField.RegisterCallback<BlurEvent>((e) => {
                stateData.Name = textField.value;
                UpdateLabel(); // Use UpdateLabel to show the proper display format
                textField.RemoveFromHierarchy();
            });

            textField.RegisterCallback<KeyDownEvent>((e) => {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    stateData.Name = textField.value;
                    UpdateLabel(); // Use UpdateLabel to show the proper display format
                    textField.RemoveFromHierarchy();
                }
            });

            evt.StopPropagation();
        }
    }

    private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Delete State", (a) => DeleteState());
    }

    private void DeleteState()
    {
        graphView.RemoveStateView(stateData.Id);
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
            // Get the display text with node prefix if the state has a parent node
            stateLabel.text = GetDisplayText();
        }
    }

    private string GetDisplayText()
    {
        // If state has a parent node, show "nodeName : stateName"
        if (!string.IsNullOrEmpty(stateData.ParentNodeId))
        {
            var parentNode = graphView.GetGraphData()?.GetNodeById(stateData.ParentNodeId);
            if (parentNode != null)
            {
                return $"{parentNode.Name} : {stateData.Name}";
            }
        }

        // If no parent node, just show the state name
        return stateData.Name;
    }
}
