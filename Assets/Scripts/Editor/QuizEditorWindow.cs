#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuizSystem
{
    public class QuizEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Quiz System/Question Editor")]
        private static void OpenWindow()
        {
            GetWindow<QuizEditorWindow>("Question Editor").Show();
        }

        private Vector2 scrollPos;
        private int selectedTab = 0;
        private string[] tabNames = new string[]
        {
            "True/False", "Fill in Blank", "Multi-Select", "Ordering",
            "Hotspot", "Slider", "Audio", "Multiple Choice", "Drag & Drop", "Connect"
        };

        public List<TrueFalseQuestionData> trueFalseQuestions = new List<TrueFalseQuestionData>();
        public List<FillInTheBlankQuestionData> fillInTheBlankQuestions = new List<FillInTheBlankQuestionData>();
        public List<MultiSelectQuestionData> multiSelectQuestions = new List<MultiSelectQuestionData>();
        public List<OrderingQuestionData> orderingQuestions = new List<OrderingQuestionData>();
        public List<HotspotQuestionData> hotspotQuestions = new List<HotspotQuestionData>();
        public List<SliderQuestionData> sliderQuestions = new List<SliderQuestionData>();
        public List<AudioQuestionData> audioQuestions = new List<AudioQuestionData>();
        public List<MultipleChoiceQuestionData> multipleChoiceQuestions = new List<MultipleChoiceQuestionData>();
        public List<DragDropQuestionData> dragDropQuestions = new List<DragDropQuestionData>();
        public List<ConnectQuestionData> connectQuestions = new List<ConnectQuestionData>();

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            // Import/Export buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export All Questions", GUILayout.Height(30)))
                ExportAllQuestions();
            if (GUILayout.Button("Import Questions", GUILayout.Height(30)))
                ImportQuestions();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load All Questions", GUILayout.Height(25)))
                LoadAllQuestions();
            if (GUILayout.Button("Export Selected Tab", GUILayout.Height(25)))
                ExportSelectedTab();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            EditorGUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0: DrawQuestionList(trueFalseQuestions, "True/False", "TrueFalseQuestion"); break;
                case 1: DrawQuestionList(fillInTheBlankQuestions, "Fill in the Blank", "FillInTheBlankQuestion"); break;
                case 2: DrawQuestionList(multiSelectQuestions, "Multi-Select", "MultiSelectQuestion"); break;
                case 3: DrawQuestionList(orderingQuestions, "Ordering", "OrderingQuestion"); break;
                case 4: DrawQuestionList(hotspotQuestions, "Hotspot", "HotspotQuestion"); break;
                case 5: DrawQuestionList(sliderQuestions, "Slider", "SliderQuestion"); break;
                case 6: DrawQuestionList(audioQuestions, "Audio", "AudioQuestion"); break;
                case 7: DrawQuestionList(multipleChoiceQuestions, "Multiple Choice", "MultipleChoiceQuestion"); break;
                case 8: DrawQuestionList(dragDropQuestions, "Drag & Drop", "DragDropQuestion"); break;
                case 9: DrawQuestionList(connectQuestions, "Connect", "ConnectQuestion"); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuestionList<T>(List<T> questions, string label, string defaultName) where T : QuestionData
        {
            EditorGUILayout.LabelField($"{label} Questions ({questions.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button($"Create {label} Question"))
            {
                CreateQuestion<T>(defaultName);
            }

            EditorGUILayout.Space(5);

            for (int i = 0; i < questions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                questions[i] = (T)EditorGUILayout.ObjectField(questions[i], typeof(T), false);
                if (questions[i] != null && GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = questions[i];
                    EditorGUIUtility.PingObject(questions[i]);
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    questions.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ExportAllQuestions()
        {
            var allQuestions = new List<QuestionData>();
            allQuestions.AddRange(trueFalseQuestions);
            allQuestions.AddRange(fillInTheBlankQuestions);
            allQuestions.AddRange(multiSelectQuestions);
            allQuestions.AddRange(orderingQuestions);
            allQuestions.AddRange(hotspotQuestions);
            allQuestions.AddRange(sliderQuestions);
            allQuestions.AddRange(audioQuestions);
            allQuestions.AddRange(multipleChoiceQuestions);
            allQuestions.AddRange(dragDropQuestions);
            allQuestions.AddRange(connectQuestions);

            if (allQuestions.Count == 0)
            {
                EditorUtility.DisplayDialog("No Questions", "No questions to export. Load questions first.", "OK");
                return;
            }

            QuestionExporter.ExportWithDialog(allQuestions);
        }

        private void ImportQuestions()
        {
            var imported = QuestionImporter.ImportWithDialog();
            if (imported.Count > 0)
            {
                LoadAllQuestions();
            }
        }

        private void ExportSelectedTab()
        {
            var questions = GetCurrentTabQuestions();
            if (questions.Count == 0)
            {
                EditorUtility.DisplayDialog("No Questions", "No questions in the current selection to export.", "OK");
                return;
            }
            QuestionExporter.ExportWithDialog(questions);
        }

        private List<QuestionData> GetCurrentTabQuestions()
        {
            var allQuestions = new List<QuestionData>();
            allQuestions.AddRange(trueFalseQuestions);
            allQuestions.AddRange(fillInTheBlankQuestions);
            allQuestions.AddRange(multiSelectQuestions);
            allQuestions.AddRange(orderingQuestions);
            allQuestions.AddRange(hotspotQuestions);
            allQuestions.AddRange(sliderQuestions);
            allQuestions.AddRange(audioQuestions);
            allQuestions.AddRange(multipleChoiceQuestions);
            allQuestions.AddRange(dragDropQuestions);
            allQuestions.AddRange(connectQuestions);
            return allQuestions;
        }

        private void LoadAllQuestions()
        {
            string[] guids = AssetDatabase.FindAssets("t:TrueFalseQuestionData");
            trueFalseQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<TrueFalseQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:FillInTheBlankQuestionData");
            fillInTheBlankQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<FillInTheBlankQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:MultiSelectQuestionData");
            multiSelectQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<MultiSelectQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:OrderingQuestionData");
            orderingQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<OrderingQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:HotspotQuestionData");
            hotspotQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<HotspotQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:SliderQuestionData");
            sliderQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<SliderQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:AudioQuestionData");
            audioQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AudioQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:MultipleChoiceQuestionData");
            multipleChoiceQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<MultipleChoiceQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:DragDropQuestionData");
            dragDropQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<DragDropQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            guids = AssetDatabase.FindAssets("t:ConnectQuestionData");
            connectQuestions = guids.Select(guid => AssetDatabase.LoadAssetAtPath<ConnectQuestionData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            Debug.Log("All questions loaded!");
        }

        private void CreateQuestion<T>(string defaultName) where T : QuestionData
        {
            string path = "Assets/Data/Questions";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            T question = ScriptableObject.CreateInstance<T>();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");
            AssetDatabase.CreateAsset(question, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = question;

            LoadAllQuestions();
        }
    }
}
#endif
