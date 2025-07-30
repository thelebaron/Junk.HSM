using UnityEngine;

namespace Junk.Web.Editor
{
    public partial class GraphView
    {
        private void EnsureValidData()
        {
            if (GraphData == null) return;

            // Fix corrupted node positions (including extremely large values)
            for (int i = 0; i < GraphData.Nodes.Count; i++)
            {
                var node = GraphData.Nodes[i];
                if (float.IsNaN(node.Position.x) || float.IsNaN(node.Position.y) || float.IsInfinity(node.Position.x) || float.IsInfinity(node.Position.y))
                {
                    UnityEngine.Debug.LogWarning($"Fixed corrupted position for node: {node.Name}");
                    node.Position = new Vector2(100 + (i % 3) * 300, 100 + (i / 3) * 200);
                }
            }

            // Fix corrupted state positions (including extremely large values)
            int stateIndex = 0;
            foreach (var node in GraphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    if (float.IsNaN(state.Position.x) || float.IsNaN(state.Position.y) || float.IsInfinity(state.Position.x) || float.IsInfinity(state.Position.y))
                    {
                        UnityEngine.Debug.LogWarning($"Fixed corrupted position for state: {state.Name}");
                        // Position states in a grid if they're corrupted
                        state.Position = new Vector2(150 + (stateIndex % 5) * 120, 150 + (stateIndex / 5) * 50);
                    }

                    stateIndex++;
                }
            }
        }
    }
}