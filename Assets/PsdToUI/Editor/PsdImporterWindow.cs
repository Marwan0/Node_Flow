using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Ntreev.Library.Psd;
using Object = UnityEngine.Object;

namespace Object_Flow.PsdToUI.Editor
{
    public class PsdImporterWindow : EditorWindow
    {
        private string psdFilePath = "";
        private RectTransform targetCanvas;
        private string outputSpritePath = "Assets/PsdToUI/GeneratedSprites";
        private float targetResolutionWidth = 1920f;
        private float targetResolutionHeight = 1080f;

        public enum TextImportMode
        {
            TextMeshPro,
            RasterizedImage
        }

        public enum LayoutFitMode
        {
            MatchPsdPixels,
            FitInsideTarget,
            FillTarget,
            StretchToTarget
        }

        public enum CompositionImportMode
        {
            Layered,
            FlatComposite
        }

        public enum PivotMode
        {
            Center,
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        public static Vector2 PivotModeToVector(PivotMode mode)
        {
            switch (mode)
            {
                case PivotMode.TopLeft:       return new Vector2(0f,   1f);
                case PivotMode.TopCenter:     return new Vector2(0.5f, 1f);
                case PivotMode.TopRight:      return new Vector2(1f,   1f);
                case PivotMode.MiddleLeft:    return new Vector2(0f,   0.5f);
                case PivotMode.Center:        return new Vector2(0.5f, 0.5f);
                case PivotMode.MiddleRight:   return new Vector2(1f,   0.5f);
                case PivotMode.BottomLeft:    return new Vector2(0f,   0f);
                case PivotMode.BottomCenter:  return new Vector2(0.5f, 0f);
                case PivotMode.BottomRight:   return new Vector2(1f,   0f);
                default:                      return new Vector2(0.5f, 0.5f);
            }
        }

        private struct LayerViewItem
        {
            public string LayerKey;
            public string LayerPath;
            public string Name;
            public int Depth;
            public int Width;
            public int Height;
            public bool IsGroup;
            public bool IsText;
            public bool HasImage;
            public bool HasFx;
        }

        private TextImportMode textImportMode = TextImportMode.TextMeshPro;
        private LayoutFitMode layoutFitMode = LayoutFitMode.MatchPsdPixels;
        private CompositionImportMode compositionImportMode = CompositionImportMode.Layered;
        private PivotMode pivotMode = PivotMode.Center;

        private readonly List<LayerViewItem> layerItems = new List<LayerViewItem>();
        private readonly Dictionary<string, LayerViewItem> layerByKey = new Dictionary<string, LayerViewItem>();
        private readonly HashSet<string> flattenedLayerKeys = new HashSet<string>();

        private string loadedLayerMetadataPath = string.Empty;
        private string layerLoadError = string.Empty;
        private string selectedLayerKey = string.Empty;
        private Vector2 layerListScroll;
        private Texture2D selectedLayerPreview;

