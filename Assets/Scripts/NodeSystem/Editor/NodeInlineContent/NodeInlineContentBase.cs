#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Base class for rendering inline content inside nodes
    /// </summary>
    public abstract class NodeInlineContentBase
    {
        protected NodeData Node { get; private set; }
        protected VisualElement Container { get; private set; }
        protected Action OnDataChanged { get; private set; }
        protected Action OnRefreshRequested { get; private set; }

        public void Initialize(NodeData node, VisualElement container, Action onDataChanged, Action onRefreshRequested = null)
        {
            Node = node;
            Container = container;
            OnDataChanged = onDataChanged;
            OnRefreshRequested = onRefreshRequested;
        }

        /// <summary>
        /// Request a refresh of the inline content (useful when type changes)
        /// </summary>
        protected void RequestRefresh()
        {
            OnRefreshRequested?.Invoke();
        }

        /// <summary>
        /// Draw the inline content
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Called before content is cleared for refresh. Override to cleanup resources.
        /// </summary>
        public virtual void Cleanup()
        {
            // Override in derived classes to cleanup cached resources
        }

        /// <summary>
        /// Mark data as changed (for saving)
        /// </summary>
        protected void MarkDirty()
        {
            OnDataChanged?.Invoke();
        }

        // === Helper Methods ===

        protected TextField CreateTextField(string value, Action<string> onChanged, string placeholder = "")
        {
            var field = new TextField() { value = value ?? "" };
            field.style.marginTop = 2;
            field.style.marginBottom = 2;
            field.style.minWidth = 80;
            
            if (!string.IsNullOrEmpty(placeholder) && string.IsNullOrEmpty(value))
            {
                field.Q<TextElement>().text = placeholder;
            }
            
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            Container.Add(field);
            return field;
        }

        protected IntegerField CreateIntField(string label, int value, Action<int> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var field = new IntegerField() { value = value };
            field.style.flexGrow = 1;
            field.style.minWidth = 40;
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            row.Add(field);

            Container.Add(row);
            return field;
        }

        protected FloatField CreateFloatField(string label, float value, Action<float> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var field = new FloatField() { value = value };
            field.style.flexGrow = 1;
            field.style.minWidth = 40;
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            row.Add(field);

            Container.Add(row);
            return field;
        }

        protected Slider CreateSlider(string label, float value, float min, float max, Action<float> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var slider = new Slider(min, max) { value = value };
            slider.style.flexGrow = 1;
            slider.style.minWidth = 60;
            slider.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            row.Add(slider);

            var valueLabel = new Label(value.ToString("F1"));
            valueLabel.style.minWidth = 25;
            valueLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            valueLabel.style.fontSize = 10;
            slider.RegisterValueChangedCallback(evt => valueLabel.text = evt.newValue.ToString("F1"));
            row.Add(valueLabel);

            Container.Add(row);
            return slider;
        }

        protected Toggle CreateToggle(string label, bool value, Action<bool> onChanged)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.style.marginTop = 2;
            toggle.style.marginBottom = 2;
            toggle.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                MarkDirty();
            });
            Container.Add(toggle);
            return toggle;
        }

        protected DropdownField CreateDropdown(string label, int index, string[] choices, Action<int> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var dropdown = new DropdownField(new System.Collections.Generic.List<string>(choices), index);
            dropdown.style.flexGrow = 1;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                onChanged(Array.IndexOf(choices, evt.newValue));
                MarkDirty();
            });
            row.Add(dropdown);

            Container.Add(row);
            return dropdown;
        }

        protected EnumField CreateEnumField<T>(string label, T value, Action<T> onChanged) where T : Enum
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var field = new EnumField(value);
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged((T)evt.newValue);
                MarkDirty();
            });
            row.Add(field);

            Container.Add(row);
            return field;
        }

        protected void CreateLabel(string text, Color? color = null)
        {
            var label = new Label(text);
            label.style.color = color ?? new Color(0.6f, 0.6f, 0.6f);
            label.style.fontSize = 10;
            label.style.marginTop = 2;
            Container.Add(label);
        }

        protected ObjectField CreateObjectField<T>(string label, UnityEngine.Object currentValue, Action<T> onChanged) where T : UnityEngine.Object
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(label))
            {
                var labelElement = new Label(label);
                labelElement.style.minWidth = 50;
                labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
                labelElement.style.fontSize = 10;
                row.Add(labelElement);
            }

            var field = new ObjectField()
            {
                objectType = typeof(T),
                allowSceneObjects = true,
                value = currentValue
            };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
            {
                onChanged((T)evt.newValue);
                MarkDirty();
            });
            row.Add(field);

            Container.Add(row);
            return field;
        }
    }
}
#endif

