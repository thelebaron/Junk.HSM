using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionView : VisualElement
{
    private ConnectionData connectionData;
    private GraphView graphView;

    public ConnectionData ConnectionData => connectionData;

    public ConnectionView(ConnectionData data, GraphView parent)
    {
        connectionData = data;
        graphView = parent;
        
        AddToClassList("connection");
        
        // Set up the visual element to draw the connection line
        generateVisualContent += OnGenerateVisualContent;
        
        // Make sure this element covers the entire graph area for drawing
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        
        // Don't capture mouse events (let them pass through)
        pickingMode = PickingMode.Ignore;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        DrawConnection(mgc);
    }

    private void DrawConnection(MeshGenerationContext mgc)
    {
        // Get source and target positions
        var sourcePos = GetSourcePosition();
        var targetPos = GetTargetPosition();
        
        if (sourcePos == Vector2.zero || targetPos == Vector2.zero)
            return;
        
        // Create a simple line using the painter
        var painter = mgc.painter2D;
        painter.strokeColor = Color.white;
        painter.lineWidth = 2.0f;
        
        // Draw a simple line (could be enhanced with bezier curves)
        painter.BeginPath();
        painter.MoveTo(sourcePos);
        painter.LineTo(targetPos);
        painter.Stroke();
        
        // Draw arrow at target
        DrawArrow(painter, sourcePos, targetPos);
    }

    private void DrawArrow(Painter2D painter, Vector2 start, Vector2 end)
    {
        var direction = (end - start).normalized;
        var arrowSize = 10f;
        var arrowAngle = 30f * Mathf.Deg2Rad;
        
        // Calculate arrow points
        var arrowPoint1 = end - direction * arrowSize;
        var perpendicular = new Vector2(-direction.y, direction.x);
        
        var arrow1 = arrowPoint1 + perpendicular * arrowSize * 0.5f;
        var arrow2 = arrowPoint1 - perpendicular * arrowSize * 0.5f;
        
        // Draw arrow
        painter.BeginPath();
        painter.MoveTo(end);
        painter.LineTo(arrow1);
        painter.MoveTo(end);
        painter.LineTo(arrow2);
        painter.Stroke();
    }

    private Vector2 GetSourcePosition()
    {
        if (connectionData.IsNodeToNodeConnection)
        {
            // Connection from entire node
            var sourceNodeView = graphView.GetNodeView(connectionData.SourceNodeId);
            if (sourceNodeView == null) return Vector2.zero;

            var nodeBounds = sourceNodeView.worldBound;
            return new Vector2(nodeBounds.xMax, nodeBounds.center.y);
        }
        else
        {
            // Connection from specific state
            var stateView = graphView.GetStateView(connectionData.SourceStateId);
            if (stateView == null) return Vector2.zero;

            return stateView.GetWorldPosition();
        }
    }

    private Vector2 GetTargetPosition()
    {
        if (connectionData.IsNodeToNodeConnection || string.IsNullOrEmpty(connectionData.TargetStateId))
        {
            // Connection to entire node
            var targetNodeView = graphView.GetNodeView(connectionData.TargetNodeId);
            if (targetNodeView == null) return Vector2.zero;

            var nodeBounds = targetNodeView.worldBound;
            return new Vector2(nodeBounds.xMin, nodeBounds.center.y);
        }
        else
        {
            // Connection to specific state
            var stateView = graphView.GetStateView(connectionData.TargetStateId);
            if (stateView == null) return Vector2.zero;

            return stateView.GetWorldPosition();
        }
    }

    public void UpdateConnection()
    {
        // Force a redraw of the connection
        MarkDirtyRepaint();
    }
}
