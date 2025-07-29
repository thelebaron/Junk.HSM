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

        // Set color based on connection type
        if (connectionData.IsNodeToNode())
        {
            painter.strokeColor = Color.indianRed; // Node to node connections
        }
        else if (connectionData.IsNodeToState())
        {
            painter.strokeColor = Color.yellow; // Node to state connections
        }
        else if (connectionData.IsStateToNode())
        {
            painter.strokeColor = Color.blue; // State to node connections
        }
        else
        {
            painter.strokeColor = Color.white; // State to state connections (default)
        }

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
        var graphData = graphView.GetGraphData();
        if (graphData == null) return Vector2.zero;

        var panOffset = graphView.GetPanOffset();

        if (connectionData.IsNodeToNode() || connectionData.IsNodeToState())
        {
            // Connection from entire node
            var sourceNode = graphData.GetNodeById(connectionData.SourceNodeId);
            if (sourceNode == null) return Vector2.zero;

            return new Vector2(
                sourceNode.Position.x + sourceNode.Size.x + panOffset.x,
                sourceNode.Position.y + sourceNode.Size.y * 0.5f + panOffset.y
            );
        }
        else if (connectionData.IsStateToState() || connectionData.IsStateToNode())
        {
            // Connection from specific state
            var sourceState = graphData.GetStateById(connectionData.SourceStateId);
            if (sourceState == null) return Vector2.zero;

            const float stateWidth = 100f;
            const float stateHeight = 25f;

            return new Vector2(
                sourceState.Position.x + stateWidth + panOffset.x,
                sourceState.Position.y + stateHeight * 0.5f + panOffset.y
            );
        }

        return Vector2.zero;
    }

    private Vector2 GetTargetPosition()
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null) return Vector2.zero;

        var panOffset = graphView.GetPanOffset();

        if (connectionData.IsNodeToNode() || connectionData.IsStateToNode())
        {
            // Connection to entire node
            var targetNode = graphData.GetNodeById(connectionData.TargetNodeId);
            if (targetNode == null) return Vector2.zero;

            return new Vector2(
                targetNode.Position.x + panOffset.x,
                targetNode.Position.y + targetNode.Size.y * 0.5f + panOffset.y
            );
        }
        else if (connectionData.IsStateToState() || connectionData.IsNodeToState())
        {
            // Connection to specific state
            var targetState = graphData.GetStateById(connectionData.TargetStateId);
            if (targetState == null) return Vector2.zero;

            const float stateHeight = 25f;

            return new Vector2(
                targetState.Position.x + panOffset.x,
                targetState.Position.y + stateHeight * 0.5f + panOffset.y
            );
        }

        return Vector2.zero;
    }

    public void UpdateConnection()
    {
        // Force a redraw of the connection
        MarkDirtyRepaint();
    }
}
