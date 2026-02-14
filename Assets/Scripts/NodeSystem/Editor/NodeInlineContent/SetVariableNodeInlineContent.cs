#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SetVariableNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SetVariableNode;
            if (node == null) return;

            CreateVariableSelector("Variable name...", node.variableName, v => 
            {
                node.variableName = v;
                RequestRefresh(); // Refresh UI to match new variable's type
            }, true);
            // Try to find existing variable to enforce type
            var graph = GetGraph();
            GraphVariable existingVar = null;
            if (graph != null)
            {
                existingVar = graph.GetVariable(node.variableName);
            }

            if (existingVar != null)
            {
                // Enforce type match
                if (node.variableType != existingVar.Type)
                {
                    node.variableType = existingVar.Type;
                    MarkDirty();
                }
                
                // Show locked type label
                var row = new UnityEngine.UIElements.VisualElement();
                row.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
                row.style.marginBottom = 2;
                var label = new UnityEngine.UIElements.Label($"Type: {node.variableType}");
                label.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
                label.style.fontSize = 11;
                row.Add(label);
                Container.Add(row);
                
                // Ensure value field is updated
                // RequestRefresh(); // REMOVED to prevent StackOverflow (infinite recursion)
            }
            else
            {
                // Fallback for missing/new variables
                CreateEnumField("", node.variableType, (VariableType v) => 
                {
                    node.variableType = v;
                    MarkDirty();
                    RequestRefresh();
                });
            }
            
            // Show value based on type - adaptive UI
            switch (node.variableType)
            {
                case VariableType.Bool:
                    // Bool: Show dropdown with True/False
                    bool boolVal = node.value?.ToLower() == "true" || node.value == "1";
                    CreateDropdown("=", boolVal ? 0 : 1, new[] { "True", "False" }, 
                        i => node.value = i == 0 ? "true" : "false");
                    break;
                    
                case VariableType.Int:
                    // Int: Show integer field
                    if (int.TryParse(node.value, out int intVal))
                    {
                        CreateIntField("=", intVal, v => node.value = v.ToString());
                    }
                    else
                    {
                        CreateIntField("=", 0, v => node.value = v.ToString());
                    }
                    break;
                    
                case VariableType.Float:
                    // Float: Show float field
                    if (float.TryParse(node.value, out float floatVal))
                    {
                        CreateFloatField("=", floatVal, v => node.value = v.ToString());
                    }
                    else
                    {
                        CreateFloatField("=", 0f, v => node.value = v.ToString());
                    }
                    break;
                    
                case VariableType.String:
                    // String: Show text field
                    CreateTextField(node.value, v => node.value = v, "Value...");
                    break;
            }
        }
    }
}
#endif

