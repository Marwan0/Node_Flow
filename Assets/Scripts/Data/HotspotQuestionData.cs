using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [System.Serializable]
    public class HotspotRegion
    {
        [LabelText("Region Name")]
        public string name;

        [LabelText("Position (Normalized 0-1)")]
        [MinValue(0f), MaxValue(1f)]
        public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        [LabelText("Size (Normalized 0-1)")]
        [MinValue(0.01f), MaxValue(1f)]
        public Vector2 normalizedSize = new Vector2(0.1f, 0.1f);

        [LabelText("Shape")]
        public HotspotShape shape = HotspotShape.Rectangle;

        [ShowIf("shape", HotspotShape.Circle)]
        [LabelText("Radius (Normalized)")]
        [MinValue(0.01f), MaxValue(0.5f)]
        public float normalizedRadius = 0.05f;
    }

    public enum HotspotShape
    {
        Rectangle,
        Circle
    }

    [CreateAssetMenu(fileName = "HotspotQuestion", menuName = "Quiz System/Hotspot Question")]
    public class HotspotQuestionData : QuestionData
    {
        [BoxGroup("Image")]
        [PreviewField(200, ObjectFieldAlignment.Left)]
        [Required]
        [Tooltip("The image to display for hotspot clicking")]
        public Sprite image;

        [BoxGroup("Hotspots")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("Clickable regions on the image")]
        public List<HotspotRegion> hotspotRegions = new List<HotspotRegion>();

        [BoxGroup("Answer")]
        [ValueDropdown("GetHotspotIndices")]
        [Tooltip("Index of the correct hotspot region to click")]
        public int correctHotspotIndex = 0;

        [BoxGroup("Settings")]
        [Tooltip("Allow clicking multiple hotspots (for multi-answer questions)")]
        public bool allowMultipleSelections = false;

        [BoxGroup("Settings")]
        [ShowIf("allowMultipleSelections")]
        [Tooltip("Indices of all correct hotspots (if multiple selections allowed)")]
        public List<int> correctHotspotIndices = new List<int>();

        private void OnEnable()
        {
            questionType = QuestionType.Hotspot;
        }

        private IEnumerable<int> GetHotspotIndices()
        {
            for (int i = 0; i < hotspotRegions.Count; i++)
            {
                yield return i;
            }
        }

        [Button("Add Hotspot")]
        [BoxGroup("Hotspots")]
        private void AddHotspot()
        {
            hotspotRegions.Add(new HotspotRegion
            {
                name = $"Hotspot {hotspotRegions.Count + 1}",
                normalizedPosition = new Vector2(0.5f, 0.5f),
                normalizedSize = new Vector2(0.1f, 0.1f)
            });
        }

        public bool IsPointInHotspot(Vector2 normalizedPoint, int hotspotIndex)
        {
            if (hotspotIndex < 0 || hotspotIndex >= hotspotRegions.Count)
                return false;

            var region = hotspotRegions[hotspotIndex];

            if (region.shape == HotspotShape.Rectangle)
            {
                Vector2 min = region.normalizedPosition - region.normalizedSize * 0.5f;
                Vector2 max = region.normalizedPosition + region.normalizedSize * 0.5f;
                return normalizedPoint.x >= min.x && normalizedPoint.x <= max.x &&
                       normalizedPoint.y >= min.y && normalizedPoint.y <= max.y;
            }
            else // Circle
            {
                float distance = Vector2.Distance(normalizedPoint, region.normalizedPosition);
                return distance <= region.normalizedRadius;
            }
        }
    }
}

