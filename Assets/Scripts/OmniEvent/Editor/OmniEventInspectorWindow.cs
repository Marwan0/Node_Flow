#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace OmniEvent.Editor
{
    /// <summary>
    /// Advanced editor window for configuring OmniEvents with:
    /// - Enhanced argument configuration (static/dynamic)
    /// - Event reordering
    /// - Better type support for Lists, Arrays, Enums
    /// - Visual preview of event arguments
    /// </summary>
    public class OmniEventInspectorWindow : EditorWindow
    {
        private class EventCallData
        {
            public SerializedProperty Property { get; set; }
            public string MethodName { get; set; }
            public List<ArgumentData> Arguments { get; set; } = new List<ArgumentData>();
        }

        private class ArgumentData
        {
            public string TypeName { get; set; }
            public string FriendlyName { get; set; }
            public bool IsDynamic { get; set; }
            public UnityEngine.Object ObjectReference { get; set; }
            public string StringValue { get; set; }
            public object StaticValue { get; set; }
        }

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_EventProperty;
        private List<EventCallData> m_EventCalls = new List<EventCallData>();
        private Vector2 m_ScrollPosition;
        private bool m_ShowAdvanced = false;
        private GUIStyle m_ArgumentStyle;
        private GUIStyle m_DynamicStyle;
        private GUIStyle m_StaticStyle;

        [MenuItem("Window/OmniEvent Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<OmniEventInspectorWindow>("OmniEvent Inspector");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("OmniEvent Inspector", EditorGUIUtility.IconContent("cs Script Icon").image);
            InitializeStyles();
        }

        private void OnGUI()
        {
            if (m_EventProperty == null || m_EventProperty.serializedObject.targetObject == null)
            {
                DrawEmptyState();
                return;
            }

            RefreshEventCalls();
            DrawHeader();
            DrawEventList();
            DrawFooter();
        }

        public void SetEvent(SerializedProperty eventProperty)
        {
            m_EventProperty = eventProperty;
            if (eventProperty != null)
            {
                m_SerializedObject = eventProperty.serializedObject;
            }
            RefreshEventCalls();
            Repaint();
        }

        private void InitializeStyles()
        {
            m_ArgumentStyle = new GUIStyle(EditorStyles.label);
            m_ArgumentStyle.padding = new RectOffset(5, 5, 2, 2);
            m_ArgumentStyle.margin = new RectOffset(0, 0, 2, 2);
            m_ArgumentStyle.fontSize = 11;

            m_DynamicStyle = new GUIStyle(m_ArgumentStyle);
            m_DynamicStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

            m_StaticStyle = new GUIStyle(m_ArgumentStyle);
            m_StaticStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.HelpBox(
                "Select an OmniEvent field in the Inspector and click the 'Open in OmniEvent Inspector' button to configure it.",
                MessageType.Info);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label("OmniEvent Configuration", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                m_ShowAdvanced = GUILayout.Toggle(m_ShowAdvanced, "Advanced", EditorStyles.toolbarButton);
                
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshEventCalls();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawEventList()
        {
            if (m_EventCalls.Count == 0)
            {
                EditorGUILayout.HelpBox("No listeners configured. Click the '+' button to add listeners.", MessageType.Info);
                return;
            }

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            for (int i = 0; i < m_EventCalls.Count; i++)
            {
                DrawEventCall(i, m_EventCalls[i]);
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEventCall(int index, EventCallData callData)
        {
            var callRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Header with reordering buttons
                DrawEventCallHeader(index, callData);

                // Arguments
                if (callData.Arguments.Count > 0)
                {
                    EditorGUILayout.Space();
                    DrawArguments(callData);
                }

                // Advanced options
                if (m_ShowAdvanced)
                {
                    EditorGUILayout.Space();
                    DrawAdvancedOptions(callData);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEventCallHeader(int index, EventCallData callData)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // Method name
                GUIContent methodLabel = new GUIContent(
                    callData.MethodName,
                    EditorGUIUtility.IconContent("Method Icon").image);
                EditorGUILayout.LabelField(methodLabel, EditorStyles.boldLabel, GUILayout.Width(200));
                
                GUILayout.FlexibleSpace();

                // Reorder buttons
                GUI.enabled = index > 0;
                if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
                {
                    SwapEventCalls(index, index - 1);
                }
                GUI.enabled = true;

                GUI.enabled = index < m_EventCalls.Count - 1;
                if (GUILayout.Button("↓", EditorStyles.miniButtonMid, GUILayout.Width(30)))
                {
                    SwapEventCalls(index, index + 1);
                }
                GUI.enabled = true;

                // Remove button
                if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                {
                    RemoveEventCall(index);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Target object reference
            var targetProp = callData.Property.FindPropertyRelative("m_Target");
            if (targetProp != null)
            {
                EditorGUILayout.PropertyField(targetProp, new GUIContent("Target"), true);
            }
        }

        private void DrawArguments(EventCallData callData)
        {
            EditorGUILayout.LabelField("Arguments:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var arg in callData.Arguments)
            {
                DrawArgument(arg);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawArgument(ArgumentData arg)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                // Type icon and name
                GUIContent typeContent = new GUIContent(
                    arg.FriendlyName,
                    OmniEventInspectorHelper.GetTypeIcon(GetTypeFromName(arg.TypeName)).image);
                EditorGUILayout.LabelField(typeContent, arg.IsDynamic ? m_DynamicStyle : m_StaticStyle, GUILayout.Width(180));

                GUILayout.FlexibleSpace();

                // Mode indicator
                GUIContent modeIcon = arg.IsDynamic
                    ? new GUIContent("● Dynamic", "Using a reference to a live object/property")
                    : new GUIContent("■ Static", "Using a fixed value");
                EditorGUILayout.LabelField(modeIcon, EditorStyles.miniLabel, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            // Static value editor
            if (!arg.IsDynamic && m_ShowAdvanced)
            {
                EditorGUI.indentLevel++;
                DrawStaticValueEditor(arg);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawStaticValueEditor(ArgumentData arg)
        {
            var type = GetTypeFromName(arg.TypeName);
            if (type == null) return;

            EditorGUILayout.LabelField("Static Value:");

            if (type == typeof(int))
            {
                var newValue = EditorGUILayout.IntField(arg.StaticValue != null ? (int)arg.StaticValue : 0);
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else if (type == typeof(float))
            {
                var newValue = EditorGUILayout.FloatField(arg.StaticValue != null ? (float)arg.StaticValue : 0f);
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else if (type == typeof(string))
            {
                var newValue = EditorGUILayout.TextField(arg.StaticValue != null ? (string)arg.StaticValue : "");
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else if (type == typeof(bool))
            {
                var newValue = EditorGUILayout.Toggle(arg.StaticValue != null ? (bool)arg.StaticValue : false);
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else if (type == typeof(Vector3))
            {
                var newValue = EditorGUILayout.Vector3Field("", arg.StaticValue != null ? (Vector3)arg.StaticValue : Vector3.zero);
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                var newValue = EditorGUILayout.EnumPopup(
                    arg.StaticValue != null ? (Enum)arg.StaticValue : (Enum)enumValues.GetValue(0)
                );
                if (GUI.changed) arg.StaticValue = newValue;
            }
            else
            {
                EditorGUILayout.HelpBox($"Complex type: {arg.FriendlyName}\nEdit this argument in the main UnityEvent inspector.", MessageType.Info);
            }
        }

        private void DrawAdvancedOptions(EventCallData callData)
        {
            EditorGUILayout.LabelField("Advanced Options:", EditorStyles.boldLabel);
            
            // Persistent call count info
            EditorGUILayout.LabelField($"Persistent Calls: {callData.Property.FindPropertyRelative("m_PersistentCalls").arraySize}");
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open in Inspector", GUILayout.Width(150)))
                {
                    // Find the target object and select it
                    if (m_SerializedObject != null && m_SerializedObject.targetObject != null)
                    {
                        Selection.activeObject = m_SerializedObject.targetObject;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshEventCalls()
        {
            if (m_EventProperty == null) return;

            m_EventCalls.Clear();
            m_SerializedObject?.Update();

            var callsProp = m_EventProperty.FindPropertyRelative("m_PersistentCalls");
            if (callsProp != null)
            {
                for (int i = 0; i < callsProp.arraySize; i++)
                {
                    var callData = ParseEventCall(callsProp.GetArrayElementAtIndex(i));
                    if (callData != null)
                    {
                        m_EventCalls.Add(callData);
                    }
                }
            }
        }

        private EventCallData ParseEventCall(SerializedProperty callProp)
        {
            var callData = new EventCallData
            {
                Property = callProp
            };

            // Get method name
            var methodNameProp = callProp.FindPropertyRelative("m_MethodName");
            if (methodNameProp != null)
            {
                callData.MethodName = methodNameProp.stringValue;
            }

            // Parse arguments
            var argumentsProp = callProp.FindPropertyRelative("m_Arguments");
            if (argumentsProp != null)
            {
                for (int i = 0; i < argumentsProp.arraySize; i++)
                {
                    var arg = ParseArgument(argumentsProp.GetArrayElementAtIndex(i));
                    if (arg != null)
                    {
                        callData.Arguments.Add(arg);
                    }
                }
            }

            return callData;
        }

        private ArgumentData ParseArgument(SerializedProperty argProp)
        {
            var typeNameProp = argProp.FindPropertyRelative("typeName");
            if (typeNameProp == null || string.IsNullOrEmpty(typeNameProp.stringValue))
                return null;

            var argData = new ArgumentData
            {
                TypeName = typeNameProp.stringValue,
                FriendlyName = OmniEventInspectorHelper.GetFriendlyTypeName(GetTypeFromName(typeNameProp.stringValue))
            };

            // Check if dynamic
            var objRefProp = argProp.FindPropertyRelative("objectArgument");
            if (objRefProp != null && objRefProp.objectReferenceValue != null)
            {
                argData.IsDynamic = true;
                argData.ObjectReference = objRefProp.objectReferenceValue;
            }

            // Get static values
            argData.StringValue = argProp.FindPropertyRelative("stringArgument")?.stringValue;
            
            return argData;
        }

        private void SwapEventCalls(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= m_EventCalls.Count || 
                indexB < 0 || indexB >= m_EventCalls.Count)
                return;

            // Swap in the underlying UnityEvent
            var callsProp = m_EventProperty.FindPropertyRelative("m_PersistentCalls");
            if (callsProp != null)
            {
                callsProp.MoveArrayElement(indexA, indexB);
                m_SerializedObject?.ApplyModifiedProperties();
                RefreshEventCalls();
            }
        }

        private void RemoveEventCall(int index)
        {
            if (index < 0 || index >= m_EventCalls.Count)
                return;

            var callsProp = m_EventProperty.FindPropertyRelative("m_PersistentCalls");
            if (callsProp != null)
            {
                callsProp.DeleteArrayElementAtIndex(index);
                m_SerializedObject?.ApplyModifiedProperties();
                RefreshEventCalls();
            }
        }

        private Type GetTypeFromName(string typeName)
        {
            try
            {
                return Type.GetType(typeName);
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
