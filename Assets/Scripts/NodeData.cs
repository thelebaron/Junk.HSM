using System;
using System.Collections.Generic;
using UnityEngine;

namespace Junk.Yard.Editor
{
    [Serializable]
    public class NodeData
    {
        public string               Name;
        public string               Id;
        public Vector2              Position;
        public Vector2              Size;
        public List<StateData>      States;
        public List<ConnectionData> OutgoingConnections = new List<ConnectionData>();
        
        public NodeData()
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = "New Node";
            Position            = Vector2.zero;
            Size                = new Vector2(200, 100); // Smaller default size
            States              = new List<StateData>();
            OutgoingConnections = new List<ConnectionData>();
        }

        public NodeData(string nodeName, Vector2 nodePosition)
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = nodeName;
            Position            = nodePosition;
            Size                = new Vector2(200, 100); // Smaller default size
            States              = new List<StateData>();
            OutgoingConnections = new List<ConnectionData>();
        }

        public void AddState(StateData state, GraphData graphData = null)
        {
            if (state == null) return;
            state.ParentNodeId = Id;
            States.Add(state);
            RecalculateSize(graphData);
        }

        public void RemoveState(StateData state, GraphData graphData = null)
        {
            if (state == null) return;
            States.Remove(state);
            RecalculateSize(graphData);
        }

        public StateData GetStateById(string stateId)
        {
            return States.Find(s => s.Id == stateId);
        }

        public void AddConnection(ConnectionData connection)
        {
            if (connection != null && !OutgoingConnections.Contains(connection))
            {
                OutgoingConnections.Add(connection);
            }
        }

        public void RemoveConnection(ConnectionData connection)
        {
            if (connection != null)
            {
                OutgoingConnections.Remove(connection);
            }
        }

        public void RemoveConnectionById(string connectionId)
        {
            OutgoingConnections.RemoveAll(c => c.Id == connectionId);
        }

        public void RecalculateSize(GraphData graphData = null)
        {
            const float stateWidth  = 100f;
            const float stateHeight = 25f;
            const float padding     = 20f;
            const float titleHeight = 30f;
            const float minWidth    = 200f;
            const float minHeight   = 60f;

            // Use internal states list - no need for GraphData dependency
            if (States.Count == 0)
            {
                Size = new Vector2(minWidth, minHeight);
                return;
            }

            // Calculate bounds of all states in world space
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var state in States)
            {
                minX = Mathf.Min(minX, state.Position.x);
                minY = Mathf.Min(minY, state.Position.y);
                maxX = Mathf.Max(maxX, state.Position.x + stateWidth);
                maxY = Mathf.Max(maxY, state.Position.y + stateHeight);
            }

            // Calculate the new node position and size to encompass all states
            float newNodeX  = minX          - padding;
            float newNodeY  = minY          - padding - titleHeight;
            float newWidth  = (maxX - minX) + (padding * 2);
            float newHeight = (maxY - minY) + (padding * 2) + titleHeight;

            // Ensure minimum size
            newWidth  = Mathf.Max(newWidth, minWidth);
            newHeight = Mathf.Max(newHeight, minHeight);

            // Update position and size
            Position = new Vector2(newNodeX, newNodeY);
            Size     = new Vector2(newWidth, newHeight);
        }
    }
}
