#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Visual representation of a CommentNode as a sticky note
    /// </summary>
    public class CommentNodeView : UnityEditor.Experimental.GraphView.Node
    {
        public CommentNode Data { get; private set; }
        private TextField _textField;

        public CommentNodeView(CommentNode data)
        {
            Data = data;
            viewDataKey = data.Guid;

            // Sticky note style
            title = "";
            style.minWidth = 200;
            style.minHeight = 150;
            style.maxWidth = 400;
            style.maxHeight = 300;

            // Set position
            SetPosition(new Rect(data.Position, Vector2.zero));

            // Background color
            style.backgroundColor = data.commentColor;

            // Border
            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderTopColor = new Color(0.8f, 0.8f, 0.3f);
            style.borderBottomColor = new Color(0.8f, 0.8f, 0.3f);
            style.borderLeftColor = new Color(0.8f, 0.8f, 0.3f);
            style.borderRightColor = new Color(0.8f, 0.8f, 0.3f);

            // Remove default title container
            var titleContainer = this.Q("title");
            if (titleContainer != null)
            {
                titleContainer.style.display = DisplayStyle.None;
            }

            // Create content area
            var content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.paddingTop = 10;
            content.style.paddingBottom = 10;
            content.style.paddingLeft = 10;
            content.style.paddingRight = 10;

            // Text field for comment
            _textField = new TextField();
            _textField.multiline = true;
            _textField.value = data.comment;
            _textField.style.flexGrow = 1;
            _textField.style.whiteSpace = WhiteSpace.Normal;
            _textField.style.fontSize = 12;
            _textField.RegisterValueChangedCallback(evt =>
            {
                data.comment = evt.newValue;
                if (Data.Runner?.Graph != null)
                {
                    EditorUtility.SetDirty(Data.Runner.Graph);
                }
            });

            content.Add(_textField);
            mainContainer.Add(content);

            // Make resizable
            capabilities |= Capabilities.Resizable;

            // Handle position changes
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Data != null)
            {
                Data.Position = GetPosition().position;
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (Data != null)
            {
                Data.Position = new Vector2(newPos.x, newPos.y);
            }
        }

        /// <summary>
        /// Change comment color
        /// </summary>
        public void SetColor(Color color)
        {
            Data.commentColor = color;
            style.backgroundColor = color;
            if (Data.Runner?.Graph != null)
            {
                EditorUtility.SetDirty(Data.Runner.Graph);
            }
        }
    }
}
#endif

