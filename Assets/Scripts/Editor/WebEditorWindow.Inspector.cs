using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.UI.Web.Editor
{
    public partial class WebEditorWindow
    {
        private void CreateInspectorPanel()
        {
            inspectorPanel = new VisualElement();
            inspectorPanel.AddToClassList("inspector-panel");
            inspectorPanel.style.width           = 250;
            inspectorPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            inspectorPanel.style.borderLeftWidth = 1;
            inspectorPanel.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            inspectorPanel.style.paddingTop      = 10;
            inspectorPanel.style.paddingBottom   = 10;
            inspectorPanel.style.paddingLeft     = 10;
            inspectorPanel.style.paddingRight    = 10;

            // Inspector title
            inspectorTitle                               = new Label("Inspector");
            inspectorTitle.style.fontSize                = 16;
            inspectorTitle.style.color                   = Color.white;
            inspectorTitle.style.marginBottom            = 10;
            inspectorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            inspectorPanel.Add(inspectorTitle);

            // Create state inspector section
            stateInspector = new VisualElement();
            stateInspector.AddToClassList("inspector-section");

            stateNameField                    = new TextField("State Name");
            stateNameField.style.marginBottom = 10;
            stateNameField.RegisterValueChangedCallback(OnStateNameChanged);
            stateInspector.Add(stateNameField);

            nodeDropdown                    = new DropdownField("Parent Node");
            nodeDropdown.style.marginBottom = 10;
            nodeDropdown.RegisterValueChangedCallback(OnNodeSelectionChanged);
            stateInspector.Add(nodeDropdown);

            // Connection management section with add button
            var connectionsContainer = new VisualElement();
            connectionsContainer.style.flexDirection = FlexDirection.Row;
            connectionsContainer.style.alignItems    = Align.Center;
            connectionsContainer.style.marginTop     = 10;
            connectionsContainer.style.marginBottom  = 5;

            var connectionsLabel = new Label("Connections");
            connectionsLabel.style.fontSize                = 12;
            connectionsLabel.style.color                   = Color.white;
            connectionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            connectionsLabel.style.flexGrow                = 1;

            // Create compact add button aligned with connections label
            var connectButton = new Button(() => ShowConnectionDropdownInInspector()) { text = "+" };
            connectButton.style.width             = 16;
            connectButton.style.height            = 16;
            connectButton.style.fontSize          = 12;
            connectButton.style.backgroundColor   = Color.clear;
            connectButton.style.borderTopWidth    = 0;
            connectButton.style.borderBottomWidth = 0;
            connectButton.style.borderLeftWidth   = 0;
            connectButton.style.borderRightWidth  = 0;
            connectButton.style.color             = new Color(1f, 1f, 1f, 1f);

            // Add hover effect
            connectButton.RegisterCallback<MouseEnterEvent>(evt => { connectButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); });
            connectButton.RegisterCallback<MouseLeaveEvent>(evt => { connectButton.style.backgroundColor = Color.clear; });

            connectionsContainer.Add(connectionsLabel);
            connectionsContainer.Add(connectButton);
            stateInspector.Add(connectionsContainer);

            inspectorPanel.Add(stateInspector);

            // Create node inspector section
            nodeInspector = new VisualElement();
            nodeInspector.AddToClassList("inspector-section");

            // Create container for node name and color button
            var nodeNameContainer = new VisualElement();
            nodeNameContainer.style.flexDirection = FlexDirection.Row;
            nodeNameContainer.style.alignItems    = Align.Center;
            nodeNameContainer.style.marginBottom  = 10;

            nodeNameField                    = new TextField("Node Name");
            nodeNameField.style.flexGrow     = 1;
            nodeNameField.style.marginBottom = 0;
            nodeNameField.style.marginRight  = 5;
            nodeNameField.RegisterValueChangedCallback(OnNodeNameChanged);

            // Create kebab menu button (three dots) for color selection
            var colorMenuButton = new Button(() => ShowColorPicker()) { text = "⋯" };
            colorMenuButton.style.width             = 20;
            colorMenuButton.style.height            = 20;
            colorMenuButton.style.fontSize          = 14;
            colorMenuButton.style.backgroundColor   = Color.clear;
            colorMenuButton.style.borderTopWidth    = 1;
            colorMenuButton.style.borderBottomWidth = 1;
            colorMenuButton.style.borderLeftWidth   = 1;
            colorMenuButton.style.borderRightWidth  = 1;
            colorMenuButton.style.borderTopColor    = new Color(0.4f, 0.4f, 0.4f, 1f);
            colorMenuButton.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colorMenuButton.style.borderLeftColor   = new Color(0.4f, 0.4f, 0.4f, 1f);
            colorMenuButton.style.borderRightColor  = new Color(0.4f, 0.4f, 0.4f, 1f);
            colorMenuButton.style.color             = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Add hover effect
            colorMenuButton.RegisterCallback<MouseEnterEvent>(evt => { colorMenuButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.3f); });
            colorMenuButton.RegisterCallback<MouseLeaveEvent>(evt => { colorMenuButton.style.backgroundColor = Color.clear; });

            nodeNameContainer.Add(nodeNameField);
            nodeNameContainer.Add(colorMenuButton);
            nodeInspector.Add(nodeNameContainer);

            // Node connection management section with add button
            var nodeConnectionsContainer = new VisualElement();
            nodeConnectionsContainer.style.flexDirection = FlexDirection.Row;
            nodeConnectionsContainer.style.alignItems    = Align.Center;
            nodeConnectionsContainer.style.marginTop     = 10;
            nodeConnectionsContainer.style.marginBottom  = 5;

            var nodeConnectionsLabel = new Label("Connections");
            nodeConnectionsLabel.style.fontSize                = 12;
            nodeConnectionsLabel.style.color                   = Color.white;
            nodeConnectionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nodeConnectionsLabel.style.flexGrow                = 1;

            // Create compact add button aligned with connections label
            var nodeConnectButton = new Button(() => ShowNodeConnectionDropdownInInspector()) { text = "+" };
            nodeConnectButton.style.width             = 16;
            nodeConnectButton.style.height            = 16;
            nodeConnectButton.style.fontSize          = 12;
            nodeConnectButton.style.backgroundColor   = Color.clear;
            nodeConnectButton.style.borderTopWidth    = 0;
            nodeConnectButton.style.borderBottomWidth = 0;
            nodeConnectButton.style.borderLeftWidth   = 0;
            nodeConnectButton.style.borderRightWidth  = 0;
            nodeConnectButton.style.color             = new Color(1f, 1f, 1f, 1f);

            // Add hover effect
            nodeConnectButton.RegisterCallback<MouseEnterEvent>(evt => { nodeConnectButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); });
            nodeConnectButton.RegisterCallback<MouseLeaveEvent>(evt => { nodeConnectButton.style.backgroundColor = Color.clear; });

            nodeConnectionsContainer.Add(nodeConnectionsLabel);
            nodeConnectionsContainer.Add(nodeConnectButton);
            nodeInspector.Add(nodeConnectionsContainer);

            inspectorPanel.Add(nodeInspector);

            // Initially hide the inspector
            SetInspectorVisible(false);
        }

        private void SetInspectorVisible(bool visible)
        {
            inspectorPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateInspectorContent()
        {
            if (currentGraph == null) return;

            // Update inspector title based on selection
            if (selectedState != null)
            {
                var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
                var nodeName   = parentNode?.Name ?? "Unknown";
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
            nodeInspector.style.display  = selectedNode  != null ? DisplayStyle.Flex : DisplayStyle.None;

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
            var nodeChoices         = new List<string> { "None" };
            var nodeIdToDisplayName = new Dictionary<string, string>();

            foreach (var node in currentGraph.Nodes)
            {
                var displayName  = node.Name;
                var counter      = 1;
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
                var nodeName   = parentNode?.Name ?? "Unknown";
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

        private void ShowConnectionDropdownInInspector()
        {
            if (selectedState == null || currentGraph == null || isConnectionDropdownOpen) return;

            isConnectionDropdownOpen = true;

            // Create dropdown choices
            var choices   = new List<string>();
            var targetMap = new Dictionary<string, object>(); // Key is choice string, value is StateData or NodeData

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
                            // Use a unique key that includes context to avoid collisions
                            var displayName = $"  {state.Name} (same node)";
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
                        // Use a unique key that includes the parent node name to avoid collisions
                        var displayName = $"  {state.Name} (from {node.Name})";
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
                    var displayName = $"  {state.Name} (unassigned)";
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

            var choices   = new List<string>();
            var targetMap = new Dictionary<string, object>(); // Key is choice string, value is NodeData or StateData

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
                    // Use a unique key that includes the parent node name to avoid collisions
                    var displayName = $"  {state.Name} (from {node.Name})";
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
                    var displayName = $"  {state.Name} (unassigned)";
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

        private void ShowColorPicker()
        {
            if (selectedNode == null) return;

            // Create a contextual menu
            var menu = new GenericMenu();

            // Add "Set Color" submenu with color options
            var colorOptions = new Dictionary<string, Color>
            {
                { "White", new Color(0.55f, 0.55f, 0.55f) },
                { "Red", new Color(0.54f, 0f, 0f) },
                { "Green", new Color(0f, 0.53f, 0f) },
                { "Blue", new Color(0f, 0.01f, 0.55f) },
                { "Yellow", new Color(0.52f, 0.48f, 0.01f) },
                { "Cyan", new Color(0f, 0.53f, 0.53f) },
                { "Magenta", new Color(0.52f, 0f, 0.52f) },
                { "Orange", new Color(1f, 0.5f, 0f, 0.52f) },
                { "Purple", new Color(0.5f, 0f, 1f, 0.52f) },
                { "Pink", new Color(1f, 0.75f, 0.8f, 0.52f) },
                { "Brown", new Color(0.6f, 0.3f, 0.1f, 0.52f) }
            };

            // Add each color as a submenu item under "Set Color"
            foreach (var colorOption in colorOptions)
            {
                var colorName = colorOption.Key;
                var color     = colorOption.Value;

                menu.AddItem(new GUIContent($"Set Color/{colorName}"), false, () => { ApplyColorToNode(color, colorName); });
            }

            // Show the menu at the mouse position
            menu.ShowAsContext();
        }

        private void ApplyColorToNode(Color selectedColor, string colorName)
        {
            if (selectedNode == null) return;

            // Update the node color
            selectedNode.NodeColor = selectedColor;

            // Update visual representation
            var nodeView = graphView.GetNodeView(selectedNode.Id);
            if (nodeView != null)
            {
                nodeView.UpdateColors();
            }

            // Mark graph as dirty
            if (currentGraph != null)
            {
                EditorUtility.SetDirty(currentGraph);
            }

            Debug.Log($"Changed {selectedNode.Name} color to {colorName}");
        }
    }
}