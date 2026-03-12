#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Visual representation of a CommentNode using Unity's built-in StickyNote.
    /// </summary>
    public class CommentNodeView : StickyNote
    {
        public CommentNode Data { get; private set; }
        private NodeGraphView _graphView;
        private bool _syncing;

        public CommentNodeView(CommentNode data, NodeGraphView graphView = null)
        {
            Data = data;
            _graphView = graphView;
            viewDataKey = data.Guid;

            // Apply persisted theme / font size
            theme = (StickyNoteTheme)data.theme;
            fontSize = (StickyNoteFontSize)data.fontSize;

            // Set title and contents from data
            title = data.commentTitle ?? "Note";
            contents = data.comment ?? "";

            // Set position and size
            SetPosition(new Rect(data.Position, data.commentSize));

            // Listen for changes
            RegisterCallback<StickyNoteChangeEvent>(OnStickyNoteChanged);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnStickyNoteChanged(StickyNoteChangeEvent evt)
        {
            if (_syncing || Data == null) return;
            _syncing = true;

            if (_graphView?.Graph != null)
                Undo.RecordObject(_graphView.Graph, "Edit Sticky Note");

            if ((evt.change & StickyNoteChange.Title) != 0)
                Data.commentTitle = title;

            if ((evt.change & StickyNoteChange.Contents) != 0)
                Data.comment = contents;

            if ((evt.change & StickyNoteChange.Theme) != 0)
                Data.theme = (int)theme;

            if ((evt.change & StickyNoteChange.FontSize) != 0)
                Data.fontSize = (int)fontSize;

            SaveGraph();
            _syncing = false;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_syncing || Data == null) return;

            var rect = GetPosition();
            var newPos = rect.position;
            var newSize = rect.size;

            if (Data.Position != newPos || Data.commentSize != newSize)
            {
                if (_graphView?.Graph != null)
                    Undo.RecordObject(_graphView.Graph, "Move Sticky Note");

                Data.Position = newPos;
                Data.commentSize = newSize;
                SaveGraph();
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (_syncing || Data == null) return;

            var newPosition = newPos.position;
            var newSize = newPos.size;

            if (Data.Position != newPosition || Data.commentSize != newSize)
            {
                if (_graphView?.Graph != null)
                    Undo.RecordObject(_graphView.Graph, "Move Sticky Note");

                Data.Position = newPosition;
                Data.commentSize = newSize;
                SaveGraph();
            }
        }

        private void SaveGraph()
        {
            if (_graphView?.Graph != null)
            {
                _graphView.Graph.Save();
                EditorUtility.SetDirty(_graphView.Graph);
            }
        }
    }
}
#endif
