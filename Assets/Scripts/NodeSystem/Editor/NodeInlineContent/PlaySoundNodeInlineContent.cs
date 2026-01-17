#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Inline content for PlaySoundNode with proper AudioClip drag-and-drop support.
    /// 
    /// KEY LEARNING: How to handle Unity Object references in nodes
    /// 
    /// The challenge: NodeData is serialized as JSON, but Unity Objects (AudioClip, etc.)
    /// can't be serialized to JSON directly.
    /// 
    /// Solution: Store the ASSET PATH as a string, then:
    /// - In Editor: Use AssetDatabase.LoadAssetAtPath() to load the object
    /// - At Runtime: Use Resources.Load() (clip must be in Resources folder)
    ///              OR use Addressables for more flexibility
    /// </summary>
    public class PlaySoundNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as PlaySoundNode;
            if (node == null) return;

            // === Audio Clip Field ===
            // Load current clip from path for display
            AudioClip currentClip = null;
            if (!string.IsNullOrEmpty(node.audioClipPath))
            {
                currentClip = AssetDatabase.LoadAssetAtPath<AudioClip>(node.audioClipPath);
            }

            CreateLabel("Audio Clip:");
            
            // Create ObjectField for AudioClip
            CreateObjectField<AudioClip>("", currentClip, clip =>
            {
                // When user selects a clip, store its path
                node.audioClipPath = clip != null ? AssetDatabase.GetAssetPath(clip) : "";
                MarkDirty();
            });

            // Show the path (for debugging/info)
            if (!string.IsNullOrEmpty(node.audioClipPath))
            {
                CreateLabel($"Path: {node.audioClipPath}");
            }

            // === Play Mode ===
            CreateEnumField("Mode", node.playMode, v => 
            {
                node.playMode = v;
                RequestRefresh(); // Refresh to show/hide mode-specific options
            });

            // === AudioSource Path (only for PlayOnSource mode) ===
            if (node.playMode == AudioPlayMode.PlayOnSource)
            {
                CreateTextField(node.audioSourcePath, v => node.audioSourcePath = v, "AudioSource path (optional)");
                
                // Loop option (only makes sense with a dedicated source)
                CreateToggle("Loop", node.loop, v => node.loop = v);
            }

            // === Volume & Pitch ===
            CreateSlider("Volume", node.volume, 0f, 1f, v => node.volume = v);
            CreateSlider("Pitch", node.pitch, 0.5f, 2f, v => node.pitch = v);

            // === Wait for Completion ===
            CreateToggle("Wait for completion", node.waitForCompletion, v => node.waitForCompletion = v);

            // === Preview Button (Editor only) ===
            AddPreviewButton(node, currentClip);
        }

        /// <summary>
        /// Adds a button to preview the audio clip in the editor
        /// </summary>
        private void AddPreviewButton(PlaySoundNode node, AudioClip clip)
        {
            if (clip == null) return;

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.marginTop = 5;

            var previewButton = new Button(() =>
            {
                // Use Unity's internal preview system
                PlayClipPreview(clip);
            });
            previewButton.text = "▶ Preview";
            previewButton.style.flexGrow = 1;
            buttonContainer.Add(previewButton);

            var stopButton = new Button(() =>
            {
                StopClipPreview();
            });
            stopButton.text = "■ Stop";
            stopButton.style.width = 50;
            buttonContainer.Add(stopButton);

            // Show clip info
            var infoLabel = new Label($"{clip.length:F2}s | {clip.frequency}Hz");
            infoLabel.style.fontSize = 9;
            infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            infoLabel.style.marginTop = 2;

            Container.Add(buttonContainer);
            Container.Add(infoLabel);
        }

        /// <summary>
        /// Preview audio clip in editor using reflection to access Unity's internal audio preview
        /// </summary>
        private static void PlayClipPreview(AudioClip clip)
        {
            // Use reflection to access Unity's internal AudioUtil class
            var audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass != null)
            {
                var method = audioUtilClass.GetMethod(
                    "PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );
                
                if (method != null)
                {
                    method.Invoke(null, new object[] { clip, 0, false });
                    return;
                }

                // Try alternate signature (older Unity versions)
                method = audioUtilClass.GetMethod(
                    "PlayClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new System.Type[] { typeof(AudioClip) },
                    null
                );
                
                if (method != null)
                {
                    method.Invoke(null, new object[] { clip });
                }
            }
        }

        private static void StopClipPreview()
        {
            var audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilClass != null)
            {
                var method = audioUtilClass.GetMethod(
                    "StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                );
                
                method?.Invoke(null, null);
            }
        }
    }
}
#endif
