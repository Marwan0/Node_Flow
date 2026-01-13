#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Visual representation of a NodeData in the GraphView
    /// </summary>
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public NodeData Data { get; private set; }
        public Action<NodeView> OnNodeSelected;

        private Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
        private Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();
        private VisualElement _titleContainer;
        private Color _originalColor;
        private VisualElement _stateIndicator;
        private NodeState _visualState = NodeState.Idle;
        private VisualElement _inlineContentContainer;
        
        /// <summary>Called when node data changes (for saving)</summary>
        public Action OnDataChanged;

        public NodeView(NodeData data)
        {
            Data = data;
            viewDataKey = data.Guid;

            // Set title and style
            title = data.Name;
            
            // Set title background color
            _titleContainer = this.Q("title");
            _originalColor = data.Color;
            if (_titleContainer != null)
            {
                _titleContainer.style.backgroundColor = data.Color;
            }

            // Add state indicator
            _stateIndicator = new VisualElement();
            _stateIndicator.name = "state-indicator";
            _stateIndicator.style.position = Position.Absolute;
            _stateIndicator.style.left = -8;
            _stateIndicator.style.top = 0;
            _stateIndicator.style.bottom = 0;
            _stateIndicator.style.width = 4;
            _stateIndicator.style.backgroundColor = Color.clear;
            Add(_stateIndicator);

            // Add breakpoint indicator
            var breakpointIndicator = new VisualElement();
            breakpointIndicator.name = "breakpoint-indicator";
            breakpointIndicator.style.position = Position.Absolute;
            breakpointIndicator.style.right = 5;
            breakpointIndicator.style.top = 5;
            breakpointIndicator.style.width = 12;
            breakpointIndicator.style.height = 12;
            breakpointIndicator.style.borderTopLeftRadius = 6;
            breakpointIndicator.style.borderTopRightRadius = 6;
            breakpointIndicator.style.borderBottomLeftRadius = 6;
            breakpointIndicator.style.borderBottomRightRadius = 6;
            breakpointIndicator.style.backgroundColor = data.hasBreakpoint ? new Color(1f, 0.2f, 0.2f) : Color.clear;
            breakpointIndicator.style.borderTopWidth = 2;
            breakpointIndicator.style.borderBottomWidth = 2;
            breakpointIndicator.style.borderLeftWidth = 2;
            breakpointIndicator.style.borderRightWidth = 2;
            breakpointIndicator.style.borderTopColor = new Color(0.8f, 0.1f, 0.1f);
            breakpointIndicator.style.borderBottomColor = new Color(0.8f, 0.1f, 0.1f);
            breakpointIndicator.style.borderLeftColor = new Color(0.8f, 0.1f, 0.1f);
            breakpointIndicator.style.borderRightColor = new Color(0.8f, 0.1f, 0.1f);
            
            // Toggle breakpoint on click
            breakpointIndicator.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left click
                {
                    data.hasBreakpoint = !data.hasBreakpoint;
                    breakpointIndicator.style.backgroundColor = data.hasBreakpoint ? new Color(1f, 0.2f, 0.2f) : Color.clear;
                    evt.StopPropagation();
                }
            });
            
            Add(breakpointIndicator);

            // Create ports
            CreatePorts();

            // Create inline content
            CreateInlineContent();

            // Set position
            SetPosition(new Rect(data.Position, Vector2.zero));

            // Apply styles
            RefreshExpandedState();
            RefreshPorts();

            // Handle double-click for SubGraphNode
            if (data is Nodes.SubGraphNode subGraphNode)
            {
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount == 2 && evt.button == 0) // Double left-click
                    {
                        if (subGraphNode.subGraph != null)
                        {
                            // Find the editor window and open sub-graph
                            var window = EditorWindow.GetWindow<NodeGraphEditorWindow>();
                            if (window != null)
                            {
                                window.OpenSubGraph(subGraphNode.subGraph);
                            }
                        }
                        evt.StopPropagation();
                    }
                });
            }
        }

        /// <summary>
        /// Create inline content area for editable properties
        /// </summary>
        private void CreateInlineContent()
        {
            // Check if this node type has inline content
            if (!NodeInlineContentFactory.HasInlineContent(Data)) return;

            // Create container
            _inlineContentContainer = new VisualElement();
            _inlineContentContainer.name = "inline-content";
            _inlineContentContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            _inlineContentContainer.style.paddingTop = 6;
            _inlineContentContainer.style.paddingBottom = 6;
            _inlineContentContainer.style.paddingLeft = 10;
            _inlineContentContainer.style.paddingRight = 10;
            _inlineContentContainer.style.marginTop = 4;
            _inlineContentContainer.style.borderTopWidth = 1;
            _inlineContentContainer.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);

            // Set minimum width for full property display
            // Make AnimationSequencerNode wider and resizable
            if (Data is Nodes.AnimationSequencerNode)
            {
                style.minWidth = 450;
                style.width = 500;
                capabilities |= Capabilities.Resizable;
            }
            else
            {
                style.minWidth = 220;
            }

            // Draw initial inline content
            RefreshInlineContent();

            // Add to node's extension container (below ports)
            extensionContainer.Add(_inlineContentContainer);
        }

        /// <summary>
        /// Refresh/rebuild the inline content (useful when type changes affect UI)
        /// </summary>
        public void RefreshInlineContent()
        {
            if (_inlineContentContainer == null) return;
            
            // Clear existing content
            _inlineContentContainer.Clear();

            // Get and draw inline content
            var content = NodeInlineContentFactory.GetContent(Data);
            if (content != null)
            {
                content.Initialize(Data, _inlineContentContainer, 
                    () => OnDataChanged?.Invoke(),
                    () => RefreshInlineContent()); // Refresh callback
                content.Draw();
            }
        }

        private void CreatePorts()
        {
            // Input ports
            foreach (var portData in Data.GetInputPorts())
            {
                var port = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Input,
                    portData.capacity == PortCapacity.Multi ? Port.Capacity.Multi : Port.Capacity.Single,
                    typeof(bool) // Port type (for compatibility checking)
                );

                port.portName = portData.name;
                port.name = portData.id;
                
                _inputPorts[portData.id] = port;
                inputContainer.Add(port);
            }

            // Output ports
            foreach (var portData in Data.GetOutputPorts())
            {
                var port = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    portData.capacity == PortCapacity.Multi ? Port.Capacity.Multi : Port.Capacity.Single,
                    typeof(bool)
                );

                port.portName = portData.name;
                port.name = portData.id;
                
                _outputPorts[portData.id] = port;
                outputContainer.Add(port);
            }
        }

        /// <summary>
        /// Get an input port by ID
        /// </summary>
        public Port GetInputPort(string portId)
        {
            _inputPorts.TryGetValue(portId, out var port);
            return port;
        }

        /// <summary>
        /// Get an output port by ID
        /// </summary>
        public Port GetOutputPort(string portId)
        {
            _outputPorts.TryGetValue(portId, out var port);
            return port;
        }

        /// <summary>
        /// Refresh ports (useful when ports are added dynamically)
        /// </summary>
        public void RefreshPorts()
        {
            // Clear existing ports
            inputContainer.Clear();
            outputContainer.Clear();
            _inputPorts.Clear();
            _outputPorts.Clear();
            
            // Recreate ports
            CreatePorts();
            
            // Refresh expanded state to ensure ports are visible
            RefreshExpandedState();
        }

        /// <summary>
        /// Update node position in data
        /// </summary>
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Data.Position = new Vector2(newPos.x, newPos.y);
        }

        /// <summary>
        /// Handle selection
        /// </summary>
        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }

        /// <summary>
        /// Update visual state for runtime visualization
        /// </summary>
        public void SetVisualState(NodeState state)
        {
            if (_visualState == state) return;
            _visualState = state;

            // Remove all state classes
            RemoveFromClassList("node-idle");
            RemoveFromClassList("node-running");
            RemoveFromClassList("node-completed");
            RemoveFromClassList("node-failed");

            // Update visuals based on state (border color handled by USS, no width change to avoid shift)
            switch (state)
            {
                case NodeState.Idle:
                    AddToClassList("node-idle");
                    _stateIndicator.style.backgroundColor = Color.clear;
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = _originalColor;
                    break;

                case NodeState.Running:
                    AddToClassList("node-running");
                    _stateIndicator.style.backgroundColor = new Color(1f, 0.6f, 0f); // Orange
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f);
                    break;

                case NodeState.Completed:
                    AddToClassList("node-completed");
                    _stateIndicator.style.backgroundColor = new Color(0.2f, 0.8f, 0.3f); // Green
                    // Update title background color - use a brighter green for visibility
                    if (_titleContainer != null)
                    {
                        _titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f); // Brighter green
                    }
                    else
                    {
                        // Fallback: try to find title container again
                        _titleContainer = this.Q("title");
                        if (_titleContainer != null)
                        {
                            _titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.3f);
                        }
                    }
                    break;

                case NodeState.Failed:
                    AddToClassList("node-failed");
                    _stateIndicator.style.backgroundColor = new Color(1f, 0.3f, 0.3f); // Red
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                    break;
            }
        }

        /// <summary>
        /// Reset visual state to idle
        /// </summary>
        public void ResetVisualState()
        {
            SetVisualState(NodeState.Idle);
        }
    }
}
#endif

