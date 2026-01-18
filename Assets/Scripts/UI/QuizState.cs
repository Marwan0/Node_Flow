using System;
using UnityEngine;

namespace QuizSystem
{
    /// <summary>
    /// Tracks quiz state for node-based flow control.
    /// Singleton that persists across quiz sessions.
    /// </summary>
    public class QuizState : MonoBehaviour
    {
        private static QuizState _instance;
        public static QuizState Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("QuizState");
                    _instance = go.AddComponent<QuizState>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // === Events ===
        public static event Action<int> OnScoreChanged;
        public static event Action<int, int> OnQuestionAnswered; // questionIndex, totalQuestions
        public static event Action<bool> OnLastAnswerResult; // wasCorrect - fires when question is COMPLETE (correct or all attempts used)
        public static event Action OnWrongAttempt; // fires on EACH wrong answer (for VFX/sounds) - doesn't complete question
        public static event Action OnQuizStarted;
        public static event Action OnQuizCompleted;
        public static event Action<float> OnTimerTick; // remainingTime

        // === State Properties ===
        [Header("Quiz Progress")]
        public int totalQuestions = 0;
        public int questionsAnswered = 0;
        public int correctAnswers = 0;
        public int wrongAnswers = 0;

        [Header("Score")]
        public int currentScore = 0;
        public int maxPossibleScore = 0;

        [Header("Last Answer")]
        public bool lastAnswerWasCorrect = false;
        public int lastQuestionIndex = -1;
        public int consecutiveCorrect = 0;
        public int consecutiveWrong = 0;

        [Header("Timer")]
        public float timerDuration = 0f;
        public float timerRemaining = 0f;
        public bool timerActive = false;

        [Header("Quiz State")]
        public bool quizActive = false;
        public bool quizCompleted = false;

        [Header("Answer Animations")]
        [NonSerialized]
        public NodeSystem.Nodes.Quiz.AnswerAnimationSettings[] currentAnswerAnimations = null;

        private void Update()
        {
            if (timerActive && timerRemaining > 0)
            {
                timerRemaining -= Time.deltaTime;
                OnTimerTick?.Invoke(timerRemaining);

                if (timerRemaining <= 0)
                {
                    timerRemaining = 0;
                    timerActive = false;
                }
            }
        }

        // === Public Methods ===

        public void StartQuiz(int questionCount, int maxScore = 0)
        {
            ResetState();
            totalQuestions = questionCount;
            maxPossibleScore = maxScore > 0 ? maxScore : questionCount * 10;
            quizActive = true;
            quizCompleted = false;
            OnQuizStarted?.Invoke();
            Debug.Log($"[QuizState] Quiz started with {questionCount} questions, max score: {maxPossibleScore}");
        }

        public void RecordAnswer(int questionIndex, bool wasCorrect, int pointsEarned)
        {
            questionsAnswered++;
            lastQuestionIndex = questionIndex;
            lastAnswerWasCorrect = wasCorrect;

            if (wasCorrect)
            {
                correctAnswers++;
                consecutiveCorrect++;
                consecutiveWrong = 0;
            }
            else
            {
                wrongAnswers++;
                consecutiveWrong++;
                consecutiveCorrect = 0;
            }

            currentScore += pointsEarned;
            
            OnLastAnswerResult?.Invoke(wasCorrect);
            OnScoreChanged?.Invoke(currentScore);
            OnQuestionAnswered?.Invoke(questionsAnswered, totalQuestions);

            Debug.Log($"[QuizState] Answer recorded: Q{questionIndex} - {(wasCorrect ? "Correct" : "Wrong")} - Score: {currentScore}");

            // Check if quiz is complete
            if (questionsAnswered >= totalQuestions)
            {
                CompleteQuiz();
            }
        }

        /// <summary>
        /// Call when user submits a wrong answer but still has attempts left.
        /// This fires OnWrongAttempt for VFX/sounds without completing the question.
        /// </summary>
        public void NotifyWrongAttempt()
        {
            OnWrongAttempt?.Invoke();
            Debug.Log("[QuizState] Wrong attempt - user can try again");
        }

        public void AddScore(int points)
        {
            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);
            Debug.Log($"[QuizState] Score added: +{points} = {currentScore}");
        }

        public void SetScore(int score)
        {
            currentScore = score;
            OnScoreChanged?.Invoke(currentScore);
            Debug.Log($"[QuizState] Score set to: {currentScore}");
        }

        public void StartTimer(float duration)
        {
            timerDuration = duration;
            timerRemaining = duration;
            timerActive = true;
            Debug.Log($"[QuizState] Timer started: {duration}s");
        }

        public void StopTimer()
        {
            timerActive = false;
            Debug.Log($"[QuizState] Timer stopped at {timerRemaining}s remaining");
        }

        public void PauseTimer()
        {
            timerActive = false;
        }

        public void ResumeTimer()
        {
            if (timerRemaining > 0)
            {
                timerActive = true;
            }
        }

        public void CompleteQuiz()
        {
            quizActive = false;
            quizCompleted = true;
            timerActive = false;
            OnQuizCompleted?.Invoke();
            Debug.Log($"[QuizState] Quiz completed! Score: {currentScore}/{maxPossibleScore}, Correct: {correctAnswers}/{totalQuestions}");
        }

        /// <summary>
        /// Set answer animation settings for the current question (called by LoadQuestionNode)
        /// </summary>
        public void SetAnswerAnimations(NodeSystem.Nodes.Quiz.AnswerAnimationSettings[] animations)
        {
            currentAnswerAnimations = animations;
        }

        public void ResetState()
        {
            totalQuestions = 0;
            questionsAnswered = 0;
            correctAnswers = 0;
            wrongAnswers = 0;
            currentScore = 0;
            maxPossibleScore = 0;
            lastAnswerWasCorrect = false;
            lastQuestionIndex = -1;
            consecutiveCorrect = 0;
            consecutiveWrong = 0;
            timerDuration = 0;
            timerRemaining = 0;
            timerActive = false;
            quizActive = false;
            quizCompleted = false;
            // Don't clear currentAnswerAnimations here - they're per-question settings
            // and should persist until the next question sets new ones
            // currentAnswerAnimations = null;
            Debug.Log("[QuizState] State reset (animations preserved)");
        }

        // === Computed Properties ===

        public float ScorePercentage => maxPossibleScore > 0 ? (float)currentScore / maxPossibleScore * 100f : 0f;
        public float CorrectPercentage => questionsAnswered > 0 ? (float)correctAnswers / questionsAnswered * 100f : 0f;
        public float ProgressPercentage => totalQuestions > 0 ? (float)questionsAnswered / totalQuestions * 100f : 0f;
        public int RemainingQuestions => totalQuestions - questionsAnswered;
        public bool TimerExpired => timerDuration > 0 && timerRemaining <= 0;

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
