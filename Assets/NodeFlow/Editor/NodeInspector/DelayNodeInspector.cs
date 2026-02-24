#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for DelayNode with slider
    /// </summary>
    public class DelayNodeInspector : NodeInspectorBase
    {
        private DelayNode _node;

        public override void DrawInspector()
        {
            _node = Node as DelayNode;
            if (_node == null) return;

            CreateLabel("Delay Settings", true);

            // Slider row with editable value field
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 5;

            var label = new Label("Seconds");
            label.style.minWidth = 60;
            row.Add(label);

            var slider = new Slider(0f, 10f) { value = _node.delaySeconds };
            slider.style.flexGrow = 1;
            row.Add(slider);

            var valueField = new FloatField() { value = _node.delaySeconds };
            valueField.style.width = 50;
            valueField.style.minWidth = 40;
            row.Add(valueField);

            bool isSyncing = false;
            slider.RegisterValueChangedCallback(evt =>
            {
                if (isSyncing) return;
                isSyncing = true;
                _node.delaySeconds = evt.newValue;
                valueField.SetValueWithoutNotify(evt.newValue);
                MarkDirty();
                isSyncing = false;
            });
            valueField.RegisterValueChangedCallback(evt =>
            {
                if (isSyncing) return;
                isSyncing = true;
                float clamped = Mathf.Clamp(evt.newValue, 0f, 10f);
                _node.delaySeconds = clamped;
                slider.SetValueWithoutNotify(clamped);
                valueField.SetValueWithoutNotify(clamped);
                MarkDirty();
                isSyncing = false;
            });

            Container.Add(row);

            // Preview
            var previewLabel = new Label($"Will wait {_node.delaySeconds:F2} seconds before continuing");
            previewLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            previewLabel.style.fontSize = 11;
            previewLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            Container.Add(previewLabel);
        }
    }
}
#endif

