using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class GraphDataAssetHandler
{
    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var asset = EditorUtility.InstanceIDToObject(instanceID);
        
        if (asset is GraphData graphData)
        {
            OpenGraphEditor(graphData);
            return true;
        }
        
        return false;
    }
    
    private static void OpenGraphEditor(GraphData graphData)
    {
        // Check if Graph Editor window is already open
        var existingWindow = EditorWindow.GetWindow<GraphEditorWindow>(false, null, false);
        
        if (existingWindow != null)
        {
            // Window exists, just load the graph and focus
            existingWindow.LoadGraph(graphData);
            existingWindow.Focus();
        }
        else
        {
            // Open new window and load the graph
            var window = GraphEditorWindow.ShowWindow();
            window.LoadGraph(graphData);
        }
    }
}
