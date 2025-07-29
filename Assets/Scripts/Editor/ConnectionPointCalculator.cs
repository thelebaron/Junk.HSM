using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility class for calculating dynamic connection points on the edges of nodes and states.
/// Handles edge detection, closest point calculation, and connection spacing to prevent overlaps.
/// </summary>
public static class ConnectionPointCalculator
{
    /// <summary>
    /// Represents a bounding box for connection calculations
    /// </summary>
    public struct BoundingBox
    {
        public Vector2 position;
        public Vector2 size;
        public Vector2 center => position + size * 0.5f;
        public float left => position.x;
        public float right => position.x + size.x;
        public float top => position.y;
        public float bottom => position.y + size.y;

        public BoundingBox(Vector2 pos, Vector2 sz)
        {
            position = pos;
            size = sz;
        }
    }

    /// <summary>
    /// Represents which edge of a bounding box a connection point is on
    /// </summary>
    public enum Edge
    {
        Left,
        Right,
        Top,
        Bottom
    }


    /// <summary>
    /// Calculate dynamic connection points between two elements (nodes or states)
    /// </summary>
    public static (Vector2 sourcePoint, Vector2 targetPoint) CalculateConnectionPoints(
        BoundingBox sourceBounds,
        BoundingBox targetBounds,
        ConnectionData connectionData,
        List<ConnectionData> allConnections)
    {
        // Find the closest edges between source and target
        var (sourceEdge, targetEdge) = FindClosestEdges(sourceBounds, targetBounds);

        // Calculate base points on the edges
        var sourceBasePoint = GetPointOnEdge(sourceBounds, sourceEdge, 0.5f);
        var targetBasePoint = GetPointOnEdge(targetBounds, targetEdge, 0.5f);

        // Apply simple spacing based on connection ID hash for now
        var sourcePoint = ApplySimpleSpacing(sourceBasePoint, sourceBounds, sourceEdge, connectionData.Id);
        var targetPoint = ApplySimpleSpacing(targetBasePoint, targetBounds, targetEdge, connectionData.Id);

        return (sourcePoint, targetPoint);
    }

    /// <summary>
    /// Apply simple spacing based on connection ID to make connections visible
    /// </summary>
    private static Vector2 ApplySimpleSpacing(Vector2 basePoint, BoundingBox bounds, Edge edge, string connectionId)
    {
        // Use connection ID hash to create consistent but different offsets
        int hash = connectionId.GetHashCode();

        // Ensure positive value and map to 0-1 range
        float offset = Mathf.Abs(hash % 1000) / 1000.0f; // 0-1 range, always positive

        // Map to a parameter along the edge (with some padding)
        float parameter = 0.1f + (offset * 0.8f); // Use 10%-90% of the edge

        // Clamp to ensure valid range
        parameter = Mathf.Clamp01(parameter);

        var result = GetPointOnEdge(bounds, edge, parameter);

        // Debug logging to see what's happening
        UnityEngine.Debug.Log($"Connection {connectionId}: hash={hash}, offset={offset}, parameter={parameter}, edge={edge}, result={result}");

        return result;
    }

    /// <summary>
    /// Get bounding box for a node
    /// </summary>
    public static BoundingBox GetNodeBoundingBox(NodeData nodeData, Vector2 panOffset)
    {
        return new BoundingBox(
            nodeData.Position + panOffset,
            nodeData.Size
        );
    }

    /// <summary>
    /// Get bounding box for a state using actual visual element size
    /// </summary>
    public static BoundingBox GetStateBoundingBox(StateData stateData, Vector2 panOffset, StateView stateView = null)
    {
        Vector2 stateSize;

        if (stateView != null && stateView.layout.width > 0 && stateView.layout.height > 0)
        {
            // Use actual rendered size from the visual element
            stateSize = new Vector2(stateView.layout.width, stateView.layout.height);
            UnityEngine.Debug.Log($"State {stateData.Name}: Using actual layout size {stateSize}");
        }
        else
        {
            // Fallback to estimated size based on text content and CSS styling
            stateSize = EstimateStateSize(stateData.Name);
            UnityEngine.Debug.Log($"State {stateData.Name}: Using estimated size {stateSize} (layout not available)");
        }

        var bounds = new BoundingBox(
            stateData.Position + panOffset,
            stateSize
        );

        UnityEngine.Debug.Log($"State {stateData.Name}: Final bounds position={bounds.position}, size={bounds.size}");
        return bounds;
    }

