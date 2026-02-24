#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for SetVariableNode with adaptive value field
    /// </summary>
    public class SetVariableNodeInspector : NodeInspectorBase
    {
        private SetVariableNode _node;
        private VisualElement _valueContainer;

        public override void DrawInspector()
        {
            _node = Node as SetVariableNode;
            if (_node == null) return;

            // Variable Name
            CreateTextField("Variable Name", _node.variableName, v => _node.variableName = v);

            // Variable Type (dropdown)
            var typeField = CreateEnumField("Type", _node.variableType, (VariableType v) =>
            {
                _node.variableType = v;
                RedrawValueField();
            });

            // Value container (will be redrawn based on type)
            _valueContainer = new VisualElement();
            Container.Add(_valueContainer);

            RedrawValueField();
        }

        private void RedrawValueField()
        {
            _valueContainer.Clear();

            switch (_node.variableType)
            {
                case VariableType.Bool:
                    DrawBoolValue();
                    break;

                case VariableType.Int:
                    DrawIntValue();
                    break;

                case VariableType.Float:
                    DrawFloatValue();
                    break;

                case VariableType.String:
                    DrawStringValue();
                    break;
            }
        }

        private void DrawBoolValue()
        {
            // Parse current value
            bool currentValue = _node.value?.ToLower() == "true" || _node.value == "1";

            var dropdown = new DropdownField("Value", 
                new System.Collections.Generic.List<string> { "True", "False" }, 
                currentValue ? 0 : 1);

            dropdown.RegisterValueChangedCallback(evt =>
            {
                _node.value = evt.newValue.ToLower();
                MarkDirty();
            });

            dropdown.style.marginBottom = 5;
            _valueContainer.Add(dropdown);
        }

        private void DrawIntValue()
        {
            int.TryParse(_node.value, out int currentValue);

            var field = new IntegerField("Value") { value = currentValue };
            field.RegisterValueChangedCallback(evt =>
            {
                _node.value = evt.newValue.ToString();
                MarkDirty();
            });

            field.style.marginBottom = 5;
            _valueContainer.Add(field);
        }

        private void DrawFloatValue()
        {
            float.TryParse(_node.value, out float currentValue);

            var field = new FloatField("Value") { value = currentValue };
            field.RegisterValueChangedCallback(evt =>
            {
                _node.value = evt.newValue.ToString();
                MarkDirty();
            });

            field.style.marginBottom = 5;
            _valueContainer.Add(field);
        }

        private void DrawStringValue()
        {
            var field = new TextField("Value") { value = _node.value ?? "" };
            field.RegisterValueChangedCallback(evt =>
            {
                _node.value = evt.newValue;
                MarkDirty();
            });

            field.style.marginBottom = 5;
            _valueContainer.Add(field);
        }
    }
}
#endif

