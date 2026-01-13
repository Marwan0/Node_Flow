using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    public class MultiSelectUI : QuestionUI
    {
        [Header("Multi-Select UI")]
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionPrefab;
        [SerializeField] private Button submitButton;

        private List<Toggle> optionToggles = new List<Toggle>();
        private MultiSelectQuestionData multiSelectData;

        protected override void SetupQuestion()
        {
            multiSelectData = currentQuestion as MultiSelectQuestionData;
            if (multiSelectData == null) return;

            // Clear existing options
            foreach (var toggle in optionToggles)
            {
                if (toggle != null) Destroy(toggle.gameObject);
            }
            optionToggles.Clear();

            // Create option toggles
            if (optionsContainer != null && optionPrefab != null)
            {
                for (int i = 0; i < multiSelectData.options.Count; i++)
                {
                    GameObject optionObj = Instantiate(optionPrefab, optionsContainer);
                    Toggle toggle = optionObj.GetComponent<Toggle>();
                    TextMeshProUGUI label = optionObj.GetComponentInChildren<TextMeshProUGUI>();

                    if (toggle != null && label != null)
                    {
                        label.text = multiSelectData.options[i];
                        toggle.isOn = false;
                        optionToggles.Add(toggle);
                    }
                }
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }
        }

        private void OnSubmitClicked()
        {
            List<int> selectedIndices = new List<int>();
            for (int i = 0; i < optionToggles.Count; i++)
            {
                if (optionToggles[i].isOn)
                {
                    selectedIndices.Add(i);
                }
            }

            if (selectedIndices.Count == 0)
            {
                ShowHint("Please select at least one answer.");
                return;
            }

            var result = validator.ValidateAnswer(selectedIndices);
            HandleValidationResult(result);

            if (result.IsCorrect || result.ShouldAutoCorrect)
            {
                if (submitButton != null)
                    submitButton.interactable = false;
                
                // Disable all toggles
                foreach (var toggle in optionToggles)
                {
                    if (toggle != null) toggle.interactable = false;
                }
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (multiSelectData != null)
            {
                List<string> correctAnswers = new List<string>();
                foreach (int index in multiSelectData.correctAnswerIndices)
                {
                    if (index >= 0 && index < multiSelectData.options.Count)
                    {
                        correctAnswers.Add(multiSelectData.options[index]);
                    }
                }
                return string.Join(", ", correctAnswers);
            }
            return "";
        }
    }
}