        [MenuItem("Window/PSD to UI Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PsdImporterWindow>("PSD Importer");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnDisable()
        {
            ReleaseSelectedLayerPreview();
        }

        private void OnGUI()
        {
            GUILayout.Label("PSD to UI Canvas Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // PSD File Selection
            GUILayout.BeginHorizontal();
            string newPath = EditorGUILayout.TextField("PSD File Path", psdFilePath);
            if (newPath != psdFilePath)
            {
                psdFilePath = newPath;
                InvalidateLayerCache();
            }

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select PSD File", "", "psd");
                if (!string.IsNullOrEmpty(path) && path != psdFilePath)
                {
                    psdFilePath = path;
                    InvalidateLayerCache();
                }
            }
            GUILayout.EndHorizontal();

            // Target Canvas Selection
            targetCanvas = (RectTransform)EditorGUILayout.ObjectField("Target Canvas (Opt)", targetCanvas, typeof(RectTransform), true);

            // Output Sprite Path
            outputSpritePath = EditorGUILayout.TextField("Output Sprite Path", outputSpritePath);

            // Resolution settings
            EditorGUILayout.Space();
            GUILayout.Label("Target Resolution (For reference framing)", EditorStyles.label);
            GUILayout.BeginHorizontal();
            targetResolutionWidth = EditorGUILayout.FloatField("Width", targetResolutionWidth);
            targetResolutionHeight = EditorGUILayout.FloatField("Height", targetResolutionHeight);
            GUILayout.EndHorizontal();

            // Import settings
            EditorGUILayout.Space();
            compositionImportMode = (CompositionImportMode)EditorGUILayout.EnumPopup("Import Mode", compositionImportMode);
            textImportMode = (TextImportMode)EditorGUILayout.EnumPopup("Import Text As", textImportMode);
            layoutFitMode = (LayoutFitMode)EditorGUILayout.EnumPopup("Layout Fit", layoutFitMode);
            pivotMode = (PivotMode)EditorGUILayout.EnumPopup("Layer Pivot", pivotMode);

            if (compositionImportMode == CompositionImportMode.FlatComposite)
            {
                EditorGUILayout.HelpBox(
                    "Flat Composite preserves Photoshop layer FX exactly, but imports one merged image (no editable individual layers/text).",
                    MessageType.Info);

                if (layoutFitMode == LayoutFitMode.MatchPsdPixels)
                {
                    EditorGUILayout.HelpBox(
                        "To avoid cropping, Flat Composite currently fits inside the target frame even when Match Psd Pixels is selected.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            DrawLayerPreviewAndOverrides();

            EditorGUILayout.Space(10);

            // Generate Button
            GUI.enabled = !string.IsNullOrEmpty(psdFilePath) && File.Exists(psdFilePath);
            if (GUILayout.Button("Generate UI", GUILayout.Height(40)))
            {
                GenerateUI();
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(psdFilePath) && !File.Exists(psdFilePath))
            {
                EditorGUILayout.HelpBox("Selected PSD file does not exist. Please check the path.", MessageType.Error);
            }
        }

        private void DrawLayerPreviewAndOverrides()
        {
            GUILayout.Label("Layer Preview and Flat Overrides", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(psdFilePath) || !File.Exists(psdFilePath))
            {
                EditorGUILayout.HelpBox("Select a PSD file to browse layers and preview selected layer content.", MessageType.Info);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Layers", GUILayout.Width(110)))
            {
                LoadLayerMetadata();
            }

            if (GUILayout.Button("Clear Flat", GUILayout.Width(90)))
            {
                flattenedLayerKeys.Clear();
            }

            if (GUILayout.Button("Flat FX Layers", GUILayout.Width(110)))
            {
                MarkFxLayersAsFlat();
            }
            GUILayout.EndHorizontal();

            EnsureLayerMetadataLoaded();

            if (!string.IsNullOrEmpty(layerLoadError))
            {
                EditorGUILayout.HelpBox(layerLoadError, MessageType.Error);
                return;
            }

            if (layerItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No importable layers were found in this PSD.", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();
            DrawLayerList();
            DrawPreviewPane();
            GUILayout.EndHorizontal();
        }

        private void DrawLayerList()
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(360), GUILayout.Height(300));
            layerListScroll = EditorGUILayout.BeginScrollView(layerListScroll, GUILayout.Height(300));

            for (int i = 0; i < layerItems.Count; i++)
            {
                LayerViewItem item = layerItems[i];
                bool isSelected = item.LayerKey == selectedLayerKey;

                GUILayout.BeginHorizontal();
                GUILayout.Space(item.Depth * 14f);

                if (GUILayout.Toggle(isSelected, BuildLayerLabel(item), "Button", GUILayout.ExpandWidth(true)))
                {
                    if (!isSelected)
                    {
                        SelectLayer(item.LayerKey);
                    }
                }

                bool isFlat = flattenedLayerKeys.Contains(item.LayerKey);
                bool nextIsFlat = EditorGUILayout.ToggleLeft("Flat", isFlat, GUILayout.Width(46));
                if (nextIsFlat != isFlat)
                {
                    if (nextIsFlat)
                    {
                        flattenedLayerKeys.Add(item.LayerKey);
                    }
                    else
                    {
                        flattenedLayerKeys.Remove(item.LayerKey);
                    }
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawPreviewPane()
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(220), GUILayout.Height(300));
            GUILayout.Label("Selected Layer", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(selectedLayerKey) || !layerByKey.ContainsKey(selectedLayerKey))
            {
                EditorGUILayout.HelpBox("Select a layer from the list to preview it.", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            LayerViewItem item = layerByKey[selectedLayerKey];
            EditorGUILayout.LabelField("Name", item.Name);
            EditorGUILayout.LabelField("Type", item.IsGroup ? "Group" : item.IsText ? "Text" : "Image");
            EditorGUILayout.LabelField("Size", $"{item.Width} x {item.Height}");
            EditorGUILayout.LabelField("Photoshop FX", item.HasFx ? "Yes (lfx2/lrFX)" : "No");

            if (selectedLayerPreview != null)
            {
                float aspect = (float)selectedLayerPreview.width / selectedLayerPreview.height;
                Rect previewRect = GUILayoutUtility.GetAspectRect(aspect, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(220));
                EditorGUI.DrawPreviewTexture(previewRect, selectedLayerPreview, null, ScaleMode.ScaleToFit);
            }
            else if (item.IsGroup)
            {
                EditorGUILayout.HelpBox("Group layers do not have direct pixels. Select a child layer for preview.", MessageType.Info);
            }
            else if (!item.HasImage)
            {
                EditorGUILayout.HelpBox("This layer has no raster pixels to preview.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Unable to build preview for this layer.", MessageType.Warning);
            }

            GUILayout.EndVertical();
        }

        private string BuildLayerLabel(LayerViewItem item)
        {
            string typeTag = item.IsGroup ? "[Group]" : item.IsText ? "[Text]" : "[Layer]";
            string fxTag = item.HasFx ? " [FX]" : string.Empty;
            return $"{typeTag} {item.Name}{fxTag}";
        }

        private void SelectLayer(string layerKey)
        {
            selectedLayerKey = layerKey;
            RefreshSelectedLayerPreview();
        }

        private void MarkFxLayersAsFlat()
        {
            EnsureLayerMetadataLoaded();
            for (int i = 0; i < layerItems.Count; i++)
            {
                LayerViewItem item = layerItems[i];
                if (item.HasFx)
                {
                    flattenedLayerKeys.Add(item.LayerKey);
                }
            }
        }

        private void EnsureLayerMetadataLoaded()
        {
            if (loadedLayerMetadataPath == psdFilePath && layerItems.Count > 0 && string.IsNullOrEmpty(layerLoadError))
            {
                return;
            }

            if (loadedLayerMetadataPath == psdFilePath && !string.IsNullOrEmpty(layerLoadError))
            {
                return;
            }

            LoadLayerMetadata();
        }

        private void LoadLayerMetadata()
        {
            string previousSelectedLayer = selectedLayerKey;
            HashSet<string> previousFlatSelections = new HashSet<string>(flattenedLayerKeys);

            layerItems.Clear();
            layerByKey.Clear();
            layerLoadError = string.Empty;
            loadedLayerMetadataPath = psdFilePath;
            flattenedLayerKeys.Clear();
            selectedLayerKey = string.Empty;
            ReleaseSelectedLayerPreview();

            if (string.IsNullOrEmpty(psdFilePath) || !File.Exists(psdFilePath))
            {
                return;
            }

            try
            {
                using (PsdDocument document = PsdDocument.Create(psdFilePath))
                {
                    for (int i = 0; i < document.Childs.Length; i++)
                    {
                        AddLayerRecursive(document.Childs[i], 0, i.ToString());
                    }
                }

                for (int i = 0; i < layerItems.Count; i++)
                {
                    LayerViewItem item = layerItems[i];
                    if (previousFlatSelections.Contains(item.LayerKey))
                    {
                        flattenedLayerKeys.Add(item.LayerKey);
                    }
                }

                if (!string.IsNullOrEmpty(previousSelectedLayer) && layerByKey.ContainsKey(previousSelectedLayer))
                {
                    selectedLayerKey = previousSelectedLayer;
                }
                else if (layerItems.Count > 0)
                {
                    selectedLayerKey = layerItems[0].LayerKey;
                }

                RefreshSelectedLayerPreview();
            }
            catch (System.Exception ex)
            {
                layerLoadError = $"Failed to read PSD layers: {ex.Message}";
            }
        }

        private void AddLayerRecursive(IPsdLayer layer, int depth, string pathKey)
        {
            bool isGroup = layer.Childs != null && layer.Childs.Length > 0;
            bool isText = layer.Resources != null && layer.Resources.Contains("TySh");
            bool hasFx = layer.Resources != null && (layer.Resources.Contains("lfx2") || layer.Resources.Contains("lrFX"));

            LayerViewItem item = new LayerViewItem
            {
                LayerKey = BuildLayerKey(layer, pathKey),
                LayerPath = pathKey,
                Name = string.IsNullOrEmpty(layer.Name) ? "(Unnamed Layer)" : layer.Name,
                Depth = depth,
                Width = layer.Width,
                Height = layer.Height,
                IsGroup = isGroup,
                IsText = isText,
                HasImage = layer.HasImage,
                HasFx = hasFx
            };

            layerItems.Add(item);
            layerByKey[item.LayerKey] = item;

            if (!isGroup)
            {
                return;
            }

            for (int i = 0; i < layer.Childs.Length; i++)
            {
                AddLayerRecursive(layer.Childs[i], depth + 1, $"{pathKey}/{i}");
            }
        }

        private void RefreshSelectedLayerPreview()
        {
            ReleaseSelectedLayerPreview();

            if (string.IsNullOrEmpty(selectedLayerKey) || !layerByKey.ContainsKey(selectedLayerKey))
            {
                return;
            }

            LayerViewItem selectedItem = layerByKey[selectedLayerKey];
            if (!selectedItem.HasImage || selectedItem.IsGroup)
            {
                return;
            }

            try
            {
                using (PsdDocument document = PsdDocument.Create(psdFilePath))
                {
                    IPsdLayer layer = ResolveLayerByPath(document, selectedItem.LayerPath);
                    selectedLayerPreview = CreatePreviewTexture(layer);
                }
            }
            catch
            {
                selectedLayerPreview = null;
            }
        }

        private static string BuildLayerKey(IPsdLayer layer, string layerPath)
        {
            if (TryGetLayerId(layer, out int layerId))
            {
                return $"lyid:{layerId}|path:{layerPath}";
            }

            return $"path:{layerPath}";
        }

        private static bool TryGetLayerId(IPsdLayer layer, out int layerId)
        {
            layerId = 0;

            if (layer == null || layer.Resources == null || layer.Resources.Contains("lyid") == false)
            {
                return false;
            }

            IProperties idProps = layer.Resources["lyid"] as IProperties;
            if (idProps == null || idProps.Contains("ID") == false)
            {
                return false;
            }

            layerId = idProps.ToInt32("ID");
            return true;
        }

        private static IPsdLayer ResolveLayerByPath(PsdDocument document, string layerPath)
        {
            if (document == null || string.IsNullOrEmpty(layerPath))
            {
                return null;
            }

            string[] parts = layerPath.Split('/');
            IPsdLayer current = null;
            IPsdLayer[] siblings = document.Childs;

            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int index) == false || siblings == null || index < 0 || index >= siblings.Length)
                {
                    return null;
                }

                current = siblings[index];
                siblings = current.Childs;
            }

            return current;
        }

        private static Texture2D CreatePreviewTexture(IPsdLayer layer)
        {
            if (layer == null || !layer.HasImage || layer.Width <= 0 || layer.Height <= 0)
            {
                return null;
            }

            byte[] red = null;
            byte[] green = null;
            byte[] blue = null;
            byte[] alpha = null;

            for (int i = 0; i < layer.Channels.Length; i++)
            {
                IChannel channel = layer.Channels[i];
                if (channel.Type == ChannelType.Red) red = channel.Data;
                else if (channel.Type == ChannelType.Green) green = channel.Data;
                else if (channel.Type == ChannelType.Blue) blue = channel.Data;
                else if (channel.Type == ChannelType.Alpha) alpha = channel.Data;
            }

            int width = layer.Width;
            int height = layer.Height;
            int pixelCount = width * height;
            Color32[] pixels = new Color32[pixelCount];
            int bitsPerChannel = layer.Depth;

            for (int i = 0; i < pixelCount; i++)
            {
                byte r = SampleChannelByte(red, i, bitsPerChannel);
                byte g = SampleChannelByte(green, i, bitsPerChannel);
                byte b = SampleChannelByte(blue, i, bitsPerChannel);
                byte a = alpha != null ? SampleChannelByte(alpha, i, bitsPerChannel) : (byte)255;
                pixels[i] = new Color32(r, g, b, a);
            }

            Color32[] flippedPixels = new Color32[pixelCount];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flippedPixels[(height - 1 - y) * width + x] = pixels[y * width + x];
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(flippedPixels);
            texture.Apply();
            return texture;
        }

        private static byte SampleChannelByte(byte[] data, int pixelIndex, int bitsPerChannel)
        {
            if (data == null)
            {
                return 0;
            }

            switch (bitsPerChannel)
            {
                case 8:
                    return pixelIndex < data.Length ? data[pixelIndex] : (byte)0;
                case 16:
                    {
                        int byteIndex = pixelIndex * 2;
                        return byteIndex < data.Length ? data[byteIndex] : (byte)0;
                    }
                case 32:
                    {
                        int byteIndex = pixelIndex * 4;
                        if (byteIndex + 3 >= data.Length)
                        {
                            return 0;
                        }

                        float value;
                        if (System.BitConverter.IsLittleEndian)
                        {
                            byte[] bytes = new byte[4]
                            {
                                data[byteIndex + 3],
                                data[byteIndex + 2],
                                data[byteIndex + 1],
                                data[byteIndex]
                            };
                            value = System.BitConverter.ToSingle(bytes, 0);
                        }
                        else
                        {
                            value = System.BitConverter.ToSingle(data, byteIndex);
                        }

                        value = Mathf.Clamp01(value);
                        return (byte)Mathf.RoundToInt(value * 255f);
                    }
                default:
                    return pixelIndex < data.Length ? data[pixelIndex] : (byte)0;
            }
        }

        private void ReleaseSelectedLayerPreview()
        {
            if (selectedLayerPreview == null)
            {
                return;
            }

            Object.DestroyImmediate(selectedLayerPreview);
            selectedLayerPreview = null;
        }

        private void InvalidateLayerCache()
        {
            loadedLayerMetadataPath = string.Empty;
            layerLoadError = string.Empty;
            layerItems.Clear();
            layerByKey.Clear();
            flattenedLayerKeys.Clear();
            selectedLayerKey = string.Empty;
            ReleaseSelectedLayerPreview();
        }

        private void GenerateUI()
        {
            Debug.Log($"Starting PSD import: {psdFilePath}");

            string[] flatLayerKeys = new string[flattenedLayerKeys.Count];
            flattenedLayerKeys.CopyTo(flatLayerKeys);

            PsdImportSettings settings = new PsdImportSettings
            {
                PsdFilePath = psdFilePath,
                TargetCanvas = targetCanvas,
                OutputSpritePath = outputSpritePath,
                TargetResolution = new Vector2(targetResolutionWidth, targetResolutionHeight),
                TextImportMode = textImportMode,
                LayoutFitMode = layoutFitMode,
                CompositionImportMode = compositionImportMode,
                PivotMode = pivotMode,
                FlattenLayerKeys = flatLayerKeys
            };

            PsdToUIGenerator.Generate(settings);
        }
    }

    public struct PsdImportSettings
    {
        public string PsdFilePath;
        public RectTransform TargetCanvas;
        public string OutputSpritePath;
        public Vector2 TargetResolution;
        public PsdImporterWindow.TextImportMode TextImportMode;
        public PsdImporterWindow.LayoutFitMode LayoutFitMode;
        public PsdImporterWindow.CompositionImportMode CompositionImportMode;
        public PsdImporterWindow.PivotMode PivotMode;
        public string[] FlattenLayerKeys;
    }
}
