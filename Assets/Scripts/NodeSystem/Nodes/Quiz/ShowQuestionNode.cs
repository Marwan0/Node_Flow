using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Shows a quiz question and waits for answer
    /// </summary>
    [Serializable]
    public class ShowQuestionNode : NodeData
    {
        [SerializeField]
        public string quizManagerPath = "";
        
        [SerializeField]
        public int questionIndex = 0;

        [NonSerialized]
        private QuizManager _quizManager;
        
        [NonSerialized]
        private bool _questionAnswered = false;

        public override string Name => "Show Question";
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
                new PortData("complete", "Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(quizManagerPath))
            {
                Debug.LogWarning("[ShowQuestionNode] No quiz manager path specified");
                Complete();
                return;
            }

            var managerObj = GameObject.Find(quizManagerPath);
            if (managerObj == null)
            {
                Debug.LogWarning($"[ShowQuestionNode] QuizManager not found: {quizManagerPath}");
                Complete();
                return;
            }

            _quizManager = managerObj.GetComponent<QuizManager>();
            if (_quizManager == null)
            {
                Debug.LogWarning($"[ShowQuestionNode] No QuizManager component on: {quizManagerPath}");
                Complete();
                return;
            }

            // Show question
            if (_quizManager.questions != null && questionIndex >= 0 && questionIndex < _quizManager.questions.Count)
            {
                Debug.Log($"[ShowQuestionNode] Showing question {questionIndex}");
                _questionAnswered = false;
                
                // Navigate to the question using public API
                Runner?.StartCoroutine(NavigateToQuestion());
            }
            else
            {
                Debug.LogWarning($"[ShowQuestionNode] Invalid question index: {questionIndex}");
                Complete();
            }
        }

        private IEnumerator NavigateToQuestion()
        {
            // Start quiz if not already started
            if (_quizManager.currentQuestionIndex == 0 && questionIndex == 0)
            {
                _quizManager.StartQuiz();
                yield return new WaitForSeconds(0.1f); // Wait for question to load
            }
            else if (questionIndex > 0)
            {
                // Ensure quiz is started
                if (_quizManager.currentQuestionIndex == 0)
                {
                    _quizManager.StartQuiz();
                    yield return new WaitForSeconds(0.1f);
                }
                
                // Navigate forward/backward to reach target index
                while (_quizManager.currentQuestionIndex < questionIndex && Runner.IsRunning)
                {
                    _quizManager.NextQuestion();
                    yield return new WaitForSeconds(0.1f); // Wait for question to load
                }
                while (_quizManager.currentQuestionIndex > questionIndex && Runner.IsRunning)
                {
                    _quizManager.PreviousQuestion();
                    yield return new WaitForSeconds(0.1f); // Wait for question to load
                }
            }
            
            // Wait for answer
            yield return Runner.StartCoroutine(WaitForAnswer());
        }

        private IEnumerator WaitForAnswer()
        {
            // Wait until question is answered
            // In a full implementation, you'd subscribe to QuizManager's answer events
            while (!_questionAnswered && Runner.IsRunning)
            {
                yield return null;
            }

            if (_questionAnswered)
            {
                // Determine if correct/incorrect based on QuizManager state
                // For now, always go to complete
                Complete();
            }
        }

        /// <summary>
        /// Called by QuizGraphBridge when question is answered
        /// </summary>
        public void OnQuestionAnswered(bool isCorrect)
        {
            _questionAnswered = true;
            
            // Set state based on correctness
            State = isCorrect ? NodeState.Completed : NodeState.Failed;
            
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            _questionAnswered = false;
        }
    }
}

