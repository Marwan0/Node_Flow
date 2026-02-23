#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for StringIdSelector.
    /// Draws:
    ///   1. A foldout list where you add/remove string IDs
    ///   2. A popup dropdown to pick one (like an enum)
    /// Pure Unity — no third-party dependencies.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringIdSelector))]
    public class StringIdSelectorDrawer : PropertyDrawer
    {
        private bool _foldout = false;
        private string _newId = "";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var idsProp = property.FindPropertyRelative("_ids");
            float h = EditorGUIUtility.singleLineHeight; // header row (label + dropdown)

            if (_foldout)
            {
                // Each existing ID row
                h += idsProp.arraySize * (EditorGUIUtility.singleLineHeight + 2f);
                // "Add new" row
                h += EditorGUIUtility.singleLineHeight + 4f;
            }

            return h + 4f; // small bottom padding
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idsProp = property.FindPropertyRelative("_ids");
            var selectedProp = property.FindPropertyRelative("_selectedId");

            EditorGUI.BeginProperty(position, label, property);

            float lineH = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            // ── Row 1: Foldout + Dropdown ────────────────────
            Rect foldoutRect = new Rect(position.x, y, position.width * 0.35f, lineH);
            Rect dropdownRect = new Rect(position.x + position.width * 0.36f, y, position.width * 0.64f, lineH);

            // Foldout to expand the ID list
            _foldout = EditorGUI.Foldout(foldoutRect, _foldout, label, true);

            // Build choices array from the _ids list
            List<string> choices = new List<string>();
            for (int i = 0; i < idsProp.arraySize; i++)
            {
                choices.Add(idsProp.GetArrayElementAtIndex(i).stringValue);
            }

            if (choices.Count == 0)
            {
                // No IDs yet — show a disabled label
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(dropdownRect, 0, new[] { "(add IDs below)" });
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // Find current selection index
                int currentIdx = choices.IndexOf(selectedProp.stringValue);
                if (currentIdx < 0) currentIdx = 0;

                int newIdx = EditorGUI.Popup(dropdownRect, currentIdx, choices.ToArray());
                if (newIdx >= 0 && newIdx < choices.Count)
                {
                    selectedProp.stringValue = choices[newIdx];
                }
            }

            y += lineH + 2f;

            // ── Foldout body: editable ID list ───────────────
            if (_foldout)
            {
                EditorGUI.indentLevel++;

                int removeIdx = -1;
                for (int i = 0; i < idsProp.arraySize; i++)
                {
                    var elem = idsProp.GetArrayElementAtIndex(i);
                    Rect rowRect = new Rect(position.x, y, position.width, lineH);

                    // Text field for existing ID
                    Rect textRect = new Rect(rowRect.x + EditorGUI.indentLevel * 15f, rowRect.y,
                        rowRect.width - 60f - EditorGUI.indentLevel * 15f, lineH);
                    Rect btnRect = new Rect(rowRect.xMax - 55f, rowRect.y, 55f, lineH);

                    string oldVal = elem.stringValue;
                    string newVal = EditorGUI.TextField(textRect, oldVal);
                    if (newVal != oldVal)
                    {
                        elem.stringValue = newVal;
                        // Update selection if the ID we renamed was selected
                        if (selectedProp.stringValue == oldVal)
                            selectedProp.stringValue = newVal;
                    }

                    if (GUI.Button(btnRect, "Remove"))
                    {
                        removeIdx = i;
                    }

                    y += lineH + 2f;
                }

                // Remove button handling
                if (removeIdx >= 0)
                {
                    string removedValue = idsProp.GetArrayElementAtIndex(removeIdx).stringValue;
                    idsProp.DeleteArrayElementAtIndex(removeIdx);

                    // If we removed the selected one, auto-select first
                    if (selectedProp.stringValue == removedValue)
                    {
                        selectedProp.stringValue = idsProp.arraySize > 0
                            ? idsProp.GetArrayElementAtIndex(0).stringValue
                            : "";
                    }
                }

                // ── "Add new ID" row ─────────────────────────
                Rect addRow = new Rect(position.x + EditorGUI.indentLevel * 15f, y,
                    position.width - EditorGUI.indentLevel * 15f, lineH);
                Rect addTextRect = new Rect(addRow.x, addRow.y, addRow.width - 45f, lineH);
                Rect addBtnRect = new Rect(addRow.xMax - 40f, addRow.y, 40f, lineH);

                _newId = EditorGUI.TextField(addTextRect, _newId);

                if (GUI.Button(addBtnRect, "+") && !string.IsNullOrWhiteSpace(_newId))
                {
                    // Check for duplicates
                    bool exists = false;
                    for (int i = 0; i < idsProp.arraySize; i++)
                    {
                        if (idsProp.GetArrayElementAtIndex(i).stringValue == _newId.Trim())
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        idsProp.InsertArrayElementAtIndex(idsProp.arraySize);
                        idsProp.GetArrayElementAtIndex(idsProp.arraySize - 1).stringValue = _newId.Trim();

                        // Auto-select if nothing selected
                        if (string.IsNullOrEmpty(selectedProp.stringValue))
                            selectedProp.stringValue = _newId.Trim();

                        _newId = "";
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif

