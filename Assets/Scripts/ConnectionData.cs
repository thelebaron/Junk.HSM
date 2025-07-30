using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Junk.UI.Web
{
    [Serializable]
    public class ConnectionData
    {
        public string Id;
        public string SourceStateId;
        public string TargetStateId;
        public string SourceNodeId;
        public string TargetNodeId;
        public bool   IsNodeToNodeConnection;

        public ConnectionData()
        {
            Id                     = Guid.NewGuid().ToString();
            SourceStateId          = string.Empty;
            TargetStateId          = string.Empty;
            SourceNodeId           = string.Empty;
            TargetNodeId           = string.Empty;
            IsNodeToNodeConnection = false;
        }

        public ConnectionData(string sourceState, string targetState, string sourceNode, string targetNode, bool nodeToNode = false)
        {
            Id                     = Guid.NewGuid().ToString();
            SourceStateId          = sourceState;
            TargetStateId          = targetState;
            SourceNodeId           = sourceNode;
            TargetNodeId           = targetNode;
            IsNodeToNodeConnection = nodeToNode;
        }

        public override bool Equals(object obj)
        {
            if (obj is ConnectionData other)
            {
                return Id == other.Id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        // Helper methods for connection type determination
        public bool IsStateToState()
        {
            return !string.IsNullOrEmpty(SourceStateId) && !string.IsNullOrEmpty(TargetStateId) && !IsNodeToNodeConnection;
        }

        public bool IsNodeToState()
        {
            return !string.IsNullOrEmpty(SourceNodeId) && !string.IsNullOrEmpty(TargetStateId) &&
                string.IsNullOrEmpty(SourceStateId)    && !IsNodeToNodeConnection;
        }

        public bool IsStateToNode()
        {
            return !string.IsNullOrEmpty(SourceStateId) && !string.IsNullOrEmpty(TargetNodeId) &&
                string.IsNullOrEmpty(TargetStateId)     && !IsNodeToNodeConnection;
        }

        public bool IsNodeToNode()
        {
            return IsNodeToNodeConnection && !string.IsNullOrEmpty(SourceNodeId) && !string.IsNullOrEmpty(TargetNodeId);
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