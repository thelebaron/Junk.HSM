# Changelog

All notable changes to this package will be documented in this file.

## [1.0.0] - 2025-07-30

### Added
- Initial release of Junk Web Graph Editor package
- Visual graph editor for state machines
- Node and state management system
- Multiple connection types support (Node-to-Node, Node-to-State, State-to-State, State-to-Node)
- Inspector integration for editing properties
- Asset integration for saving/loading graphs
- Bezier curve connections with smooth visual representation
- Color customization for nodes and states
- Assembly definitions for proper dependency management
- Package structure with proper Unity package format

### Features
- **WebEditorWindow**: Main editor window with toolbar and graph view
- **GraphView**: Visual representation of the graph with pan/zoom support
- **NodeView & StateView**: Visual components for nodes and states
- **ConnectionView**: Bezier curve connections between elements
- **AssetPaths**: Centralized stylesheet loading utility
- **GraphData, NodeData, StateData, ConnectionData**: Core data structures
- **IWebNode interface**: Extensible node type system with Unity Entities integration

### Dependencies
- Unity 2023.3 or later
- Unity Entities package (1.3.14 or later)
