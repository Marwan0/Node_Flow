#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class ConditionalNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ConditionalNode;
            if (node == null) return;

            CreateEnumField("", node.conditionType, (ConditionType v) => 
            {
                node.conditionType = v;
                MarkDirty();
                // Request refresh to update fields based on condition type
                RequestRefresh();
            });

            if (node.conditionType == ConditionType.GameObjectExists || 
                node.conditionType == ConditionType.GameObjectActive)
            {
                CreateTextField(node.gameObjectPath, v => node.gameObjectPath = v, "GameObject path...");
            }
            else
            {
                CreateTextField(node.variableName, v => node.variableName = v, "Variable...");
                
                if (node.conditionType == ConditionType.BoolVariable)
                {
                    // For bool: show comparison operator and True/False dropdown
                    CreateEnumField("", node.comparison, (ComparisonOperator v) => node.comparison = v);
                    bool val = node.compareValue?.ToLower() == "true" || node.compareValue == "1";
                    CreateDropdown("", val ? 0 : 1, new[] { "True", "False" },
                        i => node.compareValue = i == 0 ? "true" : "false");
                }
                else if (node.conditionType == ConditionType.IntComparison)
                {
                    // For int: show comparison operator and integer field
                    CreateEnumField("", node.comparison, (ComparisonOperator v) => node.comparison = v);
                    if (int.TryParse(node.compareValue, out int intVal))
                    {
                        CreateIntField("", intVal, v => node.compareValue = v.ToString());
                    }
                    else
                    {
                        CreateIntField("", 0, v => node.compareValue = v.ToString());
                    }
                }
                else if (node.conditionType == ConditionType.FloatComparison)
                {
                    // For float: show comparison operator and float field
                    CreateEnumField("", node.comparison, (ComparisonOperator v) => node.comparison = v);
                    if (float.TryParse(node.compareValue, out float floatVal))
                    {
                        CreateFloatField("", floatVal, v => node.compareValue = v.ToString());
                    }
                    else
                    {
                        CreateFloatField("", 0f, v => node.compareValue = v.ToString());
                    }
                }
                else if (node.conditionType == ConditionType.StringEquals)
                {
                    // For string: show comparison operator and text field
                    CreateEnumField("", node.comparison, (ComparisonOperator v) => node.comparison = v);
                    CreateTextField(node.compareValue, v => node.compareValue = v, "Value...");
                }
            }
        }
    }
}
#endif

