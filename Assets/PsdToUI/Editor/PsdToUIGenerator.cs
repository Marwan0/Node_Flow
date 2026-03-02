using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Ntreev.Library.Psd;
using TMPro;

namespace Object_Flow.PsdToUI.Editor
{
    public static class PsdToUIGenerator
    {
        public static void Generate(PsdImportSettings settings)
        {
            if (string.IsNullOrEmpty(settings.PsdFilePath)) return;

            using (PsdDocument document = PsdDocument.Create(settings.PsdFilePath))
            {
                // Find or create canvas
                RectTransform canvasRT = settings.TargetCanvas;
                if (canvasRT == null)
                {
                    GameObject canvasGO = new GameObject("PSD_Canvas");
                    Canvas canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(document.Width, document.Height);
                    scaler.matchWidthOrHeight = 0.5f; // Balance width and height
                    canvasGO.AddComponent<GraphicRaycaster>();
                    canvasRT = canvasGO.GetComponent<RectTransform>();
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

                // PSD global origin is (0,0) at top-left
                // Top-Left of our rootRT in local space is (-width/2, height/2)
                // We'll set the children to anchor top-left of the rootRT
                
                // PSD layers are read bottom-up in the parsed array.
                // Childs[0] is the bottom-most layer.
                // In Unity UI, the first child (index 0) renders at the bottom.
                // Therefore, iterating forward preserves exact visual Z-order.
                
                for (int i = 0; i < document.Childs.Length; i++)
                {
                    CreateLayerRecursive(document.Childs[i], rootRT, settings, 0f, 0f);
                }
            }
        }

        private static void CreateLayerRecursive(IPsdLayer layer, RectTransform parentRT, PsdImportSettings settings, float parentGlobalX, float parentGlobalY)
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

            if (isTextLayer && settings.TextImportMode == PsdImporterWindow.TextImportMode.TextMeshPro)
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
                    CreateLayerRecursive(layer.Childs[i], rt, settings, newOriginX, newOriginY);
                }
            }
        }
    }
}
