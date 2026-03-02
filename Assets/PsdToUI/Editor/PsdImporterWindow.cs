using UnityEngine;
using UnityEditor;
using System.IO;

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

        private TextImportMode textImportMode = TextImportMode.TextMeshPro;

        [MenuItem("Window/PSD to UI Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PsdImporterWindow>("PSD Importer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("PSD to UI Canvas Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // PSD File Selection
            GUILayout.BeginHorizontal();
            psdFilePath = EditorGUILayout.TextField("PSD File Path", psdFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select PSD File", "", "psd");
                if (!string.IsNullOrEmpty(path))
                {
                    psdFilePath = path;
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

            // Text Import Settings
            EditorGUILayout.Space();
            textImportMode = (TextImportMode)EditorGUILayout.EnumPopup("Import Text As", textImportMode);

            EditorGUILayout.Space(20);

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

        private void GenerateUI()
        {
            // Will hook up logic here
            Debug.Log($"Starting PSD import: {psdFilePath}");
            
            PsdImportSettings settings = new PsdImportSettings
            {
                PsdFilePath = psdFilePath,
                TargetCanvas = targetCanvas,
                OutputSpritePath = outputSpritePath,
                TargetResolution = new Vector2(targetResolutionWidth, targetResolutionHeight),
                TextImportMode = textImportMode
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
    }
}
