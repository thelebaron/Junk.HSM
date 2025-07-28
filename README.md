# Unity Graph Editor Tool

A Unity Editor-only graph tool built with UI Toolkit for creating node-based state machine graphs.

## Features

- **Node-based Graph Editor**: Create and manage nodes representing state machines
- **State Management**: Each node contains multiple internal states
- **Connection System**: Connect states within nodes or across different nodes
- **ScriptableObject Persistence**: Save and load graph data as Unity assets
- **UI Toolkit Interface**: Modern, responsive editor interface
- **Visual Styling**: Custom USS styling for professional appearance

## Getting Started

### Opening the Graph Editor

1. **Menu Access**: In Unity, go to `Window > Graph Editor` to open the graph editor window
2. **Double-Click Assets**: Double-click any GraphData asset to open it directly in the editor
3. **Auto-Focus**: If the editor is already open, double-clicking will load that graph and focus the window

### Creating a New Graph

1. Click the "New Graph" button in the toolbar
2. Choose a location and name for your graph asset
3. The new graph will be automatically loaded in the editor

### Loading an Existing Graph

1. Use the "Graph Asset" object field in the toolbar
2. Drag and drop a GraphData asset or use the object picker
3. Double-click any GraphData asset in the Project window
4. The graph will load and display all nodes and connections

### Working with Nodes

#### Creating Nodes
- Right-click in the graph view and select "Create Node"
- Nodes are created at the mouse cursor position
- New nodes are created empty and will auto-resize based on assigned states

#### Editing Nodes
- Click on a node to select it and open the inspector
- Use the inspector to rename the node
- Double-click the node title to rename it inline
- Drag nodes around the canvas to reposition them
- Nodes automatically resize to encompass their assigned states
- Right-click on a node for context menu options

#### Deleting Nodes
- Right-click on a node and select "Delete Node"
- All connections to/from the node will be automatically removed

### Working with States

#### Creating States
- Right-click in the graph view and select "Create State"
- States are created at the mouse cursor position
- States are created as independent, movable elements

#### Editing States
- Double-click a state label to rename it
- Drag states around the canvas to reposition them
- States can be assigned to nodes via context menu

#### Assigning States to Nodes
- Select a state by clicking on it
- Use the Inspector panel on the right to assign the state to a node
- Choose from the "Parent Node" dropdown in the inspector
- When assigned, states automatically move to appear in front of the parent node
- Assigned states will cause the parent node to auto-resize
- States can exist independently without being assigned to any node

#### Deleting States
- Right-click on a state and select "Delete State"

### Inspector Panel

#### Selection
- Click on any state or node to select it and open the inspector
- The inspector panel appears on the right side of the window
- Click on empty space to deselect and hide the inspector
- Only one element can be selected at a time

#### State Properties (when state is selected)
- **State Name**: Edit the name of the selected state
- **Parent Node**: Choose which node the state belongs to from dropdown
- Dropdown handles duplicate node names by adding numbers

#### Node Properties (when node is selected)
- **Node Name**: Edit the name of the selected node
- Changes update the dropdown options for state assignments

#### Auto-Save
- All changes are automatically saved to the graph asset
- Visual updates happen immediately

### Navigation

#### Panning
- **Middle Mouse**: Hold middle mouse button and drag to pan around the graph
- **Alternative**: Hold Ctrl + Right mouse button and drag to pan
- All nodes and connections move together

#### Framing
- **Frame All Button**: Click "Frame All" in the toolbar to center all content
- **Keyboard Shortcut**: Press `F` to frame all content in the view
- **Auto-Frame**: When opening a graph, the view automatically frames all content

#### Context Menus
- Right-click on empty space: Create new nodes or states
- Right-click on nodes: Node-specific actions
- Right-click on states: State-specific actions

### Copy/Paste

#### Copying Elements
- **Keyboard**: Select a node or state and press `Ctrl+C` to copy
- **Context Menu**: Right-click on selected element and choose "Copy"

#### Pasting Elements
- **Keyboard**: Press `Ctrl+V` to paste at the last right-click position
- **Context Menu**: Right-click and choose "Paste" to paste at cursor position
- **Behavior**: Pasted elements get " Copy" appended to their names
- **Connections**: Original connections are preserved when copying

### Saving

- Click the "Save" button in the toolbar to save changes
- The graph data is automatically marked as dirty when modified
- Unity's standard asset management applies

## Test Data

### Creating Test Graph
1. Go to `Tools > Create Test Graph Data` in the Unity menu
2. This creates a sample graph with "freeze" and "fire" nodes
3. The test graph demonstrates the structure shown in the reference image

### Test Graph Structure
- **Freeze Node**: Contains states "freeze : into", "freeze : loop_freeze", "freeze : to_idle"
- **Fire Node**: Contains states "fire : from", "fire : loop_fire", "fire : exit"
- **Connections**: Shows internal and cross-node connections

## Technical Details

### Architecture
- **GraphData**: ScriptableObject containing the entire graph
- **NodeData**: Individual node data with states and properties
- **StateData**: Individual state data with position and name
- **ConnectionData**: Connection information between states/nodes

### UI Toolkit Components
- **GraphEditorWindow**: Main editor window
- **GraphView**: Canvas for displaying the graph
- **NodeView**: Visual representation of nodes
- **StateView**: Visual representation of states
- **ConnectionView**: Visual representation of connections

### Styling
- Custom USS file provides dark theme styling
- Hover effects and visual feedback
- Responsive layout design

## File Structure

```
Assets/
├── Scripts/
│   ├── GraphData.cs           # Main graph ScriptableObject
│   ├── NodeData.cs            # Node data structure
│   ├── StateData.cs           # State data structure
│   ├── ConnectionData.cs      # Connection data structure
│   └── Editor/
│       ├── GraphEditorWindow.cs    # Main editor window
│       ├── GraphView.cs            # Graph canvas
│       ├── NodeView.cs             # Node visual element
│       ├── StateView.cs            # State visual element
│       ├── ConnectionView.cs       # Connection visual element
│       └── GraphTestDataCreator.cs # Test data creation utility
└── Resources/
    └── GraphEditor.uss        # UI Toolkit styles
```

## Requirements

- Unity 2022.3 or later
- UI Toolkit package (included in Unity)
- Editor-only functionality

## Limitations

- Editor-only tool (no runtime functionality)
- Basic connection visualization (could be enhanced with bezier curves)
- No undo/redo system implemented
- No copy/paste functionality

## Future Enhancements

- Advanced connection drawing with bezier curves
- Undo/redo system
- Copy/paste functionality
- Zoom functionality
- Mini-map for large graphs
- Search and filter capabilities
- Export functionality
