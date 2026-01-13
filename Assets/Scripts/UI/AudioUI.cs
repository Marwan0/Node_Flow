using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    public class AudioUI : QuestionUI
    {
        [Header("Audio UI")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private TextMeshProUGUI playCountText;
        [SerializeField] private AudioSource audioSource;

        [Header("Answer UI")]
        [SerializeField] private Transform answerContainer;
        [SerializeField] private GameObject multipleChoiceOptionPrefab;
        [SerializeField] private TMP_InputField fillBlankInput;
        [SerializeField] private Button submitButton;

        private AudioQuestionData audioData;
        private int playCount = 0;
        private int? selectedAnswerIndex = null;

        protected override void SetupQuestion()
        {
            audioData = currentQuestion as AudioQuestionData;
            if (audioData == null || audioSource == null) return;

            // Setup audio
            audioSource.clip = audioData.audioClip;
            playCount = 0;
            UpdatePlayCount();

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(PlayAudio);
                playButton.interactable = audioData.allowReplay || playCount == 0;
            }

            if (stopButton != null)
            {
                stopButton.onClick.RemoveAllListeners();
                stopButton.onClick.AddListener(StopAudio);
            }

            // Setup answer UI based on answer type
            if (audioData.answerType == AudioAnswerType.MultipleChoice)
            {
                SetupMultipleChoice();
            }
            else if (audioData.answerType == AudioAnswerType.FillInTheBlank)
            {
                SetupFillInTheBlank();
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }

            // Auto-play if enabled
            if (audioData.autoPlay)
            {
                PlayAudio();
            }
        }

        private void SetupMultipleChoice()
        {
            if (answerContainer == null || multipleChoiceOptionPrefab == null) return;

            // Clear existing options
            foreach (Transform child in answerContainer)
            {
                Destroy(child.gameObject);
            }

            // Create option buttons
            for (int i = 0; i < audioData.answerOptions.Count; i++)
            {
                GameObject optionObj = Instantiate(multipleChoiceOptionPrefab, answerContainer);
                Button button = optionObj.GetComponent<Button>();
                TextMeshProUGUI label = optionObj.GetComponentInChildren<TextMeshProUGUI>();

                if (button != null && label != null)
                {
                    label.text = audioData.answerOptions[i];
                    int capturedIndex = i;
                    button.onClick.AddListener(() => OnOptionSelected(capturedIndex));
                }
            }

            if (fillBlankInput != null)
                fillBlankInput.gameObject.SetActive(false);
        }

        private void SetupFillInTheBlank()
        {
            if (fillBlankInput != null)
            {
                fillBlankInput.gameObject.SetActive(true);
                fillBlankInput.text = "";
            }

            if (answerContainer != null)
            {
                foreach (Transform child in answerContainer)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void OnOptionSelected(int index)
        {
            selectedAnswerIndex = index;
            // Visual feedback could be added here
        }

        private void PlayAudio()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                if (playCount >= audioData.maxPlayCount)
                {
                    ShowHint($"Maximum play count ({audioData.maxPlayCount}) reached.");
                    return;
                }

                audioSource.Play();
                playCount++;
                UpdatePlayCount();

                if (playButton != null)
                {
                    playButton.interactable = audioData.allowReplay && playCount < audioData.maxPlayCount;
                }
            }
        }

        private void StopAudio()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        private void UpdatePlayCount()
        {
            if (playCountText != null)
            {
                playCountText.text = $"Plays: {playCount} / {audioData.maxPlayCount}";
            }
        }

        private void OnSubmitClicked()
        {
            object answer = null;

            if (audioData.answerType == AudioAnswerType.MultipleChoice)
            {
                if (!selectedAnswerIndex.HasValue)
                {
                    ShowHint("Please select an answer.");
                    return;
                }
                answer = selectedAnswerIndex.Value;
            }
            else if (audioData.answerType == AudioAnswerType.FillInTheBlank)
            {
                if (fillBlankInput == null || string.IsNullOrWhiteSpace(fillBlankInput.text))
                {
                    ShowHint("Please enter an answer.");
                    return;
                }
                answer = fillBlankInput.text.Trim();
            }

            if (answer != null)
            {
                var result = validator.ValidateAnswer(answer);
                HandleValidationResult(result);

                if (result.IsCorrect || result.ShouldAutoCorrect)
                {
                    if (submitButton != null)
                        submitButton.interactable = false;
                    if (playButton != null)
                        playButton.interactable = false;
                }
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (audioData == null) return "";

            if (audioData.answerType == AudioAnswerType.MultipleChoice)
            {
                if (audioData.correctAnswerIndex >= 0 && 
                    audioData.correctAnswerIndex < audioData.answerOptions.Count)
                {
                    return audioData.answerOptions[audioData.correctAnswerIndex];
                }
            }
            else if (audioData.answerType == AudioAnswerType.FillInTheBlank)
            {
                return audioData.correctAnswerText;
            }

            return "";
        }
    }
}

