#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Default inspector that uses reflection (fallback for nodes without custom inspector)
    /// </summary>
    public class DefaultNodeInspector : NodeInspectorBase
    {
        public override void DrawInspector()
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
            if (fieldType == typeof(string))
            {
                CreateTextField(FormatLabel(field.Name), (string)value ?? "", v => field.SetValue(Node, v));
            }
            else if (fieldType == typeof(float))
            {
                CreateFloatField(FormatLabel(field.Name), (float)value, v => field.SetValue(Node, v));
            }
            else if (fieldType == typeof(int))
            {
                CreateIntField(FormatLabel(field.Name), (int)value, v => field.SetValue(Node, v));
            }
            else if (fieldType == typeof(bool))
            {
                CreateToggle(FormatLabel(field.Name), (bool)value, v => field.SetValue(Node, v));
            }
            else if (fieldType.IsEnum)
            {
                var enumField = new EnumField(FormatLabel(field.Name), (Enum)value);
                enumField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(Node, evt.newValue);
                    MarkDirty();
                });
                enumField.style.marginBottom = 5;
                Container.Add(enumField);
            }
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

