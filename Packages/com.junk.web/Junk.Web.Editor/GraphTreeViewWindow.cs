using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    public class GraphTreeViewWindow : EditorWindow
    {
        private GraphData graphData;
        private ScrollView hierarchyScrollView;
        private ScrollView connectionsScrollView;
        private VisualElement propertiesPanel;
        private StateData selectedState;
        private Dictionary<string, bool> nodeExpandedStates = new Dictionary<string, bool>();
        
        public static void ShowWindow(GraphData data)
        {
            var window = GetWindow<GraphTreeViewWindow>("Graph Tree View");
            window.minSize = new Vector2(800, 400);
            window.SetGraphData(data);
            window.Show();
        }
        
        public void SetGraphData(GraphData data)
        {
            graphData = data;
            RefreshTreeView();
        }
        
        private void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Main horizontal container
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Row;
            mainContainer.style.flexGrow = 1;
            
            // Left panel - Hierarchy
            var hierarchyPanel = new VisualElement();
            hierarchyPanel.style.width = Length.Percent(30);
            hierarchyPanel.style.borderRightWidth = 1;
            hierarchyPanel.style.borderRightColor = Color.gray;
            
            var hierarchyHeader = new Label("Web, SubWebs, and Nodes");
            hierarchyHeader.style.fontSize = 12;
            hierarchyHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            hierarchyHeader.style.paddingLeft = 5;
            hierarchyHeader.style.paddingTop = 5;
            hierarchyHeader.style.paddingBottom = 5;
            hierarchyHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            hierarchyPanel.Add(hierarchyHeader);
            
            hierarchyScrollView = new ScrollView();
            hierarchyScrollView.style.flexGrow = 1;
            hierarchyPanel.Add(hierarchyScrollView);
            
            // Middle panel - Connections
            var connectionsPanel = new VisualElement();
            connectionsPanel.style.width = Length.Percent(40);
            connectionsPanel.style.borderRightWidth = 1;
            connectionsPanel.style.borderRightColor = Color.gray;
            
            var connectionsHeader = new Label("Connections");
            connectionsHeader.style.fontSize = 12;
            connectionsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            connectionsHeader.style.paddingLeft = 5;
            connectionsHeader.style.paddingTop = 5;
            connectionsHeader.style.paddingBottom = 5;
            connectionsHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            connectionsPanel.Add(connectionsHeader);
            
            connectionsScrollView = new ScrollView();
            connectionsScrollView.style.flexGrow = 1;
            connectionsPanel.Add(connectionsScrollView);
            
            // Right panel - Properties
            propertiesPanel = new VisualElement();
            propertiesPanel.style.width = Length.Percent(30);
            
            var propertiesHeader = new Label("Properties");
            propertiesHeader.style.fontSize = 12;
            propertiesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            propertiesHeader.style.paddingLeft = 5;
            propertiesHeader.style.paddingTop = 5;
            propertiesHeader.style.paddingBottom = 5;
            propertiesHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            propertiesPanel.Add(propertiesHeader);
            
            mainContainer.Add(hierarchyPanel);
            mainContainer.Add(connectionsPanel);
            mainContainer.Add(propertiesPanel);
            
            root.Add(mainContainer);
            
            RefreshTreeView();
        }
        
        private void RefreshTreeView()
        {
            if (hierarchyScrollView == null || graphData == null)
                return;
                
            hierarchyScrollView.Clear();
            
            // Create root web container
            var webContainer = new VisualElement();
            var webHeader = CreateFoldoutHeader("unnamed", "Web", true, null);
            webContainer.Add(webHeader);
            
            // Create nodes container under web
            var nodesContainer = new VisualElement();
            nodesContainer.style.paddingLeft = 20;
            
            foreach (var node in graphData.Nodes)
            {
                CreateNodeTreeItem(node, nodesContainer);
            }
            
            webContainer.Add(nodesContainer);
            hierarchyScrollView.Add(webContainer);
        }
        
        private VisualElement CreateFoldoutHeader(string name, string type, bool isExpanded, System.Action onToggle)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 5;
            header.style.paddingTop = 2;
            header.style.paddingBottom = 2;
            //header.style.cursor = StyleCursor.Link;
            
            // Foldout triangle
            var triangle = new Label(isExpanded ? "▼" : "►");
            triangle.style.fontSize = 10;
            triangle.style.color = Color.gray;
            triangle.style.marginRight = 5;
            triangle.style.width = 12;
            header.Add(triangle);
            
            // Icon based on type
            var icon = new Label();
            if (type == "Web") icon.text = "🌐";
            else if (type == "Node") icon.text = "📦";
            else if (type == "State") icon.text = "●";
            icon.style.fontSize = 12;
            icon.style.marginRight = 5;
            header.Add(icon);
            
            // Name
            var nameLabel = new Label(name);
            nameLabel.style.fontSize = 11;
            nameLabel.style.color = Color.white;
            header.Add(nameLabel);
            
            // Click handler for foldout
            if (onToggle != null)
            {
                header.RegisterCallback<MouseDownEvent>(evt => onToggle());
            }
            
            return header;
        }
        
        private void CreateNodeTreeItem(NodeData node, VisualElement parent)
        {
            var nodeContainer = new VisualElement();
            
            // Check if node is expanded
            bool isExpanded = nodeExpandedStates.ContainsKey(node.Id) ? nodeExpandedStates[node.Id] : true;
            
            // Node header with foldout
            var nodeHeader = CreateFoldoutHeader(node.Name, "Node", isExpanded, () => 
            {
                nodeExpandedStates[node.Id] = !nodeExpandedStates[node.Id];
                RefreshTreeView();
            });
            nodeContainer.Add(nodeHeader);
            
            // States container (only visible if expanded)
            if (isExpanded)
            {
                var statesContainer = new VisualElement();
                statesContainer.style.paddingLeft = 20;
                
                foreach (var state in node.States)
                {
                    CreateStateTreeItem(state, statesContainer);
                }
                
                nodeContainer.Add(statesContainer);
            }
            
            parent.Add(nodeContainer);
        }
        
        private void CreateStateTreeItem(StateData state, VisualElement parent)
        {
            var stateHeader = CreateFoldoutHeader(state.Name, "State", false, null);
            
            // Make the state clickable to show connections and properties
            stateHeader.RegisterCallback<MouseDownEvent>(evt => OnStateSelected(state));
            
            // Highlight selected state
            if (selectedState == state)
            {
                stateHeader.style.backgroundColor = new Color(0.3f, 0.5f, 1f, 0.3f);
            }
            else
            {
                stateHeader.style.backgroundColor = StyleKeyword.Null;
            }
            
            parent.Add(stateHeader);
        }
        
        private void OnStateSelected(StateData state)
        {
            selectedState = state;
            RefreshTreeView(); // Refresh to update selection highlighting
            UpdateConnectionsPanel();
            UpdatePropertiesPanel();
        }
        
        private void UpdateConnectionsPanel()
        {
            if (connectionsScrollView == null)
                return;
                
            connectionsScrollView.Clear();
            
            if (selectedState == null)
                return;
                
            // Show outgoing connections
            if (selectedState.OutgoingConnections.Count > 0)
            {
                var outgoingHeader = new Label("Outgoing Connections:");
                outgoingHeader.style.fontSize = 12;
                outgoingHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                outgoingHeader.style.color = Color.white;
                outgoingHeader.style.marginBottom = 5;
                outgoingHeader.style.paddingLeft = 5;
                connectionsScrollView.Add(outgoingHeader);
                
                foreach (var connection in selectedState.OutgoingConnections)
                {
                    CreateConnectionItem(connection, true);
                }
            }
            
            // Show incoming connections
            var incomingConnections = FindIncomingConnections(selectedState);
            if (incomingConnections.Count > 0)
            {
                var incomingHeader = new Label("Incoming Connections:");
                incomingHeader.style.fontSize = 12;
                incomingHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                incomingHeader.style.color = Color.white;
                incomingHeader.style.marginTop = 10;
                incomingHeader.style.marginBottom = 5;
                incomingHeader.style.paddingLeft = 5;
                connectionsScrollView.Add(incomingHeader);
                
                foreach (var connection in incomingConnections)
                {
                    CreateConnectionItem(connection, false);
                }
            }
            
            if (selectedState.OutgoingConnections.Count == 0 && incomingConnections.Count == 0)
            {
                var noConnections = new Label("No connections");
                noConnections.style.fontSize = 11;
                noConnections.style.color = Color.gray;
                noConnections.style.paddingLeft = 5;
                connectionsScrollView.Add(noConnections);
            }
        }
        
        private void CreateConnectionItem(ConnectionData connection, bool isOutgoing)
        {
            var connectionContainer = new VisualElement();
            connectionContainer.style.flexDirection = FlexDirection.Row;
            connectionContainer.style.alignItems = Align.Center;
            connectionContainer.style.paddingTop = 2;
            connectionContainer.style.paddingBottom = 2;
            connectionContainer.style.paddingLeft = 10;
            
            // Connection arrow icon
            var connectionIcon = new Label(isOutgoing ? "→" : "←");
            connectionIcon.style.fontSize = 12;
            connectionIcon.style.color = isOutgoing ? new Color(0.7f, 1f, 0.7f, 1f) : new Color(1f, 0.7f, 0.7f, 1f);
            connectionIcon.style.marginRight = 8;
            connectionContainer.Add(connectionIcon);
            
            // Target/Source info
            var targetState = graphData.GetStateById(isOutgoing ? connection.TargetStateId : connection.SourceStateId);
            var targetNode = graphData.GetNodeById(isOutgoing ? connection.TargetNodeId : connection.SourceNodeId);
            
            var targetInfo = new Label();
            if (targetState != null && targetNode != null)
            {
                var sourceNodeId = isOutgoing ? connection.SourceNodeId : connection.TargetNodeId;
                var targetNodeId = isOutgoing ? connection.TargetNodeId : connection.SourceNodeId;
                
                if (sourceNodeId == targetNodeId)
                {
                    // Same node connection
                    targetInfo.text = targetState.Name;
                }
                else
                {
                    // Cross-node connection  
                    targetInfo.text = $"{targetNode.Name}:{targetState.Name}";
                }
            }
            else
            {
                targetInfo.text = "[Missing Target]";
            }
            
            targetInfo.style.fontSize = 11;
            targetInfo.style.color = Color.white;
            connectionContainer.Add(targetInfo);
            
            connectionsScrollView.Add(connectionContainer);
        }
        
        private System.Collections.Generic.List<ConnectionData> FindIncomingConnections(StateData targetState)
        {
            var incomingConnections = new System.Collections.Generic.List<ConnectionData>();
            
            foreach (var node in graphData.Nodes)
            {
                foreach (var state in node.States)
                {
                    foreach (var connection in state.OutgoingConnections)
                    {
                        if (connection.TargetStateId == targetState.Id)
                        {
                            incomingConnections.Add(connection);
                        }
                    }
                }
            }
            
            return incomingConnections;
        }
        
        private void UpdatePropertiesPanel()
        {
            if (propertiesPanel == null)
                return;
                
            // Clear existing properties (except header)
            var propertiesToRemove = new System.Collections.Generic.List<VisualElement>();
            for (int i = 1; i < propertiesPanel.childCount; i++) // Skip header at index 0
            {
                propertiesToRemove.Add(propertiesPanel[i]);
            }
            foreach (var element in propertiesToRemove)
            {
                element.RemoveFromHierarchy();
            }
            
            if (selectedState == null)
                return;
                
            // State name property
            var nameContainer = new VisualElement();
            nameContainer.style.flexDirection = FlexDirection.Row;
            nameContainer.style.alignItems = Align.Center;
            nameContainer.style.paddingLeft = 5;
            nameContainer.style.paddingTop = 5;
            nameContainer.style.paddingBottom = 2;
            
            var nameLabel = new Label("Name:");
            nameLabel.style.fontSize = 11;
            nameLabel.style.color = Color.gray;
            nameLabel.style.width = 60;
            nameContainer.Add(nameLabel);
            
            var nameValue = new Label(selectedState.Name);
            nameValue.style.fontSize = 11;
            nameValue.style.color = Color.white;
            nameContainer.Add(nameValue);
            
            propertiesPanel.Add(nameContainer);
            
            // Parent node property
            var parentNodeContainer = new VisualElement();
            parentNodeContainer.style.flexDirection = FlexDirection.Row;
            parentNodeContainer.style.alignItems = Align.Center;
            parentNodeContainer.style.paddingLeft = 5;
            parentNodeContainer.style.paddingTop = 2;
            parentNodeContainer.style.paddingBottom = 2;
            
            var parentLabel = new Label("Node:");
            parentLabel.style.fontSize = 11;
            parentLabel.style.color = Color.gray;
            parentLabel.style.width = 60;
            parentNodeContainer.Add(parentLabel);
            
            var parentNode = graphData.GetNodeById(selectedState.ParentNodeId);
            var parentValue = new Label(parentNode?.Name ?? "Unknown");
            parentValue.style.fontSize = 11;
            parentValue.style.color = Color.white;
            parentNodeContainer.Add(parentValue);
            
            propertiesPanel.Add(parentNodeContainer);
            
            // ID property (for debugging)
            var idContainer = new VisualElement();
            idContainer.style.flexDirection = FlexDirection.Row;
            idContainer.style.alignItems = Align.Center;
            idContainer.style.paddingLeft = 5;
            idContainer.style.paddingTop = 2;
            idContainer.style.paddingBottom = 2;
            
            var idLabel = new Label("ID:");
            idLabel.style.fontSize = 11;
            idLabel.style.color = Color.gray;
            idLabel.style.width = 60;
            idContainer.Add(idLabel);
            
            var idValue = new Label(selectedState.Id.Substring(0, 8) + "...");
            idValue.style.fontSize = 9;
            idValue.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            idContainer.Add(idValue);
            
            propertiesPanel.Add(idContainer);
        }
    }
}