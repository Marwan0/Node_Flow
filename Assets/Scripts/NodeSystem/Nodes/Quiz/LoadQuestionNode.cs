using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Loads and displays a specific question from a QuestionData asset.
    /// Waits for the question to be answered before continuing.
    /// </summary>
    [Serializable]
    public class LoadQuestionNode : NodeData
    {
        [SerializeField]
        public string questionAssetPath = "";

        [SerializeField]
        public string quizManagerPath = "QuizManager";

        [SerializeField]
        public bool waitForAnswer = true;

        [SerializeField]
        public bool trackInQuizState = true;

        [NonSerialized]
        private QuizManager _quizManager;

        [NonSerialized]
        private bool _questionAnswered = false;

        [NonSerialized]
        private bool _lastAnswerCorrect = false;

        public override string Name => "Load Question";
        public override Color Color => new Color(0.2f, 0.7f, 0.4f); // Green
        public override string Category => "Quiz";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("correct", "Correct", PortDirection.Output),
                new PortData("incorrect", "Incorrect", PortDirection.Output),
                new PortData("on_wrong", "On Wrong Attempt", PortDirection.Output),
                new PortData("complete", "Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            _questionAnswered = false;
            _lastAnswerCorrect = false;

            // Find QuizManager
            var managerObj = GameObject.Find(quizManagerPath);
            if (managerObj == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] QuizManager not found: {quizManagerPath}");
                Complete();
                return;
            }

            _quizManager = managerObj.GetComponent<QuizManager>();
            if (_quizManager == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] No QuizManager component on: {quizManagerPath}");
                Complete();
                return;
            }

            // Load question from path
#if UNITY_EDITOR
            var question = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionData>(questionAssetPath);
#else
            QuestionData question = null;
            Debug.LogWarning("[LoadQuestionNode] Runtime question loading requires Resources folder or Addressables");
#endif

            if (question == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] Question not found: {questionAssetPath}");
                Complete();
                return;
            }

            // Add question to manager if not already there
            if (!_quizManager.questions.Contains(question))
            {
                _quizManager.questions.Add(question);
            }

            int questionIndex = _quizManager.questions.IndexOf(question);

            if (waitForAnswer)
            {
                // Subscribe to answer events
                QuizState.OnLastAnswerResult += OnAnswerReceived;
                QuizState.OnWrongAttempt += OnWrongAttemptReceived;
                Runner?.StartCoroutine(LoadAndWaitForAnswer(questionIndex));
            }
            else
            {
                // Just load the question and continue
                Runner?.StartCoroutine(LoadQuestionOnly(questionIndex));
            }
        }

        private IEnumerator LoadQuestionOnly(int questionIndex)
        {
            // Navigate to the question
            if (_quizManager.currentQuestionIndex == 0 && questionIndex == 0)
            {
                _quizManager.StartQuiz();
            }
            else
            {
                while (_quizManager.currentQuestionIndex < questionIndex)
                {
                    _quizManager.NextQuestion();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(0.1f);
            Complete();
        }

        private IEnumerator LoadAndWaitForAnswer(int questionIndex)
        {
            // Navigate to the question
            if (_quizManager.currentQuestionIndex == 0 && questionIndex == 0)
            {
                _quizManager.StartQuiz();
            }
            else
            {
                while (_quizManager.currentQuestionIndex < questionIndex)
                {
                    _quizManager.NextQuestion();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Wait for answer
            while (!_questionAnswered && Runner != null && Runner.IsRunning)
            {
                yield return null;
            }

            // Unsubscribe
            QuizState.OnLastAnswerResult -= OnAnswerReceived;
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;

            // Note: QuizState.RecordAnswer is now called by QuizManager.OnQuestionAnswered()
            // so we don't need to call it here anymore

            // Set state for branching
            State = _lastAnswerCorrect ? NodeState.Completed : NodeState.Failed;
            Complete();
        }

        private void OnAnswerReceived(bool wasCorrect)
        {
            _questionAnswered = true;
            _lastAnswerCorrect = wasCorrect;
        }

        private void OnWrongAttemptReceived()
        {
            // Fire nodes connected to "on_wrong" port for VFX/sounds
            // This does NOT complete the question - just triggers feedback
            if (Runner != null && Runner.Graph != null)
            {
                var wrongNodes = Runner.Graph.GetConnectedNodes(Guid, "on_wrong");
                foreach (var node in wrongNodes)
                {
                    Runner.ExecuteNode(node);
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            _questionAnswered = false;
            _lastAnswerCorrect = false;
            QuizState.OnLastAnswerResult -= OnAnswerReceived;
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;
        }
    }
}
