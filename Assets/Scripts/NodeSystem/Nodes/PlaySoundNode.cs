using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Plays an audio clip
    /// </summary>
    [Serializable]
    public class PlaySoundNode : NodeData
    {
        [SerializeField]
        public string audioSourcePath = "";
        
        [SerializeField]
        public string clipName = "";
        
        [SerializeField]
        public float volume = 1f;
        
        [SerializeField]
        public bool waitForCompletion = false;

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
            AudioSource audioSource = null;

            // Find AudioSource
            if (!string.IsNullOrEmpty(audioSourcePath))
            {
                var sourceObj = GameObject.Find(audioSourcePath);
                if (sourceObj != null)
                {
                    audioSource = sourceObj.GetComponent<AudioSource>();
                }
            }

            // If no specific AudioSource, try to find one or create
            if (audioSource == null)
            {
                audioSource = UnityEngine.Object.FindObjectOfType<AudioSource>();
            }

            if (audioSource == null)
            {
                Debug.LogWarning("[PlaySoundNode] No AudioSource found");
                Complete();
                return;
            }

            // Load clip from Resources if specified
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(clipName))
            {
                clip = Resources.Load<AudioClip>(clipName);
                if (clip == null)
                {
                    // Try loading from AudioSource's clips
                    Debug.LogWarning($"[PlaySoundNode] Could not load clip: {clipName}");
                }
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
                Debug.Log($"[PlaySoundNode] Playing clip: {clipName}");
                
                if (waitForCompletion)
                {
                    Runner?.StartCoroutine(WaitForClip(clip.length));
                    return;
                }
            }
            else if (audioSource.clip != null)
            {
                // Play the AudioSource's default clip
                audioSource.volume = volume;
                audioSource.Play();
                Debug.Log("[PlaySoundNode] Playing AudioSource default clip");
                
                if (waitForCompletion && audioSource.clip != null)
                {
                    Runner?.StartCoroutine(WaitForClip(audioSource.clip.length));
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[PlaySoundNode] No audio clip to play");
            }

            Complete();
        }

        private System.Collections.IEnumerator WaitForClip(float duration)
        {
            yield return new WaitForSeconds(duration);
            Complete();
        }
    }
}

