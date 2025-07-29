using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Utility class for drawing smooth bezier curve connections between nodes and states in the graph editor.
/// Provides methods to calculate control points and draw curves using Unity's Painter2D.
/// </summary>
public static class BezierCurveUtility
{
    /// <summary>
    /// Draws a smooth bezier curve between two points using the provided painter.
    /// </summary>
    /// <param name="painter">The Painter2D instance to draw with</param>
    /// <param name="startPoint">Starting position of the curve</param>
    /// <param name="endPoint">Ending position of the curve</param>
    /// <param name="curveStrength">How pronounced the curve should be (default: 100f)</param>
    public static void DrawBezierCurve(Painter2D painter, Vector2 startPoint, Vector2 endPoint, float curveStrength = 100f)
    {
        var controlPoints = CalculateControlPoints(startPoint, endPoint, curveStrength);
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
    /// Calculates appropriate control points for a smooth bezier curve between two points.
    /// The control points are positioned to create a natural-looking curve that flows horizontally
    /// from the source before curving toward the target.
    /// </summary>
    /// <param name="startPoint">Starting position</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="curveStrength">How far the control points extend horizontally</param>
    /// <returns>Tuple containing the two control points</returns>
    public static (Vector2, Vector2) CalculateControlPoints(Vector2 startPoint, Vector2 endPoint, float curveStrength = 100f)
    {
        // Calculate the distance between points to adjust curve strength dynamically
        float distance = Vector2.Distance(startPoint, endPoint);
        
        // Adjust curve strength based on distance - longer connections get more pronounced curves
        float adjustedStrength = Mathf.Min(curveStrength, distance * 0.5f);
        
        // For horizontal flow, control points extend horizontally from start and end points
        Vector2 controlPoint1 = new Vector2(startPoint.x + adjustedStrength, startPoint.y);
        Vector2 controlPoint2 = new Vector2(endPoint.x - adjustedStrength, endPoint.y);
        
        return (controlPoint1, controlPoint2);
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

    /// <summary>
    /// Draws a bezier curve with visual debug information (control points and tangent lines).
    /// Useful for debugging curve shapes during development.
    /// </summary>
    /// <param name="painter">The Painter2D instance to draw with</param>
    /// <param name="startPoint">Starting position</param>
    /// <param name="endPoint">Ending position</param>
    /// <param name="curveStrength">Curve strength</param>
    /// <param name="debugColor">Color for debug elements</param>
    public static void DrawBezierCurveWithDebug(Painter2D painter, Vector2 startPoint, Vector2 endPoint,
        float curveStrength = 100f, Color debugColor = default)
    {
        if (debugColor == default) debugColor = Color.red;

        var controlPoints = CalculateControlPoints(startPoint, endPoint, curveStrength);

        // Store original color
        var originalColor = painter.strokeColor;

        // Draw the main curve
        DrawBezierCurve(painter, startPoint, controlPoints.Item1, controlPoints.Item2, endPoint);

        // Draw debug elements
        painter.strokeColor = debugColor;
        painter.lineWidth = 1.0f;

        // Draw control point lines
        painter.BeginPath();
        painter.MoveTo(startPoint);
        painter.LineTo(controlPoints.Item1);
        painter.Stroke();

        painter.BeginPath();
        painter.MoveTo(endPoint);
        painter.LineTo(controlPoints.Item2);
        painter.Stroke();

        // Draw control points as small circles
        DrawDebugPoint(painter, controlPoints.Item1, 3f);
        DrawDebugPoint(painter, controlPoints.Item2, 3f);

        // Restore original color
        painter.strokeColor = originalColor;
    }

    /// <summary>
    /// Draws a small circle at the specified position for debugging purposes.
    /// </summary>
    private static void DrawDebugPoint(Painter2D painter, Vector2 position, float radius)
    {
        painter.BeginPath();
        painter.Arc(position, radius, 0, 2 * Mathf.PI);
        painter.Stroke();
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
