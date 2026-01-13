using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections;
using System;

namespace QuizSystem
{
    /// <summary>
    /// Editor window for previewing and customizing quiz animations at editor-time.
    /// Uses Animation Sequencer's built-in preview system when available.
    /// </summary>
    public class QuizAnimationPreviewWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Quiz System/Animation Preview")]
        private static void OpenWindow()
        {
            GetWindow<QuizAnimationPreviewWindow>("Animation Preview").Show();
        }

        [Title("Animation Preview")]
        [InfoBox("Select a QuizManager or QuestionUI component to preview animations. If Animation Sequencer components are attached, use their built-in preview. Otherwise, use custom preview.", InfoMessageType.Info)]
        
        [BoxGroup("Target Selection")]
        [Tooltip("QuizManager component to preview transitions")]
        [OnValueChanged("OnTargetChanged")]
        public QuizManager quizManager;

        [BoxGroup("Target Selection")]
        [Tooltip("QuestionUI component to preview feedback animations")]
        [OnValueChanged("OnTargetChanged")]
        public QuestionUI questionUI;

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuizManager")]
        [InfoBox("Add Animation Sequencer components to QuizManager for visual editor-time preview. Use the + button in the Animation Sequencer component to add animation steps.", InfoMessageType.Info)]
        [PropertyOrder(5)]
        [Button("Add Transition Out Sequencer", ButtonSizes.Small)]
        [ShowIf("HasQuizManager")]
        private void AddTransitionOutSequencer()
        {
            if (quizManager != null && isAnimationSequencerAvailable)
            {
                var sequencer = quizManager.gameObject.AddComponent(animationSequencerType);
                transitionOutSequencer = sequencer;
                EditorUtility.SetDirty(quizManager);
            }
        }

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuizManager")]
        [PropertyOrder(5)]
        [LabelText("Transition Out Sequencer")]
        [Tooltip("Animation Sequencer for question transition out")]
        public Component transitionOutSequencer;

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuizManager")]
        [PropertyOrder(6)]
        [Button("Add Transition In Sequencer", ButtonSizes.Small)]
        [ShowIf("HasQuizManager")]
        private void AddTransitionInSequencer()
        {
            if (quizManager != null && isAnimationSequencerAvailable)
            {
                var sequencer = quizManager.gameObject.AddComponent(animationSequencerType);
                transitionInSequencer = sequencer;
                EditorUtility.SetDirty(quizManager);
            }
        }

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuizManager")]
        [PropertyOrder(6)]
        [LabelText("Transition In Sequencer")]
        [Tooltip("Animation Sequencer for question transition in")]
        public Component transitionInSequencer;

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(7)]
        [Button("Add Correct Answer Sequencer", ButtonSizes.Small)]
        [ShowIf("HasQuestionUI")]
        private void AddCorrectAnswerSequencer()
        {
            if (questionUI != null && isAnimationSequencerAvailable)
            {
                var sequencer = questionUI.gameObject.AddComponent(animationSequencerType);
                correctAnswerSequencer = sequencer;
                EditorUtility.SetDirty(questionUI);
            }
        }

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(7)]
        [LabelText("Correct Answer Sequencer")]
        [Tooltip("Animation Sequencer for correct answer feedback")]
        public Component correctAnswerSequencer;

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(8)]
        [Button("Add Wrong Answer Sequencer", ButtonSizes.Small)]
        [ShowIf("HasQuestionUI")]
        private void AddWrongAnswerSequencer()
        {
            if (questionUI != null && isAnimationSequencerAvailable)
            {
                var sequencer = questionUI.gameObject.AddComponent(animationSequencerType);
                wrongAnswerSequencer = sequencer;
                EditorUtility.SetDirty(questionUI);
            }
        }

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(8)]
        [LabelText("Wrong Answer Sequencer")]
        [Tooltip("Animation Sequencer for wrong answer feedback")]
        public Component wrongAnswerSequencer;

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(9)]
        [Button("Add Hint Reveal Sequencer", ButtonSizes.Small)]
        [ShowIf("HasQuestionUI")]
        private void AddHintRevealSequencer()
        {
            if (questionUI != null && isAnimationSequencerAvailable)
            {
                var sequencer = questionUI.gameObject.AddComponent(animationSequencerType);
                hintRevealSequencer = sequencer;
                EditorUtility.SetDirty(questionUI);
            }
        }

        [BoxGroup("Animation Sequencer Components")]
        [ShowIf("HasQuestionUI")]
        [PropertyOrder(9)]
        [LabelText("Hint Reveal Sequencer")]
        [Tooltip("Animation Sequencer for hint reveal")]
        public Component hintRevealSequencer;

        private System.Type animationSequencerType;
        private bool isAnimationSequencerAvailable;

        private void OnEnable()
        {
            // Check if Animation Sequencer is available
            CheckAnimationSequencerAvailability();
            
            // Auto-select from scene if available
            if (quizManager == null)
            {
                quizManager = FindObjectOfType<QuizManager>();
            }
            if (questionUI == null)
            {
                questionUI = FindObjectOfType<QuestionUI>();
            }

            OnTargetChanged();
        }

        private void CheckAnimationSequencerAvailability()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                animationSequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencer");
                if (animationSequencerType != null)
                {
                    isAnimationSequencerAvailable = true;
                    return;
                }
            }
            isAnimationSequencerAvailable = false;
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Transition Out", ButtonSizes.Large)]
        [EnableIf("HasQuizManager")]
        [PropertyOrder(10)]
        private void PreviewTransitionOut()
        {
            if (quizManager == null) return;

            // Use Animation Sequencer if available
            if (isAnimationSequencerAvailable && transitionOutSequencer != null)
            {
                PlayAnimationSequencer(transitionOutSequencer);
                return;
            }

            // Fallback to custom preview
            PreviewTransitionOutCustom();
        }

        private void PreviewTransitionOutCustom()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;
            
            // Create a dummy container for preview
            GameObject previewContainer = new GameObject("Preview Container");
            previewContainer.transform.SetParent(quizManager.transform);
            RectTransform rectTransform = previewContainer.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);

            CanvasGroup canvasGroup = previewContainer.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Preview transition out
            Sequence sequence = DOTween.Sequence();
            
            switch (quizManager.transitionStyle)
            {
                case QuizManager.TransitionStyle.Fade:
                    sequence.Append(canvasGroup.DOFade(0f, quizManager.transitionDuration));
                    break;
                case QuizManager.TransitionStyle.Slide:
                    sequence.Append(rectTransform.DOAnchorPosX(rectTransform.anchoredPosition.x - 1000f, quizManager.transitionDuration));
                    break;
                case QuizManager.TransitionStyle.Scale:
                    sequence.Append(previewContainer.transform.DOScale(0f, quizManager.transitionDuration));
                    break;
            }

            sequence.SetEase(Ease.InQuad);
            sequence.OnComplete(() =>
            {
                DestroyImmediate(previewContainer);
                EditorApplication.update -= UpdateEditor;
            });

            sequence.Play();
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Transition In", ButtonSizes.Large)]
        [EnableIf("HasQuizManager")]
        [PropertyOrder(11)]
        private void PreviewTransitionIn()
        {
            if (quizManager == null) return;

            // Use Animation Sequencer if available
            if (isAnimationSequencerAvailable && transitionInSequencer != null)
            {
                PlayAnimationSequencer(transitionInSequencer);
                return;
            }

            // Fallback to custom preview
            PreviewTransitionInCustom();
        }

        private void PreviewTransitionInCustom()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;

            // Create a dummy container for preview
            GameObject previewContainer = new GameObject("Preview Container");
            previewContainer.transform.SetParent(quizManager.transform);
            RectTransform rectTransform = previewContainer.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);

            CanvasGroup canvasGroup = previewContainer.AddComponent<CanvasGroup>();

            // Reset state based on transition style
            switch (quizManager.transitionStyle)
            {
                case QuizManager.TransitionStyle.Fade:
                    canvasGroup.alpha = 0f;
                    break;
                case QuizManager.TransitionStyle.Slide:
                    rectTransform.anchoredPosition = new Vector2(1000f, 0f);
                    canvasGroup.alpha = 1f;
                    break;
                case QuizManager.TransitionStyle.Scale:
                    previewContainer.transform.localScale = Vector3.zero;
                    canvasGroup.alpha = 1f;
                    break;
            }

            // Preview transition in
            Sequence sequence = DOTween.Sequence();

            switch (quizManager.transitionStyle)
            {
                case QuizManager.TransitionStyle.Fade:
                    sequence.Append(canvasGroup.DOFade(1f, quizManager.transitionDuration));
                    break;
                case QuizManager.TransitionStyle.Slide:
                    sequence.Append(rectTransform.DOAnchorPos(Vector2.zero, quizManager.transitionDuration));
                    break;
                case QuizManager.TransitionStyle.Scale:
                    sequence.Append(previewContainer.transform.DOScale(1f, quizManager.transitionDuration));
                    break;
            }

            sequence.SetEase(Ease.OutQuad);
            sequence.OnComplete(() =>
            {
                EditorApplication.update -= UpdateEditor;
            });

            sequence.Play();
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Correct Answer", ButtonSizes.Large)]
        [EnableIf("HasQuestionUI")]
        [PropertyOrder(20)]
        private void PreviewCorrectAnswer()
        {
            if (questionUI == null) return;

            // Use Animation Sequencer if available
            if (isAnimationSequencerAvailable && correctAnswerSequencer != null)
            {
                PlayAnimationSequencer(correctAnswerSequencer);
                return;
            }

            // Fallback to custom preview
            PreviewCorrectAnswerCustom();
        }

        private void PreviewCorrectAnswerCustom()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;

            // Preview correct answer animation
            if (questionUI.transform != null)
            {
                Vector3 originalScale = questionUI.transform.localScale;
                Sequence sequence = DOTween.Sequence();
                sequence.Append(questionUI.transform.DOScale(originalScale * 1.05f, questionUI.feedbackDuration * 0.3f).SetEase(Ease.OutQuad));
                sequence.Append(questionUI.transform.DOScale(originalScale, questionUI.feedbackDuration * 0.7f).SetEase(Ease.InQuad));
                sequence.OnComplete(() => EditorApplication.update -= UpdateEditor);
                sequence.Play();
            }
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Wrong Answer", ButtonSizes.Large)]
        [EnableIf("HasQuestionUI")]
        [PropertyOrder(21)]
        private void PreviewWrongAnswer()
        {
            if (questionUI == null) return;

            // Use Animation Sequencer if available
            if (isAnimationSequencerAvailable && wrongAnswerSequencer != null)
            {
                PlayAnimationSequencer(wrongAnswerSequencer);
                return;
            }

            // Fallback to custom preview
            PreviewWrongAnswerCustom();
        }

        private void PreviewWrongAnswerCustom()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;

            // Preview wrong answer animation
            if (questionUI.transform != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(questionUI.transform.DOShakePosition(questionUI.feedbackDuration, 5f, 10, 90f, false, true));
                sequence.OnComplete(() => EditorApplication.update -= UpdateEditor);
                sequence.Play();
            }
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Hint Reveal", ButtonSizes.Large)]
        [EnableIf("HasQuestionUI")]
        [PropertyOrder(22)]
        private void PreviewHintReveal()
        {
            if (questionUI == null) return;

            // Use Animation Sequencer if available
            if (isAnimationSequencerAvailable && hintRevealSequencer != null)
            {
                PlayAnimationSequencer(hintRevealSequencer);
                return;
            }

            // Fallback to custom preview
            PreviewHintRevealCustom();
        }

        private void PreviewHintRevealCustom()
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;

            // Get hint panel components using reflection
            var hintPanelField = typeof(QuestionUI).GetField("hintPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hintPanelRectTransformField = typeof(QuestionUI).GetField("hintPanelRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hintPanelCanvasGroupField = typeof(QuestionUI).GetField("hintPanelCanvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            GameObject hintPanel = hintPanelField?.GetValue(questionUI) as GameObject;
            RectTransform rectTransform = hintPanelRectTransformField?.GetValue(questionUI) as RectTransform;
            CanvasGroup canvasGroup = hintPanelCanvasGroupField?.GetValue(questionUI) as CanvasGroup;

            if (hintPanel != null)
            {
                hintPanel.SetActive(true);

                if (rectTransform != null && canvasGroup != null)
                {
                    // Reset state
                    canvasGroup.alpha = 0f;
                    Vector2 targetPos = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = targetPos + Vector2.down * 30f;

                    // Animate in
                    Sequence sequence = DOTween.Sequence();
                    sequence.Join(rectTransform.DOAnchorPos(targetPos, questionUI.feedbackDuration * 0.8f).SetEase(Ease.OutQuad));
                    sequence.Join(canvasGroup.DOFade(1f, questionUI.feedbackDuration).SetEase(Ease.OutQuad));
                    sequence.OnComplete(() => EditorApplication.update -= UpdateEditor);
                    sequence.Play();
                }
                else
                {
                    // Fallback: just show the panel
                    EditorApplication.update -= UpdateEditor;
                }
            }
        }

        [BoxGroup("Preview Controls")]
        [Button("Preview Button Entrance", ButtonSizes.Large)]
        [EnableIf("HasMultipleChoiceUI")]
        [PropertyOrder(23)]
        private void PreviewButtonEntrance()
        {
            MultipleChoiceUI mcUI = questionUI as MultipleChoiceUI;
            if (mcUI == null) return;

            EditorApplication.isPlaying = false;
            EditorApplication.update += UpdateEditor;

            // Get answer buttons using reflection
            var answerButtonsField = typeof(MultipleChoiceUI).GetField("answerButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enableButtonEntranceField = typeof(MultipleChoiceUI).GetField("enableButtonEntrance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var buttonStaggerDelayField = typeof(MultipleChoiceUI).GetField("buttonStaggerDelay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var buttonEntranceDurationField = typeof(MultipleChoiceUI).GetField("buttonEntranceDuration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            UnityEngine.UI.Button[] buttons = answerButtonsField?.GetValue(mcUI) as UnityEngine.UI.Button[];
            bool enableEntrance = enableButtonEntranceField != null ? (bool)enableButtonEntranceField.GetValue(mcUI) : true;
            float staggerDelay = buttonStaggerDelayField != null ? (float)buttonStaggerDelayField.GetValue(mcUI) : 0.1f;
            float entranceDuration = buttonEntranceDurationField != null ? (float)buttonEntranceDurationField.GetValue(mcUI) : 0.3f;

            if (buttons != null && enableEntrance)
            {
                int completedCount = 0;
                int totalButtons = buttons.Length;

                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].transform != null)
                    {
                        Vector3 originalScale = buttons[i].transform.localScale;
                        buttons[i].transform.localScale = Vector3.zero;

                        buttons[i].transform.DOScale(originalScale, entranceDuration)
                            .SetDelay(i * staggerDelay)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() =>
                            {
                                completedCount++;
                                if (completedCount >= totalButtons)
                                {
                                    EditorApplication.update -= UpdateEditor;
                                }
                            });
                    }
                }
            }
        }

        private bool HasMultipleChoiceUI()
        {
            return questionUI != null && questionUI is MultipleChoiceUI;
        }

        [BoxGroup("Preview Controls")]
        [Button("Stop All Previews", ButtonSizes.Medium)]
        [PropertyOrder(30)]
        private void StopAllPreviews()
        {
            // Stop Animation Sequencer previews if available
            if (isAnimationSequencerAvailable)
            {
                StopAnimationSequencer(transitionOutSequencer);
                StopAnimationSequencer(transitionInSequencer);
                StopAnimationSequencer(correctAnswerSequencer);
                StopAnimationSequencer(wrongAnswerSequencer);
                StopAnimationSequencer(hintRevealSequencer);
            }

            // Stop custom DOTween animations
            DOTween.KillAll();
            EditorApplication.update -= UpdateEditor;
        }

        private void PlayAnimationSequencer(Component sequencer)
        {
            if (sequencer == null || !isAnimationSequencerAvailable) return;

            // Use reflection to call Play() method on Animation Sequencer
            var playMethod = sequencer.GetType().GetMethod("Play", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (playMethod != null)
            {
                playMethod.Invoke(sequencer, null);
            }
        }

        private void StopAnimationSequencer(Component sequencer)
        {
            if (sequencer == null || !isAnimationSequencerAvailable) return;

            // Use reflection to call Kill() or Stop() method on Animation Sequencer
            var killMethod = sequencer.GetType().GetMethod("Kill", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (killMethod != null)
            {
                killMethod.Invoke(sequencer, null);
            }
            else
            {
                var stopMethod = sequencer.GetType().GetMethod("Stop", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                stopMethod?.Invoke(sequencer, null);
            }
        }

        [Title("Animation Settings")]
        [BoxGroup("QuizManager Settings")]
        [ShowIf("HasQuizManager")]
        [InlineEditor(InlineEditorModes.FullEditor)]
        [PropertyOrder(100)]
        public QuizManager quizManagerSettings;

        [BoxGroup("QuestionUI Settings")]
        [ShowIf("HasQuestionUI")]
        [InlineEditor(InlineEditorModes.FullEditor)]
        [PropertyOrder(101)]
        public QuestionUI questionUISettings;

        private bool HasQuizManager()
        {
            return quizManager != null;
        }

        private bool HasQuestionUI()
        {
            return questionUI != null;
        }

        private void OnTargetChanged()
        {
            quizManagerSettings = quizManager;
            questionUISettings = questionUI;

            // Auto-detect Animation Sequencer components
            if (quizManager != null && isAnimationSequencerAvailable)
            {
                transitionOutSequencer = quizManager.GetComponent(animationSequencerType);
                transitionInSequencer = quizManager.GetComponent(animationSequencerType);
                
                // Try to find multiple sequencers (they might be on child objects)
                var sequencers = quizManager.GetComponentsInChildren(animationSequencerType);
                if (sequencers.Length > 0)
                {
                    transitionOutSequencer = sequencers[0];
                    if (sequencers.Length > 1)
                        transitionInSequencer = sequencers[1];
                }
            }

            if (questionUI != null && isAnimationSequencerAvailable)
            {
                correctAnswerSequencer = questionUI.GetComponent(animationSequencerType);
                wrongAnswerSequencer = questionUI.GetComponent(animationSequencerType);
                hintRevealSequencer = questionUI.GetComponent(animationSequencerType);
                
                // Try to find multiple sequencers
                var sequencers = questionUI.GetComponentsInChildren(animationSequencerType);
                if (sequencers.Length > 0)
                {
                    correctAnswerSequencer = sequencers.Length > 0 ? sequencers[0] : null;
                    wrongAnswerSequencer = sequencers.Length > 1 ? sequencers[1] : null;
                    hintRevealSequencer = sequencers.Length > 2 ? sequencers[2] : null;
                }
            }
        }

        private void UpdateEditor()
        {
            // Update DOTween in editor
            DOTween.ManualUpdate(0.016f, 0.016f);
            SceneView.RepaintAll();
        }

        private void OnDisable()
        {
            StopAllPreviews();
        }
    }
}

