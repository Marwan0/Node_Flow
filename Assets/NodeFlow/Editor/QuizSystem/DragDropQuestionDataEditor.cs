#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace QuizSystem
{
    [CustomEditor(typeof(DragDropQuestionData))]
    public class DragDropQuestionDataEditor : Editor
    {
        private bool _pairingsFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var data = (DragDropQuestionData)target;

            // Draw all default fields EXCEPT the hidden dictionary backing lists
            DrawPropertiesExcluding(serializedObject,
                "_pairingKeys", "_pairingValues", "correctPairings");

            EditorGUILayout.Space(5);

            // === Custom Pairings UI ===
            _pairingsFoldout = EditorGUILayout.Foldout(_pairingsFoldout, "Correct Pairings", true, EditorStyles.foldoutHeader);
            if (_pairingsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPairings(data);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPairings(DragDropQuestionData data)
        {
            // Build label arrays for dropdowns
            string[] dragLabels = BuildDragLabels(data.dragItems);
            string[] dropLabels = BuildDropLabels(data.dropZones);

            if (dragLabels.Length == 0 || dropLabels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add items to both Drag Items and Drop Zones first, then define pairings here.",
                    MessageType.Info);
                return;
            }

            // Get the backing lists via SerializedProperty
            var keysProp = serializedObject.FindProperty("_pairingKeys");
            var valuesProp = serializedObject.FindProperty("_pairingValues");

            int count = Mathf.Min(keysProp.arraySize, valuesProp.arraySize);
            int removeIndex = -1;

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Drag item dropdown
                int dragIdx = keysProp.GetArrayElementAtIndex(i).intValue;
                dragIdx = Mathf.Clamp(dragIdx, 0, dragLabels.Length - 1);
                int newDrag = EditorGUILayout.Popup(dragIdx, dragLabels, GUILayout.MinWidth(80));
                if (newDrag != dragIdx)
                {
                    keysProp.GetArrayElementAtIndex(i).intValue = newDrag;
                }

                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // Drop zone dropdown
                int dropIdx = valuesProp.GetArrayElementAtIndex(i).intValue;
                dropIdx = Mathf.Clamp(dropIdx, 0, dropLabels.Length - 1);
                int newDrop = EditorGUILayout.Popup(dropIdx, dropLabels, GUILayout.MinWidth(80));
                if (newDrop != dropIdx)
                {
                    valuesProp.GetArrayElementAtIndex(i).intValue = newDrop;
                }

                // Remove button
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Handle removal
            if (removeIndex >= 0)
            {
                keysProp.DeleteArrayElementAtIndex(removeIndex);
                valuesProp.DeleteArrayElementAtIndex(removeIndex);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // Add button
            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Add Pairing"))
            {
                // Find the next unused drag index
                HashSet<int> usedDrag = new HashSet<int>();
                for (int i = 0; i < keysProp.arraySize; i++)
                    usedDrag.Add(keysProp.GetArrayElementAtIndex(i).intValue);

                int nextDrag = 0;
                for (int i = 0; i < dragLabels.Length; i++)
                {
                    if (!usedDrag.Contains(i)) { nextDrag = i; break; }
                }

                // Find the next unused drop index
                HashSet<int> usedDrop = new HashSet<int>();
                for (int i = 0; i < valuesProp.arraySize; i++)
                    usedDrop.Add(valuesProp.GetArrayElementAtIndex(i).intValue);

                int nextDrop = 0;
                for (int i = 0; i < dropLabels.Length; i++)
                {
                    if (!usedDrop.Contains(i)) { nextDrop = i; break; }
                }

                keysProp.arraySize++;
                valuesProp.arraySize++;
                keysProp.GetArrayElementAtIndex(keysProp.arraySize - 1).intValue = nextDrag;
                valuesProp.GetArrayElementAtIndex(valuesProp.arraySize - 1).intValue = nextDrop;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // Auto-map button
            if (dragLabels.Length == dropLabels.Length && dragLabels.Length > 0)
            {
                if (GUILayout.Button("Auto-Map 1:1 (Drag[0]→Drop[0], Drag[1]→Drop[1], ...)"))
                {
                    keysProp.arraySize = dragLabels.Length;
                    valuesProp.arraySize = dragLabels.Length;
                    for (int i = 0; i < dragLabels.Length; i++)
                    {
                        keysProp.GetArrayElementAtIndex(i).intValue = i;
                        valuesProp.GetArrayElementAtIndex(i).intValue = i;
                    }
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private string[] BuildDragLabels(List<DragDropQuestionData.DragItem> items)
        {
            if (items == null || items.Count == 0) return new string[0];

            string[] labels = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                string label = items[i] != null && !string.IsNullOrEmpty(items[i].label) ? items[i].label : $"Drag {i}";
                labels[i] = $"[{i}] {label}";
            }
            return labels;
        }

        private string[] BuildDropLabels(List<DragDropQuestionData.DropZone> items)
        {
            if (items == null || items.Count == 0) return new string[0];

            string[] labels = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                string label = items[i] != null && !string.IsNullOrEmpty(items[i].label) ? items[i].label : $"Drop {i}";
                labels[i] = $"[{i}] {label}";
            }
            return labels;
        }
    }
}
#endif

