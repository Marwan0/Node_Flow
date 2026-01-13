#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Full property editor for inline node content
    /// Shows all serialized properties inside the node
    /// </summary>
    public class FullNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var nodeType = Node.GetType();
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Skip internal fields
                if (field.Name.StartsWith("_")) continue;
                if (field.Name == "Guid" || field.Name == "Position") continue;
                if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0) continue;

                var value = field.GetValue(Node);
                var fieldType = field.FieldType;

                DrawField(field, value, fieldType);
            }
        }

        private void DrawField(FieldInfo field, object value, Type fieldType)
        {
            string label = FormatLabel(field.Name);

            if (fieldType == typeof(string))
            {
                DrawStringField(label, field, (string)value);
            }
            else if (fieldType == typeof(float))
            {
                DrawFloatField(label, field, (float)value);
            }
            else if (fieldType == typeof(int))
            {
                DrawIntField(label, field, (int)value);
            }
            else if (fieldType == typeof(bool))
            {
                DrawBoolField(label, field, (bool)value);
            }
            else if (fieldType.IsEnum)
            {
                DrawEnumField(label, field, (Enum)value);
            }
        }

        private void DrawStringField(string label, FieldInfo field, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 70;
            labelElem.style.maxWidth = 70;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var textField = new TextField() { value = value ?? "" };
            textField.style.flexGrow = 1;
            textField.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(Node, evt.newValue);
                MarkDirty();
            });
            row.Add(textField);

            Container.Add(row);
        }

        private void DrawFloatField(string label, FieldInfo field, float value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 70;
            labelElem.style.maxWidth = 70;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            // Use slider for common ranges
            if (label.ToLower().Contains("duration") || label.ToLower().Contains("delay") || 
                label.ToLower().Contains("time") || label.ToLower().Contains("second"))
            {
                var slider = new Slider(0, 5) { value = value };
                slider.style.flexGrow = 1;
                slider.style.minWidth = 60;
                slider.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(Node, evt.newValue);
                    MarkDirty();
                });
                row.Add(slider);

                var valLabel = new Label(value.ToString("F1"));
                valLabel.style.minWidth = 30;
                valLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                valLabel.style.fontSize = 10;
                slider.RegisterValueChangedCallback(evt => valLabel.text = evt.newValue.ToString("F1"));
                row.Add(valLabel);
            }
            else if (label.ToLower().Contains("volume"))
            {
                var slider = new Slider(0, 1) { value = value };
                slider.style.flexGrow = 1;
                slider.style.minWidth = 60;
                slider.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(Node, evt.newValue);
                    MarkDirty();
                });
                row.Add(slider);

                var valLabel = new Label(value.ToString("F2"));
                valLabel.style.minWidth = 30;
                valLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                valLabel.style.fontSize = 10;
                slider.RegisterValueChangedCallback(evt => valLabel.text = evt.newValue.ToString("F2"));
                row.Add(valLabel);
            }
            else
            {
                var floatField = new FloatField() { value = value };
                floatField.style.flexGrow = 1;
                floatField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(Node, evt.newValue);
                    MarkDirty();
                });
                row.Add(floatField);
            }

            Container.Add(row);
        }

        private void DrawIntField(string label, FieldInfo field, int value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 70;
            labelElem.style.maxWidth = 70;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var intField = new IntegerField() { value = value };
            intField.style.flexGrow = 1;
            intField.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(Node, evt.newValue);
                MarkDirty();
            });
            row.Add(intField);

            Container.Add(row);
        }

        private void DrawBoolField(string label, FieldInfo field, bool value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 70;
            labelElem.style.maxWidth = 70;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var toggle = new Toggle() { value = value };
            toggle.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(Node, evt.newValue);
                MarkDirty();
            });
            row.Add(toggle);

            Container.Add(row);
        }

        private void DrawEnumField(string label, FieldInfo field, Enum value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 70;
            labelElem.style.maxWidth = 70;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var enumField = new EnumField(value);
            enumField.style.flexGrow = 1;
            enumField.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(Node, evt.newValue);
                MarkDirty();
                // Request refresh in case enum change affects other fields
                RequestRefresh();
            });
            row.Add(enumField);

            Container.Add(row);
        }

        private string FormatLabel(string fieldName)
        {
            // Convert camelCase to Title Case with spaces
            var result = System.Text.RegularExpressions.Regex.Replace(fieldName, "([a-z])([A-Z])", "$1 $2");
            return char.ToUpper(result[0]) + result.Substring(1);
        }
    }
}
#endif

