using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Object_Flow.PsdToUI.Editor
{
    public class PsLayoutImporterWindow : EditorWindow
    {
        private string jsonPath = "";
        private string importSpritesFolder = "Assets/PsdToUI/ImportedLayouts";
        private RectTransform targetCanvas;
        private bool createTextObjects = true;
        private bool respectVisibility = true;
        private bool copyImagesIntoProject = true;
        private PivotMode pivotMode = PivotMode.Center;

        public enum PivotMode
        {
            Center,
            TopLeft
        }

        private static Vector2 PivotModeToVector(PivotMode mode)
        {
            switch (mode)
            {
                case PivotMode.TopLeft: return new Vector2(0f, 1f);
                default:               return new Vector2(0.5f, 0.5f);
            }
        }

        private class RuntimeNode
        {
            public PsExportNode Data;
            public GameObject GameObject;
            public RectTransform RectTransform;
        }

        [MenuItem("Window/Photoshop Layout Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PsLayoutImporterWindow>("PS Layout Importer");
            window.minSize = new Vector2(560f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Photoshop JSON + PNG Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawPathField(
                "Layout JSON",
                ref jsonPath,
                () => EditorUtility.OpenFilePanel("Select layout JSON", "", "json"));

            DrawPathField(
                "Import Sprites To",
                ref importSpritesFolder,
                () => EditorUtility.OpenFolderPanel("Select import destination under Assets", Application.dataPath, ""));

            targetCanvas = (RectTransform)EditorGUILayout.ObjectField("Target Canvas (Opt)", targetCanvas, typeof(RectTransform), true);
            createTextObjects = EditorGUILayout.Toggle("Create Text Objects", createTextObjects);
            respectVisibility = EditorGUILayout.Toggle("Respect Visibility", respectVisibility);
            copyImagesIntoProject = EditorGUILayout.Toggle("Copy Images Into Project", copyImagesIntoProject);
            pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Layer Pivot", pivotMode);

            EditorGUILayout.Space();
            GUI.enabled = File.Exists(jsonPath);
            if (GUILayout.Button("Import Layout", GUILayout.Height(40f)))
            {
                ImportLayout();
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(jsonPath) && !File.Exists(jsonPath))
            {
                EditorGUILayout.HelpBox("Layout JSON path is invalid.", MessageType.Error);
            }
        }

        private static void DrawPathField(string label, ref string value, Func<string> browseAction)
        {
            GUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("Browse", GUILayout.Width(70f)))
            {
                string path = browseAction.Invoke();
                if (!string.IsNullOrEmpty(path))
                {
                    value = path;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ImportLayout()
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                PsExportLayout layout = JsonUtility.FromJson<PsExportLayout>(json);
                if (layout == null || layout.document == null)
                {
                    Debug.LogError("[PS Layout Importer] Invalid layout JSON.");
                    return;
                }

                List<PsExportNode> nodes = BuildNodeList(layout);
                if (nodes.Count == 0)
                {
                    Debug.LogError("[PS Layout Importer] Layout has no nodes/layers.");
                    return;
                }

                string sourceImagesFolder = ResolveSourceImagesFolder();
                Dictionary<string, string> fileToAssetPath = PrepareImageAssets(nodes, sourceImagesFolder);
                Dictionary<string, Sprite> spriteCache = LoadSprites(fileToAssetPath);

                RectTransform canvasRt = EnsureCanvas(layout.document.width, layout.document.height);
                RectTransform rootRt = CreateRoot(canvasRt, layout.document);
                BuildHierarchy(nodes, rootRt, spriteCache);

                Debug.Log($"[PS Layout Importer] Imported {nodes.Count} nodes from '{layout.document.name}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PS Layout Importer] Failed: {ex.Message}");
            }
        }

        private List<PsExportNode> BuildNodeList(PsExportLayout layout)
        {
            var nodes = new List<PsExportNode>();

            if (layout.nodes != null && layout.nodes.Length > 0)
            {
                for (int i = 0; i < layout.nodes.Length; i++)
                {
                    PsExportNode node = layout.nodes[i];
                    if (string.IsNullOrEmpty(node.id))
                    {
                        node.id = i.ToString();
                    }

                    if (node.order < 0)
                    {
                        node.order = i;
                    }

                    nodes.Add(node);
                }

                return nodes;
            }

            if (layout.layers == null)
            {
                return nodes;
            }

            for (int i = 0; i < layout.layers.Length; i++)
            {
                PsExportLayer layer = layout.layers[i];
                nodes.Add(new PsExportNode
                {
                    id = i.ToString(),
                    parentId = "",
                    name = layer.name,
                    file = layer.file,
                    x = layer.x,
                    y = layer.y,
                    width = layer.width,
                    height = layer.height,
                    opacity = layer.opacity,
                    visible = layer.visible,
                    isText = layer.isText,
                    text = layer.text,
                    isGroup = false,
                    order = layout.layers.Length - 1 - i
                });
            }

            return nodes;
        }

        private string ResolveSourceImagesFolder()
        {
            string jsonDirectory = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(jsonDirectory) && Directory.Exists(jsonDirectory))
            {
                return jsonDirectory;
            }

            throw new DirectoryNotFoundException("Could not resolve images folder from layout JSON path.");
        }

        private Dictionary<string, string> PrepareImageAssets(List<PsExportNode> nodes, string sourceFolder)
        {
            var fileToAssetPath = new Dictionary<string, string>();
            var uniqueFiles = new HashSet<string>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].file) == false)
                {
                    uniqueFiles.Add(nodes[i].file);
                }
            }

            if (uniqueFiles.Count == 0)
            {
                return fileToAssetPath;
            }

            string destinationAssetFolder = ResolveDestinationAssetFolder();
            string destinationAbsoluteFolder = AssetPathToAbsolutePath(destinationAssetFolder);
            Directory.CreateDirectory(destinationAbsoluteFolder);

            foreach (string fileName in uniqueFiles)
            {
                string sourcePath = Path.Combine(sourceFolder, fileName);
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning($"[PS Layout Importer] Missing source image: {sourcePath}");
                    continue;
                }

                string destinationAbsolutePath = Path.Combine(destinationAbsoluteFolder, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationAbsolutePath) ?? destinationAbsoluteFolder);

                if (copyImagesIntoProject)
                {
                    if (!PathsEqual(sourcePath, destinationAbsolutePath))
                    {
                        File.Copy(sourcePath, destinationAbsolutePath, true);
                    }
                }
                else
                {
                    string sourceAssetPath = AbsolutePathToAssetPath(sourcePath);
                    if (!string.IsNullOrEmpty(sourceAssetPath))
                    {
                        fileToAssetPath[fileName] = sourceAssetPath;
                        continue;
                    }

                    File.Copy(sourcePath, destinationAbsolutePath, true);
                }

                fileToAssetPath[fileName] = AbsolutePathToAssetPath(destinationAbsolutePath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return fileToAssetPath;
        }

        private string ResolveDestinationAssetFolder()
        {
            string baseFolder = NormalizeToAssetPath(importSpritesFolder);
            if (string.IsNullOrEmpty(baseFolder))
            {
                baseFolder = "Assets/PsdToUI/ImportedLayouts";
            }

            string layoutFolder = SanitizePathPart(Path.GetFileNameWithoutExtension(jsonPath));
            if (string.IsNullOrEmpty(layoutFolder))
            {
                layoutFolder = "LayoutImport";
            }

            return $"{baseFolder}/{layoutFolder}";
        }

        private Dictionary<string, Sprite> LoadSprites(Dictionary<string, string> fileToAssetPath)
        {
            var spriteCache = new Dictionary<string, Sprite>();

            foreach (var kv in fileToAssetPath)
            {
                string fileName = kv.Key;
                string assetPath = kv.Value;

                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                else
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    spriteCache[fileName] = sprite;
                }
                else
                {
                    Debug.LogWarning($"[PS Layout Importer] Failed to load sprite at '{assetPath}'.");
                }
            }

            return spriteCache;
        }

        private RectTransform EnsureCanvas(int width, int height)
        {
            if (targetCanvas != null)
            {
                CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f;
                }

                return targetCanvas;
            }

            GameObject canvasGo = new GameObject("PS_Layout_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(width, height);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo.GetComponent<RectTransform>();
        }

        private RectTransform CreateRoot(RectTransform canvasRt, PsExportDocument document)
        {
            string rootName = string.IsNullOrEmpty(document.name)
                ? Path.GetFileNameWithoutExtension(jsonPath)
                : Path.GetFileNameWithoutExtension(document.name);

            GameObject rootGo = new GameObject(rootName);
            RectTransform rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.SetParent(canvasRt, false);
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(document.width, document.height);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.localScale = Vector3.one;
            return rootRt;
        }

        private void BuildHierarchy(List<PsExportNode> nodes, RectTransform rootRt, Dictionary<string, Sprite> spriteCache)
        {
            var runtimeById = new Dictionary<string, RuntimeNode>();
            var childrenByParentId = new Dictionary<string, List<RuntimeNode>>();

            for (int i = 0; i < nodes.Count; i++)
            {
                PsExportNode node = nodes[i];
                GameObject go = new GameObject(SafeGameObjectName(node.name));
                RectTransform rt = go.AddComponent<RectTransform>();

                runtimeById[node.id] = new RuntimeNode
                {
                    Data = node,
                    GameObject = go,
                    RectTransform = rt
                };
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                RuntimeNode runtime = runtimeById[nodes[i].id];
                PsExportNode node = runtime.Data;

                string parentId = string.Empty;
                RectTransform parentRt = rootRt;
                float parentX = 0f;
                float parentY = 0f;

                if (!string.IsNullOrEmpty(node.parentId) && runtimeById.TryGetValue(node.parentId, out RuntimeNode parentRuntime))
                {
                    parentId = parentRuntime.Data.id;
                    parentRt = parentRuntime.RectTransform;
                    parentX = parentRuntime.Data.x;
                    parentY = parentRuntime.Data.y;
                }

                Vector2 pivot = PivotModeToVector(pivotMode);
                RectTransform rt = runtime.RectTransform;
                rt.SetParent(parentRt, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = pivot;
                float localX = node.x - parentX;
                float localY = -(node.y - parentY);
                float w = Mathf.Max(0f, node.width);
                float h = Mathf.Max(0f, node.height);
                float pivotOffsetX = pivot.x * w;
                float pivotOffsetY = -(1f - pivot.y) * h;
                rt.anchoredPosition = new Vector2(localX + pivotOffsetX, localY + pivotOffsetY);
                rt.sizeDelta = new Vector2(w, h);
                rt.localScale = Vector3.one;

                if (!childrenByParentId.TryGetValue(parentId, out List<RuntimeNode> childList))
                {
                    childList = new List<RuntimeNode>();
                    childrenByParentId[parentId] = childList;
                }
                childList.Add(runtime);

                if (!node.isGroup)
                {
                    bool hasFile = !string.IsNullOrEmpty(node.file);
                    Sprite sprite = null;
                    bool hasSprite = hasFile && spriteCache.TryGetValue(node.file, out sprite);

                    if (hasFile)
                    {
                        Image img = runtime.GameObject.AddComponent<Image>();
                        img.sprite = hasSprite ? sprite : null;
                        img.raycastTarget = false;
                        Color c = img.color;
                        c.a = Mathf.Clamp01(node.opacity);
                        img.color = c;
                    }

                    if (hasFile && !hasSprite)
                    {
                        Debug.LogWarning($"[PS Layout Importer] Sprite missing for layer '{node.name}' file '{node.file}'.");
                    }

                    if (createTextObjects && node.isText && !string.IsNullOrEmpty(node.text) && !hasFile)
                    {
                        TextMeshProUGUI tmp = runtime.GameObject.AddComponent<TextMeshProUGUI>();
                        tmp.text = node.text;
                        tmp.raycastTarget = false;
                        tmp.color = Color.black;
                    }
                }

                if (respectVisibility && !node.visible)
                {
                    runtime.GameObject.SetActive(false);
                }
            }

            ApplySiblingOrder(string.Empty, childrenByParentId);
        }

        private static void ApplySiblingOrder(string parentId, Dictionary<string, List<RuntimeNode>> childrenByParentId)
        {
            if (!childrenByParentId.TryGetValue(parentId, out List<RuntimeNode> children) || children.Count == 0)
            {
                return;
            }

            children.Sort((a, b) =>
            {
                int byOrder = b.Data.order.CompareTo(a.Data.order);
                if (byOrder != 0)
                {
                    return byOrder;
                }

                return string.CompareOrdinal(a.Data.id, b.Data.id);
            });

            for (int i = 0; i < children.Count; i++)
            {
                children[i].RectTransform.SetAsLastSibling();
            }

            for (int i = 0; i < children.Count; i++)
            {
                ApplySiblingOrder(children[i].Data.id, childrenByParentId);
            }
        }

        private static string NormalizeToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string normalized = path.Replace("\\", "/");
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            if (Path.IsPathRooted(normalized))
            {
                if (normalized.StartsWith(Application.dataPath.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase))
                {
                    return "Assets" + normalized.Substring(Application.dataPath.Length).Replace("\\", "/");
                }

                return string.Empty;
            }

            return "Assets/" + normalized.TrimStart('/');
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string normalized = assetPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid asset path '{assetPath}'.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, normalized.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static string AbsolutePathToAssetPath(string absolutePath)
        {
            string full = Path.GetFullPath(absolutePath).Replace("\\", "/");
            string assets = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
            if (!full.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return "Assets" + full.Substring(assets.Length);
        }

        private static bool PathsEqual(string a, string b)
        {
            string pa = Path.GetFullPath(a).TrimEnd('\\', '/');
            string pb = Path.GetFullPath(b).TrimEnd('\\', '/');
            return string.Equals(pa, pb, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Export";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value;
        }

        private static string SafeGameObjectName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Layer";
            }

            return name.Replace("/", "_");
        }
    }

    [Serializable]
    public class PsExportLayout
    {
        public int version = 1;
        public PsExportDocument document;
        public PsExportNode[] nodes;
        public PsExportLayer[] layers;
    }

    [Serializable]
    public class PsExportDocument
    {
        public int width;
        public int height;
        public string name;
    }

    [Serializable]
    public class PsExportNode
    {
        public string id;
        public string parentId;
        public string name;
        public string file;
        public float x;
        public float y;
        public float width;
        public float height;
        public float opacity = 1f;
        public bool visible = true;
        public bool isText;
        public string text;
        public bool isGroup;
        public int order = -1;
    }

    [Serializable]
    public class PsExportLayer
    {
        public string name;
        public string file;
        public float x;
        public float y;
        public float width;
        public float height;
        public float opacity = 1f;
        public bool visible = true;
        public bool isText;
        public string text;
    }
}
