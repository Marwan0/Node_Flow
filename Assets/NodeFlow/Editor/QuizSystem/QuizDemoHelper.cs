#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace QuizSystem
{
    public class QuizDemoHelper : EditorWindow
    {
        private const string DefaultPath = "Assets/NodeFlow/Data/Questions";
        private Vector2 _scrollPos;

        [MenuItem("Tools/Quiz System/Create Demo Questions")]
        private static void OpenWindow()
        {
            GetWindow<QuizDemoHelper>("Demo Questions").Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create Sample Questions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates sample questions for testing.\n" +
                "All 10 question types are supported.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Sample Multiple Choice Question", GUILayout.Height(30)))
                CreateSampleMultipleChoice();
            if (GUILayout.Button("Create Sample True/False Question", GUILayout.Height(30)))
                CreateSampleTrueFalse();
            if (GUILayout.Button("Create Sample Fill in the Blank Question", GUILayout.Height(30)))
                CreateSampleFillInTheBlank();
            if (GUILayout.Button("Create Sample Multi-Select Question", GUILayout.Height(30)))
                CreateSampleMultiSelect();
            if (GUILayout.Button("Create Sample Ordering Question", GUILayout.Height(30)))
                CreateSampleOrdering();
            if (GUILayout.Button("Create Sample Drag & Drop Question", GUILayout.Height(30)))
                CreateSampleDragDrop();
            if (GUILayout.Button("Create Sample Connect Question", GUILayout.Height(30)))
                CreateSampleConnect();
            if (GUILayout.Button("Create Sample Hotspot Question", GUILayout.Height(30)))
                CreateSampleHotspot();
            if (GUILayout.Button("Create Sample Slider Question", GUILayout.Height(30)))
                CreateSampleSlider();
            if (GUILayout.Button("Create Sample Audio Question", GUILayout.Height(30)))
                CreateSampleAudio();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create All Sample Questions", GUILayout.Height(40)))
                CreateAllSamples();

            EditorGUILayout.EndScrollView();
        }

        // ───────────────────────── helpers ─────────────────────────

        private static void EnsurePath()
        {
            if (!Directory.Exists(DefaultPath))
                Directory.CreateDirectory(DefaultPath);
        }

        private static void SaveAndSelect(ScriptableObject asset, string filePrefix)
        {
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{DefaultPath}/{filePrefix}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"Created sample question at: {assetPath}");
        }

        // ───────────────────── question creators ─────────────────────

        private void CreateSampleMultipleChoice()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
            q.questionText = "What is the capital of France?";
            q.answers = NodeSystem.StringIdSelector.Create("Paris", "London", "Berlin", "Madrid");
            q.hints = new string[]
            {
                "It's a famous city known for the Eiffel Tower",
                "It starts with the letter P",
                "It's in the north of France"
            };
            q.maxAttempts = 3;
            q.points = 10;
            q.explanation = "Paris is the capital and largest city of France.";

            SaveAndSelect(q, "Sample_MultipleChoice_Question");
        }

        private void CreateSampleTrueFalse()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<TrueFalseQuestionData>();
            q.questionText = "The Earth is round.";
            q.correctAnswer = true;
            q.hints = new string[]
            {
                "Think about what shape planets are",
                "It's not flat",
                "Scientists have proven this"
            };
            q.maxAttempts = 3;
            q.points = 5;
            q.explanation = "Yes, the Earth is approximately spherical (an oblate spheroid).";

            SaveAndSelect(q, "Sample_TrueFalse_Question");
        }

        private void CreateSampleFillInTheBlank()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<FillInTheBlankQuestionData>();
            q.questionText = "The chemical symbol for water is _____.";
            q.correctAnswer = "H2O";
            q.alternativeAnswers = new List<string> { "h2o", "H₂O" };
            q.caseSensitive = false;
            q.allowPartialMatch = false;
            q.hints = new string[]
            {
                "It contains Hydrogen and Oxygen",
                "There are two atoms of hydrogen",
                "It starts with H"
            };
            q.maxAttempts = 3;
            q.points = 10;
            q.explanation = "Water's chemical formula is H2O — two hydrogen atoms bonded to one oxygen atom.";

            SaveAndSelect(q, "Sample_FillInTheBlank_Question");
        }

        private void CreateSampleMultiSelect()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<MultiSelectQuestionData>();
            q.questionText = "Which of the following are primary colors? (Select all that apply)";
            q.options = new List<string> { "Red", "Green", "Blue", "Yellow", "Purple" };
            q.correctAnswerIndices = new List<int> { 0, 2, 3 };
            q.allowPartialCredit = true;
            q.hints = new string[]
            {
                "There are three correct answers",
                "Primary colors cannot be made by mixing other colors",
                "Think of the colors in a basic paint set"
            };
            q.maxAttempts = 3;
            q.points = 15;
            q.explanation = "The traditional primary colors are Red, Blue, and Yellow.";

            SaveAndSelect(q, "Sample_MultiSelect_Question");
        }

        private void CreateSampleOrdering()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<OrderingQuestionData>();
            q.questionText = "Form a correct sentence:";
            // Items: [0]=Marwan  [1]=and  [2]=Ali  [3]=can  [4]=swim
            q.items = new List<string> { "Marwan", "and", "Ali", "can", "swim" };
            q.shuffleItems = true;
            q.allowPartialCredit = true;
            q.validationMode = OrderingQuestionData.OrderValidationMode.NaturalIndexOrder;

            // Primary order (natural): Marwan and Ali can swim  -> [0,1,2,3,4]
            // Alternative order:       Ali and Marwan can swim  -> [2,1,0,3,4]
            q.alternativeOrders = new List<AlternativeOrder>
            {
                new AlternativeOrder { order = new List<int> { 2, 1, 0, 3, 4 } }
            };

            q.hints = new string[]
            {
                "The sentence ends with 'can swim'",
                "Two names are connected by 'and'",
                "Either name can come first"
            };
            q.maxAttempts = 3;
            q.points = 15;
            q.explanation = "Both 'Marwan and Ali can swim' and 'Ali and Marwan can swim' are correct.";

            SaveAndSelect(q, "Sample_Ordering_Question");
        }

        private void CreateSampleDragDrop()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<DragDropQuestionData>();
            q.questionText = "Match the cities with their countries:";

            q.dragItems.Add(new DragDropQuestionData.DragItem { label = "Paris" });
            q.dragItems.Add(new DragDropQuestionData.DragItem { label = "Berlin" });
            q.dragItems.Add(new DragDropQuestionData.DragItem { label = "Madrid" });

            q.dropZones.Add(new DragDropQuestionData.DropZone { label = "France" });
            q.dropZones.Add(new DragDropQuestionData.DropZone { label = "Germany" });
            q.dropZones.Add(new DragDropQuestionData.DropZone { label = "Spain" });

            q.correctPairings.Add(new DragDropQuestionData.Pairing { dragIndex = 0, dropIndex = 0 });
            q.correctPairings.Add(new DragDropQuestionData.Pairing { dragIndex = 1, dropIndex = 1 });
            q.correctPairings.Add(new DragDropQuestionData.Pairing { dragIndex = 2, dropIndex = 2 });

            q.hints = new string[]
            {
                "Paris is known for the Eiffel Tower",
                "Berlin starts with B",
                "Madrid is in the center of the country"
            };
            q.maxAttempts = 3;
            q.points = 15;
            q.explanation = "Paris->France, Berlin->Germany, Madrid->Spain";

            SaveAndSelect(q, "Sample_DragDrop_Question");
        }

        private void CreateSampleConnect()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<ConnectQuestionData>();
            q.questionText = "Connect the animals with their habitats:";

            q.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Polar Bear" });
            q.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Camel" });
            q.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Penguin" });

            q.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Arctic" });
            q.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Desert" });
            q.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Antarctica" });

            q.correctConnections[0] = 0;
            q.correctConnections[1] = 1;
            q.correctConnections[2] = 2;

            q.hints = new string[]
            {
                "Polar bears live in cold, icy regions",
                "Camels are adapted to dry, hot environments",
                "Penguins live in the southernmost continent"
            };
            q.maxAttempts = 3;
            q.points = 15;
            q.explanation = "Polar Bear->Arctic, Camel->Desert, Penguin->Antarctica";

            SaveAndSelect(q, "Sample_Connect_Question");
        }

        private void CreateSampleHotspot()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<HotspotQuestionData>();
            q.questionText = "Click on the area where the heart is located in the human body.";
            q.image = null; // No sprite bundled — assign one manually

            q.hotspotRegions = new List<HotspotRegion>
            {
                new HotspotRegion
                {
                    name = "Heart",
                    normalizedPosition = new Vector2(0.55f, 0.35f),
                    normalizedSize = new Vector2(0.12f, 0.12f),
                    shape = HotspotShape.Circle,
                    normalizedRadius = 0.06f
                },
                new HotspotRegion
                {
                    name = "Left Lung",
                    normalizedPosition = new Vector2(0.65f, 0.30f),
                    normalizedSize = new Vector2(0.15f, 0.20f),
                    shape = HotspotShape.Rectangle
                },
                new HotspotRegion
                {
                    name = "Right Lung",
                    normalizedPosition = new Vector2(0.40f, 0.30f),
                    normalizedSize = new Vector2(0.15f, 0.20f),
                    shape = HotspotShape.Rectangle
                }
            };
            q.correctHotspotIndex = 0;
            q.allowMultipleSelections = false;

            q.hints = new string[]
            {
                "It's in the chest area",
                "Slightly left of center",
                "It's between the lungs"
            };
            q.maxAttempts = 3;
            q.points = 10;
            q.explanation = "The heart is located in the chest, slightly to the left of the center, between the lungs.";

            SaveAndSelect(q, "Sample_Hotspot_Question");
        }

        private void CreateSampleSlider()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<SliderQuestionData>();
            q.questionText = "What is the boiling point of water in degrees Celsius?";
            q.valueRange = new Vector2(0, 200);
            q.correctValue = 100f;
            q.useTolerance = true;
            q.tolerance = 5f;
            q.showValueLabels = true;
            q.showCurrentValue = true;
            q.decimalPlaces = 0;
            q.hints = new string[]
            {
                "It's a round number",
                "It's exactly in the middle of the slider range",
                "Think about the Celsius scale"
            };
            q.maxAttempts = 3;
            q.points = 10;
            q.explanation = "Water boils at 100 °C at standard atmospheric pressure.";

            SaveAndSelect(q, "Sample_Slider_Question");
        }

        private void CreateSampleAudio()
        {
            EnsurePath();

            var q = ScriptableObject.CreateInstance<AudioQuestionData>();
            q.questionText = "Listen to the audio clip and identify the instrument.";
            q.audioClip = null; // No clip bundled — assign one manually
            q.allowReplay = true;
            q.autoPlay = false;
            q.maxPlayCount = 3;
            q.answerType = AudioAnswerType.MultipleChoice;
            q.answerOptions = new List<string> { "Piano", "Guitar", "Violin", "Drums" };
            q.correctAnswerIndex = 0;
            q.hints = new string[]
            {
                "It's a keyboard instrument",
                "It has 88 keys",
                "It's one of the most popular classical instruments"
            };
            q.maxAttempts = 3;
            q.points = 10;
            q.explanation = "The instrument heard in the clip is a Piano.";

            SaveAndSelect(q, "Sample_Audio_Question");
        }

        // ───────────────────────── batch ─────────────────────────

        private void CreateAllSamples()
        {
            CreateSampleMultipleChoice();
            CreateSampleTrueFalse();
            CreateSampleFillInTheBlank();
            CreateSampleMultiSelect();
            CreateSampleOrdering();
            CreateSampleDragDrop();
            CreateSampleConnect();
            CreateSampleHotspot();
            CreateSampleSlider();
            CreateSampleAudio();
            Debug.Log("All 10 sample questions created!");
        }
    }
}
#endif
