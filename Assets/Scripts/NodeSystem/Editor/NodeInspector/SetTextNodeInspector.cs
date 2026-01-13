#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using TMPro;
using NodeSystem.Nodes;
using UIText = UnityEngine.UI.Text;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for SetTextNode with Text/TMP picker
    /// </summary>
    public class SetTextNodeInspector : NodeInspectorBase
    {
        private SetTextNode _node;
        private VisualElement _typewriterOptions;

        public override void DrawInspector()
        {
            _node = Node as SetTextNode;
            if (_node == null) return;

            // Target Text picker
            CreateLabel("Target Text", true);

            // Try TMP first, then legacy Text
            var objectField = new ObjectField("Text Component")
            {
                objectType = typeof(Component),
                allowSceneObjects = true
            };

            if (!string.IsNullOrEmpty(_node.targetPath))
            {
                var go = GameObject.Find(_node.targetPath);
                if (go != null)
                {
                    // Prefer TMP
                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        objectField.value = tmp;
                    }
                    else
                    {
                        objectField.value = go.GetComponent<UIText>();
                    }
                }
            }

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is Component comp)
                {
                    // Validate it's a text component
                    if (comp is TextMeshProUGUI || comp is UIText)
                    {
                        _node.targetPath = GetGameObjectPath(comp.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("Please select a Text or TextMeshProUGUI component");
                        objectField.value = null;
                        _node.targetPath = "";
                    }
                }
                else
                {
                    _node.targetPath = "";
                }
                MarkDirty();
            });
            objectField.style.marginBottom = 5;
            Container.Add(objectField);

            // Hint
            var hintLabel = new Label("Drag a Text or TextMeshProUGUI component");
            hintLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            hintLabel.style.fontSize = 10;
            Container.Add(hintLabel);

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

            // Text content
            CreateLabel("Content", true);

            var textField = new TextField("Text");
            textField.multiline = true;
            textField.value = _node.text ?? "";
            textField.style.minHeight = 60;
            textField.RegisterValueChangedCallback(evt =>
            {
                _node.text = evt.newValue;
                MarkDirty();
            });
            textField.style.marginBottom = 10;
            Container.Add(textField);

            CreateSeparator();

            // Typewriter effect
            CreateLabel("Effects", true);

            var typewriterToggle = new Toggle("Typewriter Effect") { value = _node.typewriterEffect };
            typewriterToggle.RegisterValueChangedCallback(evt =>
            {
                _node.typewriterEffect = evt.newValue;
                UpdateTypewriterOptions();
                MarkDirty();
            });
            typewriterToggle.style.marginBottom = 5;
            Container.Add(typewriterToggle);

            _typewriterOptions = new VisualElement();
            Container.Add(_typewriterOptions);

            UpdateTypewriterOptions();
        }

        private void UpdateTypewriterOptions()
        {
            _typewriterOptions.Clear();

            if (_node.typewriterEffect)
            {
                var speedField = new FloatField("Character Delay") { value = _node.typewriterSpeed };
                speedField.RegisterValueChangedCallback(evt =>
                {
                    _node.typewriterSpeed = evt.newValue;
                    MarkDirty();
                });
                speedField.style.marginBottom = 5;
                _typewriterOptions.Add(speedField);

                var helpLabel = new Label("Delay in seconds between each character");
                helpLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                helpLabel.style.fontSize = 10;
                _typewriterOptions.Add(helpLabel);
            }
        }
    }
}
#endif

