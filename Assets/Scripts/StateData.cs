using System;
using System.Collections.Generic;
using UnityEngine;

namespace Junk.Yard.Editor
{
    [System.Serializable]
    public class StateData
    {
        [SerializeField] private string               name;
        [SerializeField] private string               id;
        [SerializeField] private Vector2              position; // Changed from localPosition to position (world position)
        [SerializeField] private string               parentNodeId;
        [SerializeField] private List<ConnectionData> outgoingConnections = new List<ConnectionData>();

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

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

        public string ParentNodeId
        {
            get => parentNodeId;
            set => parentNodeId = value;
        }

        public List<ConnectionData> OutgoingConnections
        {
            get => outgoingConnections;
            set => outgoingConnections = value;
        }

        public StateData()
        {
            id                  = Guid.NewGuid().ToString();
            name                = "New State";
            position            = Vector2.zero;
            parentNodeId        = string.Empty;
            outgoingConnections = new List<ConnectionData>();
        }

        public StateData(string stateName, Vector2 statePosition, string nodeId)
        {
            id                  = Guid.NewGuid().ToString();
            name                = stateName;
            position            = statePosition;
            parentNodeId        = nodeId;
            outgoingConnections = new List<ConnectionData>();
        }

        public void AddConnection(ConnectionData connection)
        {
            if (connection != null && !outgoingConnections.Contains(connection))
            {
                outgoingConnections.Add(connection);
            }
        }

        public void RemoveConnection(ConnectionData connection)
        {
            if (connection != null)
            {
                outgoingConnections.Remove(connection);
            }
        }

        public void RemoveConnectionById(string connectionId)
        {
            outgoingConnections.RemoveAll(c => c.Id == connectionId);
        }
    }
}