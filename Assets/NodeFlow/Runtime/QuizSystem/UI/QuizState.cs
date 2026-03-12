using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    /// <summary>
    /// Tracks quiz state for node-based flow control.
    /// Singleton that persists across quiz sessions.
    /// </summary>
    public class QuizState : MonoBehaviour
    {
        [Serializable]
        public struct AnswerTimelineEntry
        {
            public int sequenceIndex;
            public int questionIndex;
            public bool wasCorrect;
            public int scoreAfterAnswer;
            public int questionsAnsweredAfterThis;
            public float timestamp;
        }

        public enum ScoreRecordStage
        {
            Partial,
            Final
        }

        [Serializable]
        public struct ScoreTimelineEntry
        {
            public int sequenceIndex;
            public int questionIndex;
            public ScoreRecordStage stage;
            public bool hasFinalResult;
            public bool wasCorrectFinalResult;
            public int questionRawMax;
            public int rawTargetAfterEvent;
            public int rawDeltaThisEvent;
            public int distributedDeltaThisEvent;
            public int scoreAfterEvent;
            public int completedUnits;
            public int totalUnits;
            public float normalizedProgress;
            public float timestamp;
        }

        [Serializable]
        public struct PartialAnswerTimelineEntry
        {
            public int sequenceIndex;
            public int questionIndex;
            public bool wasCorrect;
            public PartialAnswerEventType eventType;
            public float timestamp;
        }

        public enum PartialAnswerEventType
        {
            WrongAttempt,
            StepResult
        }

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
        public static event Action OnCorrectAttempt; // fires on EACH correct attempt in multi-step questions
        public static event Action<bool> OnStepResult; // fires once per finalized step in multi-step questions (correct placement or auto-corrected)
        public static event Action<AnswerTimelineEntry> OnAnswerRecorded; // fires once per finalized question with ordered timeline data
        public static event Action<ScoreTimelineEntry> OnScoreRecorded; // fires for both partial and final question score records
        public static event Action<PartialAnswerTimelineEntry> OnPartialAnswerRecorded; // fires on each partial attempt/step result
        public static event Action OnQuizStarted;
        public static event Action OnQuizCompleted;
        public static event Action<float> OnTimerTick; // remainingTime

        /// <summary>Fires after the UI is locked on a correct answer. Connect feedback nodes here.</summary>
        public static event Action OnCorrectAnswerFeedbackStart;
        /// <summary>Fires after the UI is locked on a wrong answer. Connect feedback nodes here.</summary>
        public static event Action OnWrongAnswerFeedbackStart;
        /// <summary>Fires after wrong feedback finishes on auto-correct. Connect auto-correct feedback nodes here.</summary>
        public static event Action OnAutoCorrectFeedbackStart;
        /// <summary>Fires when feedback nodes finish and the UI should be unlocked.</summary>
        public static event Action OnUIUnlockRequested;
        /// <summary>Fires when the UI should be locked before any interaction (e.g. for on_load intro nodes).</summary>
        public static event Action OnUILockRequested;

        // === UI Lock State ===
        /// <summary>Whether the quiz UI is currently locked (during answer feedback).</summary>
        public static bool UILocked { get; private set; } = false;

        // === State Properties ===
        [Header("Quiz Progress")]
        public int totalQuestions = 0;
        public int questionsAnswered = 0;
        public int correctAnswers = 0;
        public int wrongAnswers = 0;

        [Header("Score")]
        public int currentScore = 0;
        public int maxPossibleScore = 0;

        [Header("Current Question")]
        public int currentQuestionAttempt = 0;
        public int lastAnsweredPointIndex = -1;

        [Header("Last Answer")]
        public bool lastAnswerWasCorrect = false;
        public int lastQuestionIndex = -1;
        public int consecutiveCorrect = 0;
        public int consecutiveWrong = 0;

        [Header("Answer Timeline")]
        [SerializeField]
        private List<AnswerTimelineEntry> answerTimeline = new List<AnswerTimelineEntry>();

        [Header("Score Timeline")]
        [SerializeField]
        private List<ScoreTimelineEntry> scoreTimeline = new List<ScoreTimelineEntry>();

        [Header("Partial Answer Timeline")]
        [SerializeField]
        private List<PartialAnswerTimelineEntry> partialAnswerTimeline = new List<PartialAnswerTimelineEntry>();

        [Header("Timer")]
        public float timerDuration = 0f;
        public float timerRemaining = 0f;
        public bool timerActive = false;

        [Header("Quiz State")]
        public bool quizActive = false;
        public bool quizCompleted = false;

        [Header("Hints")]
        [Tooltip("Whether to show hints on wrong attempts (set by StartQuizNode)")]
        public bool showHints = true;

        [Header("QuizManager Reference")]
        [Tooltip("Set by StartQuizNode so all quiz nodes can share the same QuizManager")]
        [NonSerialized]
        public QuizManager quizManagerRef;

        [Header("Audio")]
        [Tooltip("When enabled, quiz sounds (hover, feedback, node sounds) can overlap. When disabled, each new sound stops the previous one.")]
        public bool allowAudioOverlap = false;

        [Header("Answer Animations")]
        [NonSerialized]
        public NodeSystem.Nodes.Quiz.AnswerAnimationSettings[] currentAnswerAnimations = null;

        [Header("Question Transitions")]
        [NonSerialized]
        public NodeSystem.Nodes.Quiz.QuestionTransitionSettings currentEnterTransition = null;
        [NonSerialized]
        public NodeSystem.Nodes.Quiz.QuestionTransitionSettings currentExitTransition = null;

        // Centralized audio source for all quiz sounds
        private AudioSource _quizAudioSource;

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

            if (answerTimeline == null)
                answerTimeline = new List<AnswerTimelineEntry>();

            var timelineEntry = new AnswerTimelineEntry
            {
                sequenceIndex = answerTimeline.Count,
                questionIndex = questionIndex,
                wasCorrect = wasCorrect,
                scoreAfterAnswer = currentScore,
                questionsAnsweredAfterThis = questionsAnswered,
                timestamp = Time.unscaledTime
            };
            answerTimeline.Add(timelineEntry);
             
            OnLastAnswerResult?.Invoke(wasCorrect);
            OnScoreChanged?.Invoke(currentScore);
            OnQuestionAnswered?.Invoke(questionsAnswered, totalQuestions);
            OnAnswerRecorded?.Invoke(timelineEntry);

            Debug.Log($"[QuizState] Answer recorded: Q{questionIndex} - {(wasCorrect ? "Correct" : "Wrong")} - Score: {currentScore}");

            // Check if quiz is complete
            if (questionsAnswered >= totalQuestions)
            {
                CompleteQuiz();
            }
        }

        /// <summary>
        /// Records a partial score update for a question (e.g. multi-step progress).
        /// </summary>
        public void RecordScoreProgress(int questionIndex, int questionRawMax, int rawTargetAfterEvent, int rawDeltaThisEvent, int distributedDeltaThisEvent, int completedUnits, int totalUnits)
        {
            if (scoreTimeline == null)
                scoreTimeline = new List<ScoreTimelineEntry>();

            int safeTotalUnits = Mathf.Max(0, totalUnits);
            int safeCompletedUnits = Mathf.Clamp(completedUnits, 0, safeTotalUnits > 0 ? safeTotalUnits : int.MaxValue);
            float normalized = 0f;
            if (safeTotalUnits > 0)
                normalized = Mathf.Clamp01((float)safeCompletedUnits / safeTotalUnits);
            else if (questionRawMax > 0)
                normalized = Mathf.Clamp01((float)Mathf.Max(0, rawTargetAfterEvent) / questionRawMax);

            var entry = new ScoreTimelineEntry
            {
                sequenceIndex = scoreTimeline.Count,
                questionIndex = questionIndex,
                stage = ScoreRecordStage.Partial,
                hasFinalResult = false,
                wasCorrectFinalResult = false,
                questionRawMax = Mathf.Max(0, questionRawMax),
                rawTargetAfterEvent = Mathf.Max(0, rawTargetAfterEvent),
                rawDeltaThisEvent = Mathf.Max(0, rawDeltaThisEvent),
                distributedDeltaThisEvent = Mathf.Max(0, distributedDeltaThisEvent),
                scoreAfterEvent = currentScore,
                completedUnits = safeCompletedUnits,
                totalUnits = safeTotalUnits,
                normalizedProgress = normalized,
                timestamp = Time.unscaledTime
            };

            scoreTimeline.Add(entry);
            OnScoreRecorded?.Invoke(entry);
        }

        /// <summary>
        /// Records the final question-scale score snapshot when a question is finalized.
        /// </summary>
        public void RecordQuestionScoreFinal(int questionIndex, bool wasCorrectFinalResult, int questionRawMax, int rawTargetAfterEvent, int rawDeltaThisEvent, int distributedDeltaThisEvent)
        {
            if (scoreTimeline == null)
                scoreTimeline = new List<ScoreTimelineEntry>();

            int safeRawMax = Mathf.Max(0, questionRawMax);
            int safeRawTarget = Mathf.Clamp(rawTargetAfterEvent, 0, safeRawMax > 0 ? safeRawMax : int.MaxValue);
            float normalized = safeRawMax > 0 ? Mathf.Clamp01((float)safeRawTarget / safeRawMax) : 0f;

            var entry = new ScoreTimelineEntry
            {
                sequenceIndex = scoreTimeline.Count,
                questionIndex = questionIndex,
                stage = ScoreRecordStage.Final,
                hasFinalResult = true,
                wasCorrectFinalResult = wasCorrectFinalResult,
                questionRawMax = safeRawMax,
                rawTargetAfterEvent = safeRawTarget,
                rawDeltaThisEvent = Mathf.Max(0, rawDeltaThisEvent),
                distributedDeltaThisEvent = Mathf.Max(0, distributedDeltaThisEvent),
                scoreAfterEvent = currentScore,
                completedUnits = safeRawTarget,
                totalUnits = safeRawMax,
                normalizedProgress = normalized,
                timestamp = Time.unscaledTime
            };

            scoreTimeline.Add(entry);
            OnScoreRecorded?.Invoke(entry);
        }

        /// <summary>
        /// Call when user submits a wrong answer but still has attempts left.
        /// This fires OnWrongAttempt for VFX/sounds without completing the question.
        /// </summary>
        public void NotifyWrongAttempt(int pointIndex = -1)
        {
            currentQuestionAttempt++;
            lastAnsweredPointIndex = pointIndex;
            RecordPartialAnswerEvent(false, -1, PartialAnswerEventType.WrongAttempt);
            OnWrongAttempt?.Invoke();
            Debug.Log($"[QuizState] Wrong attempt #{currentQuestionAttempt} (point: {pointIndex}) - user can try again");
        }

        /// <summary>
        /// Call when user performs a correct step (e.g. one correct connect pair)
        /// before the full question is complete.
        /// </summary>
        public void NotifyCorrectAttempt(int pointIndex = -1)
        {
            lastAnsweredPointIndex = pointIndex;
            OnCorrectAttempt?.Invoke();
            Debug.Log($"[QuizState] Correct attempt (point: {pointIndex})");
        }

        /// <summary>
        /// Call when a step is finalized in a multi-step question (e.g. one ordering slot filled,
        /// one drag-drop item placed). Fires once per step - correct placement or auto-corrected.
        /// Used by Score Progress Bar slots to track per-step results.
        /// </summary>
        public void NotifyStepResult(bool wasCorrect, int questionIndex = -1)
        {
            RecordPartialAnswerEvent(wasCorrect, questionIndex, PartialAnswerEventType.StepResult);
            OnStepResult?.Invoke(wasCorrect);
            Debug.Log($"[QuizState] Step result: {(wasCorrect ? "Correct" : "Wrong")}");
        }

        /// <summary>
        /// Called by QuestionUI after locking on a correct answer.
        /// Fires OnCorrectAnswerFeedbackStart so LoadQuestionNode can run feedback nodes.
        /// Returns true if any listeners are registered (so UI knows to wait for explicit unlock).
        /// </summary>
        public bool NotifyCorrectAnswerFeedback()
        {
            UILocked = true;
            bool hasListeners = OnCorrectAnswerFeedbackStart != null;
            OnCorrectAnswerFeedbackStart?.Invoke();
            Debug.Log("[QuizState] Correct answer feedback started (UI locked)");
            return hasListeners;
        }

        /// <summary>
        /// Called by QuestionUI after locking on a wrong answer.
        /// Fires OnWrongAnswerFeedbackStart so LoadQuestionNode can run feedback nodes.
        /// Returns true if any listeners are registered (so UI knows to wait for explicit unlock).
        /// </summary>
        public bool NotifyWrongAnswerFeedback()
        {
            UILocked = true;
            bool hasListeners = OnWrongAnswerFeedbackStart != null;
            OnWrongAnswerFeedbackStart?.Invoke();
            Debug.Log("[QuizState] Wrong answer feedback started (UI locked)");
            return hasListeners;
        }

        /// <summary>
        /// Called by QuestionUI after wrong feedback finishes during auto-correct.
        /// Fires OnAutoCorrectFeedbackStart so LoadQuestionNode can run auto-correct feedback nodes.
        /// Returns true if any listeners are registered (so UI knows to wait for explicit unlock).
        /// </summary>
        public bool NotifyAutoCorrectFeedback()
        {
            UILocked = true;
            bool hasListeners = OnAutoCorrectFeedbackStart != null;
            OnAutoCorrectFeedbackStart?.Invoke();
            Debug.Log("[QuizState] Auto-correct feedback started (UI locked)");
            return hasListeners;
        }

        /// <summary>
        /// Called by LoadQuestionNode to lock the UI before question interaction begins
        /// (e.g. while playing "on_load" intro nodes like sounds/animations).
        /// Always fires the event even if already locked, because a freshly created
        /// QuestionUI needs to receive the lock signal regardless of prior state.
        /// </summary>
        public static void RequestUILockForLoad()
        {
            UILocked = true;
            OnUILockRequested?.Invoke();
            Debug.Log("[QuizState] UI lock requested for load");
        }

        /// <summary>
        /// Called by UnlockQuizUINode (or automatically) to re-enable quiz UI interaction.
        /// </summary>
        public static void RequestUIUnlock()
        {
            // Guard: only unlock once per lock cycle
            if (!UILocked) return;
            UILocked = false;
            OnUIUnlockRequested?.Invoke();
            Debug.Log("[QuizState] UI unlock requested");
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

        /// <summary>
        /// Set per-question transition overrides (called by LoadQuestionNode before showing question).
        /// When set, QuizManager uses these instead of its inspector defaults.
        /// </summary>
        public void SetQuestionTransitions(
            NodeSystem.Nodes.Quiz.QuestionTransitionSettings enter,
            NodeSystem.Nodes.Quiz.QuestionTransitionSettings exit)
        {
            currentEnterTransition = enter;
            currentExitTransition = exit;
        }

        /// <summary>
        /// Clear per-question transition overrides (reverts to QuizManager defaults).
        /// </summary>
        public void ClearQuestionTransitions()
        {
            currentEnterTransition = null;
            currentExitTransition = null;
        }

        /// <summary>
        /// Centralized audio source for all quiz sounds (hover, feedback, PlaySoundNode).
        /// All quiz audio routes through this so overlap/stop behavior is consistent.
        /// </summary>
        public AudioSource QuizAudioSource
        {
            get
            {
                if (_quizAudioSource == null)
                {
                    _quizAudioSource = gameObject.AddComponent<AudioSource>();
                    _quizAudioSource.playOnAwake = false;
                    _quizAudioSource.loop = false;
                }
                return _quizAudioSource;
            }
        }

        /// <summary>
        /// Plays a sound through the centralized quiz audio source, respecting the overlap setting.
        /// </summary>
        public void PlaySound(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;
            var source = QuizAudioSource;
            source.pitch = pitch;

            if (allowAudioOverlap)
            {
                source.PlayOneShot(clip, volume);
            }
            else
            {
                source.Stop();
                source.clip = clip;
                source.volume = volume;
                source.Play();
            }
        }

        public void ResetState()
        {
            totalQuestions = 0;
            questionsAnswered = 0;
            correctAnswers = 0;
            wrongAnswers = 0;
            currentQuestionAttempt = 0;
            currentScore = 0;
            maxPossibleScore = 0;
            lastAnswerWasCorrect = false;
            lastQuestionIndex = -1;
            lastAnsweredPointIndex = -1;
            consecutiveCorrect = 0;
            consecutiveWrong = 0;
            timerDuration = 0;
            timerRemaining = 0;
            timerActive = false;
            quizActive = false;
            quizCompleted = false;
            showHints = true;
            quizManagerRef = null;
            answerTimeline?.Clear();
            scoreTimeline?.Clear();
            partialAnswerTimeline?.Clear();
            UILocked = false;
            // Don't clear currentAnswerAnimations here - they're per-question settings
            // and should persist until the next question sets new ones
            // currentAnswerAnimations = null;
            currentEnterTransition = null;
            currentExitTransition = null;
            Debug.Log("[QuizState] State reset (animations preserved)");
        }

        // === Computed Properties ===

        public float ScorePercentage => maxPossibleScore > 0 ? (float)currentScore / maxPossibleScore * 100f : 0f;
        public float CorrectPercentage => questionsAnswered > 0 ? (float)correctAnswers / questionsAnswered * 100f : 0f;
        public float ProgressPercentage => totalQuestions > 0 ? (float)questionsAnswered / totalQuestions * 100f : 0f;
        public int RemainingQuestions => totalQuestions - questionsAnswered;
        public bool TimerExpired => timerDuration > 0 && timerRemaining <= 0;
        public IReadOnlyList<AnswerTimelineEntry> AnswerTimeline => answerTimeline;
        public IReadOnlyList<ScoreTimelineEntry> ScoreTimeline => scoreTimeline;
        public IReadOnlyList<PartialAnswerTimelineEntry> PartialAnswerTimeline => partialAnswerTimeline;

        /// <summary>
        /// Returns score records in chronological order.
        /// Use flags to include only partial records, only final records, or both.
        /// </summary>
        public ScoreTimelineEntry[] GetScoreTimelineArray(bool includePartial = true, bool includeFinal = true)
        {
            if (scoreTimeline == null || scoreTimeline.Count == 0)
                return Array.Empty<ScoreTimelineEntry>();

            if (includePartial && includeFinal)
                return scoreTimeline.ToArray();

            if (!includePartial && !includeFinal)
                return Array.Empty<ScoreTimelineEntry>();

            var result = new List<ScoreTimelineEntry>(scoreTimeline.Count);
            for (int i = 0; i < scoreTimeline.Count; i++)
            {
                var entry = scoreTimeline[i];
                if (entry.stage == ScoreRecordStage.Partial && includePartial)
                    result.Add(entry);
                else if (entry.stage == ScoreRecordStage.Final && includeFinal)
                    result.Add(entry);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns the final question-scale score record for a question (if any).
        /// </summary>
        public bool TryGetFinalQuestionScoreRecord(int questionIndex, out ScoreTimelineEntry entry)
        {
            if (scoreTimeline != null)
            {
                for (int i = scoreTimeline.Count - 1; i >= 0; i--)
                {
                    if (scoreTimeline[i].questionIndex != questionIndex)
                        continue;
                    if (scoreTimeline[i].stage != ScoreRecordStage.Final)
                        continue;
                    entry = scoreTimeline[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Returns a compact answer order string (example: "RRWRRR").
        /// Useful for end-of-quiz animations that replay answer outcomes in order.
        /// </summary>
        public string GetAnswerOrderString(string correctToken = "R", string wrongToken = "W", string separator = "", bool includePartialAnswers = false, bool includeFinalAnswers = true)
        {
            if (!includePartialAnswers && !includeFinalAnswers)
                return string.Empty;

            if (includePartialAnswers && !includeFinalAnswers)
                return GetPartialAnswerOrderString(correctToken, wrongToken, separator);

            if (!includePartialAnswers && includeFinalAnswers)
            {
                if (answerTimeline == null || answerTimeline.Count == 0)
                    return string.Empty;

                var onlyFinal = new bool[answerTimeline.Count];
                for (int i = 0; i < answerTimeline.Count; i++)
                    onlyFinal[i] = answerTimeline[i].wasCorrect;
                return BuildPatternFromBooleans(onlyFinal, correctToken, wrongToken, separator);
            }

            if ((answerTimeline == null || answerTimeline.Count == 0) &&
                (partialAnswerTimeline == null || partialAnswerTimeline.Count == 0))
                return string.Empty;

            var merged = new List<(float time, bool wasCorrect, int tie)>();
            if (partialAnswerTimeline != null)
            {
                for (int i = 0; i < partialAnswerTimeline.Count; i++)
                    merged.Add((partialAnswerTimeline[i].timestamp, partialAnswerTimeline[i].wasCorrect, i));
            }
            if (answerTimeline != null)
            {
                for (int i = 0; i < answerTimeline.Count; i++)
                    merged.Add((answerTimeline[i].timestamp, answerTimeline[i].wasCorrect, 1000000 + i));
            }

            merged.Sort((a, b) =>
            {
                int timeCompare = a.time.CompareTo(b.time);
                if (timeCompare != 0) return timeCompare;
                return a.tie.CompareTo(b.tie);
            });

            var values = new bool[merged.Count];
            for (int i = 0; i < merged.Count; i++)
                values[i] = merged[i].wasCorrect;
            return BuildPatternFromBooleans(values, correctToken, wrongToken, separator);
        }

        /// <summary>
        /// Returns partial step correctness order (true = correct step, false = wrong/auto-corrected step).
        /// </summary>
        public string GetPartialAnswerOrderString(string correctToken = "R", string wrongToken = "W", string separator = "")
        {
            if (partialAnswerTimeline == null || partialAnswerTimeline.Count == 0)
                return string.Empty;

            var values = new bool[partialAnswerTimeline.Count];
            for (int i = 0; i < partialAnswerTimeline.Count; i++)
                values[i] = partialAnswerTimeline[i].wasCorrect;
            return BuildPatternFromBooleans(values, correctToken, wrongToken, separator);
        }

        /// <summary>
        /// Returns answer correctness in order (true = correct, false = wrong).
        /// </summary>
        public bool[] GetAnswerOrderArray()
        {
            if (answerTimeline == null || answerTimeline.Count == 0)
                return Array.Empty<bool>();

            bool[] values = new bool[answerTimeline.Count];
            for (int i = 0; i < answerTimeline.Count; i++)
                values[i] = answerTimeline[i].wasCorrect;
            return values;
        }

        private int ResolveCurrentQuestionIndexForPartialRecord()
        {
            if (quizManagerRef != null)
                return Mathf.Max(0, quizManagerRef.currentQuestionIndex);

            var manager = FindObjectOfType<QuizManager>();
            if (manager != null)
                return Mathf.Max(0, manager.currentQuestionIndex);

            return Mathf.Max(0, lastQuestionIndex);
        }

        private void RecordPartialAnswerEvent(bool wasCorrect, int questionIndex, PartialAnswerEventType eventType)
        {
            if (partialAnswerTimeline == null)
                partialAnswerTimeline = new List<PartialAnswerTimelineEntry>();

            int resolvedQuestionIndex = questionIndex >= 0 ? questionIndex : ResolveCurrentQuestionIndexForPartialRecord();
            var timelineEntry = new PartialAnswerTimelineEntry
            {
                sequenceIndex = partialAnswerTimeline.Count,
                questionIndex = resolvedQuestionIndex,
                wasCorrect = wasCorrect,
                eventType = eventType,
                timestamp = Time.unscaledTime
            };

            partialAnswerTimeline.Add(timelineEntry);
            OnPartialAnswerRecorded?.Invoke(timelineEntry);
        }

        private static string BuildPatternFromBooleans(IReadOnlyList<bool> values, string correctToken, string wrongToken, string separator)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            string safeCorrect = correctToken ?? string.Empty;
            string safeWrong = wrongToken ?? string.Empty;
            string safeSeparator = separator ?? string.Empty;

            var sb = new System.Text.StringBuilder(values.Count * Mathf.Max(1, safeCorrect.Length));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    sb.Append(safeSeparator);
                sb.Append(values[i] ? safeCorrect : safeWrong);
            }
            return sb.ToString();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
