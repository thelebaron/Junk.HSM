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
                if (float.IsNaN(node.Position.x)       || float.IsNaN(node.Position.y) ||
                    Mathf.Abs(node.Position.x) > 10000 || Mathf.Abs(node.Position.y) > 10000)
                {
                    UnityEngine.Debug.LogWarning($"Fixed corrupted position for node: {node.Name}");
                    node.Position = new Vector2(100 + (i % 3) * 300, 100 + (i / 3) * 200);
                }

                if (float.IsNaN(node.Size.x) || float.IsNaN(node.Size.y) ||
                    node.Size.x <= 0         || node.Size.y <= 0)
                {
                    UnityEngine.Debug.LogWarning($"Fixed corrupted size for node: {node.Name}");
                    node.Size = new Vector2(200, 100);
                }
            }

            // Fix corrupted state positions (including extremely large values)
            int stateIndex = 0;
            foreach (var node in GraphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    if (float.IsNaN(state.Position.x)       || float.IsNaN(state.Position.y) ||
                        Mathf.Abs(state.Position.x) > 10000 || Mathf.Abs(state.Position.y) > 10000)
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