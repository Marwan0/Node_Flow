#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Search window for adding nodes
    /// </summary>
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private NodeGraphView _graphView;
        private EditorWindow _window;

        public void Initialize(NodeGraphView graphView, EditorWindow window)
        {
            _graphView = graphView;
            _window = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Add Node"), 0)
            };

            // Get all node types
            var nodeTypes = GetAllNodeTypes();

            // Group by category
            var categories = new HashSet<string>();
            foreach (var type in nodeTypes)
            {
                var instance = (NodeData)Activator.CreateInstance(type);
                categories.Add(instance.Category);
            }

            // Add categories and nodes
            foreach (var category in categories.OrderBy(c => c))
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent(category), 1));

                foreach (var type in nodeTypes)
                {
                    var instance = (NodeData)Activator.CreateInstance(type);
                    if (instance.Category == category)
                    {
                        tree.Add(new SearchTreeEntry(new GUIContent(instance.Name))
                        {
                            level = 2,
                            userData = type
                        });
                    }
                }
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is Type nodeType)
            {
                // Convert screen position to graph position
                var screenPos = context.screenMousePosition;
                var windowPos = _window.position.position;
                var localPos = screenPos - windowPos;
                
                // Transform to graph space
                var graphPos = _graphView.contentViewContainer.transform.matrix.inverse.MultiplyPoint(localPos);

                _graphView.CreateNode(nodeType, new Vector2(graphPos.x, graphPos.y));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find all NodeData types in the project
        /// </summary>
        private List<Type> GetAllNodeTypes()
        {
            var baseType = typeof(NodeData);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
                .ToList();
        }
    }
}
#endif

