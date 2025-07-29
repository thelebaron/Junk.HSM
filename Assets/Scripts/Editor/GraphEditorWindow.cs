using System.Collections.Generic;
using System.Linq;
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
    private Label inspectorTitle;
    private bool isConnectionDropdownOpen = false;

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
        inspectorTitle = new Label("Inspector");
        inspectorTitle.style.fontSize = 16;
        inspectorTitle.style.color = Color.white;
        inspectorTitle.style.marginBottom = 10;
        inspectorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        inspectorPanel.Add(inspectorTitle);

        // Create state inspector section
        stateInspector = new VisualElement();
        stateInspector.AddToClassList("inspector-section");

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

        nodeNameField = new TextField("Node Name");
        nodeNameField.style.marginBottom = 10;
        nodeNameField.RegisterValueChangedCallback(OnNodeNameChanged);
        nodeInspector.Add(nodeNameField);

        // Node connection management section
        var nodeConnectionsLabel = new Label("Connections");
        nodeConnectionsLabel.style.fontSize = 12;
        nodeConnectionsLabel.style.color = Color.white;
        nodeConnectionsLabel.style.marginTop = 10;
        nodeConnectionsLabel.style.marginBottom = 5;
        nodeConnectionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeInspector.Add(nodeConnectionsLabel);

        var nodeConnectButton = new Button(() => ShowNodeConnectionDropdownInInspector()) { text = "Add Connection" };
        nodeConnectButton.style.marginBottom = 5;
        nodeInspector.Add(nodeConnectButton);

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
        isConnectionDropdownOpen = false; // Reset dropdown state

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
        isConnectionDropdownOpen = false; // Reset dropdown state

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

        // Update inspector title based on selection
        if (selectedState != null)
        {
            var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
            var nodeName = parentNode?.Name ?? "Unknown";
            inspectorTitle.text = $"{nodeName} : {selectedState.Name}";
        }
        else if (selectedNode != null)
        {
            inspectorTitle.text = selectedNode.Name;
        }
        else
        {
            inspectorTitle.text = "Inspector";
        }

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

        // Remove existing connection displays and any open dropdown
        var existingConnections = stateInspector.Query<Button>().Where(b => b.name == "connection-button").ToList();
        foreach (var button in existingConnections)
        {
            button.RemoveFromHierarchy();
        }

        var existingDropdown = stateInspector.Q<DropdownField>("connection-dropdown");
        if (existingDropdown != null)
        {
            existingDropdown.RemoveFromHierarchy();
            isConnectionDropdownOpen = false;
        }

        // Add current connections
        foreach (var connection in selectedState.OutgoingConnections)
        {
            string targetName = "Unknown";
            if (connection.IsStateToState())
            {
                var targetState = currentGraph?.GetStateById(connection.TargetStateId);
                targetName = targetState?.Name ?? "Unknown State";
            }
            else if (connection.IsStateToNode())
            {
                var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
                targetName = targetNode?.Name ?? "Unknown Node";
            }

            var connectionButton = new Button(() => RemoveConnectionFromInspector(connection))
            {
                text = $"→ {targetName} (Remove)",
                name = "connection-button"
            };
            connectionButton.style.marginBottom = 2;
            connectionButton.style.backgroundColor = new Color(0.6f, 0.3f, 0.3f, 1f);
            stateInspector.Add(connectionButton);
        }
    }

    private void RemoveConnectionFromInspector(ConnectionData connection)
    {
        if (selectedState == null || connection == null) return;

        selectedState.RemoveConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);
        UpdateConnectionsDisplay();

        string targetName = "Unknown";
        if (connection.IsStateToState())
        {
            var targetState = currentGraph?.GetStateById(connection.TargetStateId);
            targetName = targetState?.Name ?? "Unknown State";
        }
        else if (connection.IsStateToNode())
        {
            var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
            targetName = targetNode?.Name ?? "Unknown Node";
        }

        Debug.Log($"Removed connection from {selectedState.Name} to {targetName}");
    }

    private void UpdateNodeInspector()
    {
        if (selectedNode == null) return;

        // Update node name field
        nodeNameField.SetValueWithoutNotify(selectedNode.Name);

        // Update node connections display
        UpdateNodeConnectionsDisplay();
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

            // Update inspector title
            var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
            var nodeName = parentNode?.Name ?? "Unknown";
            inspectorTitle.text = $"{nodeName} : {selectedState.Name}";

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

            // Update inspector title
            inspectorTitle.text = selectedNode.Name;

            // Update any state inspector dropdowns that might be showing
            if (selectedState != null)
            {
                UpdateStateInspector();
            }

            // Update labels for all states that belong to this node
            if (currentGraph != null)
            {
                var nodeStates = currentGraph.GetStatesForNode(selectedNode.Id);
                foreach (var state in nodeStates)
                {
                    var stateView = graphView.GetStateView(state.Id);
                    if (stateView != null)
                    {
                        stateView.UpdateLabel();
                    }
                }
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
                oldParentNode.RemoveState(selectedState, currentGraph);

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

            // Update the state label to remove node prefix
            var stateView = graphView.GetStateView(selectedState.Id);
            if (stateView != null)
            {
                stateView.UpdateLabel();
            }
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
                newParentNode.AddState(selectedState, currentGraph);

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
                    stateView.UpdateLabel(); // Update label to show new node prefix
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

        // Update inspector title to reflect new parent node
        var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
        var nodeName = parentNode?.Name ?? "Unknown";
        inspectorTitle.text = $"{nodeName} : {selectedState.Name}";

        // Mark graph as dirty
        EditorUtility.SetDirty(currentGraph);
    }

    private void ShowConnectionDropdownInInspector()
    {
        if (selectedState == null || currentGraph == null || isConnectionDropdownOpen) return;

        isConnectionDropdownOpen = true;

        // Create dropdown choices
        var choices = new List<string>();
        var targetMap = new Dictionary<string, object>(); // Can be StateData or NodeData

        // Add nodes first
        choices.Add("--- Nodes ---");
        foreach (var node in currentGraph.Nodes)
        {
            if (node.Id != selectedState.ParentNodeId) // Don't connect to own parent node
            {
                var displayName = $"  [Node] {node.Name}";
                choices.Add(displayName);
                targetMap[displayName] = node;
            }
        }

        // Add states from the same node
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
                        targetMap[displayName] = state;
                    }
                }
            }
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
                    targetMap[displayName] = state;
                }
            }
        }

        // Add unassigned states (states that exist in nodes but have empty ParentNodeId)
        var unassignedStates = new List<StateData>();
        foreach (var node in currentGraph.Nodes)
        {
            foreach (var state in node.States)
            {
                if (string.IsNullOrEmpty(state.ParentNodeId) && state.Id != selectedState.Id)
                {
                    unassignedStates.Add(state);
                }
            }
        }

        if (unassignedStates.Count > 0)
        {
            choices.Add("--- Unassigned States ---");
            foreach (var state in unassignedStates)
            {
                var displayName = $"  {state.Name}";
                choices.Add(displayName);
                targetMap[displayName] = state;
            }
        }

        if (choices.Count <= 1) // Only header, no actual targets
        {
            Debug.Log("No available targets to connect to");
            return;
        }

        // Create temporary dropdown with no pre-selection
        var dropdown = new DropdownField("Select target:", choices, -1);
        dropdown.name = "connection-dropdown"; // Add name for identification
        stateInspector.Add(dropdown);

        dropdown.RegisterValueChangedCallback(evt =>
        {
            var selectedChoice = evt.newValue;

            // Skip empty selections and header items
            if (string.IsNullOrEmpty(selectedChoice) || selectedChoice.StartsWith("---"))
            {
                return;
            }

            if (targetMap.TryGetValue(selectedChoice, out var target))
            {
                if (target is StateData targetState)
                {
                    CreateStateToStateConnectionFromInspector(targetState);
                }
                else if (target is NodeData targetNode)
                {
                    CreateStateToNodeConnectionFromInspector(targetNode);
                }
            }
            dropdown.RemoveFromHierarchy();
            isConnectionDropdownOpen = false;
            UpdateInspectorContent(); // Refresh to show new connection
        });
    }

    private void CreateStateToStateConnectionFromInspector(StateData targetState)
    {
        if (selectedState == null || targetState == null) return;

        // Check if connection already exists
        foreach (var existingConnection in selectedState.OutgoingConnections)
        {
            if (existingConnection.IsStateToState() && existingConnection.TargetStateId == targetState.Id)
            {
                Debug.Log($"State-to-state connection already exists from {selectedState.Name} to {targetState.Name}");
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

        Debug.Log($"Created state-to-state connection from {selectedState.Name} to {targetState.Name}");
    }

    private void CreateStateToNodeConnectionFromInspector(NodeData targetNode)
    {
        if (selectedState == null || targetNode == null) return;

        // Check if connection already exists
        foreach (var existingConnection in selectedState.OutgoingConnections)
        {
            if (existingConnection.IsStateToNode() && existingConnection.TargetNodeId == targetNode.Id)
            {
                Debug.Log($"State-to-node connection already exists from {selectedState.Name} to {targetNode.Name}");
                return;
            }
        }

        // Create new connection
        var connection = new ConnectionData(
            selectedState.Id,
            string.Empty, // No target state
            selectedState.ParentNodeId,
            targetNode.Id,
            false
        );

        selectedState.AddConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);

        Debug.Log($"Created state-to-node connection from {selectedState.Name} to {targetNode.Name}");
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

    private void UpdateNodeConnectionsDisplay()
    {
        if (selectedNode == null) return;

        // Remove existing connection displays
        var existingConnections = nodeInspector.Query<Button>().Where(b => b.name == "node-connection-button").ToList();
        foreach (var button in existingConnections)
        {
            button.RemoveFromHierarchy();
        }

        // Add current connections
        foreach (var connection in selectedNode.OutgoingConnections)
        {
            string targetName = "Unknown";
            if (connection.IsNodeToNode())
            {
                var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
                targetName = targetNode?.Name ?? "Unknown Node";
            }
            else if (connection.IsNodeToState())
            {
                var targetState = currentGraph?.GetStateById(connection.TargetStateId);
                targetName = targetState?.Name ?? "Unknown State";
            }

            var connectionButton = new Button(() => RemoveNodeConnectionFromInspector(connection))
            {
                text = $"→ {targetName} (Remove)",
                name = "node-connection-button"
            };
            connectionButton.style.marginBottom = 2;
            connectionButton.style.backgroundColor = new Color(0.6f, 0.3f, 0.3f, 1f);
            nodeInspector.Add(connectionButton);
        }
    }

    private void RemoveNodeConnectionFromInspector(ConnectionData connection)
    {
        if (selectedNode == null || connection == null) return;

        selectedNode.RemoveConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);
        UpdateNodeConnectionsDisplay();

        string targetName = "Unknown";
        if (connection.IsNodeToNode())
        {
            var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
            targetName = targetNode?.Name ?? "Unknown Node";
        }
        else if (connection.IsNodeToState())
        {
            var targetState = currentGraph?.GetStateById(connection.TargetStateId);
            targetName = targetState?.Name ?? "Unknown State";
        }

        Debug.Log($"Removed connection from {selectedNode.Name} to {targetName}");
    }

    private void ShowNodeConnectionDropdownInInspector()
    {
        if (selectedNode == null || currentGraph == null) return;

        var choices = new List<string>();
        var targetMap = new Dictionary<string, object>(); // Can be NodeData or StateData

        // Add other nodes section
        var otherNodes = currentGraph.Nodes.Where(n => n.Id != selectedNode.Id).ToList();
        if (otherNodes.Count > 0)
        {
            choices.Add("--- Other Nodes ---");
            foreach (var node in otherNodes)
            {
                var displayName = $"  {node.Name}";
                choices.Add(displayName);
                targetMap[displayName] = node;
            }
        }

        // Add states grouped by their parent nodes
        var nodeGroups = currentGraph.Nodes
            .Where(n => n.States.Count > 0)
            .ToList();

        foreach (var node in nodeGroups)
        {
            choices.Add($"--- {node.Name} ---");
            foreach (var state in node.States)
            {
                var displayName = $"  {state.Name}";
                choices.Add(displayName);
                targetMap[displayName] = state;
            }
        }

        // Add unassigned states (states that exist in nodes but have empty ParentNodeId)
        var unassignedStates = new List<StateData>();
        foreach (var node in currentGraph.Nodes)
        {
            foreach (var state in node.States)
            {
                if (string.IsNullOrEmpty(state.ParentNodeId))
                {
                    unassignedStates.Add(state);
                }
            }
        }

        if (unassignedStates.Count > 0)
        {
            choices.Add("--- Unassigned States ---");
            foreach (var state in unassignedStates)
            {
                var displayName = $"  {state.Name}";
                choices.Add(displayName);
                targetMap[displayName] = state;
            }
        }

        if (choices.Count == 0 || targetMap.Count == 0)
        {
            Debug.Log("No available targets to connect to");
            return;
        }

        // Create dropdown with no pre-selection
        var dropdown = new DropdownField("Connect to:", choices, -1);
        dropdown.style.marginBottom = 5;
        nodeInspector.Add(dropdown);

        // Handle selection
        dropdown.RegisterValueChangedCallback(evt =>
        {
            var selectedChoice = evt.newValue;

            // Skip empty selections and header items
            if (string.IsNullOrEmpty(selectedChoice) || selectedChoice.StartsWith("---"))
            {
                return;
            }

            if (targetMap.TryGetValue(selectedChoice, out var target))
            {
                if (target is NodeData targetNode)
                {
                    CreateNodeToNodeConnectionFromInspector(targetNode);
                }
                else if (target is StateData targetState)
                {
                    CreateNodeToStateConnectionFromInspector(targetState);
                }
            }
            dropdown.RemoveFromHierarchy();
        });
    }

    private void CreateNodeToNodeConnectionFromInspector(NodeData targetNode)
    {
        if (selectedNode == null || targetNode == null) return;

        // Check if connection already exists
        foreach (var existingConnection in selectedNode.OutgoingConnections)
        {
            if (existingConnection.IsNodeToNode() && existingConnection.TargetNodeId == targetNode.Id)
            {
                Debug.Log($"Node-to-node connection already exists from {selectedNode.Name} to {targetNode.Name}");
                return;
            }
        }

        // Create new connection
        var connection = new ConnectionData(
            string.Empty, // No source state
            string.Empty, // No target state
            selectedNode.Id,
            targetNode.Id,
            true // Node to node connection
        );

        selectedNode.AddConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);

        Debug.Log($"Created node-to-node connection from {selectedNode.Name} to {targetNode.Name}");
    }

    private void CreateNodeToStateConnectionFromInspector(StateData targetState)
    {
        if (selectedNode == null || targetState == null) return;

        // Check if connection already exists
        foreach (var existingConnection in selectedNode.OutgoingConnections)
        {
            if (existingConnection.IsNodeToState() && existingConnection.TargetStateId == targetState.Id)
            {
                Debug.Log($"Node-to-state connection already exists from {selectedNode.Name} to {targetState.Name}");
                return;
            }
        }

        // Create new connection
        var connection = new ConnectionData(
            string.Empty, // No source state
            targetState.Id,
            selectedNode.Id,
            targetState.ParentNodeId,
            false // Not a node-to-node connection
        );

        selectedNode.AddConnection(connection);
        graphView.RefreshConnections();
        EditorUtility.SetDirty(currentGraph);

        Debug.Log($"Created node-to-state connection from {selectedNode.Name} to {targetState.Name}");
    }
}
