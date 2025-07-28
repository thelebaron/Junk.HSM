using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Graph", menuName = "Graph Editor/Graph Data")]
public class GraphData : ScriptableObject
{
    [SerializeField] private List<NodeData> nodes = new List<NodeData>();
    [SerializeField] private List<StateData> states = new List<StateData>(); // All states in the graph

    public List<NodeData> Nodes
    {
        get => nodes;
        set => nodes = value;
    }

    public List<StateData> States
    {
        get => states;
        set => states = value;
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
            // Remove all states belonging to this node
            states.RemoveAll(s => s.ParentNodeId == node.Id);

            // Remove all connections related to this node from other nodes/states
            foreach (var otherNode in nodes)
            {
                if (otherNode.Id != node.Id)
                {
                    otherNode.OutgoingConnections.RemoveAll(c => c.TargetNodeId == node.Id);
                }
            }

            foreach (var state in states)
            {
                state.OutgoingConnections.RemoveAll(c => c.TargetNodeId == node.Id);
            }

            nodes.Remove(node);
        }
    }

    public void AddState(StateData state)
    {
        if (state != null && !states.Contains(state))
        {
            states.Add(state);

            // Also add to the parent node if it exists
            var parentNode = GetNodeById(state.ParentNodeId);
            if (parentNode != null && !parentNode.States.Contains(state))
            {
                parentNode.States.Add(state);
                parentNode.RecalculateSize();
            }
        }
    }

    public void RemoveState(StateData state)
    {
        if (state != null)
        {
            // Remove from global states list
            states.Remove(state);

            // Remove from parent node
            var parentNode = GetNodeById(state.ParentNodeId);
            if (parentNode != null)
            {
                parentNode.RemoveState(state);
            }

            // Remove all connections related to this state
            foreach (var node in nodes)
            {
                node.OutgoingConnections.RemoveAll(c => c.TargetStateId == state.Id);
            }

            foreach (var otherState in states)
            {
                otherState.OutgoingConnections.RemoveAll(c => c.TargetStateId == state.Id);
            }
        }
    }

    public NodeData GetNodeById(string nodeId)
    {
        return nodes.Find(n => n.Id == nodeId);
    }

    public StateData GetStateById(string stateId)
    {
        return states.Find(s => s.Id == stateId);
    }

    public List<StateData> GetStatesForNode(string nodeId)
    {
        return states.FindAll(s => s.ParentNodeId == nodeId);
    }

    public List<ConnectionData> GetAllConnections()
    {
        var allConnections = new List<ConnectionData>();

        // Collect connections from nodes
        foreach (var node in nodes)
        {
            allConnections.AddRange(node.OutgoingConnections);
        }

        // Collect connections from states
        foreach (var state in states)
        {
            allConnections.AddRange(state.OutgoingConnections);
        }

        return allConnections;
    }
}
