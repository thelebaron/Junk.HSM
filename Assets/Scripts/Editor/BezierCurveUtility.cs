using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Utility class for drawing smooth bezier curve connections between nodes and states in the graph editor.
/// Provides methods to calculate control points and draw curves using Unity's Painter2D.
/// </summary>
public static class BezierCurveUtility
{


    /// <summary>
    /// Draws a directional bezier curve between two connection points.
    /// </summary>
    /// <param name="painter">The Painter2D instance to draw with</param>
    /// <param name="startPoint">Starting position of the curve</param>
    /// <param name="endPoint">Ending position of the curve</param>
    /// <param name="sourceEdge">Edge that the source point is on</param>
    /// <param name="targetEdge">Edge that the target point is on</param>
    /// <param name="curveStrength">How pronounced the curve should be (default: 100f)</param>
    public static void DrawBezierCurve(Painter2D painter, Vector2 startPoint, Vector2 endPoint,
        ConnectionPointCalculator.Edge sourceEdge, ConnectionPointCalculator.Edge targetEdge, float curveStrength = 100f)
    {
        var controlPoints = CalculateControlPoints(startPoint, endPoint, sourceEdge, targetEdge, curveStrength);
        DrawBezierCurve(painter, startPoint, controlPoints.Item1, controlPoints.Item2, endPoint);
    }

    /// <summary>
    /// Draws a bezier curve with explicit control points.
    /// </summary>
    /// <param name="painter">The Painter2D instance to draw with</param>
    /// <param name="startPoint">Starting position</param>
    /// <param name="controlPoint1">First control point</param>
    /// <param name="controlPoint2">Second control point</param>
    /// <param name="endPoint">Ending position</param>
    public static void DrawBezierCurve(Painter2D painter, Vector2 startPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint)
    {
        painter.BeginPath();
        painter.MoveTo(startPoint);
        painter.BezierCurveTo(controlPoint1, controlPoint2, endPoint);
        painter.Stroke();
    }



    /// <summary>
    /// Calculates control points with custom direction vectors for more complex curve shapes.
    /// </summary>
    /// <param name="startPoint">Starting position</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="startDirection">Direction vector from start point</param>
    /// <param name="endDirection">Direction vector toward end point</param>
    /// <param name="curveStrength">Strength of the curve</param>
    /// <returns>Tuple containing the two control points</returns>
    public static (Vector2, Vector2) CalculateControlPointsWithDirection(Vector2 startPoint, Vector2 endPoint,
        Vector2 startDirection, Vector2 endDirection, float curveStrength = 100f)
    {
        float distance = Vector2.Distance(startPoint, endPoint);
        float adjustedStrength = Mathf.Min(curveStrength, distance * 0.5f);

        Vector2 controlPoint1 = startPoint + startDirection.normalized * adjustedStrength;
        Vector2 controlPoint2 = endPoint + endDirection.normalized * adjustedStrength;

        return (controlPoint1, controlPoint2);
    }

    /// <summary>
    /// Calculates control points based on connection edge directions.
    /// </summary>
    /// <param name="startPoint">Starting position</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="sourceEdge">Edge that the source point is on</param>
    /// <param name="targetEdge">Edge that the target point is on</param>
    /// <param name="curveStrength">Strength of the curve</param>
    /// <returns>Tuple containing the two control points</returns>
    public static (Vector2, Vector2) CalculateControlPoints(Vector2 startPoint, Vector2 endPoint,
        ConnectionPointCalculator.Edge sourceEdge, ConnectionPointCalculator.Edge targetEdge, float curveStrength = 100f)
    {
        // Get direction vectors from the edges
        Vector2 startDirection = ConnectionPointCalculator.GetDirectionFromEdge(sourceEdge);
        Vector2 endDirection = -ConnectionPointCalculator.GetDirectionFromEdge(targetEdge); // Negative because we want direction INTO the target

        return CalculateControlPointsWithDirection(startPoint, endPoint, startDirection, endDirection, curveStrength);
    }

    /// <summary>
    /// Gets the appropriate curve strength based on connection type.
    /// Different connection types can have different curve characteristics.
    /// </summary>
    /// <param name="connectionType">Type of connection (node-to-node, node-to-state, etc.)</param>
    /// <returns>Recommended curve strength for the connection type</returns>
    public static float GetCurveStrengthForConnectionType(ConnectionType connectionType)
    {
        switch (connectionType)
        {
            case ConnectionType.NodeToNode:
                return 120f; // Stronger curves for node-to-node connections
            case ConnectionType.NodeToState:
                return 80f;  // Medium curves for node-to-state connections
            case ConnectionType.StateToNode:
                return 80f;  // Medium curves for state-to-node connections
            case ConnectionType.StateToState:
                return 60f;  // Gentler curves for state-to-state connections
            default:
                return 100f; // Default curve strength
        }
    }

    /// <summary>
    /// Calculates a point along a bezier curve at parameter t (0 to 1).
    /// Useful for positioning arrows or other elements along the curve.
    /// </summary>
    /// <param name="startPoint">Starting position</param>
    /// <param name="controlPoint1">First control point</param>
    /// <param name="controlPoint2">Second control point</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="t">Parameter from 0 to 1 along the curve</param>
    /// <returns>Point on the curve at parameter t</returns>
    public static Vector2 GetPointOnBezierCurve(Vector2 startPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        
        return oneMinusT * oneMinusT * oneMinusT * startPoint +
               3f * oneMinusT * oneMinusT * t * controlPoint1 +
               3f * oneMinusT * t * t * controlPoint2 +
               t * t * t * endPoint;
    }

    /// <summary>
    /// Calculates the tangent direction at a point along a bezier curve.
    /// Useful for positioning arrows that follow the curve direction.
    /// </summary>
    /// <param name="startPoint">Starting position</param>
    /// <param name="controlPoint1">First control point</param>
    /// <param name="controlPoint2">Second control point</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="t">Parameter from 0 to 1 along the curve</param>
    /// <returns>Normalized tangent direction at parameter t</returns>
    public static Vector2 GetTangentOnBezierCurve(Vector2 startPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        
        Vector2 tangent = 3f * oneMinusT * oneMinusT * (controlPoint1 - startPoint) +
                         6f * oneMinusT * t * (controlPoint2 - controlPoint1) +
                         3f * t * t * (endPoint - controlPoint2);
        
        return tangent.normalized;
    }


}

/// <summary>
/// Enumeration of connection types for determining curve characteristics.
/// </summary>
public enum ConnectionType
{
    NodeToNode,
    NodeToState,
    StateToNode,
    StateToState
}
