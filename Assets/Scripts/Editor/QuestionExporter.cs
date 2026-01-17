using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace QuizSystem
{
    /// <summary>
    /// Exports questions to JSON format
    /// </summary>
    public static class QuestionExporter
    {
        public static string ExportToJson(List<QuestionData> questions, bool prettyPrint = true)
        {
            var exportData = new QuestionExportData();

            foreach (var question in questions)
            {
                if (question == null) continue;
                
                var entry = ConvertToEntry(question);
                if (entry != null)
                {
                    exportData.questions.Add(entry);
                }
            }

            return JsonUtility.ToJson(exportData, prettyPrint);
        }

        public static void ExportToFile(List<QuestionData> questions, string filePath)
        {
            string json = ExportToJson(questions, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[QuestionExporter] Exported {questions.Count} questions to: {filePath}");
        }

        public static void ExportWithDialog(List<QuestionData> questions)
        {
            string defaultName = $"QuizExport_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = EditorUtility.SaveFilePanel(
                "Export Questions",
                Application.dataPath,
                defaultName,
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                ExportToFile(questions, path);
                EditorUtility.DisplayDialog("Export Complete", 
                    $"Successfully exported {questions.Count} questions to:\n{path}", "OK");
            }
        }

        private static QuestionEntry ConvertToEntry(QuestionData question)
        {
            var entry = new QuestionEntry
            {
                type = question.questionType.ToString(),
                questionText = question.questionText,
                hints = question.hints,
                maxAttempts = question.maxAttempts,
                points = question.points,
                explanation = question.explanation
            };

            switch (question)
            {
                case MultipleChoiceQuestionData mc:
                    entry.answers = mc.answers;
                    entry.correctAnswerIndex = mc.correctAnswerIndex;
                    break;

                case TrueFalseQuestionData tf:
                    entry.correctAnswer = tf.correctAnswer;
                    break;

                case FillInTheBlankQuestionData fitb:
                    entry.correctText = fitb.correctAnswer;
                    entry.alternativeAnswers = fitb.alternativeAnswers?.ToArray();
                    entry.caseSensitive = fitb.caseSensitive;
                    entry.allowPartialMatch = fitb.allowPartialMatch;
                    entry.partialMatchThreshold = fitb.partialMatchThreshold;
                    break;

                case MultiSelectQuestionData ms:
                    entry.options = ms.options?.ToArray();
                    entry.correctAnswerIndices = ms.correctAnswerIndices?.ToArray();
                    entry.allowPartialCredit = ms.allowPartialCredit;
                    break;

                case OrderingQuestionData ord:
                    entry.orderedItems = ord.items?.ToArray();
                    entry.shuffleItems = ord.shuffleItems;
                    entry.allowPartialCredit = ord.allowPartialCredit;
                    break;

                case HotspotQuestionData hs:
                    if (hs.image != null)
                    {
                        entry.imagePath = AssetDatabase.GetAssetPath(hs.image);
                    }
                    if (hs.hotspotRegions != null)
                    {
                        entry.hotspotRegions = hs.hotspotRegions.Select(h => new ExportHotspotRegion
                        {
                            name = h.name,
                            posX = h.normalizedPosition.x,
                            posY = h.normalizedPosition.y,
                            sizeX = h.normalizedSize.x,
                            sizeY = h.normalizedSize.y,
                            shape = h.shape.ToString(),
                            radius = h.normalizedRadius
                        }).ToArray();
                    }
                    entry.correctHotspotIndex = hs.correctHotspotIndex;
                    entry.allowMultipleSelections = hs.allowMultipleSelections;
                    entry.correctHotspotIndices = hs.correctHotspotIndices?.ToArray();
                    break;

                case SliderQuestionData sl:
                    entry.minValue = sl.valueRange.x;
                    entry.maxValue = sl.valueRange.y;
                    entry.correctValue = sl.correctValue;
                    entry.tolerance = sl.tolerance;
                    entry.useTolerance = sl.useTolerance;
                    entry.showValueLabels = sl.showValueLabels;
                    entry.showCurrentValue = sl.showCurrentValue;
                    entry.decimalPlaces = sl.decimalPlaces;
                    break;

                case AudioQuestionData au:
                    if (au.audioClip != null)
                    {
                        entry.audioClipPath = AssetDatabase.GetAssetPath(au.audioClip);
                    }
                    entry.allowReplay = au.allowReplay;
                    entry.autoPlay = au.autoPlay;
                    entry.maxPlayCount = au.maxPlayCount;
                    entry.audioAnswerType = au.answerType.ToString();
                    entry.audioAnswerOptions = au.answerOptions?.ToArray();
                    entry.audioCorrectIndex = au.correctAnswerIndex;
                    entry.audioCorrectText = au.correctAnswerText;
                    entry.audioCaseSensitive = au.caseSensitive;
                    break;

                case DragDropQuestionData dd:
                    if (dd.dragItems != null)
                    {
                        entry.dragItems = dd.dragItems.Select(d => new ExportDragItem
                        {
                            label = d.label,
                            iconPath = d.icon != null ? AssetDatabase.GetAssetPath(d.icon) : null
                        }).ToArray();
                    }
                    if (dd.dropZones != null)
                    {
                        entry.dropZones = dd.dropZones.Select(z => new ExportDropZone
                        {
                            label = z.label,
                            iconPath = z.icon != null ? AssetDatabase.GetAssetPath(z.icon) : null
                        }).ToArray();
                    }
                    if (dd.correctPairings != null)
                    {
                        entry.correctPairings = dd.correctPairings.Select(p => new ExportPairing
                        {
                            sourceIndex = p.Key,
                            targetIndex = p.Value
                        }).ToArray();
                    }
                    break;

                case ConnectQuestionData cn:
                    if (cn.leftColumnItems != null)
                    {
                        entry.leftColumnItems = cn.leftColumnItems.Select(i => new ExportConnectItem
                        {
                            label = i.label,
                            iconPath = i.icon != null ? AssetDatabase.GetAssetPath(i.icon) : null
                        }).ToArray();
                    }
                    if (cn.rightColumnItems != null)
                    {
                        entry.rightColumnItems = cn.rightColumnItems.Select(i => new ExportConnectItem
                        {
                            label = i.label,
                            iconPath = i.icon != null ? AssetDatabase.GetAssetPath(i.icon) : null
                        }).ToArray();
                    }
                    if (cn.correctConnections != null)
                    {
                        entry.correctConnections = cn.correctConnections.Select(c => new ExportPairing
                        {
                            sourceIndex = c.Key,
                            targetIndex = c.Value
                        }).ToArray();
                    }
                    break;

                default:
                    Debug.LogWarning($"[QuestionExporter] Unsupported question type: {question.GetType().Name}");
                    break;
            }

            return entry;
        }
    }
}
