#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    public class EditVariableWindow : EditorWindow
    {
        private GraphVariable _variable;
        private NodeGraph _graph;
        private System.Action _onSave;

        public static void Show(GraphVariable variable, NodeGraph graph, System.Action onSave)
        {
            var window = GetWindow<EditVariableWindow>(true, "Edit Variable", true);
            window._variable = variable;
            window._graph = graph;
            window._onSave = onSave;
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(400, 250);
            window.Refresh(); // Ensure GUI is built with data
            window.ShowUtility();
        }

        public void Refresh()
        {
            rootVisualElement.Clear();
            CreateGUI();
        }

        private void CreateGUI()
        {
            if (_variable == null)
            {
                Close();
                return;
            }

            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

            // Title
            var title = new Label($"Editing: {_variable.Name}");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 15;
            root.Add(title);

            // Name Field
            var nameField = new TextField("Name");
            nameField.value = _variable.Name;
            nameField.RegisterValueChangedCallback(evt => 
            {
                // Validate unique name?
                // For now just update, but we should probably validate on Save
            });
            root.Add(nameField);

            // Type Field (Read-only)
            var typeRow = new VisualElement();
            typeRow.style.flexDirection = FlexDirection.Row;
            typeRow.style.marginTop = 5;
            typeRow.style.marginBottom = 5;
            var typeLabel = new Label("Type:");
            typeLabel.style.width = 120; // Match standard label width
            var typeValue = new Label(_variable.Type.ToString());
            typeValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            typeValue.style.color = new Color(0.7f, 0.7f, 0.7f);
            typeRow.Add(typeLabel);
            typeRow.Add(typeValue);
            root.Add(typeRow);

            // Value Field
            VisualElement valueField = null;
            switch (_variable.Type)
            {
                case VariableType.Bool:
                    var boolField = new Toggle("Value");
                    boolField.value = _variable.GetBoolValue();
                    boolField.RegisterValueChangedCallback(evt => _variable.SetBoolValue(evt.newValue));
                    valueField = boolField;
                    break;
                case VariableType.Int:
                    var intField = new IntegerField("Value");
                    intField.value = _variable.GetIntValue();
                    intField.RegisterValueChangedCallback(evt => _variable.SetIntValue(evt.newValue));
                    valueField = intField;
                    break;
                case VariableType.Float:
                    var floatField = new FloatField("Value");
                    floatField.value = _variable.GetFloatValue();
                    floatField.RegisterValueChangedCallback(evt => _variable.SetFloatValue(evt.newValue));
                    valueField = floatField;
                    break;
                case VariableType.String:
                    var stringField = new TextField("Value");
                    stringField.value = _variable.GetStringValue();
                    stringField.RegisterValueChangedCallback(evt => _variable.SetStringValue(evt.newValue));
                    valueField = stringField;
                    break;
            }

            if (valueField != null)
            {
                valueField.style.marginTop = 5;
                root.Add(valueField);
            }

            // Buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;
            btnRow.style.marginTop = 20;

            var cancelBtn = new Button(() => Close()) { text = "Cancel" };
            btnRow.Add(cancelBtn);

            var saveBtn = new Button(() => 
            {
                // Save Name
                string newName = nameField.value.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != _variable.Name)
                {
                    // Check duplicate
                    if (_graph.GetVariable(newName) != null)
                    {
                        EditorUtility.DisplayDialog("Error", "Variable name already exists!", "OK");
                        return;
                    }
                    _variable.Name = newName;
                }
                
                // Value is updated via callbacks already
                
                _onSave?.Invoke();
                Close();
            }) { text = "Save" };
            saveBtn.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
            btnRow.Add(saveBtn);

            root.Add(btnRow);
        }
    }
}
#endif
