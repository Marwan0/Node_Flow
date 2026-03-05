using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace QuizSystem
{
    [DisallowMultipleComponent]
    public class PointHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Configuration")]
        [Tooltip("Hover config asset. If null, no hover effects are applied.")]
        [SerializeField] private PointHoverConfig config;

        [Header("Override Target (Optional)")]
        [Tooltip("If set, hover effects apply to this Image instead of the Image on this GameObject.")]
        [SerializeField] private Image targetImage;

        [Header("Standalone Audio (Optional)")]
        [Tooltip("Assign an AudioSource directly for standalone use (outside QuestionUI). " +
                 "When used inside QuestionUI, this is set automatically via SetSharedAudioSource.")]
        [SerializeField] private AudioSource standaloneAudioSource;

        private Image _image;
        private Color _originalColor;
        private Sprite _originalSprite;
        private Vector3 _originalScale;
        private AudioSource _sharedAudioSource;
        private Tweener _colorTween;
        private Tweener _scaleTween;
        private bool _isHovered;
        private bool _initialized;

        public void SetConfig(PointHoverConfig hoverConfig)
        {
            config = hoverConfig;
            if (_initialized) CaptureOriginalState();
        }

        public void SetSharedAudioSource(AudioSource audioSource)
        {
            _sharedAudioSource = audioSource;
        }

        public void RecaptureIdleState()
        {
            CaptureOriginalState();
        }

        public void ForceExitHover()
        {
            if (_isHovered)
            {
                _isHovered = false;
                TransitionToIdle();
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            KillTweens();
            if (_image != null && _initialized)
            {
                _image.color = _originalColor;
                if (_originalSprite != null) _image.sprite = _originalSprite;
                transform.localScale = _originalScale;
            }
            _isHovered = false;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (config == null || _image == null) return;
            _isHovered = true;
            TransitionToHover();
            PlayHoverSFX();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (config == null || _image == null) return;
            _isHovered = false;
            TransitionToIdle();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _image = targetImage != null ? targetImage : GetComponent<Image>();
            if (_image == null)
                _image = GetComponentInChildren<Image>();
            CaptureOriginalState();
            _initialized = true;
        }

        private void CaptureOriginalState()
        {
            if (_image != null)
            {
                _originalColor = (config != null && config.idleColor != Color.white)
                    ? config.idleColor
                    : _image.color;
                _originalSprite = (config != null && config.idleSprite != null)
                    ? config.idleSprite
                    : _image.sprite;
            }
            _originalScale = transform.localScale;
        }

        private void TransitionToHover()
        {
            KillTweens();

            if (config.hoverSprite != null && _image != null)
                _image.sprite = config.hoverSprite;

            if (_image != null)
            {
                if (config.transitionDuration > 0f)
                {
                    _colorTween = _image.DOColor(config.hoverColor, config.transitionDuration)
                        .SetEase(config.transitionEase)
                        .SetTarget(_image);
                }
                else
                {
                    _image.color = config.hoverColor;
                }
            }

            if (config.enableScalePunch)
            {
                _scaleTween = transform.DOScale(
                    _originalScale * config.scalePunchAmount,
                    config.scalePunchDuration
                ).SetEase(Ease.OutBack).SetTarget(transform);
            }
        }

        private void TransitionToIdle()
        {
            KillTweens();

            if (_originalSprite != null && _image != null)
                _image.sprite = _originalSprite;

            if (_image != null)
            {
                if (config != null && config.transitionDuration > 0f)
                {
                    _colorTween = _image.DOColor(_originalColor, config.transitionDuration)
                        .SetEase(config.transitionEase)
                        .SetTarget(_image);
                }
                else
                {
                    _image.color = _originalColor;
                }
            }

            if (config != null && config.enableScalePunch)
            {
                _scaleTween = transform.DOScale(
                    _originalScale,
                    config.scalePunchDuration
                ).SetEase(Ease.InQuad).SetTarget(transform);
            }
        }

        private void PlayHoverSFX()
        {
            if (config.hoverSFX == null) return;

            AudioSource source = _sharedAudioSource != null ? _sharedAudioSource : standaloneAudioSource;
            if (source == null)
            {
                // Auto-create an AudioSource for standalone use
                standaloneAudioSource = gameObject.AddComponent<AudioSource>();
                standaloneAudioSource.playOnAwake = false;
                standaloneAudioSource.loop = false;
                source = standaloneAudioSource;
            }

            source.Stop();
            source.clip = config.hoverSFX;
            source.volume = config.sfxVolume;
            source.Play();
        }

        private void KillTweens()
        {
            _colorTween?.Kill();
            _colorTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
        }
    }
}
