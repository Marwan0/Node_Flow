#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace OmniEvent.Editor
{
    /// <summary>
    /// Draws OmniEvent and adds a "Parameters" block below so argument fields are always visible,
    /// even when the selected method is in Unity's Dynamic section (which normally hides them).
    /// </summary>
    [CustomPropertyDrawer(typeof(OmniEventBase), true)]
    public class OmniEventDrawer : PropertyDrawer
    {
        private const float ParamBlockPadding = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mEvent = property.FindPropertyRelative("m_Event");
            if (mEvent == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            mEvent.isExpanded = true;
            TryExpandFirstListener(mEvent);

            float eventHeight = GetEventHeight(mEvent, label);
            Rect eventRect = new Rect(position.x, position.y, position.width, eventHeight);
            EditorGUI.PropertyField(eventRect, mEvent, label, true);

            float paramY = position.y + eventHeight + EditorGUIUtility.standardVerticalSpacing;
            float paramHeight = TryDrawParameterBlock(property, position.x, paramY, position.width);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty mEvent = property.FindPropertyRelative("m_Event");
            if (mEvent == null)
                return EditorGUIUtility.singleLineHeight;

            float h = GetEventHeight(mEvent, label);
            h += EditorGUIUtility.standardVerticalSpacing;
            h += TryGetParameterBlockHeight(property);
            return h;
        }

        private static float GetEventHeight(SerializedProperty mEvent, GUIContent label)
        {
            try
            {
                return EditorGUI.GetPropertyHeight(mEvent, label, true);
            }
            catch
            {
                return EditorGUIUtility.singleLineHeight * 2;
            }
        }

        private static void TryExpandFirstListener(SerializedProperty mEvent)
        {
            try
            {
                SerializedProperty calls = mEvent.FindPropertyRelative("m_PersistentCalls");
                if (calls == null) return;
                if (!calls.isArray) return;
                int count = calls.arraySize;
                if (count <= 0) return;
                SerializedProperty first = calls.GetArrayElementAtIndex(0);
                if (first != null)
                    first.isExpanded = true;
            }
            catch { /* ignore */ }
        }

        private static float TryGetParameterBlockHeight(SerializedProperty property)
        {
            try
            {
                SerializedProperty mEvent = property.FindPropertyRelative("m_Event");
                if (mEvent == null) return 0f;
                SerializedProperty calls = mEvent.FindPropertyRelative("m_PersistentCalls");
                if (calls == null || !calls.isArray) return 0f;
                int callCount = calls.arraySize;
                if (callCount <= 0) return 0f;
                SerializedProperty first = calls.GetArrayElementAtIndex(0);
                if (first == null) return 0f;
                SerializedProperty args = first.FindPropertyRelative("m_Arguments");
                if (args == null || !args.isArray) return 0f;
                int argCount = args.arraySize;
                if (argCount <= 0) return 0f;
                float line = EditorGUIUtility.singleLineHeight;
                float header = line + ParamBlockPadding;
                float total = header + (argCount * (line + line + 2));
                return total + ParamBlockPadding;
            }
            catch
            {
                return 0f;
            }
        }

        private static float TryDrawParameterBlock(SerializedProperty property, float x, float y, float width)
        {
            try
            {
                SerializedProperty mEvent = property.FindPropertyRelative("m_Event");
                if (mEvent == null) return 0f;
                SerializedProperty calls = mEvent.FindPropertyRelative("m_PersistentCalls");
                if (calls == null || !calls.isArray) return 0f;
                int callCount = calls.arraySize;
                if (callCount <= 0) return 0f;
                SerializedProperty first = calls.GetArrayElementAtIndex(0);
                if (first == null) return 0f;
                SerializedProperty args = first.FindPropertyRelative("m_Arguments");
                if (args == null || !args.isArray) return 0f;
                int argCount = args.arraySize;
                if (argCount <= 0) return 0f;

                float line = EditorGUIUtility.singleLineHeight;
                float currentY = y;

                Rect boxRect = new Rect(x, currentY, width, 0f);
                float contentHeight = (line * 2) + (argCount * (line + line + 2)) + ParamBlockPadding * 2;
                boxRect.height = contentHeight;
                GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);
                currentY += ParamBlockPadding;

                GUI.Label(new Rect(x + 4, currentY, width - 8, line), "Parameters (set values passed to the method)", EditorStyles.miniLabel);
                currentY += line + 2;

                for (int i = 0; i < argCount; i++)
                {
                    SerializedProperty arg = args.GetArrayElementAtIndex(i);
                    if (arg == null) continue;
                    string typeName = arg.FindPropertyRelative("typeName")?.stringValue;
                    if (string.IsNullOrEmpty(typeName)) continue;

                    Type type = Type.GetType(typeName);
                    string friendlyName = type != null ? OmniEventInspectorHelper.GetFriendlyTypeName(type) : typeName;
                    GUI.Label(new Rect(x + 8, currentY, width - 16, line), $"{i + 1}. {friendlyName}", EditorStyles.miniLabel);
                    currentY += line;

                    SerializedProperty objRef = arg.FindPropertyRelative("objectArgument");
                    bool isDynamic = objRef != null && objRef.objectReferenceValue != null;

                    if (isDynamic && objRef != null)
                    {
                        float fh = EditorGUI.GetPropertyHeight(objRef);
                        EditorGUI.PropertyField(new Rect(x + 8, currentY, width - 16, fh), objRef, new GUIContent("Object"));
                        currentY += fh + 2;
                        continue;
                    }

                    SerializedProperty intProp = arg.FindPropertyRelative("intArgument");
                    SerializedProperty floatProp = arg.FindPropertyRelative("floatArgument");
                    SerializedProperty stringProp = arg.FindPropertyRelative("stringArgument");
                    SerializedProperty boolProp = arg.FindPropertyRelative("boolArgument");
                    SerializedProperty vec3Prop = arg.FindPropertyRelative("vector3Argument");
                    SerializedProperty colorProp = arg.FindPropertyRelative("colorArgument");

                    Rect fieldRect = new Rect(x + 8, currentY, width - 16, line);

                    if (type == typeof(int) && intProp != null)
                    {
                        intProp.intValue = EditorGUI.IntField(fieldRect, intProp.intValue);
                    }
                    else if (type == typeof(float) && floatProp != null)
                    {
                        floatProp.floatValue = EditorGUI.FloatField(fieldRect, floatProp.floatValue);
                    }
                    else if (type == typeof(string) && stringProp != null)
                    {
                        stringProp.stringValue = EditorGUI.TextField(fieldRect, stringProp.stringValue);
                    }
                    else if (type == typeof(bool) && boolProp != null)
                    {
                        boolProp.boolValue = EditorGUI.Toggle(fieldRect, boolProp.boolValue);
                    }
                    else if (type == typeof(Vector3) && vec3Prop != null)
                    {
                        vec3Prop.vector3Value = EditorGUI.Vector3Field(fieldRect, "", vec3Prop.vector3Value);
                    }
                    else if (type == typeof(Color) && colorProp != null)
                    {
                        colorProp.colorValue = EditorGUI.ColorField(fieldRect, colorProp.colorValue);
                    }
                    else if (type != null && type.IsEnum && intProp != null)
                    {
                        intProp.intValue = EditorGUI.IntField(fieldRect, intProp.intValue);
                    }
                    else if (objRef != null)
                    {
                        float fh = EditorGUI.GetPropertyHeight(objRef);
                        EditorGUI.PropertyField(new Rect(x + 8, currentY, width - 16, fh), objRef, new GUIContent("Object"));
                        currentY += fh;
                        currentY += 2;
                        continue;
                    }
                    else
                    {
                        GUI.Label(fieldRect, $"({friendlyName})", EditorStyles.miniLabel);
                    }

                    currentY += line + 2;
                }

                property.serializedObject.ApplyModifiedProperties();
                return currentY - y + ParamBlockPadding;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
#endif
