using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Drives a UI Slider, Image (fill amount), or Slots (LED indicators) from a score value.
    /// Value and min/max can be literals or graph variables for easy setup.
    /// Progress animates (lerp) by default instead of snapping.
    /// Safe to trigger multiple times: connect from Start Quiz, after each question type,
    /// or from any node that should refresh the bar.
    /// </summary>
    [Serializable]
    public class ScoreProgressBarNode : NodeData
    {
        public enum ValueSource
        {
            QuizScore,
            Variable
        }

        public enum DisplayMode
        {
            Slider,
            FilledImage,
            Slots
        }

        public enum FilledImageAnswerFilter
        {
            CorrectOnly,
            WrongOnly,
            AnyAnswer
        }

        public enum FilledImageAnswerSource
        {
            FinalAnswers,
            PartialAnswers
        }

        [Header("Display Mode")]
        [SerializeField]
        public DisplayMode displayMode = DisplayMode.Slider;

        [Header("Target")]
        [SerializeField]
        [Tooltip("Assign by drag-and-drop from Hierarchy/Inspector. Slider, Image, or GameObject.")]
        public UnityEngine.Object targetRef;

        [SerializeField]
        [Tooltip("Fallback path when reference is null (e.g. in builds). Set automatically when you assign Target.")]
        public string targetPath = "";

        [Header("Value (current)")]
        [SerializeField]
        public ValueSource valueSource = ValueSource.QuizScore;

        [SerializeField]
        [Tooltip("Variable name when Value Source = Variable. Int or Float.")]
        public string valueVariableName = "";

        [Header("Range (min / max)")]
        [SerializeField]
        [Tooltip("When Value from = Quiz Score: use 0 and QuizState.maxPossibleScore (from Start Quiz). Otherwise use Min/Max below.")]
        public bool useQuizRange = true;

        [SerializeField]
        [Tooltip("Min value. Ignored if Use Quiz Range or Min Variable is set.")]
        public float minLiteral = 0f;

        [SerializeField]
        [Tooltip("Variable name for min. If set, overrides Min literal. Int or Float.")]
        public string minVariableName = "";

        [SerializeField]
        [Tooltip("Max value. Ignored if Use Quiz Range or Max Variable is set.")]
        public float maxLiteral = 100f;

        [SerializeField]
        [Tooltip("Variable name for max. If set, overrides Max literal. Int or Float.")]
        public string maxVariableName = "";

        [Header("Animation")]
        [SerializeField]
        [Tooltip("Smoothly animate the bar to the new value instead of snapping.")]
        public bool animateFill = true;

        [SerializeField]
        [Tooltip("Duration of the lerp animation in seconds.")]
        [Range(0.05f, 2f)]
        public float animationDuration = 0.3f;

        [SerializeField]
        [Tooltip("When Value from = Quiz Score, keep listening to score changes and update automatically.")]
        public bool liveUpdateFromScoreEvents = true;

        [Header("Filled Image Answer Mode")]
        [SerializeField]
        [Tooltip("FilledImage only: fill by answer timeline results.")]
        public bool filledImageUseFinalAnswers = false;

        [SerializeField]
        [Tooltip("Choose finalized answers, or partial step results. In Partial mode, wrong attempts are ignored and only finalized step results are counted (correct or auto-corrected).")]
        public FilledImageAnswerSource filledImageAnswerSource = FilledImageAnswerSource.FinalAnswers;

        [SerializeField]
        public FilledImageAnswerFilter filledImageAnswerFilter = FilledImageAnswerFilter.CorrectOnly;

        [SerializeField]
        [Tooltip("Use QuizState.totalQuestions as the max count.")]
        public bool filledImageUseTotalQuestionsAsMax = true;

        [SerializeField]
        [Tooltip("Used when 'Use total questions as max' is disabled.")]
        public int filledImageMaxCount = 10;

        [Header("Slots Settings")]
        [SerializeField]
        public Color slotDefaultColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField]
        public Color slotCorrectColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        [SerializeField]
        public Color slotWrongColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        [SerializeField]
        public Sprite slotDefaultSprite;

        [SerializeField]
        public Sprite slotCorrectSprite;

        [SerializeField]
        public Sprite slotWrongSprite;

        [SerializeField]
        public bool slotAnimateOnFill = true;

        [SerializeField]
        [Range(0.05f, 2f)]
        public float slotAnimationDuration = 0.2f;

        [SerializeField]
        [Tooltip("If enabled, each wrong attempt fills a wrong slot immediately. Disable for per-point results when retries are allowed.")]
        public bool slotCountWrongAttempts = false;

        [SerializeField]
        [Tooltip("Show attempt count as text on each slot. Requires a Text or TextMeshProUGUI child on each slot Image.")]
        public bool slotShowAttemptCount = false;

        [SerializeField]
        [Tooltip("Color for slots answered correctly but after multiple attempts.")]
        public Color slotMultiAttemptColor = new Color(0.9f, 0.75f, 0.2f, 1f);

        [SerializeField]
        [Tooltip("Number of total attempts at which the multi-attempt color is used instead of correct color.")]
        [Range(2, 10)]
        public int slotMultiAttemptThreshold = 2;

        [NonSerialized]
        private bool _isScoreEventSubscribed;

        [NonSerialized]
        private bool _isAnswerEventSubscribed;

        [NonSerialized]
        private bool _isQuestionResultEventSubscribed;

        [NonSerialized]
        private bool _isWrongAttemptEventSubscribed;

        [NonSerialized]
        private bool _isFilledImageAnswerEventSubscribed;

        [NonSerialized]
        private bool _isFilledImagePartialAnswerEventSubscribed;

        [NonSerialized]
        private Coroutine _activeAnimation;

        [NonSerialized]
        private Image[] _cachedSlotImages;

        [NonSerialized]
        private int _slotFillIndex;

        [NonSerialized]
        private bool _slotsInitialized;

        [NonSerialized]
        private bool _sawStepResultSinceLastQuestionResult;

        [NonSerialized]
        private bool _sawWrongAttemptSinceLastStepResult;

        [NonSerialized]
        private int _lastWrongAttemptFrame = -9999;

        [NonSerialized]
        private int _currentPointWrongAttempts;

        [NonSerialized]
        private Component[] _cachedSlotTexts;

        public override string Name => "Score Progress Bar";
        public override Color Color => new Color(0.85f, 0.65f, 0.25f); // Amber/Gold
        public override string Category => "Quiz";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner?.Graph == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No graph runner.");
                Complete();
                return;
            }

            if (displayMode == DisplayMode.Slots)
            {
                // Only initialize slots on the FIRST execution — subsequent executions
                // (e.g. if the node is re-triggered per question) must NOT reset the fill index.
                if (!_slotsInitialized)
                {
                    ResolveSlotImages();
                    InitializeSlotsToDefault();
                    _slotsInitialized = true;
                }
                SubscribeToAnswerEventsIfNeeded();
                if (slotCountWrongAttempts || slotShowAttemptCount)
                    SubscribeToWrongAttemptEventsIfNeeded();
                SubscribeToQuestionResultEventsIfNeeded();

                // Catch up with any answers that were recorded BEFORE we subscribed.
                // This happens when the node is connected to correct/incorrect ports,
                // which fire AFTER QuizState events have already been emitted.
                CatchUpMissedSlots();

                Complete();
                return;
            }

            if (displayMode == DisplayMode.FilledImage && filledImageUseFinalAnswers)
            {
                // Defensive: if this node was previously running in score mode,
                // make sure score events cannot continue driving the same target.
                if (_isScoreEventSubscribed)
                {
                    QuizState.OnScoreChanged -= OnQuizScoreChanged;
                    _isScoreEventSubscribed = false;
                }

                if (filledImageAnswerSource == FilledImageAnswerSource.PartialAnswers)
                {
                    // Partial mode uses step-result timeline plus finalized non-step answers,
                    // so listen to both streams.
                    SubscribeToFilledImagePartialAnswerEventsIfNeeded();
                    SubscribeToFilledImageAnswerEventsIfNeeded();
                }
                else
                    SubscribeToFilledImageAnswerEventsIfNeeded();
                ApplyFilledImageByFinalAnswers(onComplete: Complete);
                return;
            }

            SubscribeToScoreEventsIfNeeded();
            ApplyCurrentValueToTarget(onComplete: Complete);
        }

        private IEnumerator AnimateToFill(float fromValue, float toValue, float duration, Action<float> onUpdate, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                float current = Mathf.Lerp(fromValue, toValue, t);
                onUpdate(current);
                yield return null;
            }
            onUpdate(toValue);
            onComplete?.Invoke();
        }

        private void ApplyCurrentValueToTarget(Action onComplete = null)
        {
            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target: assign one by drag-and-drop or set target path.");
                onComplete?.Invoke();
                return;
            }

            float value = GetValue();
            float min = GetMin();
            float max = GetMax();

            float fill = 0f;
            float range = max - min;
            if (Mathf.Abs(range) > 0.0001f)
                fill = Mathf.Clamp01((value - min) / range);
            else
                fill = value >= max ? 1f : 0f;

            var slider = targetGo.GetComponent<Slider>();
            if (slider != null)
            {
                ApplyToSlider(slider, fill, onComplete);
                return;
            }

            var image = targetGo.GetComponent<Image>();
            if (image != null && image.type == Image.Type.Filled)
            {
                ApplyToImage(image, fill, onComplete);
                return;
            }

            Debug.LogWarning($"[ScoreProgressBarNode] No Slider or filled Image on: {targetGo.name}");
            onComplete?.Invoke();
        }

        private void ApplyToSlider(Slider slider, float normalizedFill, Action onComplete)
        {
            if (slider == null)
            {
                onComplete?.Invoke();
                return;
            }

            float from = Mathf.Clamp01(slider.normalizedValue);
            if (animateFill && animationDuration > 0f && Runner != null)
            {
                StopActiveAnimation();
                _activeAnimation = Runner.StartCoroutine(AnimateToFill(
                    fromValue: from,
                    toValue: normalizedFill,
                    duration: animationDuration,
                    onUpdate: v => slider.normalizedValue = v,
                    onComplete: onComplete));
            }
            else
            {
                slider.normalizedValue = normalizedFill;
                onComplete?.Invoke();
            }
        }

        private void ApplyToImage(Image image, float normalizedFill, Action onComplete)
        {
            if (image == null)
            {
                onComplete?.Invoke();
                return;
            }

            float from = Mathf.Clamp01(image.fillAmount);
            if (animateFill && animationDuration > 0f && Runner != null)
            {
                StopActiveAnimation();
                _activeAnimation = Runner.StartCoroutine(AnimateToFill(
                    fromValue: from,
                    toValue: normalizedFill,
                    duration: animationDuration,
                    onUpdate: v => image.fillAmount = v,
                    onComplete: onComplete));
            }
            else
            {
                image.fillAmount = normalizedFill;
                onComplete?.Invoke();
            }
        }

        // === Slots Logic ===

        private void ResolveSlotImages()
        {
            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target for slots: assign a parent GameObject.");
                _cachedSlotImages = Array.Empty<Image>();
                _cachedSlotTexts = Array.Empty<Component>();
                return;
            }

            var imageList = new List<Image>();
            var textList = new List<Component>();
            for (int i = 0; i < targetGo.transform.childCount; i++)
            {
                var child = targetGo.transform.GetChild(i);
                var img = child.GetComponent<Image>();
                if (img != null)
                {
                    imageList.Add(img);
                    // Find text child: prefer TMPro, fall back to legacy Text
                    Component txt = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt == null)
                        txt = child.GetComponentInChildren<Text>();
                    textList.Add(txt);
                }
            }
            _cachedSlotImages = imageList.ToArray();
            _cachedSlotTexts = textList.ToArray();

            if (_cachedSlotImages.Length == 0)
                Debug.LogWarning($"[ScoreProgressBarNode] No child Images found under: {targetGo.name}");
            else
                Debug.Log($"[ScoreProgressBarNode] Resolved {_cachedSlotImages.Length} slot images under: {targetGo.name}");
        }

        private void InitializeSlotsToDefault()
        {
            if (_cachedSlotImages == null) return;
            _slotFillIndex = 0;
            _sawStepResultSinceLastQuestionResult = false;
            _sawWrongAttemptSinceLastStepResult = false;
            _lastWrongAttemptFrame = -9999;
            _currentPointWrongAttempts = 0;

            foreach (var img in _cachedSlotImages)
            {
                if (img == null) continue;
                img.color = slotDefaultColor;
                if (slotDefaultSprite != null)
                    img.sprite = slotDefaultSprite;
                img.transform.localScale = Vector3.one;
            }

            if (_cachedSlotTexts != null)
            {
                for (int i = 0; i < _cachedSlotTexts.Length; i++)
                    SetSlotText(i, "");
            }
        }

        /// <summary>
        /// Fills slots for any answers that were already recorded in QuizState
        /// before this node subscribed to events. Handles both step-based
        /// questions (PartialAnswerTimeline) and simple questions (AnswerTimeline).
        /// </summary>
        private void CatchUpMissedSlots()
        {
            var state = QuizState.Instance;
            if (state == null || _cachedSlotImages == null || _cachedSlotImages.Length == 0) return;

            // Check partial timeline for step results (multi-step questions)
            var partial = state.PartialAnswerTimeline;
            int stepResultCount = 0;
            if (partial != null)
            {
                for (int i = 0; i < partial.Count; i++)
                {
                    if (partial[i].eventType == QuizState.PartialAnswerEventType.StepResult)
                        stepResultCount++;
                }
            }

            if (stepResultCount > _slotFillIndex)
            {
                int step = 0;
                for (int i = 0; i < partial.Count && _slotFillIndex < _cachedSlotImages.Length; i++)
                {
                    if (partial[i].eventType != QuizState.PartialAnswerEventType.StepResult)
                        continue;
                    if (step >= _slotFillIndex)
                        FillNextSlot(partial[i].wasCorrect, "catchup_step");
                    step++;
                }
                return;
            }

            // Use final answer timeline for simple question types
            var answers = state.AnswerTimeline;
            if (answers == null) return;

            while (_slotFillIndex < answers.Count && _slotFillIndex < _cachedSlotImages.Length)
            {
                FillNextSlot(answers[_slotFillIndex].wasCorrect, "catchup");
            }
        }

        private void SubscribeToAnswerEventsIfNeeded()
        {
            if (_isAnswerEventSubscribed) return;
            QuizState.OnStepResult += OnStepResultReceived;
            _isAnswerEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnStepResult");
        }

        private void SubscribeToQuestionResultEventsIfNeeded()
        {
            if (_isQuestionResultEventSubscribed) return;
            QuizState.OnLastAnswerResult += OnQuestionResultReceived;
            _isQuestionResultEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnLastAnswerResult");
        }

        private void SubscribeToFilledImageAnswerEventsIfNeeded()
        {
            if (_isFilledImageAnswerEventSubscribed) return;
            QuizState.OnLastAnswerResult += OnFilledImageFinalAnswerReceived;
            _isFilledImageAnswerEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnLastAnswerResult (FilledImage final-answer mode)");
        }

        private void SubscribeToFilledImagePartialAnswerEventsIfNeeded()
        {
            if (_isFilledImagePartialAnswerEventSubscribed) return;
            QuizState.OnPartialAnswerRecorded += OnFilledImagePartialAnswerReceived;
            _isFilledImagePartialAnswerEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnPartialAnswerRecorded (FilledImage partial-answer mode)");
        }

        private void SubscribeToWrongAttemptEventsIfNeeded()
        {
            if (_isWrongAttemptEventSubscribed) return;
            QuizState.OnWrongAttempt += OnWrongAttemptReceived;
            _isWrongAttemptEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnWrongAttempt");
        }

        private void OnStepResultReceived(bool wasCorrect)
        {
            if (displayMode != DisplayMode.Slots) return;
            _sawStepResultSinceLastQuestionResult = true;

            // Wrong-attempt events are emitted before auto-corrected step results in sequential
            // question types. Skip this immediate false step record to avoid double-filling.
            bool skipDuplicate = (slotCountWrongAttempts || slotShowAttemptCount) && !wasCorrect
                && _sawWrongAttemptSinceLastStepResult && Time.frameCount <= _lastWrongAttemptFrame + 1;
            if (skipDuplicate)
            {
                _sawWrongAttemptSinceLastStepResult = false;
                return;
            }

            _sawWrongAttemptSinceLastStepResult = false;

            if (slotShowAttemptCount)
            {
                int totalAttempts = _currentPointWrongAttempts + 1;
                FillNextSlotWithAttempts(wasCorrect, totalAttempts, "step");
                _currentPointWrongAttempts = 0;
            }
            else
            {
                FillNextSlot(wasCorrect, "step");
            }
        }

        private void OnWrongAttemptReceived()
        {
            if (displayMode != DisplayMode.Slots) return;

            if (slotShowAttemptCount)
            {
                // In attempt-count mode: update the current slot text, don't fill/advance
                _currentPointWrongAttempts++;
                _sawWrongAttemptSinceLastStepResult = true;
                _lastWrongAttemptFrame = Time.frameCount;
                if (Runner != null && Runner.IsRunning)
                    SetSlotText(_slotFillIndex, _currentPointWrongAttempts.ToString());
                return;
            }

            if (!slotCountWrongAttempts) return;
            _sawWrongAttemptSinceLastStepResult = true;
            _lastWrongAttemptFrame = Time.frameCount;
            FillNextSlot(false, "wrong_attempt");
        }

        private void OnQuestionResultReceived(bool wasCorrect)
        {
            if (displayMode != DisplayMode.Slots) return;
            // Step-based questions already emit per-step slot events.
            // Skip the final question result in that case to avoid double-filling.
            if (_sawStepResultSinceLastQuestionResult)
            {
                _sawStepResultSinceLastQuestionResult = false;
                _sawWrongAttemptSinceLastStepResult = false;
                return;
            }

            if (slotShowAttemptCount)
            {
                int totalAttempts = _currentPointWrongAttempts + 1;
                FillNextSlotWithAttempts(wasCorrect, totalAttempts, "question");
                _currentPointWrongAttempts = 0;
            }
            else
            {
                FillNextSlot(wasCorrect, "question");
            }
            _sawStepResultSinceLastQuestionResult = false;
            _sawWrongAttemptSinceLastStepResult = false;
        }

        private void OnFilledImageFinalAnswerReceived(bool _)
        {
            if (displayMode != DisplayMode.FilledImage || !filledImageUseFinalAnswers) return;
            if (Runner == null || !Runner.IsRunning) return;
            ApplyFilledImageByFinalAnswers();
        }

        private void OnFilledImagePartialAnswerReceived(QuizState.PartialAnswerTimelineEntry _)
        {
            if (displayMode != DisplayMode.FilledImage || !filledImageUseFinalAnswers) return;
            if (filledImageAnswerSource != FilledImageAnswerSource.PartialAnswers) return;
            if (_.eventType != QuizState.PartialAnswerEventType.StepResult) return;
            if (Runner == null || !Runner.IsRunning) return;
            ApplyFilledImageByFinalAnswers();
        }

        private void ApplyFilledImageByFinalAnswers(Action onComplete = null)
        {
            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target: assign one by drag-and-drop or set target path.");
                onComplete?.Invoke();
                return;
            }

            var image = targetGo.GetComponent<Image>();
            if (image == null || image.type != Image.Type.Filled)
            {
                Debug.LogWarning($"[ScoreProgressBarNode] FilledImage final-answer mode requires a filled Image on: {targetGo.name}");
                onComplete?.Invoke();
                return;
            }

            float fill = GetFilledImageAnswerFill();
            ApplyToImage(image, fill, onComplete);
        }

        private float GetFilledImageAnswerFill()
        {
            var state = QuizState.Instance;
            if (state == null)
                return 0f;

            int matchingCount = 0;
            int sourceCount = 0;

            if (filledImageAnswerSource == FilledImageAnswerSource.PartialAnswers)
            {
                var partialAnswers = state.PartialAnswerTimeline;
                var questionsWithStepResults = new HashSet<int>();
                if (partialAnswers != null)
                {
                    sourceCount = 0;
                    for (int i = 0; i < partialAnswers.Count; i++)
                    {
                        if (partialAnswers[i].eventType != QuizState.PartialAnswerEventType.StepResult)
                            continue;

                        questionsWithStepResults.Add(partialAnswers[i].questionIndex);
                        sourceCount++;
                        bool wasCorrect = partialAnswers[i].wasCorrect;
                        if (filledImageAnswerFilter == FilledImageAnswerFilter.CorrectOnly && wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.WrongOnly && !wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.AnyAnswer)
                            matchingCount++;
                    }
                }

                // In Partial source mode, include finalized results for non-step question types
                // so quiz-wide normalization doesn't complete early before those questions finish.
                var finalAnswers = state.AnswerTimeline;
                if (finalAnswers != null)
                {
                    for (int i = 0; i < finalAnswers.Count; i++)
                    {
                        int questionIndex = finalAnswers[i].questionIndex;
                        if (questionsWithStepResults.Contains(questionIndex))
                            continue;

                        sourceCount++;
                        bool wasCorrect = finalAnswers[i].wasCorrect;
                        if (filledImageAnswerFilter == FilledImageAnswerFilter.CorrectOnly && wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.WrongOnly && !wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.AnyAnswer)
                            matchingCount++;
                    }
                }
            }
            else
            {
                var answers = state.AnswerTimeline;
                if (answers != null)
                {
                    sourceCount = answers.Count;
                    for (int i = 0; i < answers.Count; i++)
                    {
                        bool wasCorrect = answers[i].wasCorrect;
                        if (filledImageAnswerFilter == FilledImageAnswerFilter.CorrectOnly && wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.WrongOnly && !wasCorrect)
                            matchingCount++;
                        else if (filledImageAnswerFilter == FilledImageAnswerFilter.AnyAnswer)
                            matchingCount++;
                    }
                }
            }

            // AnyAnswer mode is normalized by total expected answer events across the quiz.
            if (filledImageAnswerFilter == FilledImageAnswerFilter.AnyAnswer)
            {
                int expectedTotal = GetExpectedTimelineEventCount(state);
                if (expectedTotal <= 0)
                    expectedTotal = GetAnyAnswerFallbackMaxCount(state);

                if (expectedTotal <= 0)
                    return 0f;

                return Mathf.Clamp01((float)sourceCount / expectedTotal);
            }

            int maxCount;
            if (filledImageAnswerSource == FilledImageAnswerSource.PartialAnswers)
            {
                // Partial timeline can have multiple entries per question,
                // so totalQuestions is not a meaningful denominator here.
                maxCount = Mathf.Max(0, filledImageMaxCount);
            }
            else
            {
                maxCount = filledImageUseTotalQuestionsAsMax
                    ? Mathf.Max(0, state.totalQuestions)
                    : Mathf.Max(0, filledImageMaxCount);
            }

            // Fallback for flows where totalQuestions wasn't set.
            if (maxCount <= 0)
                maxCount = sourceCount;

            if (maxCount <= 0)
                return 0f;

            return Mathf.Clamp01((float)matchingCount / maxCount);
        }

        private int GetExpectedTimelineEventCount(QuizState state)
        {
            var plannedQuestions = GatherPlannedQuizQuestions(state);
            int questionCount = plannedQuestions.Count;
            int configuredTotalQuestions = state != null ? Mathf.Max(0, state.totalQuestions) : 0;
            if (configuredTotalQuestions > 0)
                questionCount = Mathf.Min(questionCount, configuredTotalQuestions);

            int total = 0;
            for (int i = 0; i < questionCount; i++)
            {
                var question = plannedQuestions[i];
                if (question == null) continue;
                total += GetExpectedTimelineEventCountForQuestion(question);
            }

            // If the graph/runtime question list is incomplete at this moment,
            // include remaining configured questions as at least one unit each.
            // This avoids early 100% fill before later questions are reached.
            if (configuredTotalQuestions > questionCount)
            {
                total += (configuredTotalQuestions - questionCount);
            }

            return Mathf.Max(0, total);
        }

        private List<QuestionData> GatherPlannedQuizQuestions(QuizState state)
        {
            var result = new List<QuestionData>();

            // Prefer graph-level LoadQuestionNode assets: this represents the full planned quiz
            // even before each question is loaded into QuizManager.questions at runtime.
            if (Runner?.Graph != null && Runner.Graph.Nodes != null)
            {
                var nodes = Runner.Graph.Nodes;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (!(nodes[i] is LoadQuestionNode loadNode))
                        continue;

                    QuestionData question = loadNode.questionRef;
                    if (question == null)
                    {
                        question = Runner.Graph.GetNodeAssetReference(loadNode.Guid) as QuestionData;
                    }

                    if (question != null)
                        result.Add(question);
                }
            }

            // Fallback to QuizManager list if graph assets are unavailable.
            if (result.Count == 0)
            {
                QuizManager manager = state != null ? state.quizManagerRef : null;
                if (manager == null)
                    manager = UnityEngine.Object.FindObjectOfType<QuizManager>();

                if (manager != null && manager.questions != null)
                {
                    for (int i = 0; i < manager.questions.Count; i++)
                    {
                        if (manager.questions[i] != null)
                            result.Add(manager.questions[i]);
                    }
                }
            }

            return result;
        }

        private int GetAnyAnswerFallbackMaxCount(QuizState state)
        {
            if (filledImageAnswerSource == FilledImageAnswerSource.FinalAnswers)
            {
                if (filledImageUseTotalQuestionsAsMax && state != null && state.totalQuestions > 0)
                    return state.totalQuestions;

                return Mathf.Max(1, filledImageMaxCount);
            }

            // PartialAnswers fallback: prefer explicit max, otherwise totalQuestions if available.
            if (filledImageMaxCount > 0)
                return filledImageMaxCount;

            if (state != null && state.totalQuestions > 0)
                return state.totalQuestions;

            return 1;
        }

        private int GetExpectedTimelineEventCountForQuestion(QuestionData question)
        {
            if (question == null)
                return 0;

            if (filledImageAnswerSource == FilledImageAnswerSource.FinalAnswers)
                return 1;

            switch (question.questionType)
            {
                case QuestionType.Connect:
                {
                    var connect = question as ConnectQuestionData;
                    int unitCount = 0;
                    if (connect != null)
                    {
                        unitCount = connect.correctConnections != null ? connect.correctConnections.Count : 0;
                        if (unitCount <= 0)
                            unitCount = connect.leftColumnItems != null ? connect.leftColumnItems.Count : 0;
                    }
                    return Mathf.Max(0, unitCount);
                }
                case QuestionType.Ordering:
                {
                    var ordering = question as OrderingQuestionData;
                    int unitCount = ordering != null && ordering.items != null ? ordering.items.Count : 0;
                    return Mathf.Max(0, unitCount);
                }
                case QuestionType.DragDrop:
                {
                    var dragDrop = question as DragDropQuestionData;
                    return Mathf.Max(0, GetDragDropAnswerUnitCount(dragDrop));
                }
                default:
                    // Non-step question types contribute one finalized answer event.
                    return 1;
            }
        }

        private static int GetDragDropAnswerUnitCount(DragDropQuestionData dragDrop)
        {
            if (dragDrop == null)
                return 0;

            var pairings = dragDrop.correctPairings;
            if (pairings == null || pairings.Count == 0)
                return dragDrop.dragItems != null ? dragDrop.dragItems.Count : 0;

            var adjacency = new Dictionary<int, List<int>>();
            void AddEdge(int a, int b)
            {
                if (!adjacency.TryGetValue(a, out var listA))
                {
                    listA = new List<int>();
                    adjacency[a] = listA;
                }
                listA.Add(b);

                if (!adjacency.TryGetValue(b, out var listB))
                {
                    listB = new List<int>();
                    adjacency[b] = listB;
                }
                listB.Add(a);
            }

            for (int i = 0; i < pairings.Count; i++)
            {
                int dragNode = pairings[i].dragIndex;
                int dropNode = -1 - pairings[i].dropIndex;
                AddEdge(dragNode, dropNode);
            }

            int unitCount = 0;
            var visited = new HashSet<int>();
            foreach (var node in adjacency.Keys)
            {
                if (visited.Contains(node)) continue;

                bool hasDragNode = false;
                var queue = new Queue<int>();
                queue.Enqueue(node);
                visited.Add(node);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    if (current >= 0)
                        hasDragNode = true;

                    if (!adjacency.TryGetValue(current, out var neighbours)) continue;
                    for (int i = 0; i < neighbours.Count; i++)
                    {
                        int neighbour = neighbours[i];
                        if (visited.Add(neighbour))
                            queue.Enqueue(neighbour);
                    }
                }

                if (hasDragNode)
                    unitCount++;
            }

            if (unitCount <= 0 && dragDrop.dragItems != null)
                unitCount = dragDrop.dragItems.Count;
            return unitCount;
        }

        private void FillNextSlot(bool wasCorrect, string source)
        {
            Debug.Log($"[ScoreProgressBarNode] Slot event ({source}): wasCorrect={wasCorrect}, slotIndex={_slotFillIndex}, Runner={Runner != null}, IsRunning={Runner?.IsRunning}, slotCount={_cachedSlotImages?.Length ?? -1}");

            if (Runner == null || !Runner.IsRunning) return;
            if (_cachedSlotImages == null || _slotFillIndex >= _cachedSlotImages.Length) return;

            var slot = _cachedSlotImages[_slotFillIndex];
            if (slot == null) { _slotFillIndex++; return; }

            slot.color = wasCorrect ? slotCorrectColor : slotWrongColor;

            // Apply the correct sprite — use the wrong sprite even if it's the same as default
            if (wasCorrect && slotCorrectSprite != null)
                slot.sprite = slotCorrectSprite;
            else if (!wasCorrect && slotWrongSprite != null)
                slot.sprite = slotWrongSprite;

            if (slotAnimateOnFill && slotAnimationDuration > 0f && Runner != null)
                Runner.StartCoroutine(AnimateSlotPop(slot.transform, slotAnimationDuration));

            Debug.Log($"[ScoreProgressBarNode] Filled slot {_slotFillIndex} with {(wasCorrect ? "Correct" : "Wrong")} color, advancing to {_slotFillIndex + 1}");
            _slotFillIndex++;
        }

        private void FillNextSlotWithAttempts(bool wasCorrect, int totalAttempts, string source)
        {
            Debug.Log($"[ScoreProgressBarNode] Slot attempt event ({source}): wasCorrect={wasCorrect}, attempts={totalAttempts}, slotIndex={_slotFillIndex}");

            if (Runner == null || !Runner.IsRunning) return;
            if (_cachedSlotImages == null || _slotFillIndex >= _cachedSlotImages.Length) return;

            var slot = _cachedSlotImages[_slotFillIndex];
            if (slot == null) { _slotFillIndex++; return; }

            // Choose color: wrong → slotWrongColor, correct first-try → slotCorrectColor,
            // correct multi-attempt → slotMultiAttemptColor
            if (!wasCorrect)
            {
                slot.color = slotWrongColor;
                if (slotWrongSprite != null) slot.sprite = slotWrongSprite;
            }
            else if (totalAttempts >= slotMultiAttemptThreshold)
            {
                slot.color = slotMultiAttemptColor;
                if (slotCorrectSprite != null) slot.sprite = slotCorrectSprite;
            }
            else
            {
                slot.color = slotCorrectColor;
                if (slotCorrectSprite != null) slot.sprite = slotCorrectSprite;
            }

            SetSlotText(_slotFillIndex, totalAttempts.ToString());

            if (slotAnimateOnFill && slotAnimationDuration > 0f && Runner != null)
                Runner.StartCoroutine(AnimateSlotPop(slot.transform, slotAnimationDuration));

            Debug.Log($"[ScoreProgressBarNode] Filled slot {_slotFillIndex} with {totalAttempts} attempts ({(wasCorrect ? "Correct" : "Wrong")}), advancing to {_slotFillIndex + 1}");
            _slotFillIndex++;
        }

        private void SetSlotText(int slotIndex, string text)
        {
            if (_cachedSlotTexts == null || slotIndex < 0 || slotIndex >= _cachedSlotTexts.Length) return;
            var comp = _cachedSlotTexts[slotIndex];
            if (comp == null) return;

            if (comp is TextMeshProUGUI tmp)
                tmp.text = text;
            else if (comp is Text legacyText)
                legacyText.text = text;
        }

        private IEnumerator AnimateSlotPop(Transform slotTransform, float duration)
        {
            float half = duration * 0.5f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scale = Mathf.Lerp(1f, 1.3f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scale = Mathf.Lerp(1.3f, 1f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            slotTransform.localScale = Vector3.one;
        }

        // === Slider / FilledImage Logic ===

        private void SubscribeToScoreEventsIfNeeded()
        {
            if (_isScoreEventSubscribed) return;
            if (!liveUpdateFromScoreEvents) return;
            if (valueSource != ValueSource.QuizScore) return;

            QuizState.OnScoreChanged += OnQuizScoreChanged;
            _isScoreEventSubscribed = true;
        }

        private void OnQuizScoreChanged(int _)
        {
            if (displayMode == DisplayMode.Slots) return;
            if (displayMode == DisplayMode.FilledImage && filledImageUseFinalAnswers) return;
            if (Runner == null || !Runner.IsRunning) return;
            ApplyCurrentValueToTarget();
        }

        private void StopActiveAnimation()
        {
            if (_activeAnimation != null && Runner != null)
            {
                Runner.StopCoroutine(_activeAnimation);
                _activeAnimation = null;
            }
        }

        private GameObject ResolveTarget()
        {
            // First, try the direct reference
            if (targetRef != null)
            {
                if (targetRef is GameObject go && go != null)
                    return go;
                if (targetRef is Component c && c != null)
                    return c.gameObject;
            }
            
            // Reference is null or invalid - try to restore from path
            if (!string.IsNullOrEmpty(targetPath))
            {
                // Try GameObject.Find first (works if object is active)
                var found = GameObject.Find(targetPath);
                if (found != null)
                {
                    // Restore the reference for next time
                    targetRef = found;
                    return found;
                }
                
                // Try hierarchical search (works even if object is inactive)
                var parts = targetPath.Split('/');
                if (parts.Length > 0)
                {
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (var rootGo in rootObjects)
                    {
                        if (rootGo.name != parts[0]) continue;
                        if (parts.Length == 1)
                        {
                            targetRef = rootGo; // Restore reference
                            return rootGo;
                        }
                        var t = rootGo.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                        if (t != null)
                        {
                            targetRef = t.gameObject; // Restore reference
                            return t.gameObject;
                        }
                    }
                }
            }
            return null;
        }

        private float GetValue()
        {
            if (valueSource == ValueSource.QuizScore)
            {
                var state = QuizState.Instance;
                return state != null ? state.currentScore : 0f;
            }
            return GetVariableNumber(valueVariableName, 0f);
        }

        private float GetMin()
        {
            if (valueSource == ValueSource.QuizScore && useQuizRange)
                return 0f;
            if (!string.IsNullOrEmpty(minVariableName))
                return GetVariableNumber(minVariableName, minLiteral);
            return minLiteral;
        }

        private float GetMax()
        {
            if (valueSource == ValueSource.QuizScore && useQuizRange)
            {
                var state = QuizState.Instance;
                return state != null && state.maxPossibleScore > 0 ? state.maxPossibleScore : maxLiteral;
            }
            if (!string.IsNullOrEmpty(maxVariableName))
                return GetVariableNumber(maxVariableName, maxLiteral);
            return maxLiteral;
        }

        private float GetVariableNumber(string variableName, float fallback)
        {
            if (string.IsNullOrEmpty(variableName) || Runner?.Graph == null)
                return fallback;
            var v = Runner.Graph.GetVariable(variableName);
            if (v == null)
                return fallback;
            if (v.Type == VariableType.Int)
                return v.GetIntValue();
            if (v.Type == VariableType.Float)
                return v.GetFloatValue();
            return fallback;
        }

        public override void Reset()
        {
            base.Reset();
            StopActiveAnimation();

            if (_isScoreEventSubscribed)
            {
                QuizState.OnScoreChanged -= OnQuizScoreChanged;
                _isScoreEventSubscribed = false;
            }

            // NOTE: We intentionally do NOT unsubscribe from OnStepResult,
            // do NOT unsubscribe from OnWrongAttempt,
            // do NOT unsubscribe from OnLastAnswerResult,
            // do NOT clear _cachedSlotImages, and do NOT reset _slotFillIndex here.
            // The graph runner calls Reset() before re-executing a completed node.
            // For Slots mode, the fill index and cached images must survive across
            // re-executions so subsequent step results fill the NEXT slot, not slot 0.
        }
    }
}
