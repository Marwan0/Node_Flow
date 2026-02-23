#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DG.Tweening;

namespace QuizSystem
{
    /// <summary>
    /// Editor window for previewing and customizing quiz animations at editor-time.
    /// Uses Animation Sequencer's built-in preview system when available.
    /// </summary>
    public class QuizAnimationPreviewWindow : EditorWindow
    {
        [MenuItem("Tools/Quiz System/Animation Preview")]
        private static void OpenWindow()
        {
            GetWindow<QuizAnimationPreviewWindow>("Animation Preview").Show();
        }

        public QuizManager quizManager;
        public QuestionUI questionUI;

        public Component transitionOutSequencer;
        public Component transitionInSequencer;
        public Component correctAnswerSequencer;
        public Component wrongAnswerSequencer;
        public Component hintRevealSequencer;

        private System.Type animationSequencerType;
        private bool isAnimationSequencerAvailable;
        private Vector2 scrollPos;

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

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a QuizManager or QuestionUI component to preview animations.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Target Selection
            EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            quizManager = (QuizManager)EditorGUILayout.ObjectField("Quiz Manager", quizManager, typeof(QuizManager), true);
            questionUI = (QuestionUI)EditorGUILayout.ObjectField("Question UI", questionUI, typeof(QuestionUI), true);
            if (EditorGUI.EndChangeCheck())
            {
                OnTargetChanged();
            }

            EditorGUILayout.Space(10);

            // Animation Sequencer Components
            if (isAnimationSequencerAvailable)
            {
                EditorGUILayout.LabelField("Animation Sequencer Components", EditorStyles.boldLabel);

                if (quizManager != null)
                {
                    transitionOutSequencer = (Component)EditorGUILayout.ObjectField("Transition Out", transitionOutSequencer, typeof(Component), true);
                    transitionInSequencer = (Component)EditorGUILayout.ObjectField("Transition In", transitionInSequencer, typeof(Component), true);
                }

                if (questionUI != null)
                {
                    correctAnswerSequencer = (Component)EditorGUILayout.ObjectField("Correct Answer", correctAnswerSequencer, typeof(Component), true);
                    wrongAnswerSequencer = (Component)EditorGUILayout.ObjectField("Wrong Answer", wrongAnswerSequencer, typeof(Component), true);
                    hintRevealSequencer = (Component)EditorGUILayout.ObjectField("Hint Reveal", hintRevealSequencer, typeof(Component), true);
                }

                EditorGUILayout.Space(5);
            }

            // Preview Controls
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

            GUI.enabled = quizManager != null;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Transition Out", GUILayout.Height(30)))
                PreviewTransitionOut();
            if (GUILayout.Button("Preview Transition In", GUILayout.Height(30)))
                PreviewTransitionIn();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = questionUI != null;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Correct Answer", GUILayout.Height(30)))
                PreviewCorrectAnswer();
            if (GUILayout.Button("Preview Wrong Answer", GUILayout.Height(30)))
                PreviewWrongAnswer();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Hint Reveal", GUILayout.Height(30)))
                PreviewHintReveal();
            
            bool isMultipleChoice = questionUI != null && questionUI is MultipleChoiceUI;
            GUI.enabled = isMultipleChoice;
            if (GUILayout.Button("Preview Button Entrance", GUILayout.Height(30)))
                PreviewButtonEntrance();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Stop All Previews", GUILayout.Height(25)))
                StopAllPreviews();

            EditorGUILayout.EndScrollView();
        }

        private void PreviewTransitionOut()
        {
            if (quizManager == null) return;

            if (isAnimationSequencerAvailable && transitionOutSequencer != null)
            {
                PlayAnimationSequencer(transitionOutSequencer);
                return;
            }

            PreviewTransitionOutCustom();
        }

        private void PreviewTransitionOutCustom()
        {
            EditorApplication.update += UpdateEditor;
            
            GameObject previewContainer = new GameObject("Preview Container");
            previewContainer.transform.SetParent(quizManager.transform);
            RectTransform rectTransform = previewContainer.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);

            CanvasGroup canvasGroup = previewContainer.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

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

        private void PreviewTransitionIn()
        {
            if (quizManager == null) return;

            if (isAnimationSequencerAvailable && transitionInSequencer != null)
            {
                PlayAnimationSequencer(transitionInSequencer);
                return;
            }

            PreviewTransitionInCustom();
        }

        private void PreviewTransitionInCustom()
        {
            EditorApplication.update += UpdateEditor;

            GameObject previewContainer = new GameObject("Preview Container");
            previewContainer.transform.SetParent(quizManager.transform);
            RectTransform rectTransform = previewContainer.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);

