#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class RandomBranchNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as RandomBranchNode;
            if (node == null) return;

            var graphView = Container.GetFirstAncestorOfType<NodeGraphView>();
            var graph = graphView?.Graph;

            if (graph == null)
            {
                CreateLabel("Connect branches to output port.", new Color(0.7f, 0.7f, 0.7f));
                return;
            }

            var connectedNodes = graph.GetConnectedNodes(node.Guid, "output");

            if (connectedNodes.Count == 0)
            {
                CreateLabel("No branches connected yet.", new Color(0.7f, 0.7f, 0.7f));
                return;
            }

            node.CleanupWeights(connectedNodes.Select(n => n.Guid));

            // Store guid + label pairs so any slider can update ALL percentages
            var branchEntries = new List<(string guid, Label pctLabel)>();

            foreach (var cn in connectedNodes)
            {
                float w = node.GetWeight(cn.Guid);

                // Header: name + percentage
                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;
                headerRow.style.marginTop = 3;

                var nameLabel = new Label(cn.Name);
                nameLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
                nameLabel.style.fontSize = 10;
                nameLabel.style.overflow = Overflow.Hidden;
                nameLabel.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(nameLabel);

                var pctLabel = new Label();
                pctLabel.style.color = new Color(0.8f, 0.6f, 0.3f);
                pctLabel.style.fontSize = 10;
                pctLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                pctLabel.style.minWidth = 35;
                headerRow.Add(pctLabel);

                Container.Add(headerRow);

                branchEntries.Add((cn.Guid, pctLabel));

                // Slider
                var capturedGuid = cn.Guid;
                var slider = new Slider(0f, 10f) { value = w };
                slider.style.marginTop = 0;
                slider.style.marginBottom = 2;
                slider.style.height = 14;
                slider.RegisterValueChangedCallback(evt =>
                {
                    node.SetWeight(capturedGuid, evt.newValue);
                    MarkDirty();
                    UpdateAllPercentages(node, branchEntries);
                });
                Container.Add(slider);
            }

            // Initial percentage calculation
            UpdateAllPercentages(node, branchEntries);

            CreateLabel($"🎲 {connectedNodes.Count} branches", new Color(0.8f, 0.6f, 0.3f));
        }

        private void UpdateAllPercentages(RandomBranchNode node, List<(string guid, Label pctLabel)> entries)
        {
            float total = 0f;
            foreach (var (guid, _) in entries)
                total += node.GetWeight(guid);

            foreach (var (guid, pctLabel) in entries)
            {
                float pct = total > 0 ? (node.GetWeight(guid) / total * 100f) : 0f;
                pctLabel.text = $"{pct:F0}%";
            }
        }
    }
}
#endif
