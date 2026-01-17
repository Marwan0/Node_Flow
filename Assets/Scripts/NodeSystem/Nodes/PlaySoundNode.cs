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
        // Audio clip - stored as asset path for serialization
        [SerializeField]
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

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
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

            if (string.IsNullOrEmpty(audioClipPath)) return null;

#if UNITY_EDITOR
            // In editor, load directly from asset path
            _runtimeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
#else
            // At runtime, try Resources
            // Extract filename from path for Resources.Load
            string resourcePath = System.IO.Path.GetFileNameWithoutExtension(audioClipPath);
            _runtimeClip = Resources.Load<AudioClip>(resourcePath);
            
            if (_runtimeClip == null)
            {
                // Try the full path without extension
                resourcePath = audioClipPath.Replace("Assets/Resources/", "").Replace(".wav", "").Replace(".mp3", "").Replace(".ogg", "");
                _runtimeClip = Resources.Load<AudioClip>(resourcePath);
            }
#endif
            return _runtimeClip;
        }

        private void PlayOneShot(AudioClip clip)
        {
            // Get or create shared AudioSource
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

            // Create new
            var audioObject = new GameObject("NodeGraph_AudioSource");
            audioObject.hideFlags = HideFlags.DontSave; // Don't save in scene
            _sharedAudioSource = audioObject.AddComponent<AudioSource>();
            _sharedAudioSource.playOnAwake = false;
            
            // Keep alive across scenes (optional - remove if you want scene-specific audio)
            UnityEngine.Object.DontDestroyOnLoad(audioObject);

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
