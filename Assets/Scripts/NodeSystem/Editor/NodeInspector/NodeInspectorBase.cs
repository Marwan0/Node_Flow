#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Base class for custom node inspectors
    /// </summary>
    public abstract class NodeInspectorBase
    {
        protected NodeData Node { get; private set; }
        protected NodeGraph Graph { get; private set; }
        protected VisualElement Container { get; private set; }

        public void Initialize(NodeData node, NodeGraph graph, VisualElement container)
        {
            Node = node;
            Graph = graph;
            Container = container;
        }

        /// <summary>
        /// Draw the custom inspector UI
        /// </summary>
        public abstract void DrawInspector();

        /// <summary>
        /// Mark the graph as dirty when a value changes
        /// </summary>
        protected void MarkDirty()
        {
            if (Graph != null)
            {
                EditorUtility.SetDirty(Graph);
            }
        }

        // === Helper Methods for Creating UI Elements ===

        protected TextField CreateTextField(string label, string value, Action<string> onChanged)
        {
            var field = new TextField(label) { value = value ?? "" };
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected FloatField CreateFloatField(string label, float value, Action<float> onChanged)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected IntegerField CreateIntField(string label, int value, Action<int> onChanged)
        {
            var field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected Toggle CreateToggle(string label, bool value, Action<bool> onChanged)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            toggle.style.marginBottom = 5;
            Container.Add(toggle);
            return toggle;
        }

        protected EnumField CreateEnumField<T>(string label, T value, Action<T> onChanged) where T : Enum
        {
            var field = new EnumField(label, value);
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged((T)evt.newValue);
                MarkDirty();
            });
            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected DropdownField CreateDropdown(string label, string value, string[] choices, Action<string> onChanged)
        {
            var index = Array.IndexOf(choices, value);
            if (index < 0) index = 0;
            
            var field = new DropdownField(label, new System.Collections.Generic.List<string>(choices), index);
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected ObjectField CreateObjectField<T>(string label, string currentPath, Action<string> onPathChanged) where T : UnityEngine.Object
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(T),
                allowSceneObjects = true
            };

            // Try to find current object from path
            if (!string.IsNullOrEmpty(currentPath))
            {
                var go = GameObject.Find(currentPath);
                if (go != null)
                {
                    if (typeof(T) == typeof(GameObject))
                    {
                        field.value = go;
                    }
                    else
                    {
                        field.value = go.GetComponent(typeof(T));
                    }
                }
            }

            field.RegisterValueChangedCallback(evt =>
            {
                string path = "";
                if (evt.newValue != null)
                {
                    if (evt.newValue is GameObject gameObject)
                    {
                        path = GetGameObjectPath(gameObject);
                    }
                    else if (evt.newValue is Component component)
                    {
                        path = GetGameObjectPath(component.gameObject);
                    }
                }
                onPathChanged(path);
                MarkDirty();
            });

            field.style.marginBottom = 5;
            Container.Add(field);
            return field;
        }

        protected void CreateLabel(string text, bool bold = false)
        {
            var label = new Label(text);
            if (bold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            label.style.marginTop = 5;
            label.style.marginBottom = 3;
            Container.Add(label);
        }

        protected void CreateSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            separator.style.marginTop = 10;
            separator.style.marginBottom = 10;
            Container.Add(separator);
        }

        /// <summary>
        /// Get the full hierarchy path of a GameObject
        /// </summary>
        protected string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
}
#endif

