using System;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Integration
{
    /// <summary>
    /// Bridge component that connects QuizManager to Node Graph system
    /// Add this to the same GameObject as QuizManager
    /// </summary>
    public class QuizGraphBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private QuizManager quizManager;

        [Header("Node Graph Integration")]
        [SerializeField]
        private NodeGraphRunner graphRunner;

        // Events for node system
        public event Action<bool> OnQuestionAnswered; // bool = isCorrect
        public event Action OnQuizStarted;
        public event Action OnQuizEnded;

        private void Awake()
        {
            if (quizManager == null)
            {
                quizManager = GetComponent<QuizManager>();
            }
        }

        private void Start()
        {
            // Subscribe to QuizManager events if available
            // Note: QuizManager would need to expose these events for full integration
        }

        /// <summary>
        /// Start quiz from node graph
        /// </summary>
        public void StartQuiz()
        {
            if (quizManager != null)
            {
                quizManager.StartQuiz();
                OnQuizStarted?.Invoke();
            }
        }

        /// <summary>
        /// Navigate to a specific question (uses public API)
        /// </summary>
        public void NavigateToQuestion(int index)
        {
            if (quizManager != null && quizManager.questions != null)
            {
                if (index >= 0 && index < quizManager.questions.Count)
                {
                    // Start quiz if needed
                    if (quizManager.currentQuestionIndex == 0 && index == 0)
                    {
                        quizManager.StartQuiz();
                    }
                    else if (index > 0)
                    {
                        // Ensure quiz is started
                        if (quizManager.currentQuestionIndex == 0)
                        {
                            quizManager.StartQuiz();
                        }
                        
                        // Navigate to target index
                        while (quizManager.currentQuestionIndex < index)
                        {
                            quizManager.NextQuestion();
                        }
                        while (quizManager.currentQuestionIndex > index)
                        {
                            quizManager.PreviousQuestion();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get current question index
        /// </summary>
        public int GetCurrentQuestionIndex()
        {
            // Would need to expose this from QuizManager
            return 0; // Placeholder
        }

        /// <summary>
        /// Get current score
        /// </summary>
        public int GetCurrentScore()
        {
            // Would need to expose this from QuizManager
            return 0; // Placeholder
        }

        /// <summary>
        /// Notify that a question was answered
        /// Called by QuizManager when answer is submitted
        /// </summary>
        public void NotifyAnswerSubmitted(bool isCorrect)
        {
            OnQuestionAnswered?.Invoke(isCorrect);
        }
    }
}

