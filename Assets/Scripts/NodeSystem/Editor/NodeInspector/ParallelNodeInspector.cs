#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for ParallelNode
    /// </summary>
    public class ParallelNodeInspector : NodeInspectorBase
    {
        public override void DrawInspector()
        {
            CreateLabel("Parallel Execution", true);

            // Info box
            var infoBox = new VisualElement();
            infoBox.style.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            infoBox.style.borderTopLeftRadius = 5;
            infoBox.style.borderTopRightRadius = 5;
            infoBox.style.borderBottomLeftRadius = 5;
            infoBox.style.borderBottomRightRadius = 5;
            infoBox.style.paddingTop = 10;
            infoBox.style.paddingBottom = 10;
            infoBox.style.paddingLeft = 10;
            infoBox.style.paddingRight = 10;
            infoBox.style.marginTop = 10;

            var line1 = new Label("This node executes all connected");
            line1.style.color = Color.white;
            infoBox.Add(line1);

            var line2 = new Label("'Parallel' outputs at the same time.");
            line2.style.color = Color.white;
            infoBox.Add(line2);

            var line3 = new Label("");
            infoBox.Add(line3);

            var line4 = new Label("The 'All Done' output fires only");
            line4.style.color = new Color(0.5f, 1f, 0.5f);
            infoBox.Add(line4);

            var line5 = new Label("after ALL parallel nodes complete.");
            line5.style.color = new Color(0.5f, 1f, 0.5f);
            infoBox.Add(line5);

            Container.Add(infoBox);

            CreateSeparator();

            // Usage diagram
            CreateLabel("How to Use", true);

            var diagramLabel = new Label(
                "┌──► Node A ──┐\n" +
                "│             │\n" +
                "├──► Node B ──┼──► Next\n" +
                "│             │\n" +
                "└──► Node C ──┘"
            );
            diagramLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            diagramLabel.style.fontSize = 11;
            diagramLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            diagramLabel.style.whiteSpace = WhiteSpace.Pre;
            diagramLabel.style.marginTop = 5;
            Container.Add(diagramLabel);

            var usageLabel = new Label(
                "\n1. Connect nodes to 'Parallel' port\n" +
                "2. Connect 'All Done' to next node\n" +
                "3. All parallel nodes run together!"
            );
            usageLabel.style.fontSize = 11;
            usageLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            usageLabel.style.whiteSpace = WhiteSpace.Pre;
            Container.Add(usageLabel);
        }
    }
}
#endif