    /// <summary>
    /// Estimate state size based on text content and CSS styling
    /// </summary>
    private static Vector2 EstimateStateSize(string stateName)
    {
        // CSS values from GraphEditor.uss:
        // min-width: 80px, min-height: 25px, padding: 5px 10px, border-width: 2px
        const float minWidth = 80f;
        const float minHeight = 25f;
        const float horizontalPadding = 20f; // 10px left + 10px right
        const float verticalPadding = 10f;   // 5px top + 5px bottom
        const float borderWidth = 4f;        // 2px on each side
        const float connectionPointWidth = 8f + 5f; // connection point + margin

        // Estimate text width based on font size (12px) and character count
        // Using a more conservative estimate for variable-width fonts
        float estimatedTextWidth = Mathf.Max(stateName.Length * 6.5f, 40f); // ~6.5 pixels per character

        float contentWidth = estimatedTextWidth + connectionPointWidth + horizontalPadding;
        float totalWidth = Mathf.Max(minWidth, contentWidth) + borderWidth;
        float totalHeight = minHeight + verticalPadding + borderWidth;

        return new Vector2(totalWidth, totalHeight);
    }

    /// <summary>
    /// Find the closest edges between two bounding boxes
    /// </summary>
    private static (Edge sourceEdge, Edge targetEdge) FindClosestEdges(BoundingBox source, BoundingBox target)
    {
        var sourceCenter = source.center;
        var targetCenter = target.center;

        // Calculate the direction vector from source to target
        var direction = targetCenter - sourceCenter;

        // If the boxes are very close or overlapping, use a default horizontal connection
        if (direction.magnitude < 1f)
        {
            return (Edge.Right, Edge.Left);
        }

        Edge sourceEdge, targetEdge;

        // Determine which axis has the larger separation
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal separation is larger - use horizontal connection
            if (direction.x > 0)
            {
                // Target is to the right of source
                sourceEdge = Edge.Right;
                targetEdge = Edge.Left;
            }
            else
            {
                // Target is to the left of source
                sourceEdge = Edge.Left;
                targetEdge = Edge.Right;
            }
        }
        else
        {
            // Vertical separation is larger - use vertical connection
            if (direction.y > 0)
            {
                // Target is below source
                sourceEdge = Edge.Bottom;
                targetEdge = Edge.Top;
            }
            else
            {
                // Target is above source
                sourceEdge = Edge.Top;
                targetEdge = Edge.Bottom;
            }
        }

        return (sourceEdge, targetEdge);
    }

    /// <summary>
    /// Get a point on the specified edge of a bounding box
    /// </summary>
    private static Vector2 GetPointOnEdge(BoundingBox bounds, Edge edge, float parameter)
    {
        parameter = Mathf.Clamp01(parameter);
        var point = Vector2.zero;
        switch (edge)
        {
            case Edge.Left:
                point = new Vector2(bounds.left, Mathf.Lerp(bounds.top, bounds.bottom, parameter));
                //Debug.Log($"Left: {point}");
                return point;
            case Edge.Right:
                point = new Vector2(bounds.right, Mathf.Lerp(bounds.top, bounds.bottom, parameter));
                //Debug.Log($"Right: {point}");
                return point;
            case Edge.Top:
                point = new Vector2(Mathf.Lerp(bounds.left, bounds.right, parameter), bounds.top);
                //Debug.Log($"Top: {point}");
                return point;
            case Edge.Bottom:
                point = new Vector2(Mathf.Lerp(bounds.left, bounds.right, parameter), bounds.bottom);
                //Debug.Log($"Bottom: {point}");
                return point;
            default:
                return bounds.center;
        }
    }
}
