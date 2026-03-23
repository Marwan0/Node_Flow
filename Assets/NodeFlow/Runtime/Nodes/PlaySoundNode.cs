using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    public enum AudioPlayMode
    {
        PlayOneShot,        // Fire and forget - best for SFX
        PlayOnSource,       // Play on a specific AudioSource
        PlayAtPoint         // Play at a 3D position
    }

    /// <summary>
    /// Plays an audio clip with multiple playback options.
    /// For linear scenarios, uses auto-created AudioSource or PlayOneShot.
    /// </summary>
    [Serializable]
    public class PlaySoundNode : NodeData
    {
        [Header("Audio Clip")]
        [SerializeField]
        [Tooltip("Direct reference to the AudioClip asset. Drag from Project window. This works in WebGL builds without needing Resources folder.")]
        public AudioClip audioClipRef;

        [SerializeField]
        [Tooltip("Asset path (auto-synced from reference, used as fallback)")]
        public string audioClipPath = "";

        // Playback mode
        [SerializeField]
        public AudioPlayMode playMode = AudioPlayMode.PlayOneShot;

        // Optional AudioSource path (only for PlayOnSource mode)
        [SerializeField]
        public string audioSourcePath = "";

        // Volume
        [SerializeField]
        public float volume = 1f;

        // Pitch (1 = normal)
        [SerializeField]
        public float pitch = 1f;

        // Wait for clip to finish before continuing
        [SerializeField]
        public bool waitForCompletion = false;

        // Loop the audio (only for PlayOnSource mode)
        [SerializeField]
        public bool loop = false;

        // Runtime reference to the clip (loaded at runtime)
        [NonSerialized]
        private AudioClip _runtimeClip;

        // Shared AudioSource for one-shot playback
        private static AudioSource _sharedAudioSource;

        public override string Name => "Play Sound";
        public override Color Color => new Color(0.8f, 0.5f, 0.2f); // Orange
        public override string Category => "Audio";
        public override string Description => "Plays a sound clip via an AudioSource. Optionally blocks execution until the clip finishes playing.";

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
            // Load the AudioClip
            AudioClip clip = LoadAudioClip();

            if (clip == null)
            {
                Debug.LogWarning($"[PlaySoundNode] No audio clip to play. Path: {audioClipPath}");
                Complete();
                return;
            }

            float clipDuration = clip.length;

            switch (playMode)
            {
                case AudioPlayMode.PlayOneShot:
                    PlayOneShot(clip);
                    break;

                case AudioPlayMode.PlayOnSource:
                    clipDuration = PlayOnSource(clip);
                    break;

                case AudioPlayMode.PlayAtPoint:
                    AudioSource.PlayClipAtPoint(clip, Vector3.zero, volume);
                    Debug.Log($"[PlaySoundNode] Playing at point: {clip.name}");
                    break;
            }

            // Wait for completion or continue immediately
            if (waitForCompletion && clipDuration > 0)
            {
                Runner?.StartCoroutine(WaitForClip(clipDuration));
            }
            else
            {
                Complete();
            }
        }

        private AudioClip LoadAudioClip()
        {
            // Return cached clip if already loaded
            if (_runtimeClip != null) return _runtimeClip;

#if UNITY_EDITOR
            // In editor, prefer direct reference, fallback to path
            _runtimeClip = audioClipRef;
            if (_runtimeClip == null && !string.IsNullOrEmpty(audioClipPath))
            {
                _runtimeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            }
#else
            // At runtime, try multiple sources in order:
            // 1. Direct reference (might be null in WebGL)
            _runtimeClip = audioClipRef;

            // 2. Try NodeGraph's separate storage (works in WebGL)
            if (_runtimeClip == null && Runner != null && Runner.Graph != null)
            {
                var storedRef = Runner.Graph.GetNodeAssetReference(Guid);
                if (storedRef is AudioClip storedClip)
                {
                    _runtimeClip = storedClip;
                    audioClipRef = storedClip; // Cache it for next time
                    Debug.Log($"[PlaySoundNode] Restored audio clip from NodeGraph storage: {storedClip.name}");
                }
            }

            // 3. If still null, try Resources as fallback
            if (_runtimeClip == null && !string.IsNullOrEmpty(audioClipPath))
            {
                // Convert path to Resources path
                string resourcePath = audioClipPath
                    .Replace("Assets/", "")
                    .Replace("Resources/", "")
                    .Replace(".wav", "")
                    .Replace(".mp3", "")
                    .Replace(".ogg", "");

                _runtimeClip = Resources.Load<AudioClip>(resourcePath);

                // Try filename only
                if (_runtimeClip == null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(audioClipPath);
                    _runtimeClip = Resources.Load<AudioClip>(fileName);
                }

                // Try common Resources subfolders
                if (_runtimeClip == null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(audioClipPath);
                    _runtimeClip = Resources.Load<AudioClip>($"Audio/{fileName}");
                    if (_runtimeClip == null)
                    {
                        _runtimeClip = Resources.Load<AudioClip>($"Sounds/{fileName}");
                    }
                }

                if (_runtimeClip != null)
                {
                    Debug.Log($"[PlaySoundNode] Loaded audio clip from Resources: {_runtimeClip.name}");
                }
            }
#endif
            return _runtimeClip;
        }

        private void PlayOneShot(AudioClip clip)
        {
            // When a quiz is active, route through the centralized QuizState audio source
            // so all quiz sounds (hover, feedback, node sounds) share one source and respect overlap settings
            var quizState = QuizSystem.QuizState.Instance;
            if (quizState != null && quizState.quizActive)
            {
                quizState.PlaySound(clip, volume, pitch);
                Debug.Log($"[PlaySoundNode] PlayOneShot via QuizState: {clip.name}");
                return;
            }

            // Fallback: non-quiz context — use the static shared AudioSource
            AudioSource source = GetOrCreateSharedAudioSource();

            if (source != null)
            {
                source.pitch = pitch;
                source.PlayOneShot(clip, volume);
                Debug.Log($"[PlaySoundNode] PlayOneShot: {clip.name}");
            }
        }

        private float PlayOnSource(AudioClip clip)
        {
            AudioSource source = FindAudioSource();

            if (source == null)
            {
                Debug.LogWarning($"[PlaySoundNode] AudioSource not found: {audioSourcePath}. Using shared source.");
                source = GetOrCreateSharedAudioSource();
            }

            if (source != null)
            {
                source.clip = clip;
                source.volume = volume;
                source.pitch = pitch;
                source.loop = loop;
                source.Play();
                Debug.Log($"[PlaySoundNode] Playing on source: {clip.name}");

                return loop ? 0 : clip.length / pitch; // Don't wait if looping
            }

            return 0;
        }

        private AudioSource FindAudioSource()
        {
            if (string.IsNullOrEmpty(audioSourcePath)) return null;

            var sourceObj = GameObject.Find(audioSourcePath);
            if (sourceObj != null)
            {
                return sourceObj.GetComponent<AudioSource>();
            }

            return null;
        }

        /// <summary>
        /// Gets or creates a shared AudioSource for one-shot playback.
        /// This is perfect for linear scenarios - no need to manually set up AudioSources.
        /// </summary>
        private static AudioSource GetOrCreateSharedAudioSource()
        {
            // Check if shared source still exists
            if (_sharedAudioSource != null) return _sharedAudioSource;

            // Try to find existing
            var existing = GameObject.Find("NodeGraph_AudioSource");
            if (existing != null)
            {
                _sharedAudioSource = existing.GetComponent<AudioSource>();
                if (_sharedAudioSource != null) return _sharedAudioSource;
            }

            // Create new — lives in the scene so it's destroyed on scene reload
            var audioObject = new GameObject("NodeGraph_AudioSource");
            audioObject.hideFlags = HideFlags.DontSave;
            _sharedAudioSource = audioObject.AddComponent<AudioSource>();
            _sharedAudioSource.playOnAwake = false;

            Debug.Log("[PlaySoundNode] Created shared AudioSource");
            return _sharedAudioSource;
        }

        private IEnumerator WaitForClip(float duration)
        {
            yield return new WaitForSeconds(duration);
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            _runtimeClip = null;
        }
    }
}
