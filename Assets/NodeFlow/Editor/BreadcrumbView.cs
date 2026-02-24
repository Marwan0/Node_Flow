#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Breadcrumb navigation for sub-graphs
    /// </summary>
    public class BreadcrumbView : VisualElement
    {
        private List<NodeGraph> _graphStack = new List<NodeGraph>();
        private System.Action<NodeGraph> _onGraphSelected;

        public BreadcrumbView(System.Action<NodeGraph> onGraphSelected)
        {
            _onGraphSelected = onGraphSelected;
            
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 10;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        }

        public void SetGraphStack(List<NodeGraph> stack)
        {
            _graphStack = stack ?? new List<NodeGraph>();
            Refresh();
        }

        public void PushGraph(NodeGraph graph)
        {
            _graphStack.Add(graph);
            Refresh();
        }

        public void PopGraph()
        {
            if (_graphStack.Count > 1)
            {
                _graphStack.RemoveAt(_graphStack.Count - 1);
                Refresh();
            }
        }

        private void Refresh()
        {
            Clear();

            if (_graphStack.Count == 0) return;

            for (int i = 0; i < _graphStack.Count; i++)
            {
                var graph = _graphStack[i];
                bool isLast = i == _graphStack.Count - 1;

                // Graph name button
                var button = new Button(() => _onGraphSelected?.Invoke(graph))
                {
                    text = graph.graphName
                };
                
                if (isLast)
                {
                    button.style.unityFontStyleAndWeight = FontStyle.Bold;
                    button.style.color = new Color(1f, 1f, 0.5f);
                }
                else
                {
                    button.style.color = new Color(0.7f, 0.7f, 0.7f);
                }

                button.style.marginRight = 5;
                Add(button);

                // Separator (except for last)
                if (!isLast)
                {
                    var separator = new Label(">");
                    separator.style.color = new Color(0.5f, 0.5f, 0.5f);
                    separator.style.marginLeft = 5;
                    separator.style.marginRight = 5;
                    Add(separator);
                }
            }
        }
    }
}
#endif

