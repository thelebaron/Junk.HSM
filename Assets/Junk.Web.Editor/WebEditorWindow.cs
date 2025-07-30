using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public partial class WebEditorWindow : EditorWindow
    {
        private const string SessionStateKey = "WebEditor_CurrentGraph";

        private GraphData     currentGraph;
        private GraphView     graphView;
        private ObjectField   graphField;
        private VisualElement inspectorPanel;
        private StateData     selectedState;
        private NodeData      selectedNode;
        private DropdownField nodeDropdown;
        private TextField     stateNameField;
        private TextField     nodeNameField;
        private VisualElement stateInspector;
        private VisualElement nodeInspector;
        private Label         inspectorTitle;
        private bool          isConnectionDropdownOpen = false;
        
        private bool          HasValidSelection()     => selectedState != null && currentGraph != null;
        private bool          HasValidNodeSelection() => selectedNode  != null && currentGraph != null;

        [MenuItem("Window/Web Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<WebEditorWindow>();
            window.titleContent = new GUIContent("Web Editor");
            window.Show();
        }
        
        public static WebEditorWindow OpenWindow()
        {
            var window = GetWindow<WebEditorWindow>();
            window.titleContent = new GUIContent("Web Editor");
            window.Show();
            return window;
        }
        
        public void LoadGraph(GraphData graph)
        {
            currentGraph = graph;
            SaveCurrentGraphReference();
            
            if (graphField != null) 
                graphField.value = graph;

            graphView?.LoadGraph(graph);
        }

        public void CreateGUI()
        {
            // Create the root visual element
            var root = rootVisualElement;

            // Register for keydown events
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Create toolbar
            var toolbar = new Toolbar();

            // Graph selection field
            graphField = new ObjectField("Graph Asset")
            {
                objectType        = typeof(GraphData),
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
            mainContent.style.flexGrow      = 1;

            // Create graph view
            graphView                 =  new GraphView();
            graphView.style.flexGrow  =  1;
            graphView.OnStateSelected += OnStateSelected;
            graphView.OnNodeSelected  += OnNodeSelected;
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

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.S && evt.ctrlKey)
            {
                SaveGraph();
                evt.StopPropagation();
            }
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
                Debug.Log("Graph saved.");
            }
        }

        private void OnStateSelected(StateData state)
        {
            selectedState            = state;
            selectedNode             = null;  // Clear node selection
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
            selectedNode             = node;
            selectedState            = null;  // Clear state selection
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

        private void UpdateConnectionsDisplay()
        {
            if (selectedState == null) return;

            // Remove existing connection displays and any open dropdown
            var existingConnections = stateInspector.Query<VisualElement>().Where(e => e.name == "connection-button").ToList();
            foreach (var element in existingConnections)
            {
                element.RemoveFromHierarchy();
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
                string targetName     = "Unknown";
                string connectionType = "";

                if (connection.IsStateToState())
                {
                    var targetState = currentGraph?.GetStateById(connection.TargetStateId);
                    targetName     = targetState?.Name ?? "Unknown State";
                    connectionType = "State";
                }
                else if (connection.IsStateToNode())
                {
                    var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
                    targetName     = targetNode?.Name ?? "Unknown Node";
                    connectionType = "Node";
                }

                // Create container for connection entry
                var connectionContainer = new VisualElement();
                connectionContainer.style.flexDirection = FlexDirection.Row;
                connectionContainer.style.alignItems    = Align.Center;
                connectionContainer.style.marginBottom  = 2;
                connectionContainer.name                = "connection-button";

                // Create indented label
                var connectionLabel = new Label($"    → {connectionType}: {targetName}");
                connectionLabel.style.flexGrow   = 1;
                connectionLabel.style.color      = Color.white;
                connectionLabel.style.fontSize   = 11;
                connectionLabel.style.marginLeft = 5;

                // Create small delete button
                var deleteButton = new Button(() => RemoveConnectionFromInspector(connection))
                {
                    text = "−"
                };
                deleteButton.style.width             = 16;
                deleteButton.style.height            = 16;
                deleteButton.style.fontSize          = 12;
                deleteButton.style.backgroundColor   = Color.clear;
                deleteButton.style.borderTopWidth    = 0;
                deleteButton.style.borderBottomWidth = 0;
                deleteButton.style.borderLeftWidth   = 0;
                deleteButton.style.borderRightWidth  = 0;
                deleteButton.style.color             = new Color(1f, 1f, 1f, 1f);

                // Add hover effect
                deleteButton.RegisterCallback<MouseEnterEvent>(evt => { deleteButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); });
                deleteButton.RegisterCallback<MouseLeaveEvent>(evt => { deleteButton.style.backgroundColor = Color.clear; });

                connectionContainer.Add(connectionLabel);
                connectionContainer.Add(deleteButton);
                stateInspector.Add(connectionContainer);
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
                var      baseName      = selectedDisplayName;

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
                    var newNodeView = graphView.GetNodeView(newParentNode.Id);
                    if (newNodeView != null)
                    {
                        var nodeCenter = new Vector2(
                            newParentNode.Position.x + newNodeView.layout.width * 0.5f,
                            newParentNode.Position.y + newNodeView.layout.height * 0.5f
                        );
                        selectedState.Position = nodeCenter;

                        var stateView = graphView.GetStateView(selectedState.Id);
                        if (stateView != null)
                        {
                            stateView.UpdatePosition();
                            stateView.UpdateLabel(); // Update label to show new node prefix
                        }

                        newNodeView.UpdateSize();
                    }
                }
            }

            // Update connections after reassignment
            graphView.RefreshConnections();

            // Update inspector title to reflect new parent node
            var parentNode = currentGraph.GetNodeById(selectedState.ParentNodeId);
            var nodeName   = parentNode?.Name ?? "Unknown";
            inspectorTitle.text = $"{nodeName} : {selectedState.Name}";

            // Mark graph as dirty
            EditorUtility.SetDirty(currentGraph);
        }

        private void FrameAll()
        {
            graphView?.FrameAll();
        }

        private void SaveCurrentGraphReference()
        {
            if (currentGraph != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(currentGraph);
                SessionState.SetString(SessionStateKey, assetPath);
            }
            else
            {
                SessionState.EraseString(SessionStateKey);
            }
        }

        private void RestoreCurrentGraph()
        {
            var assetPath = SessionState.GetString(SessionStateKey, "");
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
            var existingConnections = nodeInspector.Query<VisualElement>().Where(e => e.name == "node-connection-button").ToList();
            foreach (var element in existingConnections)
            {
                element.RemoveFromHierarchy();
            }

            // Add current connections
            foreach (var connection in selectedNode.OutgoingConnections)
            {
                string targetName     = "Unknown";
                string connectionType = "";

                if (connection.IsNodeToNode())
                {
                    var targetNode = currentGraph?.GetNodeById(connection.TargetNodeId);
                    targetName     = targetNode?.Name ?? "Unknown Node";
                    connectionType = "Node";
                }
                else if (connection.IsNodeToState())
                {
                    var targetState = currentGraph?.GetStateById(connection.TargetStateId);
                    targetName     = targetState?.Name ?? "Unknown State";
                    connectionType = "State";
                }

                // Create container for connection entry
                var connectionContainer = new VisualElement();
                connectionContainer.style.flexDirection = FlexDirection.Row;
                connectionContainer.style.alignItems    = Align.Center;
                connectionContainer.style.marginBottom  = 2;
                connectionContainer.name                = "node-connection-button";

                // Create indented label
                var connectionLabel = new Label($"    → {connectionType}: {targetName}");
                connectionLabel.style.flexGrow   = 1;
                connectionLabel.style.color      = Color.white;
                connectionLabel.style.fontSize   = 11;
                connectionLabel.style.marginLeft = 5;

                // Create small delete button
                var deleteButton = new Button(() => RemoveNodeConnectionFromInspector(connection))
                {
                    text = "−"
                };
                deleteButton.style.width             = 16;
                deleteButton.style.height            = 16;
                deleteButton.style.fontSize          = 12;
                deleteButton.style.backgroundColor   = Color.clear;
                deleteButton.style.borderTopWidth    = 0;
                deleteButton.style.borderBottomWidth = 0;
                deleteButton.style.borderLeftWidth   = 0;
                deleteButton.style.borderRightWidth  = 0;
                deleteButton.style.color             = new Color(0.8f, 0.8f, 0.8f, 1f);

                // Add hover effect
                deleteButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    deleteButton.style.color           = new Color(1f, 1f, 1f, 1f);
                    deleteButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                });
                deleteButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    deleteButton.style.color           = new Color(0.8f, 0.8f, 0.8f, 1f);
                    deleteButton.style.backgroundColor = Color.clear;
                });

                connectionContainer.Add(connectionLabel);
                connectionContainer.Add(deleteButton);
                nodeInspector.Add(connectionContainer);
            }
        }
    }
}