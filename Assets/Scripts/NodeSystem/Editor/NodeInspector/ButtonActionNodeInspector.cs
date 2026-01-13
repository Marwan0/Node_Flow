#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeSystem.Nodes;
using UIButton = UnityEngine.UI.Button;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for ButtonActionNode with Button picker
    /// </summary>
    public class ButtonActionNodeInspector : NodeInspectorBase
    {
        private ButtonActionNode _node;

        public override void DrawInspector()
        {
            _node = Node as ButtonActionNode;
            if (_node == null) return;

            // Button picker
            CreateLabel("Wait For Button", true);
            
            var objectField = new ObjectField("Button")
            {
                objectType = typeof(UIButton),
                allowSceneObjects = true
            };

            if (!string.IsNullOrEmpty(_node.buttonPath))
            {
                var go = GameObject.Find(_node.buttonPath);
                if (go != null)
                {
                    objectField.value = go.GetComponent<UIButton>();
                }
            }

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is UIButton button)
                {
                    _node.buttonPath = GetGameObjectPath(button.gameObject);
                }
                else
                {
                    _node.buttonPath = "";
                }
                MarkDirty();
            });
            objectField.style.marginBottom = 5;
            Container.Add(objectField);

            // Show path
            if (!string.IsNullOrEmpty(_node.buttonPath))
            {
                var pathLabel = new Label($"Path: {_node.buttonPath}");
                pathLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                pathLabel.style.fontSize = 10;
                pathLabel.style.marginBottom = 10;
                Container.Add(pathLabel);
            }

            CreateSeparator();

            // Options
            CreateLabel("Options", true);
            CreateToggle("Disable After Click", _node.disableAfterClick, v => _node.disableAfterClick = v);

            // Help
            var helpLabel = new Label("Node will wait until the button is clicked before proceeding");
            helpLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            helpLabel.style.fontSize = 10;
            helpLabel.style.marginTop = 10;
            helpLabel.style.whiteSpace = WhiteSpace.Normal;
            Container.Add(helpLabel);
        }
    }
}
#endif

