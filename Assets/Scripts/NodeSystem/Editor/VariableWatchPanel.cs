#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Panel for watching variable values in real-time during execution
    /// </summary>
    public class VariableWatchPanel : VisualElement
    {
        private NodeGraph _graph;
        private VisualElement _variablesList;
        private Dictionary<string, Label> _valueLabels = new Dictionary<string, Label>();

        public VariableWatchPanel()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.paddingTop = 10;
            style.paddingBottom = 10;

            CreateHeader();
            CreateVariablesList();

            // Subscribe to update events
            EditorApplication.update += UpdateValues;
        }

        ~VariableWatchPanel()
        {
            EditorApplication.update -= UpdateValues;
        }

        public void SetGraph(NodeGraph graph)
        {
            _graph = graph;
            RefreshVariablesList();
        }

        private void CreateHeader()
        {
            var header = new Label("Variable Watch");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14;
            header.style.marginBottom = 10;
            Add(header);
        }

        private void CreateVariablesList()
        {
            _variablesList = new VisualElement();
            _variablesList.style.flexGrow = 1;
            Add(_variablesList);
        }

        private void RefreshVariablesList()
        {
            _variablesList.Clear();
            _valueLabels.Clear();

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
                CreateVariableWatchItem(variable);
            }
        }

        private void CreateVariableWatchItem(GraphVariable variable)
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

            var typeLabel = new Label($"{variable.Type}");
            typeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            typeLabel.style.fontSize = 10;
            infoContainer.Add(typeLabel);

            item.Add(infoContainer);

            // Value label (will be updated)
            var valueLabel = new Label(GetValueDisplay(variable));
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.fontSize = 12;
            valueLabel.style.color = new Color(0.5f, 0.8f, 1f);
            valueLabel.style.minWidth = 100;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            item.Add(valueLabel);

            _valueLabels[variable.Name] = valueLabel;
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

        private void UpdateValues()
        {
            if (_graph == null) return;

            foreach (var variable in _graph.Variables)
            {
                if (_valueLabels.TryGetValue(variable.Name, out var label))
                {
                    label.text = GetValueDisplay(variable);
                }
            }
        }
    }
}
#endif

