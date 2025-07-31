using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public class ConnectionView : VisualElement
    {
        private ConnectionData connectionData;
        private GraphView      graphView;

        // Cache for connection points to avoid duplicate calculations
        private Vector2                        cachedSourcePoint;
        private Vector2                        cachedTargetPoint;
        private ConnectionPointCalculator.Edge cachedSourceEdge;
        private ConnectionPointCalculator.Edge cachedTargetEdge;
        private Vector2                         lastPanOffset;
        private bool                            cacheValid;

        public ConnectionView(ConnectionData data, GraphView parent)
        {
            connectionData = data;
            graphView      = parent;

            AddToClassList("connection");

            // Set up the visual element to draw the connection line
            generateVisualContent += OnGenerateVisualContent;

            // Make sure this element covers the entire graph area for drawing
            style.position = Position.Absolute;
            style.left     = 0;
            style.top      = 0;
            style.right    = 0;
            style.bottom   = 0;

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

            // Determine if this connection should be highlighted (100% opacity)
            bool  isHighlighted = IsConnectionHighlighted();
            float opacity       = isHighlighted ? 1.0f : 0.45f;

            // Set color based on connection type with appropriate opacity
            ConnectionType connectionType;
            Color          baseColor;
            if (connectionData.IsNodeToNode())
            {
                baseColor      = Color.orangeRed; // Node to node connections
                connectionType = ConnectionType.NodeToNode;
            }
            else if (connectionData.IsNodeToState())
            {
                baseColor      = Color.yellow; // Node to state connections
                connectionType = ConnectionType.NodeToState;
            }
            else if (connectionData.IsStateToNode())
            {
                baseColor      = Color.blue; // State to node connections
                connectionType = ConnectionType.StateToNode;
            }
            else
            {
                baseColor      = Color.white; // State to state connections (default)
                connectionType = ConnectionType.StateToState;
            }

            // Apply opacity to the base color
            painter.strokeColor = new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
            painter.lineWidth   = 2.0f;
            if (isHighlighted)
                painter.lineWidth = 3.5f;

            // Draw directional bezier curve based on connection edges
            float curveStrength = BezierCurveUtility.GetCurveStrengthForConnectionType(connectionType);

            BezierCurveUtility.DrawBezierCurve(painter, sourcePos, targetPos,
                cachedSourceEdge, cachedTargetEdge, curveStrength);

            // Draw arrow at target using actual curve direction
            DrawArrowOnCurve(painter, sourcePos, targetPos,
                cachedSourceEdge, cachedTargetEdge, curveStrength);
        }



        private void DrawArrowOnCurve(Painter2D                      painter,    Vector2                        start,      Vector2 end,
                                      ConnectionPointCalculator.Edge sourceEdge, ConnectionPointCalculator.Edge targetEdge, float   curveStrength)
        {
            // Calculate control points for the curve using edge information
            var controlPoints = BezierCurveUtility.CalculateControlPoints(start, end, sourceEdge, targetEdge, curveStrength);

            // Get the direction at the end of the curve (t = 1.0)
            var direction = BezierCurveUtility.GetTangentOnBezierCurve(start, controlPoints.Item1, controlPoints.Item2, end, 1.0f);

            var arrowSize = 10f;

            // Calculate arrow points using the curve direction
            var arrowPoint1   = end - direction * arrowSize;
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
            var arrowPoint1   = end - direction * arrowSize;
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
            var graphData = graphView.GraphData;
            if (graphData == null)
                return;
            
            var panOffset = graphView.GetPanOffset();

            // Check if cache is still valid (pan offset hasn't changed)
            if (cacheValid && lastPanOffset == panOffset && cachedSourcePoint.Equals(Vector2.zero) && cachedTargetPoint.Equals(Vector2.zero) &&
                cachedSourceEdge.Equals(null) && cachedTargetEdge.Equals(null))
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
                cachedSourceEdge  = default;
                cachedTargetEdge  = default;
            }
            else
            {
                // Calculate dynamic connection points with edge information
                var (sourcePoint, targetPoint, sourceEdge, targetEdge) = ConnectionPointCalculator.CalculateConnectionPoints(
                    sourceBounds, targetBounds, connectionData, allConnections);

                cachedSourcePoint = sourcePoint;
                cachedTargetPoint = targetPoint;
                cachedSourceEdge  = sourceEdge;
                cachedTargetEdge  = targetEdge;
            }

            lastPanOffset = panOffset;
            cacheValid    = true;
        }

        private Vector2 GetSourcePosition()
        {
            CalculateConnectionPoints();
            return cachedSourcePoint;
        }

        private Vector2 GetTargetPosition()
        {
            CalculateConnectionPoints();
            return cachedTargetPoint;
        }

        private ConnectionPointCalculator.BoundingBox GetSourceBoundingBox(Vector2 panOffset)
        {
            var graphData = graphView.GraphData;
            if (graphData == null) return new ConnectionPointCalculator.BoundingBox();

            if (connectionData.IsNodeToNode() || connectionData.IsNodeToState())
            {
                // Connection from entire node
                var sourceNode = graphData.GetNodeById(connectionData.SourceNodeId);
                if (sourceNode == null) return new ConnectionPointCalculator.BoundingBox();

                var sourceNodeView = graphView.GetNodeView(connectionData.SourceNodeId);
                return ConnectionPointCalculator.GetNodeBoundingBox(sourceNode, panOffset, sourceNodeView);
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
            var graphData = graphView.GraphData;
            if (graphData == null) return new ConnectionPointCalculator.BoundingBox();

            if (connectionData.IsNodeToNode() || connectionData.IsStateToNode())
            {
                // Connection to entire node
                var targetNode = graphData.GetNodeById(connectionData.TargetNodeId);
                if (targetNode == null) return new ConnectionPointCalculator.BoundingBox();

                var targetNodeView = graphView.GetNodeView(connectionData.TargetNodeId);
                return ConnectionPointCalculator.GetNodeBoundingBox(targetNode, panOffset, targetNodeView);
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

        /// <summary>
        /// Determines if this connection should be highlighted (100% opacity) based on selection state.
        /// Only highlights outgoing connections (where the selected node/state is the source).
        /// </summary>
        private bool IsConnectionHighlighted()
        {
            // Check if source node/state is selected (outgoing connections only)
            if (connectionData.IsNodeToNode() || connectionData.IsNodeToState())
            {
                // Source is a node
                var sourceNodeView = graphView.GetNodeView(connectionData.SourceNodeId);
                if (sourceNodeView != null && sourceNodeView.IsSelected)
                    return true;
            }
            else if (connectionData.IsStateToNode() || connectionData.IsStateToState())
            {
                // Source is a state
                var sourceStateView = graphView.GetStateView(connectionData.SourceStateId);
                if (sourceStateView != null && sourceStateView.IsSelected)
                    return true;
            }

            return false;
        }
    }
}
