#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace OmniEvent.Editor
{
    /// <summary>
    /// Custom property drawer that adds a button to open the OmniEvent Inspector Window
    /// next to each OmniEvent field in the inspector.
    /// NOTE: This is an optional enhancement. The main drawer is OmniEventPropertyDrawer.
    /// Uncomment the CustomPropertyDrawer attribute to enable this feature.
    /// </summary>
    // [CustomPropertyDrawer(typeof(OmniEventBase), isForChildClasses: true)]
    public class OmniEventInspectorButton : PropertyDrawer
    {
        private const float k_ButtonWidth = 30f;
        private const float k_ButtonPadding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Reserve space for the button
            var buttonRect = new Rect(
                position.xMax - k_ButtonWidth,
                position.y,
                k_ButtonWidth,
                EditorGUIUtility.singleLineHeight);

            var fieldRect = new Rect(
                position.x,
                position.y,
                position.width - k_ButtonWidth - k_ButtonPadding,
                EditorGUI.GetPropertyHeight(property, label, true));

            // Draw the main OmniEvent field
            EditorGUI.PropertyField(fieldRect, property, label, true);

            // Draw the inspector button
            DrawInspectorButton(buttonRect, property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private void DrawInspectorButton(Rect rect, SerializedProperty property)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            var buttonContent = new GUIContent("⚙", "Open in OmniEvent Inspector");
            
            if (GUI.Button(rect, buttonContent, buttonStyle))
            {
                OpenInspectorWindow(property);
            }
        }

        private void OpenInspectorWindow(SerializedProperty property)
        {
            var window = EditorWindow.GetWindow<OmniEventInspectorWindow>("OmniEvent Inspector");
            window.SetEvent(property);
            window.Show();
        }
    }

    /// <summary>
    /// Inspector that adds a toolbar button for components with OmniEvent fields.
    /// NOTE: This is an optional enhancement. Uncomment to enable.
    /// </summary>
    // [CanEditMultipleObjects]
    // [CustomEditor(typeof(MonoBehaviour), true)]
    public class OmniEventComponentEditor : UnityEditor.Editor
    {
        private bool m_ShowOmniEventInspector = false;
        private OmniEventInspectorWindow m_InspectorWindow;

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            // Check if this component has any OmniEventBase fields
            if (HasOmniEventFields())
            {
                DrawOmniEventToolbar();
            }
        }

        private bool HasOmniEventFields()
        {
            if (target == null) return false;

            var type = target.GetType();
            var fields = type.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (typeof(OmniEventBase).IsAssignableFrom(field.FieldType))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawOmniEventToolbar()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("OmniEvent Tools", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open OmniEvent Inspector", EditorStyles.toolbarButton))
                {
                    OmniEventInspectorWindow.ShowWindow();
                }

                if (GUILayout.Button("Refresh Events", EditorStyles.toolbarButton))
                {
                    if (m_InspectorWindow != null)
                    {
                        m_InspectorWindow.Repaint();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
    }
}
#endif
