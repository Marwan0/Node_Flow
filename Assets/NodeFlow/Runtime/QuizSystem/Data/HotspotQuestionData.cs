using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [System.Serializable]
    public class HotspotRegion
    {
        public string name;

        [Tooltip("Position (Normalized 0-1)")]
        public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        [Tooltip("Size (Normalized 0-1)")]
        public Vector2 normalizedSize = new Vector2(0.1f, 0.1f);

        public HotspotShape shape = HotspotShape.Rectangle;

        [Tooltip("Radius (Normalized) - used when shape is Circle")]
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
        [Header("Image")]
        [Tooltip("The image to display for hotspot clicking")]
        public Sprite image;

        [Header("Hotspots")]
        [Tooltip("Clickable regions on the image")]
        public List<HotspotRegion> hotspotRegions = new List<HotspotRegion>();

        [Header("Answer")]
        [Tooltip("Index of the correct hotspot region to click")]
        public int correctHotspotIndex = 0;

        [Header("Settings")]
        [Tooltip("Allow clicking multiple hotspots (for multi-answer questions)")]
        public bool allowMultipleSelections = false;

        [Tooltip("Indices of all correct hotspots (if multiple selections allowed)")]
        public List<int> correctHotspotIndices = new List<int>();

        private void OnEnable()
        {
            questionType = QuestionType.Hotspot;
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
