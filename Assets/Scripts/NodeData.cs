using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeData
{
    [SerializeField] private string id;
    [SerializeField] private string name;
    [SerializeField] private Vector2 position;
    [SerializeField] private Vector2 size;
    [SerializeField] private List<StateData> states;
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
        get => position;
        set => position = value;
    }

    public Vector2 Size
    {
        get => size;
        set => size = value;
    }

    public List<StateData> States
    {
        get => states;
        set => states = value;
    }

    public List<ConnectionData> OutgoingConnections
    {
        get => outgoingConnections;
        set => outgoingConnections = value;
    }

    public NodeData()
    {
        id = Guid.NewGuid().ToString();
        name = "New Node";
        position = Vector2.zero;
        size = new Vector2(200, 100); // Smaller default size
        states = new List<StateData>();
        outgoingConnections = new List<ConnectionData>();
    }

    public NodeData(string nodeName, Vector2 nodePosition)
    {
        id = Guid.NewGuid().ToString();
        name = nodeName;
        position = nodePosition;
        size = new Vector2(200, 100); // Smaller default size
        states = new List<StateData>();
        outgoingConnections = new List<ConnectionData>();
    }

    public void AddState(StateData state)
    {
        if (state != null)
        {
            state.ParentNodeId = id;
            states.Add(state);
            RecalculateSize();
        }
    }

    public void RemoveState(StateData state)
    {
        if (state != null)
        {
            states.Remove(state);
            RecalculateSize();
        }
    }

    public StateData GetStateById(string stateId)
    {
        return states.Find(s => s.Id == stateId);
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

    public void RecalculateSize()
    {
        if (states.Count == 0)
        {
            size = new Vector2(200, 60); // Minimum size for empty node
            return;
        }

        // Calculate bounds based on state positions relative to current node position
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var state in states)
        {
            // Calculate relative positions from node position
            var relativeX = state.Position.x - position.x;
            var relativeY = state.Position.y - position.y;

            minX = Mathf.Min(minX, relativeX);
            minY = Mathf.Min(minY, relativeY);
            maxX = Mathf.Max(maxX, relativeX + 100); // Assume state width of 100
            maxY = Mathf.Max(maxY, relativeY + 25);  // Assume state height of 25
        }

        // Calculate size to encompass all states with padding
        const float padding = 20f;
        const float titleHeight = 30f;

        // Ensure minimum bounds
        minX = Mathf.Min(minX, 0);
        minY = Mathf.Min(minY, titleHeight);
        maxX = Mathf.Max(maxX, 200);
        maxY = Mathf.Max(maxY, titleHeight + 30);

        size = new Vector2(
            Mathf.Max(200, maxX - minX + padding * 2),
            Mathf.Max(60, maxY - minY + padding * 2)
        );

        // Adjust position if states extend beyond the current node bounds
        if (minX < 0)
        {
            position.x += minX - padding;
        }
        if (minY < titleHeight)
        {
            position.y += minY - titleHeight - padding;
        }
    }
}
