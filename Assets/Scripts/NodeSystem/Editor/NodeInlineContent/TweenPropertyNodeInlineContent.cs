#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class TweenPropertyNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as TweenPropertyNode;
            if (node == null) return;

            // Target path
            CreateTextField(node.targetPath, v => node.targetPath = v, "Target GameObject path...");

            // Property type
            CreateEnumField("Property", node.propertyType, (PropertyType v) => 
            {
                node.propertyType = v;
                MarkDirty();
                RequestRefresh();
            });

            // Target value - display depends on property type
            switch (node.propertyType)
            {
                case PropertyType.Alpha:
                    // Alpha is just X component
                    CreateSlider("Alpha", node.targetValue.x, 0f, 1f, v => 
                        node.targetValue = new Vector3(v, node.targetValue.y, node.targetValue.z));
                    break;
                    
                case PropertyType.Color:
                    // RGB values (0-1)
                    CreateSlider("R", node.targetValue.x, 0f, 1f, v => 
                        node.targetValue = new Vector3(v, node.targetValue.y, node.targetValue.z));
                    CreateSlider("G", node.targetValue.y, 0f, 1f, v => 
                        node.targetValue = new Vector3(node.targetValue.x, v, node.targetValue.z));
                    CreateSlider("B", node.targetValue.z, 0f, 1f, v => 
                        node.targetValue = new Vector3(node.targetValue.x, node.targetValue.y, v));
                    break;
                    
                default:
                    // Position, Rotation, Scale - Vector3
                    CreateVector3Field("Target", node.targetValue, v => node.targetValue = v);
                    break;
            }

            // Duration
            CreateFloatField("Duration", node.duration, v => node.duration = Mathf.Max(0.01f, v));

            // Delay
            CreateFloatField("Delay", node.delay, v => node.delay = Mathf.Max(0, v));
        }

        private void CreateVector3Field(string label, Vector3 value, System.Action<Vector3> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 50;
            labelElem.style.color = new Color(0.8f, 0.8f, 0.8f);
            labelElem.style.fontSize = 10;
            row.Add(labelElem);

            // X
            var xField = new FloatField() { value = value.x };
            xField.style.flexGrow = 1;
            xField.style.minWidth = 30;
            xField.RegisterValueChangedCallback(evt => 
            {
                onChanged(new Vector3(evt.newValue, value.y, value.z));
                MarkDirty();
            });
            row.Add(xField);

            // Y
            var yField = new FloatField() { value = value.y };
            yField.style.flexGrow = 1;
            yField.style.minWidth = 30;
            yField.RegisterValueChangedCallback(evt => 
            {
                onChanged(new Vector3(value.x, evt.newValue, value.z));
                MarkDirty();
            });
            row.Add(yField);

            // Z
            var zField = new FloatField() { value = value.z };
            zField.style.flexGrow = 1;
            zField.style.minWidth = 30;
            zField.RegisterValueChangedCallback(evt => 
            {
                onChanged(new Vector3(value.x, value.y, evt.newValue));
                MarkDirty();
            });
            row.Add(zField);

            Container.Add(row);
        }
    }
}
#endif

