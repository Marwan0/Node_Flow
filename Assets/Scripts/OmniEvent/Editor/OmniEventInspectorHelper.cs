#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace OmniEvent.Editor
{
    /// <summary>
    /// Helper class for OmniEvent inspector functionality.
    /// Provides utilities for enhanced type detection and argument handling.
    /// </summary>
    public static class OmniEventInspectorHelper
    {
        private static Dictionary<Type, bool> s_complexTypeCache = new Dictionary<Type, bool>();

        /// <summary>
        /// Check if a type should be displayed with enhanced UI (e.g., for Lists, Arrays, Enums).
        /// </summary>
        public static bool IsComplexType(Type type)
        {
            if (type == null) return false;
            
            if (s_complexTypeCache.TryGetValue(type, out bool isComplex))
                return isComplex;

            isComplex = CheckComplexType(type);
            s_complexTypeCache[type] = isComplex;
            return isComplex;
        }

        private static bool CheckComplexType(Type type)
        {
            // Primitive types and simple Unity types don't need enhancement
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                type == typeof(string) || type == typeof(bool) ||
                type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Color) || type == typeof(Quaternion) ||
                type == typeof(Rect) || type == typeof(Bounds) ||
                type == typeof(LayerMask))
            {
                return false;
            }

            // Enhance Enums, Lists, Arrays, and other complex types
            if (type.IsEnum || 
                type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IList<>)) ||
                type.IsArray)
            {
                return true;
            }

            // Types with generic parameters (like custom collections)
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (IsComplexType(arg))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a user-friendly display name for a type.
        /// </summary>
        public static string GetFriendlyTypeName(Type type)
        {
            if (type == null) return "null";

            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                var typeName = genericType.Name;

                // Remove the `1, `2, etc. suffix from generic type name
                var backtickIndex = typeName.IndexOf('`');
                if (backtickIndex > 0)
                    typeName = typeName.Substring(0, backtickIndex);

                var args = string.Join(", ", Array.ConvertAll(genericArgs, GetFriendlyTypeName));
                return $"{typeName}<{args}>";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{GetFriendlyTypeName(elementType)}[]";
            }

            // Handle nested types
            if (type.IsNested && type.DeclaringType != null)
            {
                return $"{GetFriendlyTypeName(type.DeclaringType)}.{type.Name}";
            }

            return type.Name;
        }

        /// <summary>
        /// Check if a SerializedProperty represents a dynamic reference or static value.
        /// </summary>
        public static bool IsDynamicReference(SerializedProperty property)
        {
            if (property == null) return false;
            
            // UnityEvent uses m_ObjectReference for dynamic object references
            var objectRef = property.FindPropertyRelative("m_ObjectReference");
            if (objectRef != null && objectRef.objectReferenceValue != null)
                return true;

            // For UnityEvent<T>, check if there are any object references in arguments
            var arguments = property.FindPropertyRelative("m_Arguments");
            if (arguments != null)
            {
                for (int i = 0; i < arguments.arraySize; i++)
                {
                    var arg = arguments.GetArrayElementAtIndex(i);
                    var objRef = arg.FindPropertyRelative("objectArgument");
                    if (objRef != null && objRef.objectReferenceValue != null)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the icon for a type (for visual enhancement in inspector).
        /// </summary>
        public static GUIContent GetTypeIcon(Type type)
        {
            if (type == null) return EditorGUIUtility.IconContent("cs Script Icon");

            if (type.IsEnum)
                return EditorGUIUtility.IconContent("Enum Icon");

            if (type == typeof(GameObject) || type.IsSubclassOf(typeof(UnityEngine.Object)))
                return EditorGUIUtility.IconContent("GameObject Icon");

            if (type == typeof(int) || type == typeof(float) || type == typeof(double))
                return EditorGUIUtility.IconContent("Numeric Icon");

            if (type == typeof(string))
                return EditorGUIUtility.IconContent("TextAsset Icon");

            if (type == typeof(bool))
                return EditorGUIUtility.IconContent("bool Icon");

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return EditorGUIUtility.IconContent("List Icon");

            if (type.IsArray)
                return EditorGUIUtility.IconContent("Array Icon");

            return EditorGUIUtility.IconContent("cs Script Icon");
        }
    }
}
#endif
