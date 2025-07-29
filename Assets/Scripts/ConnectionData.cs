using System;
using UnityEngine;

namespace Junk.Yard
{
    [System.Serializable]
    public class ConnectionData
    {
        [SerializeField] private string id;
        [SerializeField] private string sourceStateId;
        [SerializeField] private string targetStateId;
        [SerializeField] private string sourceNodeId;
        [SerializeField] private string targetNodeId;
        [SerializeField] private bool   isNodeToNodeConnection;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string SourceStateId
        {
            get => sourceStateId;
            set => sourceStateId = value;
        }

        public string TargetStateId
        {
            get => targetStateId;
            set => targetStateId = value;
        }

        public string SourceNodeId
        {
            get => sourceNodeId;
            set => sourceNodeId = value;
        }

        public string TargetNodeId
        {
            get => targetNodeId;
            set => targetNodeId = value;
        }

        public bool IsNodeToNodeConnection
        {
            get => isNodeToNodeConnection;
            set => isNodeToNodeConnection = value;
        }

        public ConnectionData()
        {
            id                     = Guid.NewGuid().ToString();
            sourceStateId          = string.Empty;
            targetStateId          = string.Empty;
            sourceNodeId           = string.Empty;
            targetNodeId           = string.Empty;
            isNodeToNodeConnection = false;
        }

        public ConnectionData(string sourceState, string targetState, string sourceNode, string targetNode, bool nodeToNode = false)
        {
            id                     = Guid.NewGuid().ToString();
            sourceStateId          = sourceState;
            targetStateId          = targetState;
            sourceNodeId           = sourceNode;
            targetNodeId           = targetNode;
            isNodeToNodeConnection = nodeToNode;
        }

        public override bool Equals(object obj)
        {
            if (obj is ConnectionData other)
            {
                return id == other.id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return id?.GetHashCode() ?? 0;
        }

        // Helper methods for connection type determination
        public bool IsStateToState()
        {
            return !string.IsNullOrEmpty(sourceStateId) && !string.IsNullOrEmpty(targetStateId) && !isNodeToNodeConnection;
        }

        public bool IsNodeToState()
        {
            return !string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetStateId) &&
                string.IsNullOrEmpty(sourceStateId)    && !isNodeToNodeConnection;
        }

        public bool IsStateToNode()
        {
            return !string.IsNullOrEmpty(sourceStateId) && !string.IsNullOrEmpty(targetNodeId) &&
                string.IsNullOrEmpty(targetStateId)     && !isNodeToNodeConnection;
        }

        public bool IsNodeToNode()
        {
            return isNodeToNodeConnection && !string.IsNullOrEmpty(sourceNodeId) && !string.IsNullOrEmpty(targetNodeId);
        }

        public string GetConnectionTypeDescription()
        {
            if (IsStateToState()) return "State → State";
            if (IsNodeToState()) return "Node → State";
            if (IsStateToNode()) return "State → Node";
            if (IsNodeToNode()) return "Node → Node";
            return "Unknown";
        }
    }
}
