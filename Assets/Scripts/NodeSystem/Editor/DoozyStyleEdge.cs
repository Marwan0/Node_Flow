#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom Edge that uses Doozy-inspired curve rendering:
    /// - Double-layer bezier (dark outline + bright inner + glossy highlight)
    /// - Dynamic tangents that adapt to relative node positions
    /// - Animated traveling dot on active/selected connections
    /// </summary>
    public class DoozyStyleEdge : Edge
    {
        protected override EdgeControl CreateEdgeControl()
        {
            return new DoozyStyleEdgeControl
            {
                capRadius = 5,
                interceptWidth = 6
            };
        }
    }

    /// <summary>
    /// Custom EdgeControl with Doozy-style rendering via Painter2D.
    /// Replaces the default flat polyline with a layered bezier curve.
    /// </summary>
    public class DoozyStyleEdgeControl : EdgeControl
    {
        // --- Dot animation ---
        private float _dotPhase;
        private IVisualElementScheduledItem _dotSchedule;
        private bool _isDotAnimating;

        // --- Curve settings (inspired by Doozy NodySettings) ---
        private const float CurveModifier = 0.35f;
        private const float MinStrength = 30f;
        private const float MaxStrength = 250f;
        private const float OutlineExtra = 3.5f;
        private const float DefaultWidth = 3f;
        private const float DotSize = 5f;
        private const float DotSpeed = 0.015f;

        // --- Color palette ---
        private static readonly Color NormalMain     = new Color(0.50f, 0.68f, 0.88f, 0.80f);
        private static readonly Color NormalOutline  = new Color(0.06f, 0.10f, 0.18f, 0.55f);
        private static readonly Color ActiveMain     = new Color(0.15f, 0.75f, 1.00f, 1.00f);
        private static readonly Color ActiveOutline  = new Color(0.04f, 0.18f, 0.35f, 0.70f);
        private static readonly Color ExecutedMain   = new Color(0.27f, 0.82f, 0.40f, 0.88f);
        private static readonly Color ExecutedOutline= new Color(0.05f, 0.18f, 0.08f, 0.55f);
        private static readonly Color SelectedMain   = new Color(0.85f, 0.92f, 1.00f, 1.00f);
        private static readonly Color SelectedOutline= new Color(0.15f, 0.25f, 0.40f, 0.70f);
        private static readonly Color DotWhite       = new Color(1.00f, 1.00f, 1.00f, 0.95f);

        // --- Hit testing ---
        private const int HitTestSamples = 40;
        private const float HitTestPadding = 8f; // Extra pixels around the curve for easier clicking

        public DoozyStyleEdgeControl()
        {
            // Replace the default mesh-based renderer with our Painter2D renderer
            generateVisualContent = OnGenerateVisualContent;
            // Allow the outline/glow to extend beyond the element bounds
            style.overflow = Overflow.Visible;
            // Ensure the element participates in picking
            pickingMode = PickingMode.Position;
        }

        // =============================================================
        //  HIT TESTING  (must match the drawn Doozy bezier exactly)
        // =============================================================

        /// <summary>
        /// Point-on-curve test. Samples the Doozy-style bezier and returns
        /// true when <paramref name="localPoint"/> is close enough to select.
        /// </summary>
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!GetLocalEndpoints(out Vector2 localStart, out Vector2 localEnd))
                return false;

            ComputeDoozyTangents(localStart, localEnd, out Vector2 cp1, out Vector2 cp2);

            float clickRadius = Mathf.Max(interceptWidth, edgeWidth + OutlineExtra) + HitTestPadding;
            float clickRadiusSq = clickRadius * clickRadius;

            for (int i = 0; i <= HitTestSamples; i++)
            {
                float t = i / (float)HitTestSamples;
                Vector2 p = EvalBezier(localStart, cp1, cp2, localEnd, t);
                if ((localPoint - p).sqrMagnitude <= clickRadiusSq)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Rectangle-overlap test (for marquee / box selection).
        /// Returns true when any sampled point on the bezier falls inside <paramref name="rect"/>.
        /// </summary>
        public override bool Overlaps(Rect rect)
        {
            if (!GetLocalEndpoints(out Vector2 localStart, out Vector2 localEnd))
                return false;

            ComputeDoozyTangents(localStart, localEnd, out Vector2 cp1, out Vector2 cp2);

            // Inflate the rect a little so near-misses still count
            Rect inflated = new Rect(
                rect.x - HitTestPadding, rect.y - HitTestPadding,
                rect.width + HitTestPadding * 2f, rect.height + HitTestPadding * 2f);

            for (int i = 0; i <= HitTestSamples; i++)
            {
                float t = i / (float)HitTestSamples;
                Vector2 p = EvalBezier(localStart, cp1, cp2, localEnd, t);
                if (inflated.Contains(p))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Convert from/to from the parent Edge's coordinate space into
        /// our local coordinate space (same transform used by rendering).
        /// </summary>
        private bool GetLocalEndpoints(out Vector2 localStart, out Vector2 localEnd)
        {
            Vector2 start = from;
            Vector2 end   = to;

            if (float.IsNaN(start.x) || float.IsNaN(end.x))
            {
                localStart = localEnd = Vector2.zero;
                return false;
            }

            Vector2 offset = new Vector2(layout.xMin, layout.yMin);
            localStart = start - offset;
            localEnd   = end   - offset;
            return true;
        }

        // =============================================================
        //  RENDERING
        // =============================================================

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (!GetLocalEndpoints(out Vector2 localStart, out Vector2 localEnd)) return;
            if (Vector2.SqrMagnitude(localEnd - localStart) < 1f) return;

            // Compute Doozy-style tangent handles
            ComputeDoozyTangents(localStart, localEnd, out Vector2 cp1, out Vector2 cp2);

            // Determine visual state
            GetVisualState(out Color mainCol, out Color outlineCol, out bool showDot, out float widthScale);

            float baseWidth = edgeWidth > 0 ? edgeWidth : DefaultWidth;
            float w = baseWidth * widthScale;

            var painter = mgc.painter2D;
            painter.lineCap  = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            // --- Layer 1: Dark outline (shadow / depth) ---
            painter.BeginPath();
            painter.MoveTo(localStart);
            painter.BezierCurveTo(cp1, cp2, localEnd);
            painter.strokeColor = outlineCol;
            painter.lineWidth   = w + OutlineExtra;
            painter.Stroke();

            // --- Layer 2: Main colored bezier ---
            painter.BeginPath();
            painter.MoveTo(localStart);
            painter.BezierCurveTo(cp1, cp2, localEnd);
            painter.strokeColor = mainCol;
            painter.lineWidth   = w;
            painter.Stroke();

            // --- Layer 3: Inner glossy highlight ---
            Color highlight = new Color(
                Mathf.Min(1f, mainCol.r * 1.4f),
                Mathf.Min(1f, mainCol.g * 1.4f),
                Mathf.Min(1f, mainCol.b * 1.4f),
                mainCol.a * 0.30f
            );
            painter.BeginPath();
            painter.MoveTo(localStart);
            painter.BezierCurveTo(cp1, cp2, localEnd);
            painter.strokeColor = highlight;
            painter.lineWidth   = Mathf.Max(1f, w * 0.30f);
            painter.Stroke();

            // --- Layer 4: Animated traveling dot ---
            if (showDot)
            {
                EnsureDotAnimating();
                Vector2 dotPos = EvalBezier(localStart, cp1, cp2, localEnd, _dotPhase);

                // Outer glow
                painter.BeginPath();
                painter.MoveTo(dotPos);
                painter.LineTo(dotPos + new Vector2(0.01f, 0f));
                painter.strokeColor = new Color(mainCol.r, mainCol.g, mainCol.b, 0.30f);
                painter.lineWidth   = DotSize * 4f;
                painter.lineCap     = LineCap.Round;
                painter.Stroke();

                // Bright center
                painter.BeginPath();
                painter.MoveTo(dotPos);
                painter.LineTo(dotPos + new Vector2(0.01f, 0f));
                painter.strokeColor = DotWhite;
                painter.lineWidth   = DotSize * 2f;
                painter.lineCap     = LineCap.Round;
                painter.Stroke();
            }
            else
            {
                StopDotAnimation();
            }
        }

        // =============================================================
        //  DOOZY-STYLE TANGENT CALCULATION
        // =============================================================

        /// <summary>
        /// Compute bezier control handles (tangents) using Doozy's algorithm.
        /// The tangent direction adapts to the relative position of the connected
        /// nodes so that curves always look organic and never cross through nodes.
        /// </summary>
        private static void ComputeDoozyTangents(
            Vector2 start, Vector2 end,
            out Vector2 cp1, out Vector2 cp2)
        {
            float dist  = Vector2.Distance(start, end);
            float dx    = end.x - start.x;
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(end.y - start.y);

            if (dx >= 0)
            {
                // --- Standard flow (left → right) ---
                float strength = Mathf.Clamp(dist * CurveModifier, MinStrength, MaxStrength);
                cp1 = start + Vector2.right * strength;
                cp2 = end   + Vector2.left  * strength;
            }
            else
            {
                // --- Reversed flow (right → left) ---
                // Wider loop so the curve sweeps around instead of cutting through.
                float reverseStrength = Mathf.Clamp(
                    absDx * 0.5f + absDy * 0.4f + 50f,
                    80f, 350f);

                if (absDy > absDx * 0.5f)
                {
                    // Large vertical offset: add a vertical nudge to the tangent
                    float vSign = Mathf.Sign(end.y - start.y);
                    cp1 = start + new Vector2(reverseStrength * 0.7f,  vSign * reverseStrength * 0.3f);
                    cp2 = end   + new Vector2(-reverseStrength * 0.7f, -vSign * reverseStrength * 0.3f);
                }
                else
                {
                    // Mostly horizontal reverse
                    cp1 = start + Vector2.right * reverseStrength;
                    cp2 = end   + Vector2.left  * reverseStrength;
                }
            }
        }

        // =============================================================
        //  VISUAL STATE
        // =============================================================

        private void GetVisualState(
            out Color mainColor, out Color outlineColor,
            out bool showDot, out float widthScale)
        {
            mainColor    = NormalMain;
            outlineColor = NormalOutline;
            showDot      = false;
            widthScale   = 1f;

            var edge = parent as Edge;
            if (edge == null) return;

            // Runtime execution state (set by NodeGraphView)
            if (edge.ClassListContains("edge-active"))
            {
                mainColor    = ActiveMain;
                outlineColor = ActiveOutline;
                showDot      = true;
                widthScale   = 1.5f;
            }
            else if (edge.ClassListContains("edge-executed"))
            {
                mainColor    = ExecutedMain;
                outlineColor = ExecutedOutline;
                widthScale   = 1.15f;
            }

            // Selection state overrides
            if (edge.selected)
            {
                mainColor    = SelectedMain;
                outlineColor = SelectedOutline;
                showDot      = true;
                widthScale   = 1.3f;
            }
        }

        // =============================================================
        //  DOT ANIMATION
        // =============================================================

        private Vector2 EvalBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }

        private void EnsureDotAnimating()
        {
            if (_isDotAnimating) return;
            _isDotAnimating = true;
            _dotPhase = 0f;

            _dotSchedule = schedule.Execute(() =>
            {
                _dotPhase += DotSpeed;
                if (_dotPhase > 1f) _dotPhase -= 1f;
                MarkDirtyRepaint();
            }).Every(40);
        }

        private void StopDotAnimation()
        {
            if (!_isDotAnimating) return;
            _isDotAnimating = false;
            _dotSchedule?.Pause();
            _dotSchedule = null;
        }
    }
}
#endif
