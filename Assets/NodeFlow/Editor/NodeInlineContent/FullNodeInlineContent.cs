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
                if (field.Name == "hasBreakpoint" || field.Name == "displayLabel") continue;
                if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0) continue;

                var value = field.GetValue(Node);
                var fieldType = field.FieldType;

                DrawField(field, value, fieldType);
            }
        }

        private void DrawField(FieldInfo field, object value, Type fieldType)
        {
            string label = FormatLabel(field.Name);

            if (fieldType == typeof(StringIdSelector))
            {
                var selector = (StringIdSelector)value;
                if (selector != null)
                {
                    CreateStringIdSelector(label, selector);
                }
                return;
            }

            if (fieldType == typeof(string))
            {
                var variableAttr = field.GetCustomAttribute<GraphVariableAttribute>();
                if (variableAttr != null)
                {
                    CreateVariableSelector(label, (string)value, newValue => field.SetValue(Node, newValue), variableAttr.AllowCreation);
                }
                else
                {
                    DrawStringField(label, field, (string)value);
                }
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
            labelElem.style.minWidth = 120;
            labelElem.style.maxWidth = 120;
            labelElem.style.marginRight = 5;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var textField = new TextField() { value = value ?? "" };
            if (IsLongTextField(field.Name, label))
            {
                textField.multiline = true;
                textField.style.minHeight = 48;
                textField.style.whiteSpace = WhiteSpace.Normal;
            }
            textField.style.flexGrow = 1;
            textField.tooltip = textField.value;
            textField.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(Node, evt.newValue);
                textField.tooltip = evt.newValue ?? string.Empty;
                MarkDirty();
            });
            row.Add(textField);

            Container.Add(row);
        }

        private bool IsLongTextField(string fieldName, string label)
        {
            string name = (fieldName ?? string.Empty).ToLowerInvariant();
            string title = (label ?? string.Empty).ToLowerInvariant();
            return name.Contains("message") || name.Contains("text") || title.Contains("message") || title.Contains("text");
        }

        private void DrawFloatField(string label, FieldInfo field, float value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 120;
            labelElem.style.maxWidth = 120;
            labelElem.style.marginRight = 5;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            // Use slider for common ranges
            if (label.ToLower().Contains("duration") || label.ToLower().Contains("delay") || 
                label.ToLower().Contains("time") || label.ToLower().Contains("second"))
            {
                AddSliderWithField(row, value, 0f, 5f, v => { field.SetValue(Node, v); MarkDirty(); });
            }
            else if (label.ToLower().Contains("volume"))
            {
                AddSliderWithField(row, value, 0f, 1f, v => { field.SetValue(Node, v); MarkDirty(); });
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

        private void AddSliderWithField(VisualElement row, float value, float min, float max, Action<float> onChanged)
        {
            var slider = new Slider(min, max) { value = value };
            slider.style.flexGrow = 1;
            slider.style.minWidth = 60;
            row.Add(slider);

            var valueField = new FloatField() { value = value };
            valueField.style.width = 50;
            valueField.style.minWidth = 40;
            valueField.style.fontSize = 10;
            row.Add(valueField);

            bool isSyncing = false;
            slider.RegisterValueChangedCallback(evt =>
            {
                if (isSyncing) return;
                isSyncing = true;
                valueField.SetValueWithoutNotify(evt.newValue);
                onChanged(evt.newValue);
                isSyncing = false;
            });
            valueField.RegisterValueChangedCallback(evt =>
            {
                if (isSyncing) return;
                isSyncing = true;
                float clamped = Mathf.Clamp(evt.newValue, min, max);
                slider.SetValueWithoutNotify(clamped);
                valueField.SetValueWithoutNotify(clamped);
                onChanged(clamped);
                isSyncing = false;
            });
        }

        private void DrawIntField(string label, FieldInfo field, int value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 120;
            labelElem.style.maxWidth = 120;
            labelElem.style.marginRight = 5;
            labelElem.style.color = new Color(0.75f, 0.75f, 0.75f);
            labelElem.style.fontSize = 11;
            row.Add(labelElem);

            var intField = new IntegerField() { value = value };
            intField.style.flexGrow = 1;
            intField.RegisterValueChangedCallback(evt =>
            {
                // Clamp if field has a Range attribute
                int newValue = evt.newValue;
                var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null)
                    newValue = Mathf.Clamp(newValue, (int)rangeAttr.min, (int)rangeAttr.max);

                field.SetValue(Node, newValue);
                if (newValue != evt.newValue)
                    intField.SetValueWithoutNotify(newValue);
                MarkDirty();
                // Refresh ports + content in case this field drives dynamic ports
                RequestRefresh();
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
            labelElem.style.minWidth = 120;
            labelElem.style.maxWidth = 120;
            labelElem.style.marginRight = 5;
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
            labelElem.style.minWidth = 120;
            labelElem.style.maxWidth = 120;
            labelElem.style.marginRight = 5;
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
