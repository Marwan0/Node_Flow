#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Visual representation of a NodeGroupData in the GraphView
    /// </summary>
    public class NodeGroupView : Group
    {
        public NodeGroupData Data { get; private set; }
        private NodeGraphView _graphView;

        public NodeGroupView(NodeGroupData data, NodeGraphView graphView)
        {
            Data = data;
            _graphView = graphView;

            title = data.Title;
            viewDataKey = data.Guid;

            // Set position
            SetPosition(new Rect(data.Position.position, data.Position.size));

            // Set color
            style.backgroundColor = data.Color;

            // Handle title changes - Group title is a property, so we'll update on geometry change
            // The title can be edited directly in the Group's title area
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            // Also listen for title changes via the title property
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                // Try to find the title field and subscribe to changes
                var titleContainer = this.Q("title");
                if (titleContainer != null)
                {
                    titleContainer.RegisterCallback<ChangeEvent<string>>(evt =>
                    {
                        if (Data != null)
                        {
                            Data.Title = title;
                            SaveGroup();
                        }
                    });
                }
            });
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Data != null)
            {
                Data.Position = GetPosition();
                // Update title if it changed
                if (Data.Title != title)
                {
                    Data.Title = title;
                }
                SaveGroup();
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            base.OnElementsAdded(elements);

            foreach (var element in elements)
            {
                if (element is NodeView nodeView)
                {
                    Data.AddNode(nodeView.Data.Guid);
                }
            }

            SaveGroup();
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            base.OnElementsRemoved(elements);

            foreach (var element in elements)
            {
                if (element is NodeView nodeView)
                {
                    Data.RemoveNode(nodeView.Data.Guid);
                }
            }

            SaveGroup();
        }

        private void SaveGroup()
        {
            if (_graphView?.Graph != null)
            {
                _graphView.Graph.Save();
            }
        }

        /// <summary>
        /// Change group color
        /// </summary>
        public void SetColor(Color color)
        {
            Data.Color = color;
            style.backgroundColor = color;
            SaveGroup();
        }
    }
}
#endif

