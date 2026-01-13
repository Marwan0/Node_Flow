using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.IO;

namespace QuizSystem
{
    public class QuizDemoHelper : OdinEditorWindow
    {
        [MenuItem("Tools/Quiz System/Create Demo Questions")]
        private static void OpenWindow()
        {
            GetWindow<QuizDemoHelper>().Show();
        }

        [Button("Create Sample Multiple Choice Question")]
        [InfoBox("Creates a sample multiple choice question for testing")]
        private void CreateSampleMultipleChoice()
        {
            string path = "Assets/Data/Questions";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            MultipleChoiceQuestionData question = ScriptableObject.CreateInstance<MultipleChoiceQuestionData>();
            question.questionText = "What is the capital of France?";
            question.answers = new string[] { "Paris", "London", "Berlin", "Madrid" };
            question.correctAnswerIndex = 0;
            question.hints = new string[] 
            { 
                "It's a famous city known for the Eiffel Tower",
                "It starts with the letter P",
                "It's in the north of France"
            };
            question.maxAttempts = 3;
            question.points = 10;
            question.explanation = "Paris is the capital and largest city of France.";

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Sample_MultipleChoice_Question.asset");
            AssetDatabase.CreateAsset(question, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = question;
            Debug.Log($"Created sample question at: {assetPath}");
        }

        [Button("Create Sample True/False Question")]
        private void CreateSampleTrueFalse()
        {
            string path = "Assets/Data/Questions";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            TrueFalseQuestionData question = ScriptableObject.CreateInstance<TrueFalseQuestionData>();
            question.questionText = "The Earth is round.";
            question.correctAnswer = true;
            question.hints = new string[] 
            { 
                "Think about what shape planets are",
                "It's not flat",
                "Scientists have proven this"
            };
            question.maxAttempts = 3;
            question.points = 5;
            question.explanation = "Yes, the Earth is approximately spherical (an oblate spheroid).";

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Sample_TrueFalse_Question.asset");
            AssetDatabase.CreateAsset(question, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = question;
            Debug.Log($"Created sample question at: {assetPath}");
        }

        [Button("Create Sample Drag & Drop Question")]
        private void CreateSampleDragDrop()
        {
            string path = "Assets/Data/Questions";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DragDropQuestionData question = ScriptableObject.CreateInstance<DragDropQuestionData>();
            question.questionText = "Match the countries with their capitals:";
            
            // Add drag items (countries)
            question.dragItems.Add(new DragDropQuestionData.DragItem { label = "France" });
            question.dragItems.Add(new DragDropQuestionData.DragItem { label = "Germany" });
            question.dragItems.Add(new DragDropQuestionData.DragItem { label = "Spain" });
            
            // Add drop zones (capitals)
            question.dropZones.Add(new DragDropQuestionData.DropZone { label = "Paris" });
            question.dropZones.Add(new DragDropQuestionData.DropZone { label = "Berlin" });
            question.dropZones.Add(new DragDropQuestionData.DropZone { label = "Madrid" });
            
            // Set correct pairings: France->Paris (0->0), Germany->Berlin (1->1), Spain->Madrid (2->2)
            question.correctPairings[0] = 0; // France -> Paris
            question.correctPairings[1] = 1; // Germany -> Berlin
            question.correctPairings[2] = 2; // Spain -> Madrid
            
            question.hints = new string[] 
            { 
                "France's capital is known for the Eiffel Tower",
                "Germany's capital starts with B",
                "Spain's capital is in the center of the country"
            };
            question.maxAttempts = 3;
            question.points = 15;
            question.explanation = "France->Paris, Germany->Berlin, Spain->Madrid";

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Sample_DragDrop_Question.asset");
            AssetDatabase.CreateAsset(question, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = question;
            Debug.Log($"Created sample question at: {assetPath}");
        }

        [Button("Create Sample Connect Question")]
        private void CreateSampleConnect()
        {
            string path = "Assets/Data/Questions";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            ConnectQuestionData question = ScriptableObject.CreateInstance<ConnectQuestionData>();
            question.questionText = "Connect the animals with their habitats:";
            
            // Left column (animals)
            question.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Polar Bear" });
            question.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Camel" });
            question.leftColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Penguin" });
            
            // Right column (habitats)
            question.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Arctic" });
            question.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Desert" });
            question.rightColumnItems.Add(new ConnectQuestionData.ConnectItem { label = "Antarctica" });
            
            // Set correct connections
            question.correctConnections[0] = 0; // Polar Bear -> Arctic
            question.correctConnections[1] = 1; // Camel -> Desert
            question.correctConnections[2] = 2; // Penguin -> Antarctica
            
            question.hints = new string[] 
            { 
                "Polar bears live in cold, icy regions",
                "Camels are adapted to dry, hot environments",
                "Penguins live in the southernmost continent"
            };
            question.maxAttempts = 3;
            question.points = 15;
            question.explanation = "Polar Bear->Arctic, Camel->Desert, Penguin->Antarctica";

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Sample_Connect_Question.asset");
            AssetDatabase.CreateAsset(question, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = question;
            Debug.Log($"Created sample question at: {assetPath}");
        }

        [Button("Create All Sample Questions")]
        [InfoBox("Creates all sample questions at once")]
        private void CreateAllSamples()
        {
            CreateSampleMultipleChoice();
            CreateSampleTrueFalse();
            CreateSampleDragDrop();
            CreateSampleConnect();
            Debug.Log("All sample questions created!");
        }
    }
}

