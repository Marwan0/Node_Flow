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

            // Draw all default fields EXCEPT the hand-drawn list
            DrawPropertiesExcluding(serializedObject, "correctPairings");

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

            // Get the backing list via SerializedProperty
            var pairingsProp = serializedObject.FindProperty("correctPairings");

            int count = pairingsProp.arraySize;
            int removeIndex = -1;

            for (int i = 0; i < count; i++)
            {
                var pairingProp = pairingsProp.GetArrayElementAtIndex(i);
                var dragProp = pairingProp.FindPropertyRelative("dragIndex");
                var dropProp = pairingProp.FindPropertyRelative("dropIndex");

                EditorGUILayout.BeginHorizontal();

                // Drag item dropdown
                int dragIdx = dragProp.intValue;
                dragIdx = Mathf.Clamp(dragIdx, 0, dragLabels.Length - 1);
                int newDrag = EditorGUILayout.Popup(dragIdx, dragLabels, GUILayout.MinWidth(80));
                if (newDrag != dragIdx)
                {
                    dragProp.intValue = newDrag;
                }

                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // Drop zone dropdown
                int dropIdx = dropProp.intValue;
                dropIdx = Mathf.Clamp(dropIdx, 0, dropLabels.Length - 1);
                int newDrop = EditorGUILayout.Popup(dropIdx, dropLabels, GUILayout.MinWidth(80));
                if (newDrop != dropIdx)
                {
                    dropProp.intValue = newDrop;
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
                pairingsProp.DeleteArrayElementAtIndex(removeIndex);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // Add button
            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Add Pairing"))
            {
                pairingsProp.arraySize++;
                var newElement = pairingsProp.GetArrayElementAtIndex(pairingsProp.arraySize - 1);
                newElement.FindPropertyRelative("dragIndex").intValue = 0;
                newElement.FindPropertyRelative("dropIndex").intValue = 0;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // Auto-map button
            if (dragLabels.Length == dropLabels.Length && dragLabels.Length > 0)
            {
                if (GUILayout.Button("Auto-Map 1:1 (Drag[0]→Drop[0], Drag[1]→Drop[1], ...)"))
                {
                    pairingsProp.arraySize = dragLabels.Length;
                    for (int i = 0; i < dragLabels.Length; i++)
                    {
                        var element = pairingsProp.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("dragIndex").intValue = i;
                        element.FindPropertyRelative("dropIndex").intValue = i;
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

