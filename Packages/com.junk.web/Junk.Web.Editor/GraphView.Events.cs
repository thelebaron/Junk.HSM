using Unity.Assertions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public partial class GraphView
    {
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
                panOffset += delta;
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
            var mousePosition = evt.localMousePosition;
            var zoomDelta = -evt.delta.y * ZoomSpeed;
            var newZoomLevel = Mathf.Clamp(zoomLevel + zoomDelta, MinZoom, MaxZoom);
            
            if (Mathf.Approximately(newZoomLevel, zoomLevel))
                return; // No change in zoom level
                
            // Get the viewport center (where Unity scales from)
            var viewportCenter = new Vector2(layout.width * 0.5f, layout.height * 0.5f);
            
            // Calculate mouse position relative to viewport center
            var mouseFromCenter = mousePosition - viewportCenter;
            
            // Calculate the point in world space that should stay under the mouse
            var worldPoint = (mouseFromCenter / zoomLevel) + (viewportCenter - panOffset) / zoomLevel;
            
            // Update zoom level
            zoomLevel = newZoomLevel;
            ApplyZoom();
            
            // Calculate where that world point will be after zoom
            var newScreenPoint = (worldPoint * zoomLevel) - (viewportCenter - panOffset);
            
            // Adjust pan offset to keep the world point under the mouse
            var offset = mouseFromCenter - newScreenPoint;
            panOffset += offset;
            
            UpdateAllPositions();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (GraphData == null) 
                return;
            
            Assert.IsNotNull(GraphData);
            
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
    }
}
