using System.Collections.Generic;
using UnityEngine;

namespace Junk.Yard.Editor
{
    [CreateAssetMenu(fileName = "New Graph", menuName = "Graph Editor/Graph Data")]
    public class GraphData : ScriptableObject
    {
        [SerializeField] private List<NodeData> nodes = new List<NodeData>();

        public List<NodeData> Nodes
        {
            get => nodes;
            set => nodes = value;
        }

        public void AddNode(NodeData node)
        {
            if (node != null && !nodes.Contains(node))
            {
                nodes.Add(node);
            }
        }

        public void RemoveNode(NodeData node)
        {
            if (node != null)
            {
                // Remove all connections related to this node from other nodes/states
                foreach (var otherNode in nodes)
                {
                    if (otherNode.Id != node.Id)
                    {
                        otherNode.OutgoingConnections.RemoveAll(c => c.TargetNodeId == node.Id);

                        // Remove connections from states within other nodes
                        foreach (var state in otherNode.States)
                        {
                            state.OutgoingConnections.RemoveAll(c => c.TargetNodeId == node.Id);
                        }
                    }
                }

                nodes.Remove(node);
            }
        }

        public void AddState(StateData state)
        {
            if (state != null)
            {
                // Add to the parent node if it exists
                var parentNode = GetNodeById(state.ParentNodeId);
                if (parentNode != null && !parentNode.States.Contains(state))
                {
                    parentNode.AddState(state, this);
                }
            }
        }

        public void RemoveState(StateData state)
        {
            if (state != null)
            {
                // Remove from parent node
                var parentNode = GetNodeById(state.ParentNodeId);
                if (parentNode != null)
                {
                    parentNode.RemoveState(state, this);
                }

                // Remove all connections related to this state from all nodes and their states
                foreach (var node in nodes)
                {
                    node.OutgoingConnections.RemoveAll(c => c.TargetStateId == state.Id);

                    foreach (var nodeState in node.States)
                    {
                        nodeState.OutgoingConnections.RemoveAll(c => c.TargetStateId == state.Id);
                    }
                }
            }
        }

        public NodeData GetNodeById(string nodeId)
        {
            return nodes.Find(n => n.Id == nodeId);
        }

        public StateData GetStateById(string stateId)
        {
            foreach (var node in nodes)
            {
                var state = node.GetStateById(stateId);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        public List<StateData> GetStatesForNode(string nodeId)
        {
            var node = GetNodeById(nodeId);
            return node?.States ?? new List<StateData>();
        }

        public List<ConnectionData> GetAllConnections()
        {
            var allConnections = new List<ConnectionData>();

            // Collect connections from nodes and their states
            foreach (var node in nodes)
            {
                allConnections.AddRange(node.OutgoingConnections);

                foreach (var state in node.States)
                {
                    allConnections.AddRange(state.OutgoingConnections);
                }
            }

            return allConnections;
        }
    }
}
