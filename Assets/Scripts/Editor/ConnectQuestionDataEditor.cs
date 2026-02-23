#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace QuizSystem
{
    [CustomEditor(typeof(ConnectQuestionData))]
    public class ConnectQuestionDataEditor : Editor
    {
        private bool _connectionsFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var data = (ConnectQuestionData)target;

            // Draw all default fields EXCEPT the hidden dictionary backing lists
            DrawPropertiesExcluding(serializedObject,
                "_connectionKeys", "_connectionValues", "correctConnections");

            EditorGUILayout.Space(5);

            // === Custom Connections UI ===
            _connectionsFoldout = EditorGUILayout.Foldout(_connectionsFoldout, "Correct Connections", true, EditorStyles.foldoutHeader);
            if (_connectionsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawConnections(data);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConnections(ConnectQuestionData data)
        {
            // Build label arrays for dropdowns
            string[] leftLabels = BuildLabels(data.leftColumnItems, "Left");
            string[] rightLabels = BuildLabels(data.rightColumnItems, "Right");

            if (leftLabels.Length == 0 || rightLabels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add items to both Left Column and Right Column first, then define connections here.",
                    MessageType.Info);
                return;
            }

            // Get the backing lists via SerializedProperty
            var keysProp = serializedObject.FindProperty("_connectionKeys");
            var valuesProp = serializedObject.FindProperty("_connectionValues");

            int count = Mathf.Min(keysProp.arraySize, valuesProp.arraySize);
            int removeIndex = -1;

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Left dropdown
                int leftIdx = keysProp.GetArrayElementAtIndex(i).intValue;
                leftIdx = Mathf.Clamp(leftIdx, 0, leftLabels.Length - 1);
                int newLeft = EditorGUILayout.Popup(leftIdx, leftLabels, GUILayout.MinWidth(80));
                if (newLeft != leftIdx)
                {
                    keysProp.GetArrayElementAtIndex(i).intValue = newLeft;
                }

                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // Right dropdown
                int rightIdx = valuesProp.GetArrayElementAtIndex(i).intValue;
                rightIdx = Mathf.Clamp(rightIdx, 0, rightLabels.Length - 1);
                int newRight = EditorGUILayout.Popup(rightIdx, rightLabels, GUILayout.MinWidth(80));
                if (newRight != rightIdx)
                {
                    valuesProp.GetArrayElementAtIndex(i).intValue = newRight;
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
                // Force the ISerializationCallbackReceiver to sync
                EditorUtility.SetDirty(target);
            }

            // Add button
            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Add Connection"))
            {
                // Find the next unused left index
                HashSet<int> usedLeft = new HashSet<int>();
                for (int i = 0; i < keysProp.arraySize; i++)
                    usedLeft.Add(keysProp.GetArrayElementAtIndex(i).intValue);

                int nextLeft = 0;
                for (int i = 0; i < leftLabels.Length; i++)
                {
                    if (!usedLeft.Contains(i)) { nextLeft = i; break; }
                }

                // Find the next unused right index
                HashSet<int> usedRight = new HashSet<int>();
                for (int i = 0; i < valuesProp.arraySize; i++)
                    usedRight.Add(valuesProp.GetArrayElementAtIndex(i).intValue);

                int nextRight = 0;
                for (int i = 0; i < rightLabels.Length; i++)
                {
                    if (!usedRight.Contains(i)) { nextRight = i; break; }
                }

                keysProp.arraySize++;
                valuesProp.arraySize++;
                keysProp.GetArrayElementAtIndex(keysProp.arraySize - 1).intValue = nextLeft;
                valuesProp.GetArrayElementAtIndex(valuesProp.arraySize - 1).intValue = nextRight;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            // Auto-map button (when both columns have the same count)
            if (leftLabels.Length == rightLabels.Length && leftLabels.Length > 0)
            {
                if (GUILayout.Button("Auto-Map 1:1 (Left[0]→Right[0], Left[1]→Right[1], ...)"))
                {
                    keysProp.arraySize = leftLabels.Length;
                    valuesProp.arraySize = leftLabels.Length;
                    for (int i = 0; i < leftLabels.Length; i++)
                    {
                        keysProp.GetArrayElementAtIndex(i).intValue = i;
                        valuesProp.GetArrayElementAtIndex(i).intValue = i;
                    }
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private string[] BuildLabels<T>(List<T> items, string prefix) where T : class
        {
            if (items == null || items.Count == 0) return new string[0];

            string[] labels = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i] as ConnectQuestionData.ConnectItem;
                string label = item != null && !string.IsNullOrEmpty(item.label) ? item.label : $"{prefix} {i}";
                labels[i] = $"[{i}] {label}";
            }
            return labels;
        }
    }
}
#endif

