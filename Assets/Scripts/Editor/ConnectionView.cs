using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionView : VisualElement
{
    private ConnectionData connectionData;
    private GraphView graphView;

    // Cache for connection points to avoid duplicate calculations
    private Vector2? cachedSourcePoint;
    private Vector2? cachedTargetPoint;
    private Vector2 lastPanOffset;
    private bool cacheValid = false;

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

        // Create a painter for drawing
        var painter = mgc.painter2D;

        // Set color based on connection type
        ConnectionType connectionType;
        if (connectionData.IsNodeToNode())
        {
            painter.strokeColor = Color.indianRed; // Node to node connections
            connectionType = ConnectionType.NodeToNode;
        }
        else if (connectionData.IsNodeToState())
        {
            painter.strokeColor = Color.yellow; // Node to state connections
            connectionType = ConnectionType.NodeToState;
        }
        else if (connectionData.IsStateToNode())
        {
            painter.strokeColor = Color.blue; // State to node connections
            connectionType = ConnectionType.StateToNode;
        }
        else
        {
            painter.strokeColor = Color.white; // State to state connections (default)
            connectionType = ConnectionType.StateToState;
        }

        painter.lineWidth = 2.0f;

        // Draw smooth bezier curve instead of straight line
        float curveStrength = BezierCurveUtility.GetCurveStrengthForConnectionType(connectionType);
        BezierCurveUtility.DrawBezierCurve(painter, sourcePos, targetPos, curveStrength);

        // Draw arrow at target using curve direction
        DrawArrowOnCurve(painter, sourcePos, targetPos, curveStrength);
    }

    private void DrawArrowOnCurve(Painter2D painter, Vector2 start, Vector2 end, float curveStrength)
    {
        // Calculate control points for the curve
        var controlPoints = BezierCurveUtility.CalculateControlPoints(start, end, curveStrength);

        // Get the direction at the end of the curve (t = 1.0)
        var direction = BezierCurveUtility.GetTangentOnBezierCurve(start, controlPoints.Item1, controlPoints.Item2, end, 1.0f);

        var arrowSize = 10f;

        // Calculate arrow points using the curve direction
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

    // Keep the old DrawArrow method for backward compatibility if needed
    private void DrawArrow(Painter2D painter, Vector2 start, Vector2 end)
    {
        var direction = (end - start).normalized;
        var arrowSize = 10f;

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

    private void CalculateConnectionPoints()
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null)
        {
            cachedSourcePoint = Vector2.zero;
            cachedTargetPoint = Vector2.zero;
            cacheValid = true;
            return;
        }

        var panOffset = graphView.GetPanOffset();

        // Check if cache is still valid (pan offset hasn't changed)
        if (cacheValid && lastPanOffset == panOffset && cachedSourcePoint.HasValue && cachedTargetPoint.HasValue)
        {
            return; // Use cached values
        }

        var allConnections = graphData.GetAllConnections();

        // Get source and target bounding boxes
        var sourceBounds = GetSourceBoundingBox(panOffset);
        var targetBounds = GetTargetBoundingBox(panOffset);

        if (sourceBounds.size == Vector2.zero || targetBounds.size == Vector2.zero)
        {
            cachedSourcePoint = Vector2.zero;
            cachedTargetPoint = Vector2.zero;
        }
        else
        {
            // Calculate dynamic connection points once
            var (sourcePoint, targetPoint) = ConnectionPointCalculator.CalculateConnectionPoints(
                sourceBounds, targetBounds, connectionData, allConnections);

            cachedSourcePoint = sourcePoint;
            cachedTargetPoint = targetPoint;
        }

        lastPanOffset = panOffset;
        cacheValid = true;
    }

    private Vector2 GetSourcePosition()
    {
        CalculateConnectionPoints();
        return cachedSourcePoint ?? Vector2.zero;
    }

    private Vector2 GetTargetPosition()
    {
        CalculateConnectionPoints();
        return cachedTargetPoint ?? Vector2.zero;
    }

    private ConnectionPointCalculator.BoundingBox GetSourceBoundingBox(Vector2 panOffset)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null) return new ConnectionPointCalculator.BoundingBox();

        if (connectionData.IsNodeToNode() || connectionData.IsNodeToState())
        {
            // Connection from entire node
            var sourceNode = graphData.GetNodeById(connectionData.SourceNodeId);
            if (sourceNode == null) return new ConnectionPointCalculator.BoundingBox();

            return ConnectionPointCalculator.GetNodeBoundingBox(sourceNode, panOffset);
        }
        else if (connectionData.IsStateToState() || connectionData.IsStateToNode())
        {
            // Connection from specific state
            var sourceState = graphData.GetStateById(connectionData.SourceStateId);
            if (sourceState == null) return new ConnectionPointCalculator.BoundingBox();

            // Get the actual StateView for accurate sizing
            var sourceStateView = graphView.GetStateView(connectionData.SourceStateId);
            return ConnectionPointCalculator.GetStateBoundingBox(sourceState, panOffset, sourceStateView);
        }

        return new ConnectionPointCalculator.BoundingBox();
    }

    private ConnectionPointCalculator.BoundingBox GetTargetBoundingBox(Vector2 panOffset)
    {
        var graphData = graphView.GetGraphData();
        if (graphData == null) return new ConnectionPointCalculator.BoundingBox();

        if (connectionData.IsNodeToNode() || connectionData.IsStateToNode())
        {
            // Connection to entire node
            var targetNode = graphData.GetNodeById(connectionData.TargetNodeId);
            if (targetNode == null) return new ConnectionPointCalculator.BoundingBox();

            return ConnectionPointCalculator.GetNodeBoundingBox(targetNode, panOffset);
        }
        else if (connectionData.IsStateToState() || connectionData.IsNodeToState())
        {
            // Connection to specific state
            var targetState = graphData.GetStateById(connectionData.TargetStateId);
            if (targetState == null) return new ConnectionPointCalculator.BoundingBox();

            // Get the actual StateView for accurate sizing
            var targetStateView = graphView.GetStateView(connectionData.TargetStateId);
            return ConnectionPointCalculator.GetStateBoundingBox(targetState, panOffset, targetStateView);
        }

        return new ConnectionPointCalculator.BoundingBox();
    }

    public void UpdateConnection()
    {
        // Invalidate cache and force a redraw of the connection
        cacheValid = false;
        MarkDirtyRepaint();
    }
}
