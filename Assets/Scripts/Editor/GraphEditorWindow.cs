using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphEditorWindow : EditorWindow
{
    private const string SESSION_STATE_KEY = "GraphEditor_CurrentGraph";

    private GraphData currentGraph;
    private GraphView graphView;
    private ObjectField graphField;
    private VisualElement inspectorPanel;
    private StateData selectedState;
    private NodeData selectedNode;
    private DropdownField nodeDropdown;
    private TextField stateNameField;
    private TextField nodeNameField;
    private VisualElement stateInspector;
    private VisualElement nodeInspector;

    [MenuItem("Window/Graph Editor")]
    public static GraphEditorWindow ShowWindow()
    {
        var window = GetWindow<GraphEditorWindow>();
        window.titleContent = new GUIContent("Graph Editor");
        window.Show();
        return window;
    }

    public void LoadGraph(GraphData graph)
    {
        currentGraph = graph;
        SaveCurrentGraphReference();
        if (graphField != null)
        {
            graphField.value = graph;
        }
        if (graphView != null)
        {
            graphView.LoadGraph(graph);
        }
    }

    public void CreateGUI()
    {
        // Create the root visual element
        var root = rootVisualElement;

        // Create toolbar
        var toolbar = new Toolbar();

        // Graph selection field
        graphField = new ObjectField("Graph Asset")
        {
            objectType = typeof(GraphData),
            allowSceneObjects = false
        };
        graphField.RegisterValueChangedCallback(OnGraphChanged);
        toolbar.Add(graphField);

        // New graph button
        var newGraphButton = new Button(CreateNewGraph) { text = "New Graph" };
        toolbar.Add(newGraphButton);

        // Save button
        var saveButton = new Button(SaveGraph) { text = "Save" };
        toolbar.Add(saveButton);

        // Frame All button
        var frameAllButton = new Button(FrameAll) { text = "Frame All" };
        toolbar.Add(frameAllButton);

        root.Add(toolbar);

        // Create main content area with horizontal layout
        var mainContent = new VisualElement();
        mainContent.style.flexDirection = FlexDirection.Row;
        mainContent.style.flexGrow = 1;

        // Create graph view
        graphView = new GraphView();
        graphView.style.flexGrow = 1;
        graphView.OnStateSelected += OnStateSelected;
        graphView.OnNodeSelected += OnNodeSelected;
        mainContent.Add(graphView);

        // Create inspector panel
        CreateInspectorPanel();
        mainContent.Add(inspectorPanel);

        root.Add(mainContent);

        // Load styles
        var styleSheet = Resources.Load<StyleSheet>("GraphEditor");
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }

        // Restore previously loaded graph after domain reload
        RestoreCurrentGraph();
    }

    private void OnGraphChanged(ChangeEvent<Object> evt)
    {
        currentGraph = evt.newValue as GraphData;
        SaveCurrentGraphReference();
        if (graphView != null)
        {
            graphView.LoadGraph(currentGraph);
        }
    }

    private void CreateNewGraph()
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "Create New Graph",
            "NewGraph",
            "asset",
            "Please enter a file name for the new graph");
            
        if (!string.IsNullOrEmpty(path))
        {
            var newGraph = CreateInstance<GraphData>();
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            
            graphField.value = newGraph;
        }
    }

    private void SaveGraph()
    {
        if (currentGraph != null)
        {
            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log("Graph saved successfully!");
        }
    }

    private void CreateInspectorPanel()
    {
        inspectorPanel = new VisualElement();
        inspectorPanel.AddToClassList("inspector-panel");
        inspectorPanel.style.width = 250;
        inspectorPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        inspectorPanel.style.borderLeftWidth = 1;
        inspectorPanel.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        inspectorPanel.style.paddingTop = 10;
        inspectorPanel.style.paddingBottom = 10;
        inspectorPanel.style.paddingLeft = 10;
        inspectorPanel.style.paddingRight = 10;

        // Inspector title
        var title = new Label("Inspector");
        title.style.fontSize = 16;
        title.style.color = Color.white;
        title.style.marginBottom = 10;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        inspectorPanel.Add(title);

        // Create state inspector section
        stateInspector = new VisualElement();
        stateInspector.AddToClassList("inspector-section");

        var stateTitle = new Label("State Properties");
        stateTitle.style.fontSize = 14;
        stateTitle.style.color = Color.white;
        stateTitle.style.marginBottom = 5;
        stateTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        stateInspector.Add(stateTitle);

        stateNameField = new TextField("State Name");
        stateNameField.style.marginBottom = 10;
        stateNameField.RegisterValueChangedCallback(OnStateNameChanged);
        stateInspector.Add(stateNameField);

        nodeDropdown = new DropdownField("Parent Node");
        nodeDropdown.style.marginBottom = 10;
        nodeDropdown.RegisterValueChangedCallback(OnNodeSelectionChanged);
        stateInspector.Add(nodeDropdown);

        // Connection management section
        var connectionsLabel = new Label("Connections");
        connectionsLabel.style.fontSize = 12;
        connectionsLabel.style.color = Color.white;
        connectionsLabel.style.marginTop = 10;
        connectionsLabel.style.marginBottom = 5;
        connectionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        stateInspector.Add(connectionsLabel);

        var connectButton = new Button(() => ShowConnectionDropdownInInspector()) { text = "Add Connection" };
        connectButton.style.marginBottom = 5;
        stateInspector.Add(connectButton);

        inspectorPanel.Add(stateInspector);

        // Create node inspector section
        nodeInspector = new VisualElement();
        nodeInspector.AddToClassList("inspector-section");

        var nodeTitle = new Label("Node Properties");
        nodeTitle.style.fontSize = 14;
        nodeTitle.style.color = Color.white;
        nodeTitle.style.marginBottom = 5;
        nodeTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeInspector.Add(nodeTitle);

        nodeNameField = new TextField("Node Name");
        nodeNameField.style.marginBottom = 10;
        nodeNameField.RegisterValueChangedCallback(OnNodeNameChanged);
        nodeInspector.Add(nodeNameField);

        inspectorPanel.Add(nodeInspector);

        // Initially hide the inspector
        SetInspectorVisible(false);
    }

    private void SetInspectorVisible(bool visible)
    {
        inspectorPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnStateSelected(StateData state)
    {
        selectedState = state;
        selectedNode = null; // Clear node selection

        if (state != null)
        {
            SetInspectorVisible(true);
            UpdateInspectorContent();
        }
        else
        {
            SetInspectorVisible(false);
        }
    }

    private void OnNodeSelected(NodeData node)
    {
        selectedNode = node;
        selectedState = null; // Clear state selection

        if (node != null)
        {
            SetInspectorVisible(true);
            UpdateInspectorContent();
        }
        else
        {
            SetInspectorVisible(false);
        }
    }

    private void UpdateInspectorContent()
    {
        if (currentGraph == null) return;

        // Show/hide appropriate inspector sections
        stateInspector.style.display = selectedState != null ? DisplayStyle.Flex : DisplayStyle.None;
        nodeInspector.style.display = selectedNode != null ? DisplayStyle.Flex : DisplayStyle.None;

        if (selectedState != null)
        {
            UpdateStateInspector();
        }

        if (selectedNode != null)
        {
            UpdateNodeInspector();
        }
    }

    private void UpdateStateInspector()
    {
        if (selectedState == null || currentGraph == null) return;

        // Update state name field
        stateNameField.SetValueWithoutNotify(selectedState.Name);

        // Update node dropdown with unique identifiers
        var nodeChoices = new List<string> { "None" };
        var nodeIdToDisplayName = new Dictionary<string, string>();

        foreach (var node in currentGraph.Nodes)
        {
            var displayName = node.Name;
            var counter = 1;
            var originalName = displayName;

            // Handle duplicate names by adding numbers
            while (nodeIdToDisplayName.ContainsValue(displayName))
            {
                displayName = $"{originalName} ({counter})";
                counter++;
            }

            nodeChoices.Add(displayName);
            nodeIdToDisplayName[node.Id] = displayName;
        }

        nodeDropdown.choices = nodeChoices;

        // Set current selection
        if (string.IsNullOrEmpty(selectedState.ParentNodeId))
        {
            nodeDropdown.SetValueWithoutNotify("None");
        }
        else
        {
            if (nodeIdToDisplayName.TryGetValue(selectedState.ParentNodeId, out var displayName))
            {
                nodeDropdown.SetValueWithoutNotify(displayName);
            }
            else
            {
                nodeDropdown.SetValueWithoutNotify("None");
            }
        }

        // Update connections display
        UpdateConnectionsDisplay();
    }

    private void UpdateConnectionsDisplay()
    {
        if (selectedState == null) return;

        // Remove existing connection displays
        var existingConnections = stateInspector.Query<Button>().Where(b => b.name == "connection-button").ToList();
        foreach (var button in existingConnections)
        {
            button.RemoveFromHierarchy();
        }

        // Add current connections
        foreach (var connection in selectedState.OutgoingConnections)
        {
            var targetState = currentGraph?.GetStateById(connection.TargetStateId);
            if (targetState != null)
            {
                var connectionButton = new Button(() => RemoveConnectionFromInspector(connection))
                {
                    text = $"→ {targetState.Name} (Remove)",
                    name = "connection-button"
                };
                connectionButton.style.marginBottom = 2;
                connectionButton.style.backgroundColor = new Color(0.6f, 0.3f, 0.3f, 1f);
                stateInspector.Add(connectionButton);
            }
        }
    }

    private void RemoveConnectionFromInspector(ConnectionData connection)
    {
        if (selectedState == null || connection == null) return;

        selectedState.RemoveConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);
        UpdateConnectionsDisplay();

        var targetState = currentGraph?.GetStateById(connection.TargetStateId);
        Debug.Log($"Removed connection from {selectedState.Name} to {targetState?.Name ?? "Unknown"}");
    }

    private void UpdateNodeInspector()
    {
        if (selectedNode == null) return;

        // Update node name field
        nodeNameField.SetValueWithoutNotify(selectedNode.Name);
    }

    private void OnStateNameChanged(ChangeEvent<string> evt)
    {
        if (selectedState != null)
        {
            selectedState.Name = evt.newValue;

            // Update the visual representation
            var stateView = graphView.GetStateView(selectedState.Id);
            if (stateView != null)
            {
                stateView.UpdateLabel();
            }

            // Mark graph as dirty
            if (currentGraph != null)
            {
                EditorUtility.SetDirty(currentGraph);
            }
        }
    }

    private void OnNodeNameChanged(ChangeEvent<string> evt)
    {
        if (selectedNode != null)
        {
            selectedNode.Name = evt.newValue;

            // Update the visual representation
            var nodeView = graphView.GetNodeView(selectedNode.Id);
            if (nodeView != null)
            {
                nodeView.UpdateLabel();
            }

            // Update any state inspector dropdowns that might be showing
            if (selectedState != null)
            {
                UpdateStateInspector();
            }

            // Mark graph as dirty
            if (currentGraph != null)
            {
                EditorUtility.SetDirty(currentGraph);
            }
        }
    }

    private void OnNodeSelectionChanged(ChangeEvent<string> evt)
    {
        if (selectedState == null || currentGraph == null) return;

        var selectedDisplayName = evt.newValue;

        // Remove from previous parent node
        if (!string.IsNullOrEmpty(selectedState.ParentNodeId))
        {
            var oldParentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
            if (oldParentNode != null)
            {
                oldParentNode.RemoveState(selectedState);

                var oldNodeView = graphView.GetNodeView(oldParentNode.Id);
                if (oldNodeView != null)
                {
                    oldNodeView.UpdateSize();
                }
            }
        }

        // Assign to new parent node
        if (selectedDisplayName == "None")
        {
            selectedState.ParentNodeId = string.Empty;
        }
        else
        {
            // Find node by display name (handling duplicates)
            NodeData newParentNode = null;
            var baseName = selectedDisplayName;

            // Remove the counter suffix if present
            var parenIndex = selectedDisplayName.LastIndexOf(" (");
            if (parenIndex > 0)
            {
                baseName = selectedDisplayName.Substring(0, parenIndex);
            }

            // Find the node with matching base name
            var candidateNodes = currentGraph.Nodes.FindAll(n => n.Name == baseName);
            if (candidateNodes.Count == 1)
            {
                newParentNode = candidateNodes[0];
            }
            else if (candidateNodes.Count > 1)
            {
                // For duplicate names, we need to match the exact display name
                // This is a simplified approach - in practice you might want to store the mapping
                var index = 0;
                if (parenIndex > 0)
                {
                    var counterStr = selectedDisplayName.Substring(parenIndex + 2, selectedDisplayName.Length - parenIndex - 3);
                    if (int.TryParse(counterStr, out var counter))
                    {
                        index = counter - 1;
                    }
                }

                if (index < candidateNodes.Count)
                {
                    newParentNode = candidateNodes[index];
                }
            }

            if (newParentNode != null)
            {
                selectedState.ParentNodeId = newParentNode.Id;
                newParentNode.AddState(selectedState);

                // Position state in front of the node
                var nodeCenter = new Vector2(
                    newParentNode.Position.x + newParentNode.Size.x * 0.5f,
                    newParentNode.Position.y + newParentNode.Size.y * 0.5f
                );
                selectedState.Position = nodeCenter;

                var stateView = graphView.GetStateView(selectedState.Id);
                if (stateView != null)
                {
                    stateView.UpdatePosition();
                }

                var newNodeView = graphView.GetNodeView(newParentNode.Id);
                if (newNodeView != null)
                {
                    newNodeView.UpdateSize();
                }
            }
        }

        // Update connections after reassignment
        graphView.RefreshConnections();

        // Mark graph as dirty
        EditorUtility.SetDirty(currentGraph);
    }

    private void ShowConnectionDropdownInInspector()
    {
        if (selectedState == null || currentGraph == null) return;

        // Create dropdown choices (same logic as in StateView)
        var choices = new List<string>();
        var stateIdMap = new Dictionary<string, StateData>();

        // Add states from the same node first
        if (!string.IsNullOrEmpty(selectedState.ParentNodeId))
        {
            var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
            if (parentNode != null)
            {
                choices.Add($"--- {parentNode.Name} (Same Node) ---");
                foreach (var state in parentNode.States)
                {
                    if (state.Id != selectedState.Id)
                    {
                        var displayName = $"  {state.Name}";
                        choices.Add(displayName);
                        stateIdMap[displayName] = state;
                    }
                }
            }
        }

        // Add separator
        if (choices.Count > 0)
        {
            choices.Add("--- Other Nodes ---");
        }

        // Add states from other nodes, grouped by node
        foreach (var node in currentGraph.Nodes)
        {
            if (node.Id == selectedState.ParentNodeId) continue;

            if (node.States.Count > 0)
            {
                choices.Add($"--- {node.Name} ---");
                foreach (var state in node.States)
                {
                    var displayName = $"  {state.Name}";
                    choices.Add(displayName);
                    stateIdMap[displayName] = state;
                }
            }
        }

        // Add unassigned states
        var unassignedStates = currentGraph.States.FindAll(s => string.IsNullOrEmpty(s.ParentNodeId) && s.Id != selectedState.Id);
        if (unassignedStates.Count > 0)
        {
            choices.Add("--- Unassigned States ---");
            foreach (var state in unassignedStates)
            {
                var displayName = $"  {state.Name}";
                choices.Add(displayName);
                stateIdMap[displayName] = state;
            }
        }

        if (choices.Count == 0)
        {
            Debug.Log("No available states to connect to");
            return;
        }

        // Create temporary dropdown
        var dropdown = new DropdownField("Select target:", choices, 0);
        stateInspector.Add(dropdown);

        dropdown.RegisterValueChangedCallback(evt =>
        {
            var selectedChoice = evt.newValue;
            if (stateIdMap.TryGetValue(selectedChoice, out var targetState))
            {
                CreateConnectionFromInspector(targetState);
            }
            dropdown.RemoveFromHierarchy();
            UpdateInspectorContent(); // Refresh to show new connection
        });
    }

    private void CreateConnectionFromInspector(StateData targetState)
    {
        if (selectedState == null || targetState == null) return;

        // Check if connection already exists
        foreach (var existingConnection in selectedState.OutgoingConnections)
        {
            if (existingConnection.TargetStateId == targetState.Id)
            {
                Debug.Log($"Connection already exists from {selectedState.Name} to {targetState.Name}");
                return;
            }
        }

        // Create new connection
        var connection = new ConnectionData(
            selectedState.Id,
            targetState.Id,
            selectedState.ParentNodeId,
            targetState.ParentNodeId,
            false
        );

        selectedState.AddConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);

        Debug.Log($"Created connection from {selectedState.Name} to {targetState.Name}");
    }

    private void FrameAll()
    {
        if (graphView != null)
        {
            graphView.FrameAll();
        }
    }

    private void SaveCurrentGraphReference()
    {
        if (currentGraph != null)
        {
            var assetPath = AssetDatabase.GetAssetPath(currentGraph);
            SessionState.SetString(SESSION_STATE_KEY, assetPath);
        }
        else
        {
            SessionState.EraseString(SESSION_STATE_KEY);
        }
    }

    private void RestoreCurrentGraph()
    {
        var assetPath = SessionState.GetString(SESSION_STATE_KEY, "");
        if (!string.IsNullOrEmpty(assetPath))
        {
            var graph = AssetDatabase.LoadAssetAtPath<GraphData>(assetPath);
            if (graph != null)
            {
                currentGraph = graph;
                if (graphField != null)
                {
                    graphField.SetValueWithoutNotify(graph);
                }
                if (graphView != null)
                {
                    graphView.LoadGraph(graph);
                }
            }
        }
    }
}
