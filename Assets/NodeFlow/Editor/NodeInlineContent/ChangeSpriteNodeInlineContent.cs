#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class ChangeSpriteNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ChangeSpriteNode;
            if (node == null) return;

            // Target GameObject - drag and drop support
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                currentTarget = FindGameObjectByPath(node.targetPath);
            }

            CreateObjectField<GameObject>("Target", currentTarget, (GameObject go) =>
            {
                if (go != null)
                {
                    node.targetPath = GetGameObjectPath(go);
                }
                else
                {
                    node.targetPath = "";
                }
                MarkDirty();
            });

            // Sprite field
            CreateObjectField<Sprite>("Sprite", node.sprite, (Sprite s) =>
            {
                node.sprite = s;
                MarkDirty();
                RequestRefresh();
            });

            // Sprite preview
            if (node.sprite != null && node.sprite.texture != null)
            {
                var preview = new VisualElement();
                preview.style.marginTop = 4;
                preview.style.marginBottom = 2;
                preview.style.alignSelf = Align.Center;
                preview.style.width = 64;
                preview.style.height = 64;
                preview.style.backgroundImage = new StyleBackground(node.sprite.texture);
#if UNITY_2022_2_OR_NEWER
                preview.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                preview.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                preview.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                preview.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
#else
                preview.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
#endif
                preview.style.borderTopWidth = 1;
                preview.style.borderBottomWidth = 1;
                preview.style.borderLeftWidth = 1;
                preview.style.borderRightWidth = 1;
                preview.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                preview.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                preview.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                preview.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                preview.style.borderTopLeftRadius = 4;
                preview.style.borderTopRightRadius = 4;
                preview.style.borderBottomLeftRadius = 4;
                preview.style.borderBottomRightRadius = 4;
                Container.Add(preview);
            }

            // Color toggle + picker
            CreateToggle("Change Color", node.changeColor, v =>
            {
                node.changeColor = v;
                MarkDirty();
                RequestRefresh();
            });

            if (node.changeColor)
            {
                CreateColorField("Color", node.targetColor, c =>
                {
                    node.targetColor = c;
                    MarkDirty();
                });
            }

            // Show component status
            if (currentTarget != null)
            {
                if (currentTarget.GetComponent<Image>() != null)
                {
                    CreateLabel("Image component found", new Color(0.4f, 0.8f, 0.4f));
                }
                else if (currentTarget.GetComponent<SpriteRenderer>() != null)
                {
                    CreateLabel("SpriteRenderer found", new Color(0.4f, 0.8f, 0.4f));
                }
                else
                {
                    CreateLabel("No Image or SpriteRenderer!", new Color(0.8f, 0.4f, 0.4f));
                }
            }
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
