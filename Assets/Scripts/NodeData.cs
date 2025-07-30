using System;
using System.Collections.Generic;
using UnityEngine;


namespace Junk.Web
{
    [Serializable]
    public class NodeData
    {
        public string               Name;
        public string               Id;
        public Vector2              Position;
        public List<StateData>      States;
        public List<ConnectionData> OutgoingConnections = new List<ConnectionData>();
        public Color                NodeColor = Color.white; // Default color for nodes
        
        public NodeData()
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = "New Node";
            Position            = Vector2.zero;
            States              = new List<StateData>();
            OutgoingConnections = new List<ConnectionData>();
            NodeColor           = Color.white; // Default color
        }

        public NodeData(string nodeName, Vector2 nodePosition)
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = nodeName;
            Position            = nodePosition;
            States              = new List<StateData>();
            OutgoingConnections = new List<ConnectionData>();
            NodeColor           = Color.white; // Default color
        }

        public void AddState(StateData state, GraphData graphData = null)
        {
            if (state == null) return;
            state.ParentNodeId = Id;
            States.Add(state);
        }

        public void RemoveState(StateData state, GraphData graphData = null)
        {
            if (state == null) return;
            States.Remove(state);
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

        

        /// <summary>
        /// Gets the darkened version of the node color for use on the node itself
        /// </summary>
        public Color GetDarkenedNodeColor()
        {
            // Darken the color by reducing HSV value by 30%
            Color.RGBToHSV(NodeColor, out float h, out float s, out float v);
            v *= 0.7f; // Darken by 30%
            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        /// Gets the state color (original node color)
        /// </summary>
        public Color GetStateColor()
        {
            return NodeColor;
        }
    }
}
