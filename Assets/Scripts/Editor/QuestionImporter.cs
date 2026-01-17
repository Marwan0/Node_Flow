using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace QuizSystem
{
    /// <summary>
    /// Imports questions from JSON format
    /// </summary>
    public static class QuestionImporter
    {
        public static List<QuestionData> ImportFromJson(string json, string savePath = "Assets/Data/Questions/Imported")
        {
            var importedQuestions = new List<QuestionData>();

            try
            {
                var exportData = JsonUtility.FromJson<QuestionExportData>(json);
                
                if (exportData == null || exportData.questions == null)
                {
                    Debug.LogError("[QuestionImporter] Invalid JSON format");
                    return importedQuestions;
                }

                // Ensure save directory exists
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                foreach (var entry in exportData.questions)
                {
                    var question = ConvertFromEntry(entry, savePath);
                    if (question != null)
                    {
                        importedQuestions.Add(question);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[QuestionImporter] Imported {importedQuestions.Count} questions from JSON (version {exportData.exportVersion})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestionImporter] Failed to import: {e.Message}\n{e.StackTrace}");
            }

            return importedQuestions;
        }

        public static List<QuestionData> ImportFromFile(string filePath, string savePath = "Assets/Data/Questions/Imported")
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[QuestionImporter] File not found: {filePath}");
                return new List<QuestionData>();
            }

            string json = File.ReadAllText(filePath);
            return ImportFromJson(json, savePath);
        }

        public static List<QuestionData> ImportWithDialog(string savePath = "Assets/Data/Questions/Imported")
        {
            string path = EditorUtility.OpenFilePanel(
                "Import Questions",
                Application.dataPath,
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var questions = ImportFromFile(path, savePath);
                
                if (questions.Count > 0)
                {
                    EditorUtility.DisplayDialog("Import Complete",
                        $"Successfully imported {questions.Count} questions.\nSaved to: {savePath}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Import Failed",
                        "No questions were imported. Check the console for errors.", "OK");
                }

                return questions;
            }

            return new List<QuestionData>();
        }

        private static QuestionData ConvertFromEntry(QuestionEntry entry, string savePath)
        {
            QuestionData question = null;

            if (!Enum.TryParse<QuestionType>(entry.type, out var questionType))
            {
                Debug.LogWarning($"[QuestionImporter] Unknown question type: {entry.type}");
                return null;
            }

            switch (questionType)
            {
                case QuestionType.MultipleChoice:
                    var mc = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
                    mc.answers = entry.answers ?? new string[4];
                    mc.correctAnswerIndex = entry.correctAnswerIndex;
                    question = mc;
                    break;

                case QuestionType.TrueFalse:
                    var tf = ScriptableObject.CreateInstance<TrueFalseQuestionData>();
                    tf.correctAnswer = entry.correctAnswer;
                    question = tf;
                    break;

                case QuestionType.FillInTheBlank:
                    var fitb = ScriptableObject.CreateInstance<FillInTheBlankQuestionData>();
                    fitb.correctAnswer = entry.correctText ?? "";
                    fitb.alternativeAnswers = entry.alternativeAnswers != null 
                        ? new List<string>(entry.alternativeAnswers) 
                        : new List<string>();
                    fitb.caseSensitive = entry.caseSensitive;
                    fitb.allowPartialMatch = entry.allowPartialMatch;
                    fitb.partialMatchThreshold = entry.partialMatchThreshold > 0 
                        ? entry.partialMatchThreshold 
                        : 0.8f;
                    question = fitb;
                    break;

                case QuestionType.MultiSelect:
                    var ms = ScriptableObject.CreateInstance<MultiSelectQuestionData>();
                    ms.options = entry.options != null 
                        ? new List<string>(entry.options) 
                        : new List<string>();
                    ms.correctAnswerIndices = entry.correctAnswerIndices != null 
                        ? new List<int>(entry.correctAnswerIndices) 
                        : new List<int>();
                    ms.allowPartialCredit = entry.allowPartialCredit;
                    question = ms;
                    break;

                case QuestionType.Ordering:
                    var ord = ScriptableObject.CreateInstance<OrderingQuestionData>();
                    ord.items = entry.orderedItems != null 
                        ? new List<string>(entry.orderedItems) 
                        : new List<string>();
                    ord.shuffleItems = entry.shuffleItems;
                    ord.allowPartialCredit = entry.allowPartialCredit;
                    question = ord;
                    break;

                case QuestionType.Hotspot:
                    var hs = ScriptableObject.CreateInstance<HotspotQuestionData>();
                    if (!string.IsNullOrEmpty(entry.imagePath))
                    {
                        hs.image = AssetDatabase.LoadAssetAtPath<Sprite>(entry.imagePath);
                    }
                    if (entry.hotspotRegions != null)
                    {
                        hs.hotspotRegions = entry.hotspotRegions.Select(h => new HotspotRegion
                        {
                            name = h.name,
                            normalizedPosition = new Vector2(h.posX, h.posY),
                            normalizedSize = new Vector2(h.sizeX, h.sizeY),
                            shape = h.shape == "Circle" ? HotspotShape.Circle : HotspotShape.Rectangle,
                            normalizedRadius = h.radius
                        }).ToList();
                    }
                    else
                    {
                        hs.hotspotRegions = new List<HotspotRegion>();
                    }
                    hs.correctHotspotIndex = entry.correctHotspotIndex;
                    hs.allowMultipleSelections = entry.allowMultipleSelections;
                    hs.correctHotspotIndices = entry.correctHotspotIndices != null 
                        ? new List<int>(entry.correctHotspotIndices) 
                        : new List<int>();
                    question = hs;
                    break;

                case QuestionType.Slider:
                    var sl = ScriptableObject.CreateInstance<SliderQuestionData>();
                    sl.valueRange = new Vector2(entry.minValue, entry.maxValue);
                    sl.correctValue = entry.correctValue;
                    sl.tolerance = entry.tolerance;
                    sl.useTolerance = entry.useTolerance;
                    sl.showValueLabels = entry.showValueLabels;
                    sl.showCurrentValue = entry.showCurrentValue;
                    sl.decimalPlaces = entry.decimalPlaces;
                    question = sl;
                    break;

                case QuestionType.Audio:
                    var au = ScriptableObject.CreateInstance<AudioQuestionData>();
                    if (!string.IsNullOrEmpty(entry.audioClipPath))
                    {
                        au.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.audioClipPath);
                    }
                    au.allowReplay = entry.allowReplay;
                    au.autoPlay = entry.autoPlay;
                    au.maxPlayCount = entry.maxPlayCount > 0 ? entry.maxPlayCount : 3;
                    au.answerType = entry.audioAnswerType == "FillInTheBlank" 
                        ? AudioAnswerType.FillInTheBlank 
                        : AudioAnswerType.MultipleChoice;
                    au.answerOptions = entry.audioAnswerOptions != null 
                        ? new List<string>(entry.audioAnswerOptions) 
                        : new List<string>();
                    au.correctAnswerIndex = entry.audioCorrectIndex;
                    au.correctAnswerText = entry.audioCorrectText ?? "";
                    au.caseSensitive = entry.audioCaseSensitive;
                    question = au;
                    break;

                case QuestionType.DragDrop:
                    var dd = ScriptableObject.CreateInstance<DragDropQuestionData>();
                    if (entry.dragItems != null)
                    {
                        dd.dragItems = entry.dragItems.Select(d => new DragDropQuestionData.DragItem
                        {
                            label = d.label,
                            icon = !string.IsNullOrEmpty(d.iconPath) 
                                ? AssetDatabase.LoadAssetAtPath<Sprite>(d.iconPath) 
                                : null
                        }).ToList();
                    }
                    else
                    {
                        dd.dragItems = new List<DragDropQuestionData.DragItem>();
                    }
                    if (entry.dropZones != null)
                    {
                        dd.dropZones = entry.dropZones.Select(z => new DragDropQuestionData.DropZone
                        {
                            label = z.label,
                            icon = !string.IsNullOrEmpty(z.iconPath) 
                                ? AssetDatabase.LoadAssetAtPath<Sprite>(z.iconPath) 
                                : null
                        }).ToList();
                    }
                    else
                    {
                        dd.dropZones = new List<DragDropQuestionData.DropZone>();
                    }
                    dd.correctPairings = new Dictionary<int, int>();
                    if (entry.correctPairings != null)
                    {
                        foreach (var p in entry.correctPairings)
                        {
                            dd.correctPairings[p.sourceIndex] = p.targetIndex;
                        }
                    }
                    question = dd;
                    break;

                case QuestionType.Connect:
                    var cn = ScriptableObject.CreateInstance<ConnectQuestionData>();
                    if (entry.leftColumnItems != null)
                    {
                        cn.leftColumnItems = entry.leftColumnItems.Select(i => new ConnectQuestionData.ConnectItem
                        {
                            label = i.label,
                            icon = !string.IsNullOrEmpty(i.iconPath) 
                                ? AssetDatabase.LoadAssetAtPath<Sprite>(i.iconPath) 
                                : null
                        }).ToList();
                    }
                    else
                    {
                        cn.leftColumnItems = new List<ConnectQuestionData.ConnectItem>();
                    }
                    if (entry.rightColumnItems != null)
                    {
                        cn.rightColumnItems = entry.rightColumnItems.Select(i => new ConnectQuestionData.ConnectItem
                        {
                            label = i.label,
                            icon = !string.IsNullOrEmpty(i.iconPath) 
                                ? AssetDatabase.LoadAssetAtPath<Sprite>(i.iconPath) 
                                : null
                        }).ToList();
                    }
                    else
                    {
                        cn.rightColumnItems = new List<ConnectQuestionData.ConnectItem>();
                    }
                    cn.correctConnections = new Dictionary<int, int>();
                    if (entry.correctConnections != null)
                    {
                        foreach (var c in entry.correctConnections)
                        {
                            cn.correctConnections[c.sourceIndex] = c.targetIndex;
                        }
                    }
                    question = cn;
                    break;

                default:
                    Debug.LogWarning($"[QuestionImporter] Unhandled question type: {questionType}");
                    return null;
            }

            if (question != null)
            {
                // Set common properties
                question.questionText = entry.questionText ?? "";
                question.hints = entry.hints ?? new string[3];
                question.maxAttempts = entry.maxAttempts > 0 ? entry.maxAttempts : 3;
                question.points = entry.points > 0 ? entry.points : 10;
                question.explanation = entry.explanation ?? "";
                question.questionType = questionType;

                // Generate unique filename
                string sanitizedText = SanitizeFileName(entry.questionText);
                if (string.IsNullOrEmpty(sanitizedText)) sanitizedText = "Question";
                if (sanitizedText.Length > 30) sanitizedText = sanitizedText.Substring(0, 30);
                string filename = $"{questionType}_{sanitizedText}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/{filename}.asset");

                AssetDatabase.CreateAsset(question, assetPath);
            }

            return question;
        }

        private static string SanitizeFileName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Question";
            
            // Remove invalid filename characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            
            // Replace spaces with underscores
            text = text.Replace(' ', '_');
            
            // Remove multiple consecutive underscores
            while (text.Contains("__"))
            {
                text = text.Replace("__", "_");
            }

            return text.Trim('_');
        }
    }
}
