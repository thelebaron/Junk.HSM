using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor.Utilities
{
    public static class ArrowUtility
    {
        /// <summary>
        /// Draws a solid triangle arrow on a bezier curve at the target position
        /// </summary>
        /// <param name="painter">The Painter2D instance</param>
        /// <param name="start">Curve start position</param>
        /// <param name="end">Curve end position (arrow tip)</param>
        /// <param name="sourceEdge">Source connection edge</param>
        /// <param name="targetEdge">Target connection edge</param>
        /// <param name="curveStrength">Bezier curve strength</param>
        /// <param name="arrowSize">Size of the arrow (default: 10f)</param>
        /// <param name="color">Arrow color (default: white)</param>
        public static void DrawArrowOnCurve(Painter2D                      painter,
                                            Vector2                        start,
                                            Vector2                        end,
                                            ConnectionPointCalculator.Edge sourceEdge,
                                            ConnectionPointCalculator.Edge targetEdge,
                                            float                          curveStrength,
                                            float                          arrowSize = 10f,
                                            Color?                         color = null)
        {
            // Calculate control points for the curve using edge information
            var controlPoints = BezierCurveUtility.CalculateControlPoints(start, end, sourceEdge, targetEdge, curveStrength);

            // Get the direction at the end of the curve (t = 1.0)
            var direction = BezierCurveUtility.GetTangentOnBezierCurve(start, controlPoints.Item1, controlPoints.Item2, end, 1.0f);

            DrawSolidTriangleArrow(painter, end, direction, arrowSize, color ?? Color.white);
        }

        /// <summary>
        /// Draws a solid triangle arrow using a simple direction vector
        /// </summary>
        /// <param name="painter">The Painter2D instance</param>
        /// <param name="start">Line start position</param>
        /// <param name="end">Line end position (arrow tip)</param>
        /// <param name="arrowSize">Size of the arrow (default: 10f)</param>
        /// <param name="color">Arrow color (default: white)</param>
        public static void DrawArrow(Painter2D painter, Vector2 start, Vector2 end, float arrowSize = 10f, Color? color = null)
        {
            var direction = (end - start).normalized;
            DrawSolidTriangleArrow(painter, end, direction, arrowSize, color ?? Color.white);
        }

        /// <summary>
        /// Core method to draw a solid triangle arrow
        /// </summary>
        /// <param name="painter">The Painter2D instance</param>
        /// <param name="tip">Arrow tip position</param>
        /// <param name="direction">Arrow direction (normalized)</param>
        /// <param name="size">Arrow size</param>
        /// <param name="color">Arrow color</param>
        private static void DrawSolidTriangleArrow(Painter2D painter, Vector2 tip, Vector2 direction, float size, Color color)
        {
            // Calculate triangle points
            var basePoint     = tip - direction * size;
            var perpendicular = new Vector2(-direction.y, direction.x);

            var left  = basePoint + perpendicular * size * 0.5f;
            var right = basePoint - perpendicular * size * 0.5f;

            // Store current fill color and set to white
            var originalFillColor = painter.fillColor;
            painter.fillColor = color;

            // Draw solid triangle arrow
            painter.BeginPath();
            painter.MoveTo(tip);
            painter.LineTo(left);
            painter.LineTo(right);
            painter.ClosePath();
            painter.Fill();

            // Restore original fill color
            painter.fillColor = originalFillColor;
        }
    }
}