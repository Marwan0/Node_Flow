#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
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

            // Delay slider
            var slider = new Slider("Seconds", 0f, 10f) { value = _node.delaySeconds };
            slider.RegisterValueChangedCallback(evt =>
            {
                _node.delaySeconds = evt.newValue;
                MarkDirty();
            });
            slider.style.marginBottom = 5;
            Container.Add(slider);

            // Precise input
            var floatField = new FloatField("Precise Value") { value = _node.delaySeconds };
            floatField.RegisterValueChangedCallback(evt =>
            {
                _node.delaySeconds = Mathf.Max(0, evt.newValue);
                slider.value = _node.delaySeconds;
                MarkDirty();
            });
            floatField.style.marginBottom = 10;
            Container.Add(floatField);

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

