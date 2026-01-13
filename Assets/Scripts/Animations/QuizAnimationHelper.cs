using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

namespace QuizSystem
{
    /// <summary>
    /// Helper utilities for quiz animations using DOTween.
    /// Provides reusable animation methods for transitions and feedback.
    /// </summary>
    public static class QuizAnimationHelper
    {
        // Transition durations
        public const float FADE_DURATION = 0.3f;
        public const float SLIDE_DURATION = 0.4f;
        public const float SCALE_DURATION = 0.3f;

        // Feedback durations
        public const float FEEDBACK_DURATION = 0.5f;
        public const float PULSE_DURATION = 0.3f;
        public const float SHAKE_DURATION = 0.4f;

        /// <summary>
        /// Fade out a CanvasGroup (for question transitions)
        /// </summary>
        public static Tween FadeOut(CanvasGroup canvasGroup, float duration = FADE_DURATION, System.Action onComplete = null)
        {
            if (canvasGroup == null) return null;

            canvasGroup.alpha = 1f;
            return canvasGroup.DOFade(0f, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Fade in a CanvasGroup (for question transitions)
        /// </summary>
        public static Tween FadeIn(CanvasGroup canvasGroup, float duration = FADE_DURATION, System.Action onComplete = null)
        {
            if (canvasGroup == null) return null;

            canvasGroup.alpha = 0f;
            return canvasGroup.DOFade(1f, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Slide out a RectTransform (for question transitions)
        /// </summary>
        public static Tween SlideOut(RectTransform rectTransform, Vector2 direction, float duration = SLIDE_DURATION, System.Action onComplete = null)
        {
            if (rectTransform == null) return null;

            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + direction * 1000f; // Slide off screen

            return rectTransform.DOAnchorPos(endPos, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Slide in a RectTransform (for question transitions)
        /// </summary>
        public static Tween SlideIn(RectTransform rectTransform, Vector2 fromDirection, float duration = SLIDE_DURATION, System.Action onComplete = null)
        {
            if (rectTransform == null) return null;

            Vector2 targetPos = rectTransform.anchoredPosition;
            Vector2 startPos = targetPos + fromDirection * 1000f; // Start off screen

            rectTransform.anchoredPosition = startPos;
            return rectTransform.DOAnchorPos(targetPos, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Scale bounce animation (for correct answer feedback)
        /// </summary>
        public static Tween ScaleBounce(Transform transform, float duration = FEEDBACK_DURATION, System.Action onComplete = null)
        {
            if (transform == null) return null;

            Vector3 originalScale = transform.localScale;
            return transform.DOScale(originalScale * 1.2f, duration * 0.3f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(originalScale, duration * 0.7f)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() => onComplete?.Invoke());
                });
        }

        /// <summary>
        /// Shake animation (for wrong answer feedback)
        /// </summary>
        public static Tween Shake(Transform transform, float strength = 10f, float duration = SHAKE_DURATION, System.Action onComplete = null)
        {
            if (transform == null) return null;

            return transform.DOShakePosition(duration, strength, 10, 90f, false, true)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Pulse animation (for correct answer feedback)
        /// </summary>
        public static Tween Pulse(Image image, Color pulseColor, float duration = PULSE_DURATION, System.Action onComplete = null)
        {
            if (image == null) return null;

            Color originalColor = image.color;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(image.DOColor(pulseColor, duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.Append(image.DOColor(originalColor, duration * 0.5f).SetEase(Ease.InQuad));
            sequence.OnComplete(() => onComplete?.Invoke());
            return sequence;
        }

        /// <summary>
        /// Fade in text reveal (for hint panel)
        /// </summary>
        public static Tween FadeInText(TextMeshProUGUI text, float duration = FADE_DURATION, System.Action onComplete = null)
        {
            if (text == null) return null;

            Color originalColor = text.color;
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            return text.DOFade(1f, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Slide up and fade in (for hint panel reveal)
        /// </summary>
        public static Tween SlideUpAndFadeIn(RectTransform rectTransform, CanvasGroup canvasGroup, float duration = SLIDE_DURATION, System.Action onComplete = null)
        {
            if (rectTransform == null || canvasGroup == null) return null;

            Vector2 targetPos = rectTransform.anchoredPosition;
            Vector2 startPos = targetPos + Vector2.down * 50f;

            rectTransform.anchoredPosition = startPos;
            canvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(rectTransform.DOAnchorPos(targetPos, duration).SetEase(Ease.OutQuad));
            sequence.Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
            sequence.OnComplete(() => onComplete?.Invoke());
            return sequence;
        }

        /// <summary>
        /// Staggered entrance animation for multiple buttons
        /// </summary>
        public static void StaggeredButtonEntrance(Button[] buttons, float staggerDelay = 0.1f, float duration = 0.3f)
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].transform != null)
                {
                    Vector3 originalScale = buttons[i].transform.localScale;
                    buttons[i].transform.localScale = Vector3.zero;

                    buttons[i].transform.DOScale(originalScale, duration)
                        .SetDelay(i * staggerDelay)
                        .SetEase(Ease.OutBack);
                }
            }
        }

        /// <summary>
        /// Scale from zero animation (for UI element entrance)
        /// </summary>
        public static Tween ScaleFromZero(Transform transform, float duration = SCALE_DURATION, System.Action onComplete = null)
        {
            if (transform == null) return null;

            Vector3 originalScale = transform.localScale;
            transform.localScale = Vector3.zero;
            return transform.DOScale(originalScale, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}

