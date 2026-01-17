using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    /// <summary>
    /// Serializable data structure for exporting/importing questions
    /// </summary>
    [Serializable]
    public class QuestionExportData
    {
        public string exportVersion = "1.0";
        public string exportDate;
        public List<QuestionEntry> questions = new List<QuestionEntry>();

        public QuestionExportData()
        {
            exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    [Serializable]
    public class QuestionEntry
    {
        // Common fields
        public string type;
        public string questionText;
        public string[] hints;
        public int maxAttempts;
        public int points;
        public string explanation;

        // Multiple Choice specific
        public string[] answers;
        public int correctAnswerIndex;

        // True/False specific
        public bool correctAnswer;

        // Fill in the Blank specific
        public string correctText;
        public string[] alternativeAnswers;
        public bool caseSensitive;
        public bool allowPartialMatch;
        public float partialMatchThreshold;

        // Multi-Select specific
        public string[] options;
        public int[] correctAnswerIndices;
        public bool allowPartialCredit;

        // Ordering specific
        public string[] orderedItems;
        public bool shuffleItems;

        // Hotspot specific
        public string imagePath;
        public ExportHotspotRegion[] hotspotRegions;
        public int correctHotspotIndex;
        public bool allowMultipleSelections;
        public int[] correctHotspotIndices;

        // Slider specific
        public float minValue;
        public float maxValue;
        public float correctValue;
        public float tolerance;
        public bool useTolerance;
        public bool showValueLabels;
        public bool showCurrentValue;
        public int decimalPlaces;

        // Audio specific
        public string audioClipPath;
        public bool allowReplay;
        public bool autoPlay;
        public int maxPlayCount;
        public string audioAnswerType; // "MultipleChoice" or "FillInTheBlank"
        public string[] audioAnswerOptions;
        public int audioCorrectIndex;
        public string audioCorrectText;
        public bool audioCaseSensitive;

        // Drag & Drop specific
        public ExportDragItem[] dragItems;
        public ExportDropZone[] dropZones;
        public ExportPairing[] correctPairings;

        // Connect specific
        public ExportConnectItem[] leftColumnItems;
        public ExportConnectItem[] rightColumnItems;
        public ExportPairing[] correctConnections;
    }

    [Serializable]
    public class ExportHotspotRegion
    {
        public string name;
        public float posX;
        public float posY;
        public float sizeX;
        public float sizeY;
        public string shape; // "Rectangle" or "Circle"
        public float radius;
    }

    [Serializable]
    public class ExportDragItem
    {
        public string label;
        public string iconPath;
    }

    [Serializable]
    public class ExportDropZone
    {
        public string label;
        public string iconPath;
    }

    [Serializable]
    public class ExportConnectItem
    {
        public string label;
        public string iconPath;
    }

    [Serializable]
    public class ExportPairing
    {
        public int sourceIndex;
        public int targetIndex;
    }
}
