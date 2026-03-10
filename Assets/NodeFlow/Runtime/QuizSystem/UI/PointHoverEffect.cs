using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace QuizSystem
{
    public enum HoverConfigMode { Asset, Inline }

    [DisallowMultipleComponent]
    public class PointHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Configuration")]
        [Tooltip("Use an existing PointHoverConfig asset, or define settings inline on this component.")]
        [SerializeField] private HoverConfigMode configMode = HoverConfigMode.Asset;

        [Tooltip("Hover config asset (used when Mode = Asset).")]
        [SerializeField] private PointHoverConfig config;

        [Header("Inline Settings (used when Mode = Inline)")]
        [SerializeField] private Color inlineIdleColor = Color.white;
        [SerializeField] private Sprite inlineIdleSprite;
        [SerializeField] private Color inlineHoverColor = new Color(0.8f, 0.9f, 1f, 1f);
        [SerializeField] private Sprite inlineHoverSprite;
        [SerializeField] private AudioClip inlineHoverSFX;
        [Range(0f, 1f)]
        [SerializeField] private float inlineTransitionDuration = 0.15f;
        [SerializeField] private Ease inlineTransitionEase = Ease.OutQuad;
        [SerializeField] private bool inlineEnableScalePunch = false;
        [Range(1f, 1.5f)]
        [SerializeField] private float inlineScalePunchAmount = 1.08f;
        [Range(0.05f, 0.5f)]
        [SerializeField] private float inlineScalePunchDuration = 0.15f;
        [Range(0f, 1f)]
        [SerializeField] private float inlineSfxVolume = 1f;

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
        private bool _feedbackActive;
        private bool _allowOverlap;

        // --- Accessors: read from config asset or inline fields ---
        private bool HasConfig => configMode == HoverConfigMode.Asset ? config != null : true;
        private Color IdleColor => configMode == HoverConfigMode.Asset ? config.idleColor : inlineIdleColor;
        private Sprite IdleSprite => configMode == HoverConfigMode.Asset ? config.idleSprite : inlineIdleSprite;
        private Color HoverColor => configMode == HoverConfigMode.Asset ? config.hoverColor : inlineHoverColor;
        private Sprite HoverSprite => configMode == HoverConfigMode.Asset ? config.hoverSprite : inlineHoverSprite;
        private AudioClip HoverSFX => configMode == HoverConfigMode.Asset ? config.hoverSFX : inlineHoverSFX;
        private float TransitionDuration => configMode == HoverConfigMode.Asset ? config.transitionDuration : inlineTransitionDuration;
        private Ease TransitionEase => configMode == HoverConfigMode.Asset ? config.transitionEase : inlineTransitionEase;
        private bool EnableScalePunch => configMode == HoverConfigMode.Asset ? config.enableScalePunch : inlineEnableScalePunch;
        private float ScalePunchAmount => configMode == HoverConfigMode.Asset ? config.scalePunchAmount : inlineScalePunchAmount;
        private float ScalePunchDuration => configMode == HoverConfigMode.Asset ? config.scalePunchDuration : inlineScalePunchDuration;
        private float SfxVolume => configMode == HoverConfigMode.Asset ? config.sfxVolume : inlineSfxVolume;

        public void SetConfig(PointHoverConfig hoverConfig)
        {
            config = hoverConfig;
            configMode = HoverConfigMode.Asset;
            if (_initialized) CaptureOriginalState();
        }

        public void SetSharedAudioSource(AudioSource audioSource)
        {
            _sharedAudioSource = audioSource;

            // Destroy any per-button standalone source — all audio goes through the shared one now
            if (_sharedAudioSource != null && standaloneAudioSource != null)
            {
                Destroy(standaloneAudioSource);
                standaloneAudioSource = null;
            }
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

        /// <summary>
        /// When active, blocks all hover enter/exit so answer feedback visuals aren't overridden.
        /// </summary>
        public void SetFeedbackActive(bool active)
        {
            _feedbackActive = active;
            if (active)
            {
                KillTweens();
                _isHovered = false;
            }
        }

        public void SetAudioOverlapAllowed(bool allow)
        {
            _allowOverlap = allow;
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
            if (!HasConfig || _image == null || _feedbackActive) return;
            _isHovered = true;
            TransitionToHover();
            PlayHoverSFX();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!HasConfig || _image == null || _feedbackActive) return;
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
                _originalColor = (HasConfig && IdleColor != Color.white)
                    ? IdleColor
                    : _image.color;
                _originalSprite = (HasConfig && IdleSprite != null)
                    ? IdleSprite
                    : _image.sprite;
            }
            _originalScale = transform.localScale;
        }

        private void TransitionToHover()
        {
            KillTweens();

            if (HoverSprite != null && _image != null)
                _image.sprite = HoverSprite;

            if (_image != null)
            {
                if (TransitionDuration > 0f)
                {
                    _colorTween = _image.DOColor(HoverColor, TransitionDuration)
                        .SetEase(TransitionEase)
                        .SetTarget(_image);
                }
                else
                {
                    _image.color = HoverColor;
                }
            }

            if (EnableScalePunch)
            {
                _scaleTween = transform.DOScale(
                    _originalScale * ScalePunchAmount,
                    ScalePunchDuration
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
                if (HasConfig && TransitionDuration > 0f)
                {
                    _colorTween = _image.DOColor(_originalColor, TransitionDuration)
                        .SetEase(TransitionEase)
                        .SetTarget(_image);
                }
                else
                {
                    _image.color = _originalColor;
                }
            }

            if (HasConfig && EnableScalePunch)
            {
                _scaleTween = transform.DOScale(
                    _originalScale,
                    ScalePunchDuration
                ).SetEase(Ease.InQuad).SetTarget(transform);
            }
        }

        private void PlayHoverSFX()
        {
            if (HoverSFX == null) return;

            AudioSource source = _sharedAudioSource != null ? _sharedAudioSource : standaloneAudioSource;
            if (source == null)
            {
                // Auto-create an AudioSource for standalone use
                standaloneAudioSource = gameObject.AddComponent<AudioSource>();
                standaloneAudioSource.playOnAwake = false;
                standaloneAudioSource.loop = false;
                source = standaloneAudioSource;
            }

            if (_allowOverlap)
            {
                source.PlayOneShot(HoverSFX, SfxVolume);
            }
            else
            {
                source.Stop();
                source.clip = HoverSFX;
                source.volume = SfxVolume;
                source.Play();
            }
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
