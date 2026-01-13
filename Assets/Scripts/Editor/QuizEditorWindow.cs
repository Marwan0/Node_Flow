using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuizSystem
{
    public class QuizEditorWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Quiz System/Question Editor")]
        private static void OpenWindow()
        {
            GetWindow<QuizEditorWindow>().Show();
        }

        [TabGroup("True/False")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<TrueFalseQuestionData> trueFalseQuestions = new List<TrueFalseQuestionData>();

        [TabGroup("Fill in the Blank")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<FillInTheBlankQuestionData> fillInTheBlankQuestions = new List<FillInTheBlankQuestionData>();

        [TabGroup("Multi-Select")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<MultiSelectQuestionData> multiSelectQuestions = new List<MultiSelectQuestionData>();

        [TabGroup("Ordering")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<OrderingQuestionData> orderingQuestions = new List<OrderingQuestionData>();

        [TabGroup("Hotspot")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<HotspotQuestionData> hotspotQuestions = new List<HotspotQuestionData>();

        [TabGroup("Slider")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<SliderQuestionData> sliderQuestions = new List<SliderQuestionData>();

        [TabGroup("Audio")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<AudioQuestionData> audioQuestions = new List<AudioQuestionData>();

        [TabGroup("Multiple Choice")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<MultipleChoiceQuestionData> multipleChoiceQuestions = new List<MultipleChoiceQuestionData>();

        [TabGroup("Drag & Drop")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<DragDropQuestionData> dragDropQuestions = new List<DragDropQuestionData>();

        [TabGroup("Connect")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, HideAddButton = false, HideRemoveButton = false)]
        [Searchable]
        public List<ConnectQuestionData> connectQuestions = new List<ConnectQuestionData>();

        [Button("Load All Questions")]
        [PropertyOrder(-1)]
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

        [Button("Create True/False Question")]
        [TabGroup("True/False")]
        private void CreateTrueFalseQuestion()
        {
            CreateQuestion<TrueFalseQuestionData>("TrueFalseQuestion");
        }

        [Button("Create Fill in the Blank Question")]
        [TabGroup("Fill in the Blank")]
        private void CreateFillInTheBlankQuestion()
        {
            CreateQuestion<FillInTheBlankQuestionData>("FillInTheBlankQuestion");
        }

        [Button("Create Multi-Select Question")]
        [TabGroup("Multi-Select")]
        private void CreateMultiSelectQuestion()
        {
            CreateQuestion<MultiSelectQuestionData>("MultiSelectQuestion");
        }

        [Button("Create Ordering Question")]
        [TabGroup("Ordering")]
        private void CreateOrderingQuestion()
        {
            CreateQuestion<OrderingQuestionData>("OrderingQuestion");
        }

        [Button("Create Hotspot Question")]
        [TabGroup("Hotspot")]
        private void CreateHotspotQuestion()
        {
            CreateQuestion<HotspotQuestionData>("HotspotQuestion");
        }

        [Button("Create Slider Question")]
        [TabGroup("Slider")]
        private void CreateSliderQuestion()
        {
            CreateQuestion<SliderQuestionData>("SliderQuestion");
        }

        [Button("Create Audio Question")]
        [TabGroup("Audio")]
        private void CreateAudioQuestion()
        {
            CreateQuestion<AudioQuestionData>("AudioQuestion");
        }

        [Button("Create Multiple Choice Question")]
        [TabGroup("Multiple Choice")]
        private void CreateMultipleChoiceQuestion()
        {
            CreateQuestion<MultipleChoiceQuestionData>("MultipleChoiceQuestion");
        }

        [Button("Create Drag & Drop Question")]
        [TabGroup("Drag & Drop")]
        private void CreateDragDropQuestion()
        {
            CreateQuestion<DragDropQuestionData>("DragDropQuestion");
        }

        [Button("Create Connect Question")]
        [TabGroup("Connect")]
        private void CreateConnectQuestion()
        {
            CreateQuestion<ConnectQuestionData>("ConnectQuestion");
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

