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
        private VisualElement _runtimeDot;
        private VisualElement _glowElement;
        private NodeState _visualState = NodeState.Idle;
        private VisualElement _inlineContentContainer;
        private Button _hintButton;
        
        // Pulse animation state
        private IVisualElementScheduledItem _pulseSchedule;
        private float _pulsePhase = 0f;
        private bool _isPulsing = false;
        
        /// <summary>Called when node data changes (for saving)</summary>
        public Action OnDataChanged;

        public NodeView(NodeData data)
        {
            Data = data;
            viewDataKey = data.Guid;
            capabilities |= Capabilities.Groupable | Capabilities.Ascendable;

            // Set title and style
            title = data.Name;
            
            // Set title background color
            _titleContainer = this.Q("title");
            _originalColor = data.Color;
            if (_titleContainer != null)
            {
                _titleContainer.style.backgroundColor = data.Color;
                _titleContainer.style.position = Position.Relative; // Enable absolute positioning for children
                
                // Ensure title label has padding to prevent overlap with input field
                var titleLabel = _titleContainer.Q<Label>("title-label");
                if (titleLabel != null)
                {
                    // Keep title clear of the right-side UI (breakpoint + fold).
                    titleLabel.style.paddingRight = 44;
                    titleLabel.style.paddingLeft = 28;
                    titleLabel.style.overflow = Overflow.Hidden; // Prevent text from extending into input field
                }
                
                CreateHintButton();
            }

            // Add glow element behind everything.
            _glowElement = new VisualElement();
            _glowElement.name = "node-glow";
            _glowElement.pickingMode = PickingMode.Ignore; // Don't intercept clicks
            Insert(0, _glowElement);

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

            // Add small runtime state dot in bottom-right corner
            _runtimeDot = new VisualElement();
            _runtimeDot.name = "runtime-dot";
            _runtimeDot.style.position = Position.Absolute;
            _runtimeDot.style.right = 6;
            _runtimeDot.style.bottom = 6;
            _runtimeDot.style.width = 10;
            _runtimeDot.style.height = 10;
            _runtimeDot.style.borderTopLeftRadius = 5;
            _runtimeDot.style.borderTopRightRadius = 5;
            _runtimeDot.style.borderBottomLeftRadius = 5;
            _runtimeDot.style.borderBottomRightRadius = 5;
            _runtimeDot.style.backgroundColor = Color.clear; // hidden by default
            Add(_runtimeDot);

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

        private void CreateHintButton()
        {
            var hintText = Data.GetHintText();
            if (string.IsNullOrWhiteSpace(hintText)) return;

            _hintButton = new Button();
            _hintButton.name = "node-hint-button";
            _hintButton.text = "?";
            _hintButton.tooltip = hintText;
            _hintButton.style.position = Position.Absolute;
            _hintButton.style.left = 4;
            _hintButton.style.top = 2;
            _hintButton.style.width = 20;
            _hintButton.style.height = 20;
            _hintButton.style.minWidth = 20;
            _hintButton.style.paddingLeft = 0;
            _hintButton.style.paddingRight = 0;
            _hintButton.style.paddingTop = 0;
            _hintButton.style.paddingBottom = 0;
            _hintButton.style.fontSize = 12;
            _hintButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _hintButton.style.color = new Color(0.85f, 0.92f, 1f);
            _hintButton.style.backgroundColor = new Color(0.08f, 0.17f, 0.27f, 0.95f);
            _hintButton.style.borderTopLeftRadius = 10;
            _hintButton.style.borderTopRightRadius = 10;
            _hintButton.style.borderBottomLeftRadius = 10;
            _hintButton.style.borderBottomRightRadius = 10;
            _hintButton.style.borderTopColor = new Color(0.2f, 0.45f, 0.75f);
            _hintButton.style.borderBottomColor = new Color(0.2f, 0.45f, 0.75f);
            _hintButton.style.borderLeftColor = new Color(0.2f, 0.45f, 0.75f);
            _hintButton.style.borderRightColor = new Color(0.2f, 0.45f, 0.75f);

            _hintButton.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            _titleContainer.Add(_hintButton);
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
            
            // Call cleanup on existing content before clearing
            var existingContent = NodeInlineContentFactory.GetContent(Data);
            if (existingContent != null)
            {
                existingContent.Initialize(Data, _inlineContentContainer, null, null);
                existingContent.Cleanup();
            }
            
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
            // Use Port.Create<FlowStyleEdge> so user-dragged connections
            // automatically create our custom edge type.
            foreach (var portData in Data.GetInputPorts())
            {
                var port = Port.Create<FlowStyleEdge>(
                    Orientation.Horizontal,
                    Direction.Input,
                    portData.capacity == PortCapacity.Multi ? Port.Capacity.Multi : Port.Capacity.Single,
                    typeof(bool)
                );

                port.portName = portData.name;
                port.name = portData.id;
                
                _inputPorts[portData.id] = port;
                inputContainer.Add(port);
            }

            // Output ports
            foreach (var portData in Data.GetOutputPorts())
            {
                var port = Port.Create<FlowStyleEdge>(
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
        /// Update visual state for runtime visualization.
        /// Uses CSS transitions for smooth color changes.
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

            // CSS transitions in USS handle the smooth color interpolation.
            // We only need to toggle classes and update the non-USS-driven properties.
            switch (state)
            {
                case NodeState.Idle:
                    AddToClassList("node-idle");
                    _stateIndicator.style.backgroundColor = Color.clear;
                    if (_runtimeDot != null)
                        _runtimeDot.style.backgroundColor = Color.clear;
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = _originalColor;
                    StopPulse();
                    break;

                case NodeState.Running:
                    AddToClassList("node-running");
                    _stateIndicator.style.backgroundColor = new Color(0.12f, 0.56f, 1f); // Electric blue
                    if (_runtimeDot != null)
                        _runtimeDot.style.backgroundColor = new Color(0.2f, 0.7f, 1f); // Bright blue dot
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.08f, 0.4f, 0.75f); // Deep blue
                    StartPulse();
                    break;

                case NodeState.Completed:
                    AddToClassList("node-completed");
                    _stateIndicator.style.backgroundColor = new Color(0.12f, 0.56f, 1f); // Electric blue
                    if (_runtimeDot != null)
                        _runtimeDot.style.backgroundColor = new Color(0.2f, 0.7f, 1f); // Bright blue dot
                    if (_titleContainer != null)
                    {
                        _titleContainer.style.backgroundColor = new Color(0.08f, 0.4f, 0.75f); // Deep blue
                    }
                    else
                    {
                        _titleContainer = this.Q("title");
                        if (_titleContainer != null)
                            _titleContainer.style.backgroundColor = new Color(0.08f, 0.4f, 0.75f);
                    }
                    StopPulse();
                    break;

                case NodeState.Failed:
                    AddToClassList("node-failed");
                    _stateIndicator.style.backgroundColor = new Color(0.12f, 0.56f, 1f); // Electric blue
                    if (_runtimeDot != null)
                        _runtimeDot.style.backgroundColor = new Color(0.2f, 0.7f, 1f); // Bright blue dot
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.08f, 0.4f, 0.75f); // Deep blue
                    StopPulse();
                    break;
            }
        }

        /// <summary>
        /// Start a pulsing glow animation on the running node.
        /// Electric blue pulse that is large and unmissable.
        /// </summary>
        private void StartPulse()
        {
            if (_isPulsing) return;
            _isPulsing = true;
            _pulsePhase = 0f;
            
            // Schedule a repeating callback every 30ms (~33fps for snappy, punchy blinks)
            _pulseSchedule = schedule.Execute(() =>
            {
                if (!_isPulsing || _glowElement == null) return;
                
                _pulsePhase += 0.09f; // Faster cycle for a strong blink effect
                float t = 0.5f + 0.5f * Mathf.Sin(_pulsePhase * Mathf.PI * 2f);
                
                // Background: vivid blue, oscillates between 0.20 and 0.65 alpha (big swing)
                float bgAlpha = Mathf.Lerp(0.20f, 0.65f, t);
                _glowElement.style.backgroundColor = new Color(0.15f, 0.60f, 1f, bgAlpha);
                
                // Border: bright cyan ring, oscillates between 0.4 and 1.0 alpha (full flash)
                float borderAlpha = Mathf.Lerp(0.4f, 1f, t);
                var borderColor = new Color(0.3f, 0.78f, 1f, borderAlpha);
                _glowElement.style.borderTopColor = borderColor;
                _glowElement.style.borderBottomColor = borderColor;
                _glowElement.style.borderLeftColor = borderColor;
                _glowElement.style.borderRightColor = borderColor;
            }).Every(30);
        }

        /// <summary>
        /// Stop the pulse animation and clear inline glow styles so USS takes over.
        /// </summary>
        private void StopPulse()
        {
            if (!_isPulsing) return;
            _isPulsing = false;
            
            _pulseSchedule?.Pause();
            _pulseSchedule = null;
            
            // IMPORTANT: Clear the inline styles that StartPulse() wrote.
            // Without this, the last pulse frame's inline colors persist and
            // override the USS rules (which would set transparent for idle).
            if (_glowElement != null)
            {
                _glowElement.style.backgroundColor = Color.clear;
                _glowElement.style.borderTopColor = Color.clear;
                _glowElement.style.borderBottomColor = Color.clear;
                _glowElement.style.borderLeftColor = Color.clear;
                _glowElement.style.borderRightColor = Color.clear;
            }
        }

        /// <summary>
        /// Apply or remove the delete-preview styling (red highlight).
        /// Called by NodeGraphView when Alt key is held and mouse hovers.
        /// </summary>
        public void SetDeletePreview(bool show)
        {
            if (show)
                AddToClassList("node-delete-preview");
            else
                RemoveFromClassList("node-delete-preview");
        }

        /// <summary>
        /// Apply zoom-based detail level.
        /// Conservative thresholds - title bar and node color are NEVER hidden.
        /// LOD 1 (below 0.35x): only hide inline content (the heavy property editors)
        /// LOD 2 (below 0.20x): also hide port labels and small indicators
        /// </summary>
        public void SetZoomDetailLevel(float zoomLevel)
        {
            // LOD 1: Hide inline content only (< 0.35 zoom)
            if (zoomLevel < 0.35f && !ClassListContains("zoom-lod-1"))
            {
                AddToClassList("zoom-lod-1");
            }
            else if (zoomLevel >= 0.35f)
            {
                RemoveFromClassList("zoom-lod-1");
            }
            
            // LOD 2: Also hide port labels and minor UI (< 0.20 zoom)
            if (zoomLevel < 0.20f && !ClassListContains("zoom-lod-2"))
            {
                AddToClassList("zoom-lod-2");
            }
            else if (zoomLevel >= 0.20f)
            {
                RemoveFromClassList("zoom-lod-2");
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
