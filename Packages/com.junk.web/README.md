# Junk Web Graph Editor

A graph-based state machine editor with visual node and state management capabilities for Unity projects.

## Features

- **Visual Graph Editor**: Create and edit state machines using a node-based visual interface
- **Node Management**: Create, edit, and connect nodes with customizable properties
- **State Management**: Add states to nodes with visual representation and connections
- **Connection System**: Support for multiple connection types:
  - Node to Node connections
  - Node to State connections  
  - State to State connections
  - State to Node connections
- **Inspector Integration**: Edit node and state properties through Unity's inspector
- **Asset Integration**: Save and load graphs as Unity assets
- **Bezier Curve Connections**: Smooth visual connections between elements
- **Color Customization**: Customizable colors for nodes and states

## Requirements

- Unity 2023.3 or later
- Unity Entities package (1.3.14 or later)

## Installation

This package is included as a local package in the project. It should automatically be available in the Package Manager under "In Project" packages.

## Usage

1. **Opening the Editor**: Go to `Window > Web Editor` to open the graph editor window
2. **Creating Graphs**: Click "New Graph" to create a new graph asset
3. **Adding Nodes**: Right-click in the graph view to add new nodes
4. **Adding States**: Select a node and use the inspector to add states
5. **Creating Connections**: Use the connection dropdowns in the inspector to create connections between elements
6. **Saving**: Use Ctrl+S or the Save button to save your changes

## Package Structure

- `Junk.Web/`: Runtime scripts for graph data structures
- `Junk.Web.Editor/`: Editor scripts for the visual graph editor
  - `GraphEditor.uss`: Stylesheet for the graph editor UI
  - `WebEditorWindow.cs`: Main editor window
  - `GraphView.cs`: Graph visualization component
  - `NodeView.cs` & `StateView.cs`: Visual representations of nodes and states
  - `Utilities/`: Helper utilities including bezier curve calculations

## Dependencies

- `com.unity.entities`: Required for IComponentData interfaces used in node definitions
