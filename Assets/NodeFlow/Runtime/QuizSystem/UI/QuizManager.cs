using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace QuizSystem
{
    public class QuizManager : MonoBehaviour
    {
        [Header("Quiz Settings")]
        [Tooltip("List of questions for this quiz")]
        public List<QuestionData> questions = new List<QuestionData>();

        [Tooltip("Shuffle questions before starting")]
        public bool shuffleQuestions = false;

        [Header("UI References")]
        [Tooltip("Parent to instantiate each question under. You can disable this or animate it to remove the current question before the next one.")]
        public Transform questionContainer;

        [Tooltip("Prefab for True/False questions")]
        public GameObject trueFalseUIPrefab;

        [Tooltip("Prefab for Fill in the Blank questions")]
        public GameObject fillInTheBlankUIPrefab;

        [Tooltip("Prefab for Multi-Select questions")]
        public GameObject multiSelectUIPrefab;

        [Tooltip("Prefab for Ordering questions")]
        public GameObject orderingUIPrefab;

        [Tooltip("Prefab for Hotspot questions")]
        public GameObject hotspotUIPrefab;

        [Tooltip("Prefab for Slider questions")]
        public GameObject sliderUIPrefab;

        [Tooltip("Prefab for Audio questions")]
        public GameObject audioUIPrefab;

        [Tooltip("Prefab for Multiple Choice questions")]
        public GameObject multipleChoiceUIPrefab;

        [Tooltip("Prefab for Drag & Drop questions")]
        public GameObject dragDropUIPrefab;

        [Tooltip("Prefab for Connect questions")]
        public GameObject connectUIPrefab;

        [Header("Score")]
        [Tooltip("Current score")]
        public int currentScore = 0;

        [Tooltip("Current question index")]
        public int currentQuestionIndex = 0;

        [Header("Animations")]
        [Tooltip("Enable smooth transitions between questions")]
        public bool enableTransitions = true;

        [Tooltip("Duration of fade transition")]
        [Range(0.1f, 1f)]
        public float transitionDuration = 0.3f;

        [Tooltip("Transition style")]
        public TransitionStyle transitionStyle = TransitionStyle.Fade;

        public enum TransitionStyle
        {
            Fade,
            Slide,
            Scale
        }

        private List<QuestionData> shuffledQuestions;
        private QuestionUI currentQuestionUI;
        private IQuestionValidator currentValidator;
        private Transform currentQuestionWrapper;
        private Sequence currentTransitionSequence;
        private Dictionary<int, GameObject> _runtimeUIPrefabOverrides;
        private int _totalQuestionWeight;
        private float _scoreRemainder;
        private Dictionary<int, int> _awardedRawByQuestion = new Dictionary<int, int>();
        private int _lastScoreConfigSignature = int.MinValue;

        private struct ScoreAwardResult
        {
            public int questionRawMax;
            public int rawTarget;
            public int rawDelta;
            public int distributedDelta;
            public int totalScoreAfterAward;
        }

        private void Awake()
        {
            // No longer add CanvasGroup to questionContainer; each question gets its own wrapper with transition applied
        }

        public void StartQuiz()
        {
            if (questions == null || questions.Count == 0)
            {
                Debug.LogError("No questions assigned to quiz!");
                return;
            }
            EnsureQuizStarted();
            LoadQuestion(0);
        }

        /// <summary>
        /// Set a UI prefab to use for the next time the question at this index is loaded.
        /// Used by LoadQuestionNode to pass a layout override per load. Cleared after that load.
        /// </summary>
        public void SetUIPrefabOverrideForLoad(int questionIndex, GameObject uiPrefab)
        {
            if (uiPrefab == null) return;
            if (_runtimeUIPrefabOverrides == null)
                _runtimeUIPrefabOverrides = new Dictionary<int, GameObject>();
            _runtimeUIPrefabOverrides[questionIndex] = uiPrefab;
        }

        /// <summary>
        /// Ensures shuffledQuestions and current state are initialized (e.g. when using node graph without StartQuiz).
        /// Does not load or show a question; use LoadQuestion(0) or StartQuiz() for that.
        /// </summary>
        public void EnsureQuizStarted()
        {
            if (shuffledQuestions == null)
            {
                if (questions == null || questions.Count == 0) return;
                shuffledQuestions = new List<QuestionData>(questions);
                if (shuffleQuestions)
                {
                    for (int i = 0; i < shuffledQuestions.Count; i++)
                    {
                        QuestionData temp = shuffledQuestions[i];
                        int randomIndex = Random.Range(i, shuffledQuestions.Count);
                        shuffledQuestions[i] = shuffledQuestions[randomIndex];
                        shuffledQuestions[randomIndex] = temp;
                    }
                }
                currentScore = 0;
                currentQuestionIndex = 0;
                _scoreRemainder = 0f;
                _awardedRawByQuestion.Clear();
                RecalculateScoreWeights();
                LogScoreConfigurationIfNeeded();
                return;
            }

            // Keep shuffled list in sync for node-driven flows that append questions at runtime.
            bool changed = false;
            if (questions != null)
            {
                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    if (q == null) continue;
                    if (!shuffledQuestions.Contains(q))
                    {
                        shuffledQuestions.Add(q);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                RecalculateScoreWeights();
            }

            LogScoreConfigurationIfNeeded();
        }

        /// <summary>
        /// Force (re)build and shuffle the question order. Call from ShuffleQuestionsNode so node-based flow can randomize order.
        /// </summary>
        public void ShuffleQuestionsNow()
        {
            shuffledQuestions = null;
            shuffleQuestions = true;
            EnsureQuizStarted();
        }

        /// <summary>
        /// Show the question at the given index immediately. Used by LoadQuestionNode so the correct
        /// question is shown when the node runs (avoids wrong order when multiple nodes run in parallel).
        /// </summary>
        public void ShowQuestionByIndex(int index)
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || index < 0 || index >= shuffledQuestions.Count) return;
            currentQuestionIndex = index;
            LoadQuestion(index);
        }

        public void NextQuestion()
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || shuffledQuestions.Count == 0)
            {
                Debug.LogWarning("NextQuestion: no questions loaded.");
                return;
            }
            if (currentQuestionIndex < shuffledQuestions.Count - 1)
            {
                currentQuestionIndex++;
                LoadQuestion(currentQuestionIndex);
            }
            else
            {
                EndQuiz();
            }
        }

        public void PreviousQuestion()
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || shuffledQuestions.Count == 0) return;
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                LoadQuestion(currentQuestionIndex);
            }
        }

        private void LoadQuestion(int index)
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || index < 0 || index >= shuffledQuestions.Count)
            {
                Debug.LogError($"Invalid question index: {index}");
                return;
            }

            QuestionData question = shuffledQuestions[index];
            if (question == null)
            {
                Debug.LogError($"Question at index {index} is null!");
                return;
            }

            if (enableTransitions && currentQuestionUI != null)
            {
                // Animate transition out, then load new question
                TransitionOut(() => LoadNewQuestion(index));
            }
            else
            {
                // No transition, remove current question (and its wrapper) then load next
                if (currentQuestionWrapper != null)
                {
                    Destroy(currentQuestionWrapper.gameObject);
                    currentQuestionWrapper = null;
                }
                else if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
                currentQuestionUI = null;
                LoadNewQuestion(index);
            }
        }

        private void TransitionOut(System.Action onComplete)
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            Transform target = currentQuestionWrapper != null ? currentQuestionWrapper : questionContainer;
            if (target == null)
            {
                if (currentQuestionUI != null) Destroy(currentQuestionUI.gameObject);
                onComplete?.Invoke();
                return;
            }

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cgOut = target.GetComponent<CanvasGroup>();
                    if (cgOut == null) cgOut = target.gameObject.AddComponent<CanvasGroup>();
                    currentTransitionSequence.Append(cgOut.DOFade(0f, transitionDuration));
                    break;

                case TransitionStyle.Slide:
                    RectTransform rtOut = target as RectTransform;
                    if (rtOut == null) rtOut = target.GetComponent<RectTransform>();
                    if (rtOut != null)
                        currentTransitionSequence.Append(rtOut.DOAnchorPosX(rtOut.anchoredPosition.x - 1000f, transitionDuration));
                    break;

                case TransitionStyle.Scale:
                    currentTransitionSequence.Append(target.DOScale(0f, transitionDuration));
                    break;
            }

            currentTransitionSequence.OnComplete(() =>
            {
                if (currentQuestionWrapper != null)
                {
                    Destroy(currentQuestionWrapper.gameObject);
                    currentQuestionWrapper = null;
                }
                else if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
                currentQuestionUI = null;
                onComplete?.Invoke();
            });
        }

        private void LoadNewQuestion(int index)
        {
            QuestionData question = shuffledQuestions[index];

            // Create validator
            currentValidator = ValidatorFactory.CreateValidator(question);
            if (currentValidator == null)
            {
                Debug.LogError($"Failed to create validator for question type: {question.questionType}");
                return;
            }

            // Create appropriate UI (node override > question custom prefab > type default)
            GameObject uiPrefab = null;
            if (_runtimeUIPrefabOverrides != null && _runtimeUIPrefabOverrides.TryGetValue(index, out var overr))
            {
                _runtimeUIPrefabOverrides.Remove(index);
                uiPrefab = overr;
            }
            if (uiPrefab == null)
                uiPrefab = GetUIPrefabForQuestion(question);
            if (uiPrefab == null)
            {
                string prefabFieldName = GetUIPrefabFieldName(question.questionType);
                Debug.LogError($"No UI prefab found for question type: {question.questionType}. " +
                    $"Please assign the {prefabFieldName} field in the QuizManager component in the scene. " +
                    $"Expected prefab: Prefabs/Connect_Q/ConnectChoiceUI.prefab");
                return;
            }

            // Create a wrapper under questionContainer so we can disable or animate just this question
            currentQuestionWrapper = CreateQuestionWrapper();
            GameObject uiInstance = Instantiate(uiPrefab, currentQuestionWrapper);
            currentQuestionUI = uiInstance.GetComponent<QuestionUI>();
            if (currentQuestionUI == null)
            {
                Debug.LogError($"UI prefab doesn't have QuestionUI component!");
                Destroy(currentQuestionWrapper.gameObject);
                currentQuestionWrapper = null;
                return;
            }

            if (enableTransitions)
            {
                ResetWrapperForTransitionIn(currentQuestionWrapper);
            }

            // Note: Animations should be set in QuizState BEFORE Initialize() is called
            // This is done by LoadQuestionNode or other nodes that control question loading
            // The QuestionUI will check QuizState.Instance.currentAnswerAnimations in SetupQuestion()
            
            currentQuestionUI.Initialize(question, currentValidator, this);

            // Animate transition in
            if (enableTransitions)
            {
                TransitionIn();
            }
        }

        private Transform CreateQuestionWrapper()
        {
            if (questionContainer == null) return null;
            var wrapperGo = new GameObject("QuestionWrapper");
            wrapperGo.transform.SetParent(questionContainer, false);
            var rt = questionContainer as RectTransform;
            if (rt != null)
            {
                var wrapperRt = wrapperGo.AddComponent<RectTransform>();
                wrapperRt.anchorMin = Vector2.zero;
                wrapperRt.anchorMax = Vector2.one;
                wrapperRt.sizeDelta = Vector2.zero;
                wrapperRt.anchoredPosition = Vector2.zero;
            }
            if (transitionStyle == TransitionStyle.Fade)
            {
                var cg = wrapperGo.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
            return wrapperGo.transform;
        }

        private void ResetWrapperForTransitionIn(Transform wrapper)
        {
            if (wrapper == null) return;
            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cg = wrapper.GetComponent<CanvasGroup>();
                    if (cg == null) cg = wrapper.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    break;

                case TransitionStyle.Slide:
                    RectTransform rt = wrapper as RectTransform ?? wrapper.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 pos = rt.anchoredPosition;
                        rt.anchoredPosition = new Vector2(pos.x + 1000f, pos.y);
                    }
                    break;

                case TransitionStyle.Scale:
                    wrapper.localScale = Vector3.zero;
                    break;
            }
        }

        private void TransitionIn()
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            Transform target = currentQuestionWrapper != null ? currentQuestionWrapper : questionContainer;
            if (target == null) return;

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cgIn = target.GetComponent<CanvasGroup>();
                    if (cgIn != null)
                        currentTransitionSequence.Append(cgIn.DOFade(1f, transitionDuration));
                    break;

                case TransitionStyle.Slide:
                    RectTransform rtIn = target as RectTransform ?? target.GetComponent<RectTransform>();
                    if (rtIn != null)
                    {
                        Vector2 targetPos = rtIn.anchoredPosition;
                        targetPos.x -= 1000f;
                        currentTransitionSequence.Append(rtIn.DOAnchorPos(targetPos, transitionDuration));
                    }
                    break;

                case TransitionStyle.Scale:
                    currentTransitionSequence.Append(target.DOScale(1f, transitionDuration));
                    break;
            }

            currentTransitionSequence.SetEase(Ease.OutQuad);
        }

        private GameObject GetUIPrefabForQuestion(QuestionData question)
        {
            if (question != null && question.customUIPrefab != null)
                return question.customUIPrefab;
            return GetUIPrefabForQuestionType(question != null ? question.questionType : default);
        }

        private GameObject GetUIPrefabForQuestionType(QuestionType type)
        {
            GameObject prefab = null;
            
            switch (type)
            {
                case QuestionType.TrueFalse:
                    prefab = trueFalseUIPrefab;
                    break;
                case QuestionType.FillInTheBlank:
                    prefab = fillInTheBlankUIPrefab;
                    break;
                case QuestionType.MultiSelect:
                    prefab = multiSelectUIPrefab;
                    break;
                case QuestionType.Ordering:
                    prefab = orderingUIPrefab;
                    break;
                case QuestionType.Hotspot:
                    prefab = hotspotUIPrefab;
                    break;
                case QuestionType.Slider:
                    prefab = sliderUIPrefab;
                    break;
                case QuestionType.Audio:
                    prefab = audioUIPrefab;
                    break;
                case QuestionType.MultipleChoice:
                    prefab = multipleChoiceUIPrefab;
                    break;
                case QuestionType.DragDrop:
                    prefab = dragDropUIPrefab;
                    break;
                case QuestionType.Connect:
                    prefab = connectUIPrefab;
                    break;
                default:
                    return null;
            }

            // If prefab is null, try loading from Resources as fallback
            if (prefab == null)
            {
                prefab = TryLoadUIPrefabFromResources(type);
            }

            return prefab;
        }

        /// <summary>
        /// Try to load UI prefab from Resources folder as fallback
        /// </summary>
        private GameObject TryLoadUIPrefabFromResources(QuestionType type)
        {
            string[] resourcePaths = {
                $"UI/{type}UI",
                $"Prefabs/{type}UI",
                $"UI/{type}",
                $"{type}UI",
                $"ConnectChoiceUI", // Specific fallback for Connect type
            };

            foreach (var path in resourcePaths)
            {
                var loaded = Resources.Load<GameObject>(path);
                if (loaded != null)
                {
                    Debug.Log($"[QuizManager] Loaded UI prefab from Resources: {path} for {type}");
                    return loaded;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the field name for a question type's UI prefab (for error messages)
        /// </summary>
        private string GetUIPrefabFieldName(QuestionType type)
        {
            switch (type)
            {
                case QuestionType.TrueFalse: return "trueFalseUIPrefab";
                case QuestionType.FillInTheBlank: return "fillInTheBlankUIPrefab";
                case QuestionType.MultiSelect: return "multiSelectUIPrefab";
                case QuestionType.Ordering: return "orderingUIPrefab";
                case QuestionType.Hotspot: return "hotspotUIPrefab";
                case QuestionType.Slider: return "sliderUIPrefab";
                case QuestionType.Audio: return "audioUIPrefab";
                case QuestionType.MultipleChoice: return "multipleChoiceUIPrefab";
                case QuestionType.DragDrop: return "dragDropUIPrefab";
                case QuestionType.Connect: return "connectUIPrefab";
                default: return "UI prefab";
            }
        }

        public void OnQuestionAnswered(bool isCorrect, int points)
        {
            OnQuestionAnswered(isCorrect, points, null);
        }

        public void OnQuestionAnswered(bool isCorrect, int points, QuestionData answeredQuestion)
        {
            int questionIndex = ResolveQuestionIndex(answeredQuestion);
            ScoreAwardResult award = AwardRawPointsForQuestion(questionIndex, points, isCorrect);
            if (award.distributedDelta > 0)
            {
                Debug.Log($"[QuizManager] Finalized question {questionIndex}: +{award.distributedDelta} distributed points (raw total: {points}). Total: {currentScore}");
            }
             
            // Notify QuizState to fire the OnLastAnswerResult event
            // This allows LoadQuestionNode to detect when an answer is submitted
            if (QuizState.Instance != null)
            {
                QuizState.Instance.RecordQuestionScoreFinal(
                    questionIndex: questionIndex,
                    wasCorrectFinalResult: isCorrect,
                    questionRawMax: award.questionRawMax,
                    rawTargetAfterEvent: award.rawTarget,
                    rawDeltaThisEvent: award.rawDelta,
                    distributedDeltaThisEvent: award.distributedDelta);

                // Score was already applied live via AddScore in AwardRawPointsForQuestion.
                // RecordAnswer here should only advance quiz progression and emit answer-result events.
                QuizState.Instance.RecordAnswer(questionIndex, isCorrect, 0);
            }
        }

        /// <summary>
        /// Live progress scoring for multi-step questions (e.g., Connect).
        /// Updates score without marking the question as answered.
        /// </summary>
        public void UpdateCurrentQuestionProgress(int completedUnits, int totalUnits)
        {
            UpdateQuestionProgress(null, completedUnits, totalUnits);
        }

        public void UpdateQuestionProgress(QuestionData question, int completedUnits, int totalUnits)
        {
            if (totalUnits <= 0) return;

            int questionIndex = ResolveQuestionIndex(question);
            int questionRawMax = GetQuestionRawWeight(questionIndex);
            if (questionRawMax <= 0) return;

            float normalized = Mathf.Clamp01((float)completedUnits / totalUnits);
            int rawTarget = Mathf.RoundToInt(questionRawMax * normalized);
            ScoreAwardResult award = AwardRawPointsForQuestion(questionIndex, rawTarget, false);
            int deltaDistributed = award.distributedDelta;

            if (QuizState.Instance != null)
            {
                QuizState.Instance.RecordScoreProgress(
                    questionIndex: questionIndex,
                    questionRawMax: award.questionRawMax,
                    rawTargetAfterEvent: award.rawTarget,
                    rawDeltaThisEvent: award.rawDelta,
                    distributedDeltaThisEvent: award.distributedDelta,
                    completedUnits: completedUnits,
                    totalUnits: totalUnits);
            }

            if (deltaDistributed > 0)
            {
                Debug.Log($"[QuizManager] Live score update on Q{questionIndex}: +{deltaDistributed} (progress {completedUnits}/{totalUnits})");
            }
        }

        private int ResolveQuestionIndex(QuestionData question)
        {
            EnsureQuizStarted();

            if (question != null && shuffledQuestions != null)
            {
                int idx = shuffledQuestions.IndexOf(question);
                if (idx >= 0)
                {
                    return idx;
                }
            }

            return Mathf.Clamp(currentQuestionIndex, 0, Mathf.Max(0, (shuffledQuestions?.Count ?? 1) - 1));
        }

        private void RecalculateScoreWeights()
        {
            _totalQuestionWeight = 0;
            if (shuffledQuestions == null) return;

            int limit = GetScoreWeightQuestionLimit();
            int count = shuffledQuestions.Count;
            if (limit > 0)
            {
                count = Mathf.Min(count, limit);
            }

            for (int i = 0; i < count; i++)
            {
                var question = shuffledQuestions[i];
                if (question == null) continue;
                _totalQuestionWeight += Mathf.Max(0, question.points);
            }
        }

        private int GetScoreWeightQuestionLimit()
        {
            var state = QuizState.Instance;
            if (state == null) return 0;
            return state.totalQuestions > 0 ? state.totalQuestions : 0;
        }

        private int GetDistributedPointsForRawDelta(int rawPointsDelta)
        {
            int targetMaxScore = GetTargetMaxScore();
            if (targetMaxScore <= 0)
                return Mathf.Max(0, rawPointsDelta);

            if (rawPointsDelta <= 0)
                return 0;

            if (_totalQuestionWeight <= 0)
            {
                int questionCount = shuffledQuestions != null ? shuffledQuestions.Count : 0;
                if (questionCount <= 0)
                    return rawPointsDelta;

                float equalShare = ((float)targetMaxScore / questionCount) + _scoreRemainder;
                int equalPoints = Mathf.FloorToInt(equalShare);
                _scoreRemainder = equalShare - equalPoints;
                return Mathf.Max(0, equalPoints);
            }

            float weightedShare = ((float)targetMaxScore * rawPointsDelta / _totalQuestionWeight) + _scoreRemainder;
            int awardedPoints = Mathf.FloorToInt(weightedShare);
            _scoreRemainder = weightedShare - awardedPoints;
            return Mathf.Max(0, awardedPoints);
        }

        private ScoreAwardResult AwardRawPointsForQuestion(int questionIndex, int requestedRawPoints, bool forceFullOnCorrect)
        {
            ScoreAwardResult result = new ScoreAwardResult
            {
                questionRawMax = 0,
                rawTarget = 0,
                rawDelta = 0,
                distributedDelta = 0,
                totalScoreAfterAward = currentScore
            };

            int questionRawMax = GetQuestionRawWeight(questionIndex);
            result.questionRawMax = questionRawMax;
            if (questionRawMax <= 0) return result;

            int rawTarget = Mathf.Clamp(Mathf.Max(0, requestedRawPoints), 0, questionRawMax);
            if (forceFullOnCorrect && rawTarget <= 0)
            {
                rawTarget = questionRawMax;
            }
            result.rawTarget = rawTarget;

            int alreadyAwardedRaw = 0;
            _awardedRawByQuestion.TryGetValue(questionIndex, out alreadyAwardedRaw);
            if (rawTarget <= alreadyAwardedRaw)
            {
                return result;
            }

            int rawDelta = rawTarget - alreadyAwardedRaw;
            int distributedDelta = Mathf.Max(0, rawDelta);
            int targetMaxScore = GetTargetMaxScore();
            if (targetMaxScore > 0)
            {
                int remaining = Mathf.Max(0, targetMaxScore - currentScore);
                distributedDelta = Mathf.Min(distributedDelta, remaining);
            }

            _awardedRawByQuestion[questionIndex] = rawTarget;
            if (distributedDelta > 0)
            {
                currentScore += distributedDelta;
                QuizState.Instance?.AddScore(distributedDelta);
            }

            result.rawDelta = rawDelta;
            result.distributedDelta = distributedDelta;
            result.totalScoreAfterAward = currentScore;
            return result;
        }

        private int GetQuestionRawWeight(int questionIndex)
        {
            if (shuffledQuestions == null || questionIndex < 0 || questionIndex >= shuffledQuestions.Count)
            {
                return 0;
            }

            var question = shuffledQuestions[questionIndex];
            if (question == null) return 0;
            return Mathf.Max(0, question.points);
        }

        private int GetTargetMaxScore()
        {
            var state = QuizState.Instance;
            if (state != null && state.maxPossibleScore > 0)
                return state.maxPossibleScore;

            return shuffledQuestions != null ? shuffledQuestions.Count * 10 : 0;
        }

        private void LogScoreConfigurationIfNeeded()
        {
            var state = QuizState.Instance;
            if (state == null || shuffledQuestions == null) return;

            int limit = GetScoreWeightQuestionLimit();
            int count = shuffledQuestions.Count;
            if (limit > 0)
            {
                count = Mathf.Min(count, limit);
            }

            int totalPoints = 0;
            for (int i = 0; i < count; i++)
            {
                var q = shuffledQuestions[i];
                if (q == null) continue;
                totalPoints += Mathf.Max(0, q.points);
            }

            int max = state.maxPossibleScore;
            int signature = (count * 486187739) ^ (totalPoints * 16777619) ^ max;
            if (signature == _lastScoreConfigSignature) return;
            _lastScoreConfigSignature = signature;

            if (max > 0 && totalPoints > 0 && totalPoints != max)
            {
                Debug.LogWarning($"[QuizManager] Score config mismatch: loaded question points sum = {totalPoints}, StartQuiz maxScore = {max}, questions considered = {count}. Progress may not match expected percentages.");
            }
        }

        private void EndQuiz()
        {
            Debug.Log($"Quiz Complete! Final Score: {currentScore}");
            // You can add UI for quiz completion here
        }

        public void ResetQuiz()
        {
            if (currentQuestionUI != null)
            {
                Destroy(currentQuestionUI.gameObject);
            }
            currentQuestionUI = null;
            currentValidator = null;
            currentScore = 0;
            currentQuestionIndex = 0;
            _scoreRemainder = 0f;
            _awardedRawByQuestion.Clear();
        }
    }
}
