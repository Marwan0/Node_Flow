#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SetActiveNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SetActiveNode;
            if (node == null) return;

            // Mode selector
            CreateEnumField("Mode", node.mode, v =>
            {
                node.mode = v;
                MarkDirty();
                RequestRefresh();
            });

            // Active toggle
            CreateToggle("Set Active", node.setActive, v =>
            {
                node.setActive = v;
                MarkDirty();
            });

            switch (node.mode)
            {
                case SetActiveMode.Single:
                    DrawSingleTarget(node);
                    break;
                case SetActiveMode.List:
                case SetActiveMode.Random:
                    DrawTargetList(node);
                    break;
            }
        }

        private void DrawSingleTarget(SetActiveNode node)
        {
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                currentTarget = FindGameObjectByPath(node.targetPath);
            }

            CreateObjectField<GameObject>("Target", currentTarget, (GameObject go) =>
            {
                node.targetPath = go != null ? GetGameObjectPath(go) : "";
                MarkDirty();
            });

            if (currentTarget != null)
            {
                string status = currentTarget.activeSelf ? "\u2713 Currently Active" : "\u2717 Currently Inactive";
                Color statusColor = currentTarget.activeSelf ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
                CreateLabel(status, statusColor);
            }
        }

        private void DrawTargetList(SetActiveNode node)
        {
            if (node.targetPaths == null)
                node.targetPaths = new System.Collections.Generic.List<string>();

            if (node.mode == SetActiveMode.Random)
            {
                CreateLabel("One random target will be activated, others deactivated", new Color(0.7f, 0.7f, 0.5f));
            }

            // --- Drop zone: drag GameObjects from Hierarchy to add them all at once ---
            var dropZone = new VisualElement();
            dropZone.style.marginTop = 4;
            dropZone.style.marginBottom = 4;
            dropZone.style.paddingTop = 6;
            dropZone.style.paddingBottom = 6;
            dropZone.style.paddingLeft = 4;
            dropZone.style.paddingRight = 4;
            dropZone.style.borderTopWidth = 1;
            dropZone.style.borderBottomWidth = 1;
            dropZone.style.borderLeftWidth = 1;
            dropZone.style.borderRightWidth = 1;
            dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderTopLeftRadius = 4;
            dropZone.style.borderTopRightRadius = 4;
            dropZone.style.borderBottomLeftRadius = 4;
            dropZone.style.borderBottomRightRadius = 4;
            dropZone.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            dropZone.style.alignItems = Align.Center;
            dropZone.style.justifyContent = Justify.Center;

            var dropLabel = new Label("Drag & Drop GameObjects Here");
            dropLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            dropLabel.style.fontSize = 10;
            dropLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            dropLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            dropZone.Add(dropLabel);

            // Highlight on drag enter
            dropZone.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasGameObjectsInDrag())
                {
                    dropZone.style.borderTopColor = new Color(0.4f, 0.8f, 0.4f);
                    dropZone.style.borderBottomColor = new Color(0.4f, 0.8f, 0.4f);
                    dropZone.style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f);
                    dropZone.style.borderRightColor = new Color(0.4f, 0.8f, 0.4f);
                    dropZone.style.backgroundColor = new Color(0.2f, 0.35f, 0.2f, 0.5f);
                    dropLabel.text = "Drop to Add";
                    dropLabel.style.color = new Color(0.4f, 0.8f, 0.4f);
                }
            });

            dropZone.RegisterCallback<DragLeaveEvent>(evt =>
            {
                ResetDropZoneStyle(dropZone, dropLabel);
            });

            // Accept the drag
            dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasGameObjectsInDrag())
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            // Perform the drop
            dropZone.RegisterCallback<DragPerformEvent>(evt =>
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    GameObject go = obj as GameObject;
                    if (go == null) continue;

                    string path = GetGameObjectPath(go);
                    if (!node.targetPaths.Contains(path))
                        node.targetPaths.Add(path);
                }

                MarkDirty();
                RequestRefresh();
            });

            Container.Add(dropZone);

            // --- Draw existing entries ---
            for (int i = 0; i < node.targetPaths.Count; i++)
            {
                int index = i; // capture for closure
                string path = node.targetPaths[index];

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 1;
                row.style.marginBottom = 1;

                // Index label
                var indexLabel = new Label($"{index + 1}.");
                indexLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                indexLabel.style.fontSize = 10;
                indexLabel.style.minWidth = 16;
                row.Add(indexLabel);

                // Object field
                GameObject current = null;
                if (!string.IsNullOrEmpty(path))
                    current = FindGameObjectByPath(path);

                var field = new UnityEditor.UIElements.ObjectField()
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true,
                    value = current
                };
                field.style.flexGrow = 1;
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == null && evt.previousValue == null)
                        return;
                    var go = evt.newValue as GameObject;
                    node.targetPaths[index] = go != null ? GetGameObjectPath(go) : "";
                    MarkDirty();
                });
                row.Add(field);

                // Remove button
                var removeBtn = new Button(() =>
                {
                    node.targetPaths.RemoveAt(index);
                    MarkDirty();
                    RequestRefresh();
                });
                removeBtn.text = "\u2212";
                removeBtn.style.width = 20;
                removeBtn.style.height = 18;
                removeBtn.style.fontSize = 14;
                removeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                removeBtn.style.paddingTop = 0;
                removeBtn.style.paddingBottom = 0;
                removeBtn.style.paddingLeft = 0;
                removeBtn.style.paddingRight = 0;
                row.Add(removeBtn);

                Container.Add(row);
            }

            // Bottom row: Add button + Clear All
            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.marginTop = 4;

            var addBtn = new Button(() =>
            {
                node.targetPaths.Add("");
                MarkDirty();
                RequestRefresh();
            });
            addBtn.text = "+ Add";
            addBtn.style.height = 20;
            addBtn.style.fontSize = 10;
            addBtn.style.flexGrow = 1;
            bottomRow.Add(addBtn);

            if (node.targetPaths.Count > 0)
            {
                var clearBtn = new Button(() =>
                {
                    node.targetPaths.Clear();
                    MarkDirty();
                    RequestRefresh();
                });
                clearBtn.text = "Clear All";
                clearBtn.style.height = 20;
                clearBtn.style.fontSize = 10;
                clearBtn.style.flexGrow = 1;
                bottomRow.Add(clearBtn);
            }

            Container.Add(bottomRow);

            // Count label
            CreateLabel($"{node.targetPaths.Count} target(s)", new Color(0.5f, 0.5f, 0.5f));
        }

        private bool HasGameObjectsInDrag()
        {
            if (DragAndDrop.objectReferences == null) return false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject) return true;
            }
            return false;
        }

        private void ResetDropZoneStyle(VisualElement dropZone, Label dropLabel)
        {
            dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            dropZone.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            dropLabel.text = "Drag & Drop GameObjects Here";
            dropLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        }

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

            var found = GameObject.Find(path);
            if (found != null) return found;

            string[] pathParts = path.Split('/');
            string rootName = pathParts[0];

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    if (rootGo.name == rootName)
                    {
                        if (pathParts.Length == 1)
                            return rootGo;

                        Transform current = rootGo.transform;
                        for (int j = 1; j < pathParts.Length; j++)
                        {
                            current = current.Find(pathParts[j]);
                            if (current == null) break;
                        }

                        if (current != null)
                            return current.gameObject;
                    }
                }
            }

            string targetName = pathParts[pathParts.Length - 1];
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var result = FindInHierarchy(rootGo.transform, targetName);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInHierarchy(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif
