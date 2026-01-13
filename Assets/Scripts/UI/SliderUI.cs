using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    public class SliderUI : QuestionUI
    {
        [Header("Slider UI")]
        [SerializeField] private Slider valueSlider;
        [SerializeField] private TextMeshProUGUI valueDisplay;
        [SerializeField] private TextMeshProUGUI minLabel;
        [SerializeField] private TextMeshProUGUI maxLabel;
        [SerializeField] private Button submitButton;

        private SliderQuestionData sliderData;
        private float currentValue;

        protected override void SetupQuestion()
        {
            sliderData = currentQuestion as SliderQuestionData;
            if (sliderData == null) return;

            if (valueSlider != null)
            {
                valueSlider.minValue = sliderData.valueRange.x;
                valueSlider.maxValue = sliderData.valueRange.y;
                valueSlider.value = (sliderData.valueRange.x + sliderData.valueRange.y) * 0.5f;
                valueSlider.onValueChanged.RemoveAllListeners();
                valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (minLabel != null && sliderData.showValueLabels)
            {
                minLabel.text = FormatValue(sliderData.valueRange.x);
            }

            if (maxLabel != null && sliderData.showValueLabels)
            {
                maxLabel.text = FormatValue(sliderData.valueRange.y);
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }

            UpdateValueDisplay();
        }

        private void OnSliderValueChanged(float value)
        {
            currentValue = value;
            UpdateValueDisplay();
        }

        private void UpdateValueDisplay()
        {
            if (valueDisplay != null && sliderData != null && sliderData.showCurrentValue)
            {
                valueDisplay.text = FormatValue(currentValue);
            }
        }

        private string FormatValue(float value)
        {
            if (sliderData != null)
            {
                return value.ToString($"F{sliderData.decimalPlaces}");
            }
            return value.ToString("F0");
        }

        private void OnSubmitClicked()
        {
            var result = validator.ValidateAnswer(currentValue);
            HandleValidationResult(result);

            if (result.IsCorrect || result.ShouldAutoCorrect)
            {
                if (submitButton != null)
                    submitButton.interactable = false;
                if (valueSlider != null)
                    valueSlider.interactable = false;
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (sliderData != null)
            {
                if (sliderData.useTolerance)
                {
                    return $"{FormatValue(sliderData.correctValue)} (±{FormatValue(sliderData.tolerance)})";
                }
                else
                {
                    return FormatValue(sliderData.correctValue);
                }
            }
            return "";
        }
    }
}

