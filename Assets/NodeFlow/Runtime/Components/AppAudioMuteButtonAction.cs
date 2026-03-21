using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Global app audio mute/unmute helper.
    /// Attach to a GameObject and wire public methods to a Button OnClick event.
    /// </summary>
    [AddComponentMenu("Node System/UI/App Audio Mute Button Action")]
    public class AppAudioMuteButtonAction : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool startMuted;

        private const string MutePrefsKey = "AppAudioMuted";
        private float _lastVolumeBeforeMute = 1f;

        public bool IsMuted => AudioListener.pause || AudioListener.volume <= 0f;

        private void Awake()
        {
            bool muted = PlayerPrefs.GetInt(MutePrefsKey, startMuted ? 1 : 0) == 1;
            ApplyMuteState(muted);
        }

        /// <summary>
        /// Mutes all app audio.
        /// </summary>
        public void MuteAudio()
        {
            ApplyMuteState(true);
        }

        /// <summary>
        /// Unmutes app audio.
        /// </summary>
        public void UnmuteAudio()
        {
            ApplyMuteState(false);
        }

        /// <summary>
        /// Toggles global app audio mute state.
        /// </summary>
        public void ToggleMute()
        {
            ApplyMuteState(!IsMuted);
        }

        private void ApplyMuteState(bool muted)
        {
            if (muted)
            {
                // Cache volume once before muting, so we can restore the user's previous level.
                if (AudioListener.volume > 0f)
                {
                    _lastVolumeBeforeMute = AudioListener.volume;
                }

                AudioListener.pause = true;
                AudioListener.volume = 0f;
            }
            else
            {
                AudioListener.pause = false;
                AudioListener.volume = Mathf.Clamp01(_lastVolumeBeforeMute <= 0f ? 1f : _lastVolumeBeforeMute);
            }

            PlayerPrefs.SetInt(MutePrefsKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
