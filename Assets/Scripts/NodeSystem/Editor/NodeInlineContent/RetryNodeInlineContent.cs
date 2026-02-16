#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class RetryNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as RetryNode;
            if (node == null) return;

            var graph = GetGraph();
            if (graph == null)
            {
                CreateLabel("Open a graph to select target node.", new Color(0.7f, 0.7f, 0.7f));
                CreateTextField(node.targetNodeGuid, guid => { node.targetNodeGuid = guid; }, "Paste GUID");
                CreateIntField("Max Retries", node.maxRetries, v => { node.maxRetries = v; });
                return;
            }

            var nodes = graph.Nodes;
            var choices = new List<string>();
            var guids = new List<string>();

            foreach (var n in nodes)
            {
                if (n.Guid == node.Guid) continue; // Don't list self
                // Always include full GUID so two nodes of same type can be told apart
                string label = string.IsNullOrEmpty(n.displayLabel)
                    ? $"{n.Name} [{n.Guid}]"
                    : $"{n.Name} - {n.displayLabel} [{n.Guid}]";
                choices.Add(label);
                guids.Add(n.Guid);
            }

            if (choices.Count == 0)
            {
                CreateLabel("No other nodes in graph.", new Color(0.7f, 0.7f, 0.7f));
                CreateTextField(node.targetNodeGuid, guid => { node.targetNodeGuid = guid; }, "Paste GUID");
            }
            else
            {
                int index = guids.IndexOf(node.targetNodeGuid);
                if (index < 0)
                {
                    choices.Insert(0, "(Select target node)");
                    guids.Insert(0, "");
                    index = 0;
                }

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2;
                row.style.marginBottom = 2;

                var labelElement = new Label("Target Node");
                labelElement.style.minWidth = 70;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);

                var guidLabel = new Label(string.IsNullOrEmpty(node.targetNodeGuid) ? "" : "GUID: " + node.targetNodeGuid);
                guidLabel.style.fontSize = 9;
                guidLabel.style.color = new Color(0.55f, 0.55f, 0.55f);
                guidLabel.style.marginTop = 0;
                guidLabel.style.marginBottom = 2;
                guidLabel.style.overflow = Overflow.Hidden;
                guidLabel.style.textOverflow = TextOverflow.Ellipsis;
                guidLabel.style.whiteSpace = WhiteSpace.NoWrap;
                guidLabel.tooltip = node.targetNodeGuid;

                var dropdown = new PopupField<string>(choices, index);
                dropdown.style.flexGrow = 1;
                dropdown.style.minWidth = 80;
                dropdown.tooltip = index > 0 && index < choices.Count ? choices[index] : "";
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    int i = choices.IndexOf(evt.newValue);
                    if (i >= 0 && i < guids.Count)
                    {
                        node.targetNodeGuid = guids[i];
                        dropdown.tooltip = evt.newValue;
                        guidLabel.text = string.IsNullOrEmpty(guids[i]) ? "" : "GUID: " + guids[i];
                        guidLabel.tooltip = guids[i];
                        MarkDirty();
                    }
                });
                row.Add(dropdown);
                Container.Add(row);
                Container.Add(guidLabel);
            }

            CreateIntField("Max Retries", node.maxRetries, v => { node.maxRetries = v; });
        }
    }
}
#endif
