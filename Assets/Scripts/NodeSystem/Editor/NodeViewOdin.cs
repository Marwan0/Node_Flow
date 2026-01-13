#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace NodeSystem.Editor
{
    /// <summary>
    /// Node view with Odin Inspector embedded inside the node
    /// Use this instead of NodeView if you have Odin Inspector
    /// </summary>
    public class NodeViewOdin : UnityEditor.Experimental.GraphView.Node
    {
        public NodeData Data { get; private set; }
        public Action<NodeViewOdin> OnNodeSelected;
        public Action OnDataChanged;

        private Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
        private Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();
        private VisualElement _titleContainer;
        private Color _originalColor;
        private VisualElement _stateIndicator;
        private NodeState _visualState = NodeState.Idle;

#if ODIN_INSPECTOR
        private PropertyTree _propertyTree;
        private IMGUIContainer _odinContainer;
#endif

        public NodeViewOdin(NodeData data)
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

            // Create ports
            CreatePorts();

            // Create Odin inline content
            CreateOdinContent();

            // Set position
            SetPosition(new Rect(data.Position, Vector2.zero));

            // Expand to show Odin content
            expanded = true;
            
            // Apply styles
            RefreshExpandedState();
            RefreshPorts();
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

        private void CreateOdinContent()
        {
#if ODIN_INSPECTOR
            // Create PropertyTree for the node data
            _propertyTree = PropertyTree.Create(Data);

            // Create IMGUI container to render Odin
            _odinContainer = new IMGUIContainer(() =>
            {
                if (_propertyTree == null) return;

                EditorGUI.BeginChangeCheck();
                
                // Draw with Odin - skip certain properties
                _propertyTree.BeginDraw(false);
                foreach (var property in _propertyTree.EnumerateTree(false))
                {
                    // Skip internal properties
                    if (property.Name == "_guid" || property.Name == "_position") continue;
                    if (property.Name == "Guid" || property.Name == "Position") continue;
                    
                    // Skip non-serialized fields
                    var info = property.Info;
                    if (info != null && info.GetMemberInfo() != null)
                    {
                        var attrs = info.GetMemberInfo().GetCustomAttributes(typeof(NonSerializedAttribute), true);
                        if (attrs.Length > 0) continue;
                    }

                    property.Draw();
                }
                _propertyTree.EndDraw();

                if (EditorGUI.EndChangeCheck())
                {
                    _propertyTree.ApplyChanges();
                    OnDataChanged?.Invoke();
                }
            });

            // Style the container
            _odinContainer.style.paddingLeft = 5;
            _odinContainer.style.paddingRight = 5;
            _odinContainer.style.paddingTop = 3;
            _odinContainer.style.paddingBottom = 3;
            _odinContainer.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.9f);

            // Set minimum width for the node
            style.minWidth = 200;

            // Add to extension container
            extensionContainer.Add(_odinContainer);
#else
            // Fallback message when Odin is not installed
            var label = new Label("Odin Inspector not found");
            label.style.color = new Color(1f, 0.5f, 0.5f);
            label.style.fontSize = 10;
            label.style.paddingLeft = 5;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
            extensionContainer.Add(label);
#endif
        }

        public Port GetInputPort(string portId)
        {
            _inputPorts.TryGetValue(portId, out var port);
            return port;
        }

        public Port GetOutputPort(string portId)
        {
            _outputPorts.TryGetValue(portId, out var port);
            return port;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Data.Position = new Vector2(newPos.x, newPos.y);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }

        public void SetVisualState(NodeState state)
        {
            if (_visualState == state) return;
            _visualState = state;

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
                    _stateIndicator.style.backgroundColor = new Color(1f, 0.6f, 0f);
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f);
                    break;

                case NodeState.Completed:
                    AddToClassList("node-completed");
                    _stateIndicator.style.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.15f, 0.45f, 0.2f);
                    break;

                case NodeState.Failed:
                    AddToClassList("node-failed");
                    _stateIndicator.style.backgroundColor = new Color(1f, 0.3f, 0.3f);
                    if (_titleContainer != null)
                        _titleContainer.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                    break;
            }
        }

        public void ResetVisualState()
        {
            SetVisualState(NodeState.Idle);
        }

#if ODIN_INSPECTOR
        ~NodeViewOdin()
        {
            _propertyTree?.Dispose();
        }
#endif
    }
}
#endif

