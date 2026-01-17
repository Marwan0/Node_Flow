#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NodeSystem.Nodes;
using UnityEngine.SceneManagement;

namespace NodeSystem.Editor
{
    public class TweenPropertyNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as TweenPropertyNode;
            if (node == null) return;

            // === TARGET ===
            AddSectionLabel("Target");
            CreateGameObjectField("Target", node.targetPath, path => {
                node.targetPath = path;
                MarkDirty();
            });

            // === PROPERTY TYPE ===
            AddSectionLabel("Property");
            CreateEnumField("Type", node.propertyType, (TweenPropertyType v) => 
            {
                node.propertyType = v;
                MarkDirty();
                RequestRefresh();
            });

            // === FROM VALUE ===
            AddSectionLabel("From");
            
            // Use current toggle
            CreateToggle("Use Current", node.useCurrentAsFrom, v => {
                node.useCurrentAsFrom = v;
                MarkDirty();
                RequestRefresh();
            });

            if (!node.useCurrentAsFrom)
            {
                CreateEnumField("Mode", node.fromMode, (TweenValueMode v) => {
                    node.fromMode = v;
                    MarkDirty();
                    RequestRefresh();
                });

                if (node.fromMode == TweenValueMode.FromTransform)
                {
                    CreateGameObjectField("From Ref", node.fromTransformPath, path => {
                        node.fromTransformPath = path;
                        MarkDirty();
                    });
                }
                else
                {
                    DrawValueField("From", node.propertyType, node.fromValue, node.fromFloat,
                        (v3) => { node.fromValue = v3; MarkDirty(); },
                        (f) => { node.fromFloat = f; MarkDirty(); });
                }
            }

            // === TO VALUE ===
            AddSectionLabel("To");
            
            CreateEnumField("Mode", node.toMode, (TweenValueMode v) => {
                node.toMode = v;
                MarkDirty();
                RequestRefresh();
            });

            if (node.toMode == TweenValueMode.FromTransform)
            {
                CreateGameObjectField("To Ref", node.toTransformPath, path => {
                    node.toTransformPath = path;
                    MarkDirty();
                });
            }
            else
            {
                DrawValueField("To", node.propertyType, node.toValue, node.toFloat,
                    (v3) => { node.toValue = v3; MarkDirty(); },
                    (f) => { node.toFloat = f; MarkDirty(); });
            }

