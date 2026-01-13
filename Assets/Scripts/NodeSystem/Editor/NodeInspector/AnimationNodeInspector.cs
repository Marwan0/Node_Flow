#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for AnimationNode with object picker and conditional fields
    /// </summary>
    public class AnimationNodeInspector : NodeInspectorBase
    {
        private AnimationNode _node;
        private VisualElement _optionsContainer;

        public override void DrawInspector()
        {
            _node = Node as AnimationNode;
            if (_node == null) return;

            // Target GameObject (object picker)
            CreateLabel("Target", true);
            
            var objectField = new ObjectField("GameObject")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true
            };

            if (!string.IsNullOrEmpty(_node.targetPath))
            {
                objectField.value = GameObject.Find(_node.targetPath);
            }

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is GameObject go)
                {
                    _node.targetPath = GetGameObjectPath(go);
                }
                else
                {
                    _node.targetPath = "";
                }
                MarkDirty();
            });
            objectField.style.marginBottom = 5;
            Container.Add(objectField);

            // Show path
            if (!string.IsNullOrEmpty(_node.targetPath))
            {
                var pathLabel = new Label($"Path: {_node.targetPath}");
                pathLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                pathLabel.style.fontSize = 10;
                pathLabel.style.marginBottom = 10;
                Container.Add(pathLabel);
            }

            CreateSeparator();

            // Animation Type
            CreateLabel("Animation", true);
            
            CreateEnumField("Type", _node.animationType, (AnimationType v) =>
            {
                _node.animationType = v;
                RedrawOptions();
            });

            // Options container
            _optionsContainer = new VisualElement();
            Container.Add(_optionsContainer);

            RedrawOptions();

            CreateSeparator();

            // Timing
            CreateLabel("Timing", true);
            CreateFloatField("Duration", _node.duration, v => _node.duration = v);
            CreateFloatField("Delay", _node.delay, v => _node.delay = v);
        }

        private void RedrawOptions()
        {
            _optionsContainer.Clear();

            // Show slide direction only for slide animations
            if (_node.animationType == AnimationType.SlideIn || 
                _node.animationType == AnimationType.SlideOut)
            {
                var dirField = new EnumField("Direction", _node.slideDirection);
                dirField.RegisterValueChangedCallback(evt =>
                {
                    _node.slideDirection = (SlideDirection)evt.newValue;
                    MarkDirty();
                });
                dirField.style.marginBottom = 5;
                _optionsContainer.Add(dirField);
            }
        }
    }
}
#endif

