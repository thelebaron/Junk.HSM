using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Junk.Yard.Editor
{
    [Serializable]
    public class StateData
    {
        public                   string               Name;
        public                   string               Id;
        [SerializeField] private Vector2              position; // Changed from localPosition to position (world position)
        public                   string               ParentNodeId; // should never be null
        public                   List<ConnectionData> OutgoingConnections;

        public Vector2 Position
        {
            get
            {
                // Safety check for NaN values
                if (float.IsNaN(position.x) || float.IsNaN(position.y))
                {
                    position = Vector2.zero;
                }

                return position;
            }
            set => position = value;
        }

        public StateData()
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = "New State";
            position            = Vector2.zero;
            ParentNodeId        = string.Empty;
            OutgoingConnections = new List<ConnectionData>();
        }

        public StateData(string stateName, Vector2 statePosition, string nodeId)
        {
            Id                  = Guid.NewGuid().ToString();
            Name                = stateName;
            position            = statePosition;
            ParentNodeId        = nodeId;
            OutgoingConnections = new List<ConnectionData>();
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
    }
}