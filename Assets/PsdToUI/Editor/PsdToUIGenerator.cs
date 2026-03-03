using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Ntreev.Library.Psd;
using TMPro;
using System.Collections.Generic;

namespace Object_Flow.PsdToUI.Editor
{
    public static class PsdToUIGenerator
    {
        public static void Generate(PsdImportSettings settings)
        {
            if (string.IsNullOrEmpty(settings.PsdFilePath)) return;

            using (PsdDocument document = PsdDocument.Create(settings.PsdFilePath))
            {
                PsdImporterWindow.LayoutFitMode effectiveLayoutFit = ResolveEffectiveLayoutFit(settings);
                bool matchPsdPixels = effectiveLayoutFit == PsdImporterWindow.LayoutFitMode.MatchPsdPixels;
                Vector2 targetFrame = matchPsdPixels
                    ? new Vector2(document.Width, document.Height)
                    : ResolveTargetFrame(settings.TargetResolution, document.Width, document.Height);

                // Find or create canvas
                RectTransform canvasRT = settings.TargetCanvas;
                if (canvasRT == null)
                {
                    GameObject canvasGO = new GameObject("PSD_Canvas");
                    Canvas canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                    ConfigureCanvasScaler(scaler, matchPsdPixels, targetFrame);
                    canvasGO.AddComponent<GraphicRaycaster>();
                    canvasRT = canvasGO.GetComponent<RectTransform>();
                }
                else
                {
                    CanvasScaler existingScaler = canvasRT.GetComponent<CanvasScaler>();
                    if (existingScaler != null)
                    {
                        ConfigureCanvasScaler(existingScaler, matchPsdPixels, targetFrame);
                    }
                }

                // Root container for this specific PSD
                GameObject rootGO = new GameObject(System.IO.Path.GetFileNameWithoutExtension(settings.PsdFilePath));
                RectTransform rootRT = rootGO.AddComponent<RectTransform>();
                rootRT.SetParent(canvasRT, false);
                
                // Match resolution to PSD document
                rootRT.anchorMin = new Vector2(0.5f, 0.5f);
                rootRT.anchorMax = new Vector2(0.5f, 0.5f);
                rootRT.pivot = new Vector2(0.5f, 0.5f);
                rootRT.sizeDelta = new Vector2(document.Width, document.Height);
                rootRT.anchoredPosition = Vector2.zero;
                ApplyRootScale(rootRT, document.Width, document.Height, targetFrame, effectiveLayoutFit);

                // PSD global origin is (0,0) at top-left
                // Top-Left of our rootRT in local space is (-width/2, height/2)
                // We'll set the children to anchor top-left of the rootRT
                
                // PSD layers are read bottom-up in the parsed array.
                // Childs[0] is the bottom-most layer.
                // In Unity UI, the first child (index 0) renders at the bottom.
                // Therefore, iterating forward preserves exact visual Z-order.

                if (settings.CompositionImportMode == PsdImporterWindow.CompositionImportMode.FlatComposite)
                {
                    CreateFlatComposite(document, rootRT, settings);
                    return;
                }

                HashSet<string> flattenedLayerKeys = BuildFlattenedLayerKeySet(settings.FlattenLayerKeys);

                List<string> layersWithUnsupportedFx = new List<string>();
                CollectLayersWithUnsupportedFx(document.Childs, layersWithUnsupportedFx);
                if (layersWithUnsupportedFx.Count > 0)
                {
                    LogUnsupportedFxWarning(settings.PsdFilePath, layersWithUnsupportedFx);
                }

                for (int i = 0; i < document.Childs.Length; i++)
                {
                    CreateLayerRecursive(
                        document.Childs[i],
                        rootRT,
                        settings,
                        0f,
                        0f,
                        i.ToString(),
                        flattenedLayerKeys,
                        false);
                }
            }
        }

        private static HashSet<string> BuildFlattenedLayerKeySet(string[] flattenLayerKeys)
        {
            HashSet<string> flattenedLayerKeys = new HashSet<string>();
            if (flattenLayerKeys == null)
            {
                return flattenedLayerKeys;
            }

            for (int i = 0; i < flattenLayerKeys.Length; i++)
            {
                string key = flattenLayerKeys[i];
                if (string.IsNullOrEmpty(key) == false)
                {
                    flattenedLayerKeys.Add(key);
                }
            }

            return flattenedLayerKeys;
        }