            CanvasGroup canvasGroup = previewContainer.AddComponent<CanvasGroup>();

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

        private void PreviewCorrectAnswer()
        {
            if (questionUI == null) return;

            if (isAnimationSequencerAvailable && correctAnswerSequencer != null)
            {
                PlayAnimationSequencer(correctAnswerSequencer);
                return;
            }

            PreviewCorrectAnswerCustom();
        }

        private void PreviewCorrectAnswerCustom()
        {
            EditorApplication.update += UpdateEditor;

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

        private void PreviewWrongAnswer()
        {
            if (questionUI == null) return;

            if (isAnimationSequencerAvailable && wrongAnswerSequencer != null)
            {
                PlayAnimationSequencer(wrongAnswerSequencer);
                return;
            }

            PreviewWrongAnswerCustom();
        }

        private void PreviewWrongAnswerCustom()
        {
            EditorApplication.update += UpdateEditor;

            if (questionUI.transform != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(questionUI.transform.DOShakePosition(questionUI.feedbackDuration, 5f, 10, 90f, false, true));
                sequence.OnComplete(() => EditorApplication.update -= UpdateEditor);
                sequence.Play();
            }
        }

        private void PreviewHintReveal()
        {
            if (questionUI == null) return;

            if (isAnimationSequencerAvailable && hintRevealSequencer != null)
            {
                PlayAnimationSequencer(hintRevealSequencer);
                return;
            }

            PreviewHintRevealCustom();
        }

        private void PreviewHintRevealCustom()
        {
            EditorApplication.update += UpdateEditor;

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
                    canvasGroup.alpha = 0f;
                    Vector2 targetPos = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = targetPos + Vector2.down * 30f;

                    Sequence sequence = DOTween.Sequence();
                    sequence.Join(rectTransform.DOAnchorPos(targetPos, questionUI.feedbackDuration * 0.8f).SetEase(Ease.OutQuad));
                    sequence.Join(canvasGroup.DOFade(1f, questionUI.feedbackDuration).SetEase(Ease.OutQuad));
                    sequence.OnComplete(() => EditorApplication.update -= UpdateEditor);
                    sequence.Play();
                }
                else
                {
                    EditorApplication.update -= UpdateEditor;
                }
            }
        }

        private void PreviewButtonEntrance()
        {
            MultipleChoiceUI mcUI = questionUI as MultipleChoiceUI;
            if (mcUI == null) return;

            EditorApplication.update += UpdateEditor;

            var answerButtonsField = typeof(MultipleChoiceUI).GetField("answerButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var buttonStaggerDelayField = typeof(MultipleChoiceUI).GetField("buttonStaggerDelay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var buttonEntranceDurationField = typeof(MultipleChoiceUI).GetField("buttonEntranceDuration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            UnityEngine.UI.Button[] buttons = answerButtonsField?.GetValue(mcUI) as UnityEngine.UI.Button[];
            float staggerDelay = buttonStaggerDelayField != null ? (float)buttonStaggerDelayField.GetValue(mcUI) : 0.1f;
            float entranceDuration = buttonEntranceDurationField != null ? (float)buttonEntranceDurationField.GetValue(mcUI) : 0.3f;

            if (buttons != null)
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

        private void StopAllPreviews()
        {
            if (isAnimationSequencerAvailable)
            {
                StopAnimationSequencer(transitionOutSequencer);
                StopAnimationSequencer(transitionInSequencer);
                StopAnimationSequencer(correctAnswerSequencer);
                StopAnimationSequencer(wrongAnswerSequencer);
                StopAnimationSequencer(hintRevealSequencer);
            }

            DOTween.KillAll();
            EditorApplication.update -= UpdateEditor;
        }

        private void PlayAnimationSequencer(Component sequencer)
        {
            if (sequencer == null || !isAnimationSequencerAvailable) return;

            var playMethod = sequencer.GetType().GetMethod("Play", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (playMethod != null)
            {
                playMethod.Invoke(sequencer, null);
            }
        }

        private void StopAnimationSequencer(Component sequencer)
        {
            if (sequencer == null || !isAnimationSequencerAvailable) return;

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

        private void OnTargetChanged()
        {
            if (quizManager != null && isAnimationSequencerAvailable)
            {
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
            DOTween.ManualUpdate(0.016f, 0.016f);
            SceneView.RepaintAll();
        }

        private void OnDisable()
        {
            StopAllPreviews();
        }
    }
}
#endif
