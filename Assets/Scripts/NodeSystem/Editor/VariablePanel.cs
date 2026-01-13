#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Panel for managing graph variables
    /// </summary>
    public class VariablePanel : VisualElement
    {
        private NodeGraph _graph;
        private VisualElement _variablesList;
        private TextField _nameField;
        private EnumField _typeField;
        private VisualElement _valueContainer;
        private VisualElement _valueField;

        public VariablePanel()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.paddingTop = 10;
            style.paddingBottom = 10;

            CreateHeader();
            CreateAddSection();
            CreateVariablesList();
        }

        public void SetGraph(NodeGraph graph)
        {
            _graph = graph;
            RefreshVariablesList();
        }

        private void CreateHeader()
        {
            var header = new Label("Graph Variables");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14;
            header.style.marginBottom = 10;
            Add(header);
        }

        private void CreateAddSection()
        {
            var addSection = new VisualElement();
            addSection.style.marginBottom = 15;
            addSection.style.paddingBottom = 10;
            addSection.style.borderBottomWidth = 1;
            addSection.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            var addLabel = new Label("Add Variable");
            addLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            addLabel.style.fontSize = 12;
            addLabel.style.marginBottom = 5;
            addSection.Add(addLabel);

            // Name field
            _nameField = new TextField("Name");
            _nameField.style.marginBottom = 5;
            addSection.Add(_nameField);

            // Type field
            _typeField = new EnumField("Type", VariableType.Bool);
            _typeField.RegisterValueChangedCallback(evt =>
            {
                UpdateValueField();
            });
            _typeField.style.marginBottom = 5;
            addSection.Add(_typeField);

            // Value container
            _valueContainer = new VisualElement();
            addSection.Add(_valueContainer);

            UpdateValueField();

            // Add button
            var addButton = new Button(() => AddVariable())
            {
                text = "Add Variable"
            };
            addButton.style.marginTop = 5;
            addSection.Add(addButton);

            Add(addSection);
        }

        private void UpdateValueField()
        {
            _valueContainer.Clear();

            var type = (VariableType)_typeField.value;
            VisualElement field = null;

            switch (type)
            {
                case VariableType.Bool:
                    var boolField = new DropdownField("Value", 
                        new List<string> { "False", "True" }, 0);
                    field = boolField;
                    break;

                case VariableType.Int:
                    var intField = new IntegerField("Value");
                    intField.value = 0;
                    field = intField;
                    break;

                case VariableType.Float:
                    var floatField = new FloatField("Value");
                    floatField.value = 0f;
                    field = floatField;
                    break;

                case VariableType.String:
                    var stringField = new TextField("Value");
                    stringField.value = "";
                    field = stringField;
                    break;
            }

            if (field != null)
            {
                field.style.marginBottom = 5;
                _valueContainer.Add(field);
                _valueField = field;
            }
        }

        private void AddVariable()
        {
            if (_graph == null) return;

            string name = _nameField.value?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Variable name cannot be empty.", "OK");
                return;
            }

            // Check if variable already exists
            if (_graph.GetVariable(name) != null)
            {
                EditorUtility.DisplayDialog("Duplicate Variable", $"Variable '{name}' already exists.", "OK");
                return;
            }

            var type = (VariableType)_typeField.value;
            GraphVariable variable = null;

            switch (type)
            {
                case VariableType.Bool:
                    if (_valueField is DropdownField dropdown)
                        variable = GraphVariable.CreateBool(name, dropdown.index == 1);
                    else
                        variable = GraphVariable.CreateBool(name, false);
                    break;

                case VariableType.Int:
                    if (_valueField is IntegerField intField)
                        variable = GraphVariable.CreateInt(name, intField.value);
                    else
                        variable = GraphVariable.CreateInt(name, 0);
                    break;

                case VariableType.Float:
                    if (_valueField is FloatField floatField)
                        variable = GraphVariable.CreateFloat(name, floatField.value);
                    else
                        variable = GraphVariable.CreateFloat(name, 0f);
                    break;

                case VariableType.String:
                    if (_valueField is TextField stringField)
                        variable = GraphVariable.CreateString(name, stringField.value);
                    else
                        variable = GraphVariable.CreateString(name, "");
                    break;
            }

            if (variable != null)
            {
                _graph.AddVariable(variable);
                RefreshVariablesList();

                // Reset form
                _nameField.value = "";
                _typeField.value = VariableType.Bool;
                UpdateValueField();
            }
        }

        private void CreateVariablesList()
        {
            var listLabel = new Label("Variables");
            listLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            listLabel.style.fontSize = 12;
            listLabel.style.marginTop = 10;
            listLabel.style.marginBottom = 5;
            Add(listLabel);

            _variablesList = new VisualElement();
            _variablesList.style.flexGrow = 1;
            Add(_variablesList);
        }

        private void RefreshVariablesList()
        {
            _variablesList.Clear();

            if (_graph == null)
            {
                var emptyLabel = new Label("No graph loaded");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                _variablesList.Add(emptyLabel);
                return;
            }

            var variables = _graph.Variables;
            if (variables.Count == 0)
            {
                var emptyLabel = new Label("No variables defined");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.fontSize = 11;
                _variablesList.Add(emptyLabel);
                return;
            }

            foreach (var variable in variables)
            {
                CreateVariableItem(variable);
            }
        }

        private void CreateVariableItem(GraphVariable variable)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginBottom = 5;
            item.style.paddingTop = 5;
            item.style.paddingBottom = 5;
            item.style.paddingLeft = 5;
            item.style.paddingRight = 5;
            item.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            item.style.borderTopLeftRadius = 3;
            item.style.borderTopRightRadius = 3;
            item.style.borderBottomLeftRadius = 3;
            item.style.borderBottomRightRadius = 3;

            // Name and type
            var infoContainer = new VisualElement();
            infoContainer.style.flexGrow = 1;

            var nameLabel = new Label(variable.Name);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 12;
            infoContainer.Add(nameLabel);

            var typeLabel = new Label($"{variable.Type}: {GetValueDisplay(variable)}");
            typeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            typeLabel.style.fontSize = 10;
            infoContainer.Add(typeLabel);

            item.Add(infoContainer);

            // Edit button
            var editButton = new Button(() => EditVariable(variable))
            {
                text = "Edit"
            };
            editButton.style.marginLeft = 5;
            editButton.style.minWidth = 50;
            item.Add(editButton);

            // Delete button
            var deleteButton = new Button(() => DeleteVariable(variable))
            {
                text = "Delete"
            };
            deleteButton.style.marginLeft = 5;
            deleteButton.style.minWidth = 50;
            deleteButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            item.Add(deleteButton);

            _variablesList.Add(item);
        }

        private string GetValueDisplay(GraphVariable variable)
        {
            switch (variable.Type)
            {
                case VariableType.Bool:
                    return variable.GetBoolValue().ToString();
                case VariableType.Int:
                    return variable.GetIntValue().ToString();
                case VariableType.Float:
                    return variable.GetFloatValue().ToString("F2");
                case VariableType.String:
                    string val = variable.GetStringValue();
                    return string.IsNullOrEmpty(val) ? "\"\"" : $"\"{val}\"";
                default:
                    return variable.Value;
            }
        }

        private void EditVariable(GraphVariable variable)
        {
            // Simple edit dialog
            var dialog = EditorUtility.DisplayDialogComplex(
                "Edit Variable",
                $"Edit variable: {variable.Name}\nType: {variable.Type}\nCurrent Value: {GetValueDisplay(variable)}",
                "OK",
                "Cancel",
                "Delete"
            );

            if (dialog == 0) // OK - could implement inline editing here
            {
                // For now, just refresh
                RefreshVariablesList();
            }
            else if (dialog == 2) // Delete
            {
                DeleteVariable(variable);
            }
        }

        private void DeleteVariable(GraphVariable variable)
        {
            if (EditorUtility.DisplayDialog(
                "Delete Variable",
                $"Are you sure you want to delete variable '{variable.Name}'?",
                "Delete",
                "Cancel"))
            {
                _graph.RemoveVariable(variable);
                RefreshVariablesList();
            }
        }
    }
}
#endif

