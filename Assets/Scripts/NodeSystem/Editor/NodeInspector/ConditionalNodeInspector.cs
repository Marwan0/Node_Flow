#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for ConditionalNode with adaptive fields
    /// </summary>
    public class ConditionalNodeInspector : NodeInspectorBase
    {
        private ConditionalNode _node;
        private VisualElement _fieldsContainer;

        public override void DrawInspector()
        {
            _node = Node as ConditionalNode;
            if (_node == null) return;

            // Condition Type (dropdown)
            CreateEnumField("Condition Type", _node.conditionType, (ConditionType v) =>
            {
                _node.conditionType = v;
                RedrawFields();
            });

            // Dynamic fields container
            _fieldsContainer = new VisualElement();
            Container.Add(_fieldsContainer);

            RedrawFields();
        }

        private void RedrawFields()
        {
            _fieldsContainer.Clear();

            switch (_node.conditionType)
            {
                case ConditionType.BoolVariable:
                    DrawBoolVariableFields();
                    break;

                case ConditionType.IntComparison:
                case ConditionType.FloatComparison:
                    DrawNumericComparisonFields();
                    break;

                case ConditionType.StringEquals:
                    DrawStringComparisonFields();
                    break;

                case ConditionType.GameObjectExists:
                case ConditionType.GameObjectActive:
                    DrawGameObjectFields();
                    break;
            }
        }

        private void DrawBoolVariableFields()
        {
            // Variable name
            var nameField = new TextField("Variable Name") { value = _node.variableName ?? "" };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _node.variableName = evt.newValue;
                MarkDirty();
            });
            nameField.style.marginBottom = 5;
            _fieldsContainer.Add(nameField);

            // Comparison (Equals / Not Equals only for bool)
            var comparisonChoices = new System.Collections.Generic.List<string> { "Equals", "Not Equals" };
            int compIdx = _node.comparison == ComparisonOperator.Equals ? 0 : 1;
            
            var compField = new DropdownField("Check", comparisonChoices, compIdx);
            compField.RegisterValueChangedCallback(evt =>
            {
                _node.comparison = evt.newValue == "Equals" ? ComparisonOperator.Equals : ComparisonOperator.NotEquals;
                MarkDirty();
            });
            compField.style.marginBottom = 5;
            _fieldsContainer.Add(compField);

            // Value (True/False dropdown)
            bool currentVal = _node.compareValue?.ToLower() == "true" || _node.compareValue == "1";
            var valueField = new DropdownField("Value",
                new System.Collections.Generic.List<string> { "True", "False" },
                currentVal ? 0 : 1);
            valueField.RegisterValueChangedCallback(evt =>
            {
                _node.compareValue = evt.newValue.ToLower();
                MarkDirty();
            });
            valueField.style.marginBottom = 5;
            _fieldsContainer.Add(valueField);

            // Help text
            var helpLabel = new Label("Checks: Is [Variable] [Equals/Not Equals] [True/False]?");
            helpLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpLabel.style.fontSize = 10;
            helpLabel.style.marginTop = 5;
            _fieldsContainer.Add(helpLabel);
        }

        private void DrawNumericComparisonFields()
        {
            // Variable name
            var nameField = new TextField("Variable Name") { value = _node.variableName ?? "" };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _node.variableName = evt.newValue;
                MarkDirty();
            });
            nameField.style.marginBottom = 5;
            _fieldsContainer.Add(nameField);

            // Comparison operator (full list)
            var compField = new EnumField("Comparison", _node.comparison);
            compField.RegisterValueChangedCallback(evt =>
            {
                _node.comparison = (ComparisonOperator)evt.newValue;
                MarkDirty();
            });
            compField.style.marginBottom = 5;
            _fieldsContainer.Add(compField);

            // Value
            if (_node.conditionType == ConditionType.IntComparison)
            {
                int.TryParse(_node.compareValue, out int currentVal);
                var valueField = new IntegerField("Compare To") { value = currentVal };
                valueField.RegisterValueChangedCallback(evt =>
                {
                    _node.compareValue = evt.newValue.ToString();
                    MarkDirty();
                });
                valueField.style.marginBottom = 5;
                _fieldsContainer.Add(valueField);
            }
            else
            {
                float.TryParse(_node.compareValue, out float currentVal);
                var valueField = new FloatField("Compare To") { value = currentVal };
                valueField.RegisterValueChangedCallback(evt =>
                {
                    _node.compareValue = evt.newValue.ToString();
                    MarkDirty();
                });
                valueField.style.marginBottom = 5;
                _fieldsContainer.Add(valueField);
            }

            // Help text
            var helpLabel = new Label("Checks: Is [Variable] [Comparison] [Value]?");
            helpLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpLabel.style.fontSize = 10;
            helpLabel.style.marginTop = 5;
            _fieldsContainer.Add(helpLabel);
        }

        private void DrawStringComparisonFields()
        {
            // Variable name
            var nameField = new TextField("Variable Name") { value = _node.variableName ?? "" };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _node.variableName = evt.newValue;
                MarkDirty();
            });
            nameField.style.marginBottom = 5;
            _fieldsContainer.Add(nameField);

            // Comparison (Equals / Not Equals)
            var comparisonChoices = new System.Collections.Generic.List<string> { "Equals", "Not Equals" };
            int compIdx = _node.comparison == ComparisonOperator.Equals ? 0 : 1;

            var compField = new DropdownField("Check", comparisonChoices, compIdx);
            compField.RegisterValueChangedCallback(evt =>
            {
                _node.comparison = evt.newValue == "Equals" ? ComparisonOperator.Equals : ComparisonOperator.NotEquals;
                MarkDirty();
            });
            compField.style.marginBottom = 5;
            _fieldsContainer.Add(compField);

            // Value
            var valueField = new TextField("Compare To") { value = _node.compareValue ?? "" };
            valueField.RegisterValueChangedCallback(evt =>
            {
                _node.compareValue = evt.newValue;
                MarkDirty();
            });
            valueField.style.marginBottom = 5;
            _fieldsContainer.Add(valueField);
        }

        private void DrawGameObjectFields()
        {
            // Object picker
            var objectField = new ObjectField("Target GameObject")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true
            };

            // Try to find current object
            if (!string.IsNullOrEmpty(_node.gameObjectPath))
            {
                objectField.value = GameObject.Find(_node.gameObjectPath);
            }

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is GameObject go)
                {
                    _node.gameObjectPath = GetGameObjectPath(go);
                }
                else
                {
                    _node.gameObjectPath = "";
                }
                MarkDirty();
            });
            objectField.style.marginBottom = 5;
            _fieldsContainer.Add(objectField);

            // Show the path (read-only)
            if (!string.IsNullOrEmpty(_node.gameObjectPath))
            {
                var pathLabel = new Label($"Path: {_node.gameObjectPath}");
                pathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                pathLabel.style.fontSize = 10;
                _fieldsContainer.Add(pathLabel);
            }

            // Help text
            string helpText = _node.conditionType == ConditionType.GameObjectExists
                ? "Checks if the GameObject exists in the scene"
                : "Checks if the GameObject is active";
            
            var helpLabel = new Label(helpText);
            helpLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpLabel.style.fontSize = 10;
            helpLabel.style.marginTop = 5;
            _fieldsContainer.Add(helpLabel);
        }
    }
}
#endif

