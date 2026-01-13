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

            CreateTextField(node.variableName, v => node.variableName = v, "Variable name...");
            CreateEnumField("", node.variableType, (VariableType v) => 
            {
                node.variableType = v;
                MarkDirty();
                // Request refresh to update value field based on new type
                RequestRefresh();
            });
            
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

