using UnityEngine;
using UnityEngine.UI;

namespace QuizSystem
{
    /// <summary>
    /// Draws a line in Canvas (UI) space so it renders correctly with the rest of the UI.
    /// Use this instead of Unity's LineRenderer for Canvas-based connect/matching questions.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UILineRenderer : Graphic
    {
        [SerializeField] [Min(1f)] private float lineThickness = 4f;

        private Vector2 _startPos;
        private Vector2 _endPos;
        private Color _startColor = Color.white;
        private Color _endColor = Color.white;

        protected override void Awake()
        {
            base.Awake();
            color = new Color(1f, 1f, 1f, 1f);
            if (material == null || material.name.Contains("Default"))
                material = defaultMaterial;
        }

        /// <summary>Line thickness in pixels.</summary>
        public float LineThickness
        {
            get => lineThickness;
            set { lineThickness = Mathf.Max(1f, value); SetAllDirty(); }
        }

        /// <summary>Set the line endpoints in this RectTransform's local space.</summary>
        public void SetPositions(Vector2 start, Vector2 end)
        {
            _startPos = start;
            _endPos = end;
            SetVerticesDirty();
        }

        /// <summary>Set colors for gradient along the line.</summary>
        public void SetColors(Color start, Color end)
        {
            _startColor = start;
            _endColor = end;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 dir = _endPos - _startPos;
            float len = dir.magnitude;
            if (len < 0.001f)
                return;

            Vector2 perp = new Vector2(-dir.y, dir.x).normalized * (lineThickness * 0.5f);

            Vector2 p0 = _startPos - perp;
            Vector2 p1 = _startPos + perp;
            Vector2 p2 = _endPos + perp;
            Vector2 p3 = _endPos - perp;

            Color c0 = _startColor;
            Color c2 = _endColor;
            if (c0.a <= 0f) c0 = Color.white;
            if (c2.a <= 0f) c2 = Color.white;

            UIVertex v = UIVertex.simpleVert;
            v.color = c0;
            v.position = p0;
            v.uv0 = new Vector2(0, 0);
            vh.AddVert(v);
            v.position = p1;
            vh.AddVert(v);
            v.color = c2;
            v.position = p2;
            vh.AddVert(v);
            v.position = p3;
            vh.AddVert(v);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