        private static PsdImporterWindow.LayoutFitMode ResolveEffectiveLayoutFit(PsdImportSettings settings)
        {
            if (settings.CompositionImportMode == PsdImporterWindow.CompositionImportMode.FlatComposite &&
                settings.LayoutFitMode == PsdImporterWindow.LayoutFitMode.MatchPsdPixels)
            {
                // In flat composite mode, strict 1:1 pixel sizing can crop when game/canvas aspect differs.
                // Use fit-inside to preserve full composition by default.
                return PsdImporterWindow.LayoutFitMode.FitInsideTarget;
            }

            return settings.LayoutFitMode;
        }

        private static void CreateFlatComposite(PsdDocument document, RectTransform rootRT, PsdImportSettings settings)
        {
            Sprite compositeSprite = PsdTextureExporter.ExportSprite((IPsdLayer)document, settings.OutputSpritePath);
            if (compositeSprite == null)
            {
                Debug.LogWarning("[PSD Importer] Failed to export flat composite image from PSD document.");
                return;
            }

            GameObject compositeGO = new GameObject("Composite");
            RectTransform compositeRT = compositeGO.AddComponent<RectTransform>();
            compositeRT.SetParent(rootRT, false);
            compositeRT.anchorMin = new Vector2(0f, 1f);
            compositeRT.anchorMax = new Vector2(0f, 1f);
            compositeRT.pivot = new Vector2(0f, 1f);
            compositeRT.anchoredPosition = Vector2.zero;
            compositeRT.sizeDelta = new Vector2(document.Width, document.Height);

            Image compositeImage = compositeGO.AddComponent<Image>();
            compositeImage.sprite = compositeSprite;
            compositeImage.raycastTarget = false;
        }

        private static void CollectLayersWithUnsupportedFx(IPsdLayer[] layers, List<string> output)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                IPsdLayer layer = layers[i];
                if (LayerHasUnsupportedFx(layer))
                {
                    output.Add(layer.Name);
                }