            // === TIMING ===
            AddSectionLabel("Timing");
            CreateFloatField("Duration", node.duration, v => {
                node.duration = Mathf.Max(0.01f, v);
                MarkDirty();
            });
            CreateFloatField("Delay", node.delay, v => {
                node.delay = Mathf.Max(0, v);
                MarkDirty();
            });

#if DOTWEEN
            // Ease type
            CreateEnumField("Ease", node.easeType, (DG.Tweening.Ease v) => {
                node.easeType = v;
                MarkDirty();
            });
#endif
        }

        private void AddSectionLabel(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 11;
            label.style.color = new Color(0.9f, 0.9f, 0.9f);
            label.style.marginTop = 6;
            label.style.marginBottom = 2;
            Container.Add(label);
        }

        private void DrawValueField(string label, TweenPropertyType propertyType, Vector3 vectorValue, float floatValue,
            System.Action<Vector3> onVectorChanged, System.Action<float> onFloatChanged)
        {
            switch (propertyType)
            {
                case TweenPropertyType.Alpha:
                    CreateSlider(label, floatValue, 0f, 1f, onFloatChanged);
                    break;
                    
                case TweenPropertyType.Color:
                    // RGB sliders
                    CreateSlider("R", vectorValue.x, 0f, 1f, v => 
                        onVectorChanged(new Vector3(v, vectorValue.y, vectorValue.z)));
                    CreateSlider("G", vectorValue.y, 0f, 1f, v => 
                        onVectorChanged(new Vector3(vectorValue.x, v, vectorValue.z)));
                    CreateSlider("B", vectorValue.z, 0f, 1f, v => 
                        onVectorChanged(new Vector3(vectorValue.x, vectorValue.y, v)));
                    break;
                    
                case TweenPropertyType.AnchoredPosition:
                case TweenPropertyType.SizeDelta:
                    // Vector2 field
                    CreateVector2Field(label, new Vector2(vectorValue.x, vectorValue.y), v => 
                        onVectorChanged(new Vector3(v.x, v.y, 0)));
                    break;
                    
                default:
                    // Vector3 field for Position, Rotation, Scale
                    CreateVector3Field(label, vectorValue, onVectorChanged);
                    break;
            }
        }

        private void CreateGameObjectField(string label, string currentPath, System.Action<string> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 60;
            labelElem.style.color = new Color(0.8f, 0.8f, 0.8f);
            labelElem.style.fontSize = 10;
            row.Add(labelElem);

            // Find current GameObject for the field
            GameObject currentObj = null;
            if (!string.IsNullOrEmpty(currentPath))
            {
                currentObj = FindGameObjectByPath(currentPath);
            }

            var objectField = new ObjectField();
            objectField.objectType = typeof(GameObject);
            objectField.value = currentObj;
            objectField.style.flexGrow = 1;
            objectField.RegisterValueChangedCallback(evt =>
            {
                var go = evt.newValue as GameObject;
                string path = go != null ? GetGameObjectPath(go) : "";
                onChanged(path);
            });
            row.Add(objectField);

            Container.Add(row);

            // Show path hint
            if (!string.IsNullOrEmpty(currentPath))
            {
                var pathHint = new Label(currentPath);
                pathHint.style.fontSize = 9;
                pathHint.style.color = new Color(0.5f, 0.5f, 0.5f);
                pathHint.style.marginLeft = 62;
                pathHint.style.marginBottom = 2;
                Container.Add(pathHint);
            }
        }

        private void CreateVector3Field(string label, Vector3 value, System.Action<Vector3> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 40;
            labelElem.style.color = new Color(0.8f, 0.8f, 0.8f);
            labelElem.style.fontSize = 10;
            row.Add(labelElem);

            var xField = CreateCompactFloatField("X", value.x, v => 
                onChanged(new Vector3(v, value.y, value.z)));
            row.Add(xField);

            var yField = CreateCompactFloatField("Y", value.y, v => 
                onChanged(new Vector3(value.x, v, value.z)));
            row.Add(yField);

            var zField = CreateCompactFloatField("Z", value.z, v => 
                onChanged(new Vector3(value.x, value.y, v)));
            row.Add(zField);

            Container.Add(row);
        }

        private void CreateVector2Field(string label, Vector2 value, System.Action<Vector2> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;

            var labelElem = new Label(label);
            labelElem.style.minWidth = 40;
            labelElem.style.color = new Color(0.8f, 0.8f, 0.8f);
            labelElem.style.fontSize = 10;
            row.Add(labelElem);

            var xField = CreateCompactFloatField("X", value.x, v => 
                onChanged(new Vector2(v, value.y)));
            row.Add(xField);

            var yField = CreateCompactFloatField("Y", value.y, v => 
                onChanged(new Vector2(value.x, v)));
            row.Add(yField);

            Container.Add(row);
        }

        private VisualElement CreateCompactFloatField(string label, float value, System.Action<float> onChanged)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginLeft = 2;

            var labelElem = new Label(label);
            labelElem.style.fontSize = 9;
            labelElem.style.color = new Color(0.6f, 0.6f, 0.6f);
            labelElem.style.marginRight = 2;
            container.Add(labelElem);

            var field = new FloatField();
            field.value = value;
            field.style.width = 45;
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            container.Add(field);

            return container;
        }

        #region GameObject Path Helpers

        private string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "";
            
            string path = go.name;
            Transform current = go.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }

        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            // Try direct find first
            var obj = GameObject.Find(path);
            if (obj != null) return obj;
            
            // Search through hierarchy (including disabled)
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == path) return root;
                    
                    if (path.StartsWith(root.name + "/"))
                    {
                        string remaining = path.Substring(root.name.Length + 1);
                        var found = FindInHierarchy(root.transform, remaining);
                        if (found != null) return found;
                    }
                    
                    var result = FindInHierarchy(root.transform, path);
                    if (result != null) return result;
                }
            }
            
            return null;
        }

        private GameObject FindInHierarchy(Transform parent, string path)
        {
            var direct = parent.Find(path);
            if (direct != null) return direct.gameObject;
            
            string[] parts = path.Split('/');
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == parts[0])
                {
                    if (parts.Length == 1) return child.gameObject;
                    string remaining = string.Join("/", parts, 1, parts.Length - 1);
                    var found = FindInHierarchy(child, remaining);
                    if (found != null) return found;
                }
            }
            
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindInHierarchy(parent.GetChild(i), path);
                if (found != null) return found;
            }
            
            return null;
        }

        #endregion
    }
}
#endif
