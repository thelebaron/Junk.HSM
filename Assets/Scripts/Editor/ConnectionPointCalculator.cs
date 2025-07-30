using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Junk.Web.Editor
{
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
            public float   left   => position.x;
            public float   right  => position.x + size.x;
            public float   top    => position.y;
            public float   bottom => position.y + size.y;

            public BoundingBox(Vector2 pos, Vector2 sz)
            {
                position = pos;
                size     = sz;
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
        public static (Vector2 sourcePoint, Vector2 targetPoint, Edge sourceEdge, Edge targetEdge) CalculateConnectionPoints(
            BoundingBox          sourceBounds,
            BoundingBox          targetBounds,
            ConnectionData       connectionData,
            List<ConnectionData> allConnections)
        {
            // Find the closest edges between source and target
            var (sourceEdge, targetEdge) = FindClosestEdges(sourceBounds, targetBounds);

            // Create a connection context that includes bounding box information for edge calculation
            var connectionContext = new ConnectionContext
            {
                AllConnections    = allConnections,
                CurrentConnection = connectionData,
                SourceBounds      = sourceBounds,
                TargetBounds      = targetBounds
            };

            // Apply spacing to prevent overlapping connections
            var sourcePoint = ApplySpacing(sourceBounds, sourceEdge, connectionContext, true);
            var targetPoint = ApplySpacing(targetBounds, targetEdge, connectionContext, false);

            return (sourcePoint, targetPoint, sourceEdge, targetEdge);
        }

        /// <summary>
        /// Context information for connection calculations
        /// </summary>
        private struct ConnectionContext
        {
            public List<ConnectionData> AllConnections;
            public ConnectionData       CurrentConnection;
            public BoundingBox          SourceBounds;
            public BoundingBox          TargetBounds;
        }

        /// <summary>
        /// Get the direction vector for a given edge
        /// </summary>
        public static Vector2 GetDirectionFromEdge(Edge edge)
        {
            switch (edge)
            {
                case Edge.Left:
                    return Vector2.left;
                case Edge.Right:
                    return Vector2.right;
                case Edge.Top:
                    return Vector2.up;
                case Edge.Bottom:
                    return Vector2.down;
                default:
                    return Vector2.right; // Default to right
            }
        }

        /// <summary>
        /// Apply spacing to prevent overlapping connections with ordered positioning to minimize crossings
        /// </summary>
        private static Vector2 ApplySpacing(BoundingBox bounds, Edge edge, ConnectionContext context, bool isSource)
        {
            // Create a unique key for this source element and edge combination
            string sourceElementId = GetSourceElementId(context.CurrentConnection, isSource);
            bool   sourceIsNode    = IsSourceElementNode(context.CurrentConnection, isSource);
            string sourceKey       = $"{(sourceIsNode ? "node" : "state")}_{sourceElementId}_{edge}";

            // Find all connections that share the same source element and edge
            var sameSourceAndEdgeConnections = new List<ConnectionData>();

            foreach (var connection in context.AllConnections)
            {
                string connectionSourceId = GetSourceElementId(connection, isSource);
                bool   connectionIsNode   = IsSourceElementNode(connection, isSource);

                if (connectionSourceId == sourceElementId && connectionIsNode == sourceIsNode)
                {
                    // Calculate what edge this connection would use
                    var connectionEdge = CalculateConnectionEdge(connection, context, isSource);

                    // Only include if it uses the same edge
                    if (connectionEdge == edge)
                    {
                        sameSourceAndEdgeConnections.Add(connection);
                    }
                }
            }

            // Sort them by target ID to create consistent ordering
            sameSourceAndEdgeConnections.Sort((a, b) =>
            {
                string targetA = isSource ? GetTargetId(a) : GetSourceId(a);
                string targetB = isSource ? GetTargetId(b) : GetSourceId(b);
                return string.Compare(targetA, targetB, System.StringComparison.Ordinal);
            });

            // Find the index of current connection in sorted list
            int connectionIndex = sameSourceAndEdgeConnections.FindIndex(c => c.Id == context.CurrentConnection.Id);

            // If not found, fall back to hash-based approach
            if (connectionIndex == -1)
            {
                string hashInput = context.CurrentConnection.Id + "_" + (isSource ? "src" : "tgt") + "_" + edge.ToString();
                int    hash      = hashInput.GetHashCode();
                float  offset    = Mathf.Abs(hash % 10000) / 10000.0f;
                float  p         = 0.15f + (offset * 0.7f);
                return GetPointOnEdge(bounds, edge, Mathf.Clamp01(p));
            }

            // Calculate parameter based on sorted position
            float parameter;
            if (sameSourceAndEdgeConnections.Count == 1)
            {
                parameter = 0.5f; // Center single connections
            }
            else
            {
                // Distribute evenly across the edge (0.15 to 0.85 range)
                parameter = 0.15f + (connectionIndex / (float)(sameSourceAndEdgeConnections.Count - 1)) * 0.7f;
            }

            return GetPointOnEdge(bounds, edge, Mathf.Clamp01(parameter));
        }

        /// <summary>
        /// Calculate which edge a connection would use based on a simplified heuristic
        /// </summary>
        private static Edge CalculateConnectionEdge(ConnectionData connection, ConnectionContext context, bool isSource)
        {
            // For connections other than the current one, we use a simplified heuristic
            // based on the connection IDs to determine likely edge usage

            // Use a hash-based approach to consistently assign edges
            string sourceId = GetSourceElementId(connection, isSource);
            string targetId = isSource ? GetTargetId(connection) : GetSourceId(connection);

            string hashInput = sourceId + "_to_" + targetId;
            int    hash      = hashInput.GetHashCode();

            // Use the hash to determine edge preference
            int edgeIndex = Mathf.Abs(hash) % 4;
            return (Edge)edgeIndex;
        }



        /// <summary>
        /// Get the source element ID for a connection (node or state ID)
        /// </summary>
        private static string GetSourceElementId(ConnectionData connection, bool isSource)
        {
            if (isSource)
            {
                // Getting source element
                if (connection.IsNodeToNode() || connection.IsNodeToState())
                    return connection.SourceNodeId ?? "";
                else
                    return connection.SourceStateId ?? "";
            }
            else
            {
                // Getting target element
                if (connection.IsNodeToNode() || connection.IsStateToNode())
                    return connection.TargetNodeId ?? "";
                else
                    return connection.TargetStateId ?? "";
            }
        }

        /// <summary>
        /// Check if the source element is a node (vs a state)
        /// </summary>
        private static bool IsSourceElementNode(ConnectionData connection, bool isSource)
        {
            if (isSource)
            {
                return connection.IsNodeToNode() || connection.IsNodeToState();
            }
            else
            {
                return connection.IsNodeToNode() || connection.IsStateToNode();
            }
        }



        /// <summary>
        /// Get target ID for sorting
        /// </summary>
        private static string GetTargetId(ConnectionData connection)
        {
            if (connection.IsNodeToNode() || connection.IsStateToNode())
                return connection.TargetNodeId ?? "";
            else
                return connection.TargetStateId ?? "";
        }

        /// <summary>
        /// Get source ID for sorting
        /// </summary>
        private static string GetSourceId(ConnectionData connection)
        {
            if (connection.IsNodeToNode() || connection.IsNodeToState())
                return connection.SourceNodeId ?? "";
            else
                return connection.SourceStateId ?? "";
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
            }
            else
            {
                // Fallback to estimated size based on text content and CSS styling
                stateSize = EstimateStateSize(stateData.Name);
            }

            var bounds = new BoundingBox(
                stateData.Position + panOffset,
                stateSize
            );

            return bounds;
        }

        /// <summary>
        /// Estimate state size based on text content and CSS styling
        /// </summary>
        private static Vector2 EstimateStateSize(string stateName)
        {
            // CSS values from GraphEditor.uss:
            // min-width: 80px, min-height: 25px, padding: 5px 10px, border-width: 2px
            const float minWidth             = 80f;
            const float minHeight            = 25f;
            const float horizontalPadding    = 20f;     // 10px left + 10px right
            const float verticalPadding      = 10f;     // 5px top + 5px bottom
            const float borderWidth          = 4f;      // 2px on each side
            const float connectionPointWidth = 8f + 5f; // connection point + margin

            // Estimate text width based on font size (12px) and character count
            // Using a more conservative estimate for variable-width fonts
            float estimatedTextWidth = Mathf.Max(stateName.Length * 6.5f, 40f); // ~6.5 pixels per character

            float contentWidth = estimatedTextWidth                + connectionPointWidth + horizontalPadding;
            float totalWidth   = Mathf.Max(minWidth, contentWidth) + borderWidth;
            float totalHeight  = minHeight                         + verticalPadding + borderWidth;

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
}
