using UnityEditor;
using UnityEngine;

namespace Junk.Yard.Editor
{
    public static class GraphTestDataCreator
    {
        [MenuItem("Tools/Create Test Graph Data")]
        public static void CreateTestGraphData()
        {
            // Create the graph data asset
            var graphData = ScriptableObject.CreateInstance<GraphData>();

            // Create freeze node
            var freezeNode = new NodeData("freeze", new Vector2(50, 50));

            // Create fire node
            var fireNode = new NodeData("fire", new Vector2(350, 50));

            // Add nodes to graph
            graphData.AddNode(freezeNode);
            graphData.AddNode(fireNode);

            // Create states for freeze node (positioned relative to node)
            var freezeInto   = new StateData("freeze : into", new Vector2(70, 90), freezeNode.Id);
            var freezeLoop   = new StateData("freeze : loop", new Vector2(70, 120), freezeNode.Id);
            var freezeToIdle = new StateData("freeze : to_idle", new Vector2(170, 120), freezeNode.Id);

            // Create states for fire node (positioned relative to node)
            var fireFrom = new StateData("fire : from", new Vector2(370, 90), fireNode.Id);
            var fireLoop = new StateData("fire : loop", new Vector2(370, 120), fireNode.Id);
            var fireExit = new StateData("fire : exit", new Vector2(470, 120), fireNode.Id);

            // Add states to graph
            graphData.AddState(freezeInto);
            graphData.AddState(freezeLoop);
            graphData.AddState(freezeToIdle);
            graphData.AddState(fireFrom);
            graphData.AddState(fireLoop);
            graphData.AddState(fireExit);

            // Create connections as shown in the example image
            // Connection from freeze:loop_freeze to freeze:to_idle
            var connection1 = new ConnectionData(
                freezeLoop.Id,
                freezeToIdle.Id,
                freezeNode.Id,
                freezeNode.Id,
                false
            );

            // Connection from freeze:to_idle to fire:from (cross-node connection)
            var connection2 = new ConnectionData(
                freezeToIdle.Id,
                fireFrom.Id,
                freezeNode.Id,
                fireNode.Id,
                false
            );

            // Add connections to the source states
            freezeLoop.AddConnection(connection1);
            freezeToIdle.AddConnection(connection2);

            // Save the asset
            var path = "Assets/TestGraph.asset";
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();

            // Select the created asset
            Selection.activeObject = graphData;
            EditorGUIUtility.PingObject(graphData);

            Debug.Log($"Test graph data created at: {path}");
        }
    }
}
