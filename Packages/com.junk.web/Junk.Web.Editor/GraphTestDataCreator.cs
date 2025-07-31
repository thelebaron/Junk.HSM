using UnityEditor;
using UnityEngine;

namespace Junk.Web.Editor
{
    public static class GraphTestDataCreator
    {
        [MenuItem("Tools/Create Test Graph Data")]
        public static void CreateTestGraphData()
        {
            // Create the graph data asset
            var graphData = ScriptableObject.CreateInstance<GraphData>();

            // Create nodes matching the image
            var relaxedNode = new NodeData("relaxed", new Vector2(-167, -496.75f));
            relaxedNode.NodeColor = new Color(0f, 0.01f, 0.55f, 1f); // Blue

            var combatNode = new NodeData("combat", new Vector2(184, -506.375f));
            combatNode.NodeColor = new Color(0.52f, 0.48f, 0.01f, 1f); // Olive/Yellow

            var stunNode = new NodeData("stun", new Vector2(70, -255));
            stunNode.NodeColor = new Color(0f, 0.53f, 0.53f, 1f); // Teal

            var fireNode = new NodeData("fire", new Vector2(552, -442));
            fireNode.NodeColor = new Color(0.55f, 0.55f, 0.55f, 1f); // Gray

            // Add nodes to graph
            graphData.AddNode(relaxedNode);
            graphData.AddNode(combatNode);
            graphData.AddNode(stunNode);
            graphData.AddNode(fireNode);

            // Create states for relaxed node
            var relaxedIdle = new StateData("idle", new Vector2(-125, -446.75f), relaxedNode.Id);
            var relaxedInto = new StateData("into", new Vector2(-147, -355.375f), relaxedNode.Id);
            var relaxedWalk = new StateData("walk", new Vector2(7.5f, -443.6875f), relaxedNode.Id);

            // Create states for combat node
            var combatInto = new StateData("into", new Vector2(204, -448.75f), combatNode.Id);
            var combatIdle = new StateData("idle", new Vector2(336, -456.375f), combatNode.Id);
            var combatRun = new StateData("run", new Vector2(336.5f, -385.375f), combatNode.Id);
            var combatExit = new StateData("exit", new Vector2(308.5f, -364.1875f), combatNode.Id);

            // Create states for stun node
            var stunInto = new StateData("into", new Vector2(90, -178), stunNode.Id);
            var stunLoop = new StateData("loop", new Vector2(251, -148), stunNode.Id);
            var stunExit = new StateData("exit", new Vector2(375, -205), stunNode.Id);

            // Create states for fire node
            var fireInto = new StateData("into", new Vector2(576, -392), fireNode.Id);
            var fireLoop = new StateData("loop", new Vector2(689, -355), fireNode.Id);
            var fireExit = new StateData("exit", new Vector2(572, -275), fireNode.Id);

            // Add all states to graph
            graphData.AddState(relaxedIdle);
            graphData.AddState(relaxedInto);
            graphData.AddState(relaxedWalk);
            graphData.AddState(combatInto);
            graphData.AddState(combatIdle);
            graphData.AddState(combatRun);
            graphData.AddState(combatExit);
            graphData.AddState(stunInto);
            graphData.AddState(stunLoop);
            graphData.AddState(stunExit);
            graphData.AddState(fireInto);
            graphData.AddState(fireLoop);
            graphData.AddState(fireExit);

            // Create connections within relaxed node
            var relaxedIntoToIdle = new ConnectionData(relaxedInto.Id, relaxedIdle.Id, relaxedNode.Id, relaxedNode.Id, false);
            var relaxedIdleToWalk = new ConnectionData(relaxedIdle.Id, relaxedWalk.Id, relaxedNode.Id, relaxedNode.Id, false);

            // Create connections within combat node
            var combatIntoToIdle = new ConnectionData(combatInto.Id, combatIdle.Id, combatNode.Id, combatNode.Id, false);
            var combatIdleToRun = new ConnectionData(combatIdle.Id, combatRun.Id, combatNode.Id, combatNode.Id, false);
            var combatRunToExit = new ConnectionData(combatRun.Id, combatExit.Id, combatNode.Id, combatNode.Id, false);

            // Create connections within stun node
            var stunIntoToLoop = new ConnectionData(stunInto.Id, stunLoop.Id, stunNode.Id, stunNode.Id, false);
            var stunLoopToExit = new ConnectionData(stunLoop.Id, stunExit.Id, stunNode.Id, stunNode.Id, false);

            // Create connections within fire node
            var fireIntoToLoop = new ConnectionData(fireInto.Id, fireLoop.Id, fireNode.Id, fireNode.Id, false);
            var fireLoopToExit = new ConnectionData(fireLoop.Id, fireExit.Id, fireNode.Id, fireNode.Id, false);

            // Create cross-node connections
            var stunExitToCombatInto = new ConnectionData(stunExit.Id, combatInto.Id, stunNode.Id, combatNode.Id, false);
            var fireExitToCombatInto = new ConnectionData(fireExit.Id, combatInto.Id, fireNode.Id, combatNode.Id, false);
            var combatExitToRelaxedIdle = new ConnectionData(combatExit.Id, relaxedIdle.Id, combatNode.Id, relaxedNode.Id, false);

            // Add connections to their source states
            relaxedInto.AddConnection(relaxedIntoToIdle);
            relaxedIdle.AddConnection(relaxedIdleToWalk);
            combatInto.AddConnection(combatIntoToIdle);
            combatIdle.AddConnection(combatIdleToRun);
            combatRun.AddConnection(combatRunToExit);
            stunInto.AddConnection(stunIntoToLoop);
            stunLoop.AddConnection(stunLoopToExit);
            fireInto.AddConnection(fireIntoToLoop);
            fireLoop.AddConnection(fireLoopToExit);
            stunExit.AddConnection(stunExitToCombatInto);
            fireExit.AddConnection(fireExitToCombatInto);
            combatExit.AddConnection(combatExitToRelaxedIdle);

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
