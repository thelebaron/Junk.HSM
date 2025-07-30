using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public class NodeView : VisualElement
    {
        private NodeData  nodeData;
        private GraphView graphView;
        private Label     titleLabel;
        private bool      isDragging = false;
        private Vector2   dragStartPosition;
        private bool      isSelected = false;

        public NodeData NodeData   => nodeData;
        public bool     IsSelected => isSelected;

        public NodeView(NodeData data, GraphView parent)
        {
            nodeData  = data;
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
            UpdateColors();

            // Register events
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);

            // Context menu
            //this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            // Make title editable
            titleLabel.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
        }

        public void UpdatePosition()
        {
            var panOffset = graphView.GetPanOffset();
            style.left = nodeData.Position.x + panOffset.x;
            style.top  = nodeData.Position.y + panOffset.y;
        }

        public void UpdateSize()
        {
            RecalculateSizeAndPosition();
        }

        private void RecalculateSizeAndPosition()
        {
            var oldPosition = nodeData.Position;

            const float padding = 20f;
            const float titleHeight = 30f;
            const float minWidth = 200f;
            const float minHeight = 60f;

            var graphData = graphView.GraphData;
            var childStates = graphData.GetStatesForNode(nodeData.Id);

            if (childStates.Count == 0)
            {
                style.width = minWidth;
                style.height = minHeight;
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var state in childStates)
            {
                var stateView = graphView.GetStateView(state.Id);
                if (stateView == null) continue;

                minX = Mathf.Min(minX, state.Position.x);
                minY = Mathf.Min(minY, state.Position.y);
                maxX = Mathf.Max(maxX, state.Position.x + stateView.layout.width);
                maxY = Mathf.Max(maxY, state.Position.y + stateView.layout.height);
            }

            float newNodeX = minX - padding;
            float newNodeY = minY - padding - titleHeight;
            float newWidth = (maxX - minX) + (padding * 2);
            float newHeight = (maxY - minY) + (padding * 2) + titleHeight;

            newWidth = Mathf.Max(newWidth, minWidth);
            newHeight = Mathf.Max(newHeight, minHeight);

            nodeData.Position = new Vector2(newNodeX, newNodeY);
            style.width = newWidth;
            style.height = newHeight;

            UpdatePosition();

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

                isDragging        = true;
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
                var graphData = graphView.GraphData;
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
                textField.value          = nodeData.Name;
                textField.style.position = Position.Absolute;
                textField.style.left     = titleLabel.layout.x;
                textField.style.top      = titleLabel.layout.y;
                textField.style.width    = titleLabel.layout.width;

                Add(textField);
                textField.Focus();

                textField.RegisterCallback<BlurEvent>((e) =>
                {
                    nodeData.Name   = textField.value;
                    titleLabel.text = nodeData.Name;
                    UpdateChildStateLabels();
                    textField.RemoveFromHierarchy();
                });

                textField.RegisterCallback<KeyDownEvent>((e) =>
                {
                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        nodeData.Name   = textField.value;
                        titleLabel.text = nodeData.Name;
                        UpdateChildStateLabels();
                        textField.RemoveFromHierarchy();
                    }
                });

                evt.StopPropagation();
            }
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete Node", (a) => DeleteNode());
            evt.menu.AppendSeparator();
        }


        private void DeleteNode()
        {
            graphView.RemoveNodeView(nodeData.Id);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (isSelected)
            {
                AddToClassList("node-selected");
                style.borderTopColor    = Color.white;
                style.borderBottomColor = Color.white;
                style.borderLeftColor   = Color.white;
                style.borderRightColor  = Color.white;
            }
            else
            {
                RemoveFromClassList("node-selected");
                UpdateColors();
            }
        }

        public void UpdateLabel()
        {
            if (titleLabel != null)
            {
                titleLabel.text = nodeData.Name;
            }
        }

        private void UpdateChildStateLabels()
        {
            // Update labels for all states that belong to this node
            var graphData = graphView.GraphData;
            if (graphData != null)
            {
                var nodeStates = graphData.GetStatesForNode(nodeData.Id);
                foreach (var state in nodeStates)
                {
                    var stateView = graphView.GetStateView(state.Id);
                    if (stateView != null)
                    {
                        stateView.UpdateLabel();
                    }
                }
            }
        }

        public void UpdateColors()
        {
            // Apply the darkened node color to the node background
            var darkenedColor = nodeData.GetDarkenedNodeColor();
            style.backgroundColor = new Color(darkenedColor.r, darkenedColor.g, darkenedColor.b, 0.35f);

            // Use 50% grey for all node borders
            var greyBorderColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // 50% grey
            style.borderTopColor = greyBorderColor;
            style.borderBottomColor = greyBorderColor;
            style.borderLeftColor = greyBorderColor;
            style.borderRightColor = greyBorderColor;

            // Update child states colors
            var graphData = graphView.GraphData;
            if (graphData != null)
            {
                var nodeStates = graphData.GetStatesForNode(nodeData.Id);
                foreach (var state in nodeStates)
                {
                    var stateView = graphView.GetStateView(state.Id);
                    if (stateView != null)
                    {
                        stateView.UpdateColors();
                    }
                }
            }
        }
    }
}