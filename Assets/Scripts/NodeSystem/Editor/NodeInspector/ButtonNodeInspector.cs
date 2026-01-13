#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeSystem.Nodes;
using UIButton = UnityEngine.UI.Button;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for ButtonActivationNode with Button picker
    /// </summary>
    public class ButtonNodeInspector : NodeInspectorBase
    {
        private ButtonActivationNode _node;

        public override void DrawInspector()
        {
            _node = Node as ButtonActivationNode;
            if (_node == null) return;

            // Button picker
            CreateLabel("Target Button", true);
            
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

            // Action
            CreateLabel("Action", true);

            var actionField = new DropdownField("Set Interactable",
                new System.Collections.Generic.List<string> { "Enable", "Disable" },
                _node.setInteractable ? 0 : 1);

            actionField.RegisterValueChangedCallback(evt =>
            {
                _node.setInteractable = evt.newValue == "Enable";
                MarkDirty();
            });
            actionField.style.marginBottom = 5;
            Container.Add(actionField);
        }
    }
}
#endif

