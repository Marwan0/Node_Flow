using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    /// <summary>
    /// How the submit control behaves for fill-in-the-blank questions.
    /// </summary>
    public enum FillBlankSubmitPolicy
    {
        [Tooltip("Submit stays disabled until every blank has non-whitespace text.")]
        RequireAllBlanksFilled,

        [Tooltip("Submit is always available (when the UI is unlocked). Empty blanks are submitted as empty strings and count as wrong until all match.")]
        AllowIncompleteBlanks
    }

    public class FillInTheBlankUI : QuestionUI
    {
        [Header("Fill in the Blank UI")]
        [Tooltip("Used when the question has a single blank (if not using Manual Blank Inputs), or as the template to clone for automatic multiple blanks.")]
        [SerializeField] private TMP_InputField answerInput;

        [Tooltip("Parent for extra input fields when there are multiple blanks (automatic mode only). If unset, children are created under this transform.")]
        [SerializeField] private RectTransform blanksContainer;

        [Tooltip("Optional prefab for each blank (automatic mode only). If unset, answerInput is duplicated.")]
        [SerializeField] private TMP_InputField blankFieldPrefab;

        [Header("Submit")]
        [Tooltip("Require all blanks: submit button stays disabled until every field has text. Allow incomplete: submit always enabled (when unlocked); missing blanks are validated as empty/wrong.")]
        [SerializeField] private FillBlankSubmitPolicy submitPolicy = FillBlankSubmitPolicy.RequireAllBlanksFilled;

        [Header("Manual placement (optional)")]
        [Tooltip("Assign one TMP_InputField per blank in order (matches Blanks slot 0, 1, 2…). When assigned, these are used instead of cloning — position and size them in the prefab. If there are more fields than blanks, extras are hidden.")]
        [SerializeField] private TMP_InputField[] manualBlankInputs;

        [Header("Auto-correct")]
        [Tooltip("When the learner runs out of attempts, each blank is filled with the correct answer using this text color.")]
        [SerializeField] private Color autoCorrectAnswerTextColor = new Color(0.2f, 0.85f, 0.35f, 1f);

        private readonly List<TMP_InputField> _blankInputs = new List<TMP_InputField>();
        private readonly List<GameObject> _spawnedBlankRoots = new List<GameObject>();
        private readonly List<Color> _blankInputOriginalTextColors = new List<Color>();

        protected override void SetupQuestion()
        {
            RestorePreviousBlankTextColorsIfAny();

            ClearSpawnedBlanks();
            DeactivateAllManualBlankInputs();

            if (!(currentQuestion is FillInTheBlankQuestionData fillData))
                return;

            int n = fillData.GetBlankSlotCount();
            _blankInputs.Clear();

            if (!TrySetupManualBlankInputs(n))
            {
                if (n == 1)
                {
                    if (answerInput != null)
                    {
                        answerInput.gameObject.SetActive(true);
                        answerInput.text = "";
                        WireBlankField(answerInput);
                        _blankInputs.Add(answerInput);
                    }
                }
                else
                {
                    if (answerInput != null)
                        answerInput.gameObject.SetActive(false);

                    var template = blankFieldPrefab != null ? blankFieldPrefab : answerInput;
                    Transform parent = blanksContainer != null ? blanksContainer : transform;

                    if (template == null)
                    {
                        Debug.LogWarning("[FillInTheBlankUI] Multiple blanks require answerInput or blankFieldPrefab.");
                    }
                    else
                    {
                        for (int i = 0; i < n; i++)
                        {
                            var instance = Instantiate(template.gameObject, parent);
                            instance.SetActive(true);
                            instance.name = $"BlankInput_{i}";
                            _spawnedBlankRoots.Add(instance);

                            var field = instance.GetComponent<TMP_InputField>();
                            if (field != null)
                            {
                                field.text = "";
                                WireBlankField(field);
                                _blankInputs.Add(field);
                            }
                        }
                    }
                }
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnAllBlanksSubmitted);
                RefreshSubmitButtonState();
            }

            ClearRegisteredHoverEffects();
            if (submitButton != null) RegisterHoverEffect(submitButton.gameObject);

            _blankInputOriginalTextColors.Clear();
            foreach (var field in _blankInputs)
            {
                if (field != null && field.textComponent != null)
                    _blankInputOriginalTextColors.Add(field.textComponent.color);
                else
                    _blankInputOriginalTextColors.Add(Color.white);
            }
        }

        private void WireBlankField(TMP_InputField field)
        {
            field.onSubmit.RemoveAllListeners();
            field.onSubmit.AddListener(_ => OnAllBlanksSubmitted());

            field.onValueChanged.RemoveAllListeners();
            if (submitPolicy == FillBlankSubmitPolicy.RequireAllBlanksFilled)
                field.onValueChanged.AddListener(_ => RefreshSubmitButtonState());
        }

        private void RefreshSubmitButtonState()
        {
            if (submitButton == null) return;

            if (submitPolicy == FillBlankSubmitPolicy.AllowIncompleteBlanks)
            {
                submitButton.interactable = _blankInputs.Count > 0;
                return;
            }

            if (_blankInputs.Count == 0)
            {
                submitButton.interactable = false;
                return;
            }

            foreach (var f in _blankInputs)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.text))
                {
                    submitButton.interactable = false;
                    return;
                }
            }

            submitButton.interactable = true;
        }

        /// <summary>
        /// After wrong-answer feedback, re-apply submit gating when the UI unlocks.
        /// </summary>
        public override void UnlockUI()
        {
            base.UnlockUI();
            RefreshSubmitButtonState();
        }

        private void RestorePreviousBlankTextColorsIfAny()
        {
            if (_blankInputOriginalTextColors.Count == 0 || _blankInputs.Count == 0)
                return;

            for (int i = 0; i < _blankInputs.Count && i < _blankInputOriginalTextColors.Count; i++)
            {
                var field = _blankInputs[i];
                if (field == null || field.textComponent == null) continue;
                field.textComponent.color = _blankInputOriginalTextColors[i];
            }
        }

        private void DeactivateAllManualBlankInputs()
        {
            if (manualBlankInputs == null) return;
            foreach (var field in manualBlankInputs)
            {
                if (field != null)
                    field.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Uses manually assigned fields when there are at least as many as the question needs.
        /// </summary>
        private bool TrySetupManualBlankInputs(int n)
        {
            if (manualBlankInputs == null || manualBlankInputs.Length == 0)
                return false;

            if (manualBlankInputs.Length < n)
            {
                Debug.LogWarning(
                    $"[FillInTheBlankUI] Manual blank inputs: {manualBlankInputs.Length} assigned but this question has {n} blanks — falling back to automatic placement. Add more fields or leave Manual Blank Inputs empty.");
                return false;
            }

            for (int i = 0; i < n; i++)
            {
                if (manualBlankInputs[i] == null)
                {
                    Debug.LogError(
                        $"[FillInTheBlankUI] Manual blank inputs has a null entry at index {i}. Fix references or use automatic placement.");
                    return false;
                }
            }

            for (int i = 0; i < manualBlankInputs.Length; i++)
            {
                var field = manualBlankInputs[i];
                if (i < n)
                {
                    field.gameObject.SetActive(true);
                    field.text = "";
                    WireBlankField(field);
                    _blankInputs.Add(field);
                }
                else
                    field.gameObject.SetActive(false);
            }

            if (answerInput != null)
            {
                bool answerInputUsed = false;
                for (int i = 0; i < n; i++)
                {
                    if (manualBlankInputs[i] == answerInput)
                    {
                        answerInputUsed = true;
                        break;
                    }
                }

                if (!answerInputUsed)
                    answerInput.gameObject.SetActive(false);
            }

            return true;
        }

        private void ClearSpawnedBlanks()
        {
            foreach (var go in _spawnedBlankRoots)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedBlankRoots.Clear();
        }

        protected override void OnDestroy()
        {
            ClearSpawnedBlanks();
            base.OnDestroy();
        }

        private void OnAllBlanksSubmitted()
        {
            if (_blankInputs.Count == 0)
            {
                ShowHint("No answer fields are set up.");
                return;
            }

            if (submitPolicy == FillBlankSubmitPolicy.RequireAllBlanksFilled)
            {
                foreach (var f in _blankInputs)
                {
                    if (f == null || string.IsNullOrWhiteSpace(f.text))
                    {
                        ShowHint("Please fill in all blanks.");
                        return;
                    }
                }
            }

            var values = new string[_blankInputs.Count];
            for (int i = 0; i < _blankInputs.Count; i++)
            {
                if (_blankInputs[i] == null)
                    values[i] = "";
                else
                    values[i] = string.IsNullOrWhiteSpace(_blankInputs[i].text) ? "" : _blankInputs[i].text.Trim();
            }

            var result = validator.ValidateAnswer(values);
            HandleValidationResult(result);

            if (result.IsCorrect || result.ShouldAutoCorrect)
                SetAllInputsInteractable(false);
        }

        private void SetAllInputsInteractable(bool on)
        {
            foreach (var field in _blankInputs)
            {
                if (field != null)
                    field.interactable = on;
            }

            if (submitButton == null) return;
            if (!on)
                submitButton.interactable = false;
            else
                RefreshSubmitButtonState();
        }

        public override void OnAnswerSubmitted()
        {
            OnAllBlanksSubmitted();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (currentQuestion is FillInTheBlankQuestionData fillData)
                return fillData.GetJoinedCorrectAnswersDisplay();
            return "";
        }

        /// <summary>
        /// After wrong-answer feedback, fills each blank with the canonical correct text and applies <see cref="autoCorrectAnswerTextColor"/>.
        /// </summary>
        protected override void ApplyAutoCorrectVisuals()
        {
            if (currentQuestion is not FillInTheBlankQuestionData fillData)
                return;

            int n = fillData.GetBlankSlotCount();
            for (int i = 0; i < _blankInputs.Count && i < n; i++)
            {
                var field = _blankInputs[i];
                if (field == null) continue;

                field.text = fillData.GetPrimaryCorrectAnswerForDisplay(i);

                if (field.textComponent != null)
                    field.textComponent.color = autoCorrectAnswerTextColor;
            }
        }
    }
}