                if (layer.Childs != null && layer.Childs.Length > 0)
                {
                    CollectLayersWithUnsupportedFx(layer.Childs, output);
                }
            }
        }

        private static bool LayerHasUnsupportedFx(IPsdLayer layer)
        {
            if (layer == null || layer.Resources == null)
            {
                return false;
            }

            return layer.Resources.Contains("lfx2") || layer.Resources.Contains("lrFX");
        }

        private static void LogUnsupportedFxWarning(string psdFilePath, List<string> layerNames)
        {
            const int previewCount = 8;
            int count = Mathf.Min(previewCount, layerNames.Count);
            string[] preview = new string[count];
            for (int i = 0; i < count; i++)
            {
                preview[i] = layerNames[i];
            }

            string previewText = string.Join(", ", preview);
            if (layerNames.Count > previewCount)
            {
                previewText += ", ...";
            }

            string fileName = System.IO.Path.GetFileName(psdFilePath);
            Debug.LogWarning(
                $"[PSD Importer] Found Photoshop layer FX (lfx2/lrFX) on {layerNames.Count} layer(s) in '{fileName}'. " +
                "Layered import may not match Photoshop exactly. Use Import Mode = FlatComposite to preserve final look. " +
                $"Examples: {previewText}");
        }

        private static void ConfigureCanvasScaler(CanvasScaler scaler, bool matchPsdPixels, Vector2 targetFrame)
        {
            if (matchPsdPixels)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = targetFrame;
            scaler.matchWidthOrHeight = 0.5f; // Balance width and height
        }

        private static Vector2 ResolveTargetFrame(Vector2 requestedResolution, int documentWidth, int documentHeight)
        {
            float width = requestedResolution.x > 0f ? requestedResolution.x : documentWidth;
            float height = requestedResolution.y > 0f ? requestedResolution.y : documentHeight;
            return new Vector2(width, height);
        }

        private static void ApplyRootScale(
            RectTransform rootRT,
            int documentWidth,
            int documentHeight,
            Vector2 targetFrame,
            PsdImporterWindow.LayoutFitMode fitMode)
        {
            if (documentWidth <= 0 || documentHeight <= 0)
            {
                rootRT.localScale = Vector3.one;
                return;
            }

            float scaleX = targetFrame.x / documentWidth;
            float scaleY = targetFrame.y / documentHeight;

            switch (fitMode)
            {
                case PsdImporterWindow.LayoutFitMode.MatchPsdPixels:
                    scaleX = 1f;
                    scaleY = 1f;
                    break;
                case PsdImporterWindow.LayoutFitMode.FitInsideTarget:
                    float fitScale = Mathf.Min(scaleX, scaleY);
                    scaleX = fitScale;
                    scaleY = fitScale;
                    break;
                case PsdImporterWindow.LayoutFitMode.FillTarget:
                    float fillScale = Mathf.Max(scaleX, scaleY);
                    scaleX = fillScale;
                    scaleY = fillScale;
                    break;
                case PsdImporterWindow.LayoutFitMode.StretchToTarget:
                    break;
            }

            rootRT.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private static void CreateLayerRecursive(
            IPsdLayer layer,
            RectTransform parentRT,
            PsdImportSettings settings,
            float parentGlobalX,
            float parentGlobalY,
            string layerPath,
            HashSet<string> flattenedLayerKeys,
            bool inheritedFlattenState)
        {
            // Skip hidden layers?
            // Ntreev PSD layer IsVisible isn't on IPsdLayer interface directly, we might need casting if needed.
            // For now, we import everything.
            
            // Skip layers with no valid size unless it's a group
            bool isGroup = layer.Childs.Length > 0;
            if (!isGroup && (layer.Width <= 0 || layer.Height <= 0))
            {
                return;
            }

            GameObject go = new GameObject(layer.Name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parentRT, false);

            // Anchor to Top-Left
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float newOriginX = parentGlobalX;
            float newOriginY = parentGlobalY;

            if (layer.Width > 0 && layer.Height > 0)
            {
                newOriginX = layer.Left;
                newOriginY = layer.Top;

                float localX = layer.Left - parentGlobalX;
                float localY = -(layer.Top - parentGlobalY);
                
                rt.anchoredPosition = new Vector2(localX, localY);
                rt.sizeDelta = new Vector2(layer.Width, layer.Height);
            }
            else
            {
                // Boundless group - just set at (0,0) of parent
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }

            bool isTextLayer = layer.Resources.Contains("TySh");
            string layerKey = BuildLayerKey(layer, layerPath);
            bool flattenThisLayer = inheritedFlattenState || flattenedLayerKeys.Contains(layerKey);

            if (isTextLayer && settings.TextImportMode == PsdImporterWindow.TextImportMode.TextMeshPro && !flattenThisLayer)
            {
                // Import as TextMeshPro
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                
                // Extract text string from TySh resource descriptor if possible
                string textContent = layer.Name; // Default placeholder
                try
                {
                    // Attempt to extract real text
                    var tySh = layer.Resources["TySh"] as IProperties;
                    if (tySh != null && tySh.Contains("Text"))
                    {
                        var textProps = tySh["Text"] as IProperties;
                        if (textProps != null && textProps.Contains("Txt ")) // The key is often "Txt "
                        {
                            textContent = textProps["Txt "].ToString();
                            // Fix newlines from psd
                            textContent = textContent.Replace("\r", "\n"); 
                        }
                    }
                }
                catch { /* Ignore extraction errors and fallback to layer name */ }

                tmp.text = textContent;
                tmp.color = Color.black; // Fallback color
                tmp.raycastTarget = false;
            }
            else if (!isGroup && layer.HasImage)
            {
                // Export and assign sprite
                Sprite sprite = PsdTextureExporter.ExportSprite(layer, settings.OutputSpritePath);
                if (sprite != null)
                {
                    Image img = go.AddComponent<Image>();
                    img.sprite = sprite;
                    img.raycastTarget = false;
                    
                    // Respect opacity
                    Color c = img.color;
                    c.a = layer.Opacity;
                    img.color = c;
                }
            }

            // Process children
            if (isGroup)
            {
                for (int i = 0; i < layer.Childs.Length; i++)
                {
                    CreateLayerRecursive(
                        layer.Childs[i],
                        rt,
                        settings,
                        newOriginX,
                        newOriginY,
                        $"{layerPath}/{i}",
                        flattenedLayerKeys,
                        flattenThisLayer);
                }
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
    }
}
