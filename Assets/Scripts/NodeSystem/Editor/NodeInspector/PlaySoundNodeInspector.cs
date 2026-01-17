#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Custom inspector for PlaySoundNode with AudioSource and AudioClip pickers
    /// </summary>
    public class PlaySoundNodeInspector : NodeInspectorBase
    {
        private PlaySoundNode _node;

        public override void DrawInspector()
        {
            _node = Node as PlaySoundNode;
            if (_node == null) return;

            // Audio Clip picker
            CreateLabel("Audio Clip", true);

            // Load current clip from path
            AudioClip currentClip = null;
            if (!string.IsNullOrEmpty(_node.audioClipPath))
            {
                currentClip = AssetDatabase.LoadAssetAtPath<AudioClip>(_node.audioClipPath);
            }

            var clipField = new ObjectField("Clip")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false,
                value = currentClip
            };

            clipField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is AudioClip clip)
                {
                    // Store the asset path for serialization
                    _node.audioClipPath = AssetDatabase.GetAssetPath(clip);
                }
                else
                {
                    _node.audioClipPath = "";
                }
                MarkDirty();
            });
            clipField.style.marginBottom = 5;
            Container.Add(clipField);

            // Show path info
            if (!string.IsNullOrEmpty(_node.audioClipPath))
            {
                var pathLabel = new Label($"Path: {_node.audioClipPath}");
                pathLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                pathLabel.style.fontSize = 9;
                pathLabel.style.marginBottom = 5;
                Container.Add(pathLabel);
            }

            CreateSeparator();

            // Play Mode
            CreateLabel("Play Mode", true);
            
            var modeField = new EnumField("Mode", _node.playMode);
            modeField.RegisterValueChangedCallback(evt =>
            {
                _node.playMode = (AudioPlayMode)evt.newValue;
                MarkDirty();
                // Note: Inspector doesn't auto-refresh like inline content
                // User needs to re-select the node to see updated options
            });
            modeField.style.marginBottom = 5;
            Container.Add(modeField);

            // Audio Source picker (only for PlayOnSource mode)
            if (_node.playMode == AudioPlayMode.PlayOnSource)
            {
                CreateSeparator();
                CreateLabel("Audio Source", true);
                
                var sourceField = new ObjectField("AudioSource")
                {
                    objectType = typeof(AudioSource),
                    allowSceneObjects = true
                };

                if (!string.IsNullOrEmpty(_node.audioSourcePath))
                {
                    var go = GameObject.Find(_node.audioSourcePath);
                    if (go != null)
                    {
                        sourceField.value = go.GetComponent<AudioSource>();
                    }
                }

                sourceField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue is AudioSource source)
                    {
                        _node.audioSourcePath = GetGameObjectPath(source.gameObject);
                    }
                    else
                    {
                        _node.audioSourcePath = "";
                    }
                    MarkDirty();
                });
                sourceField.style.marginBottom = 5;
                Container.Add(sourceField);

                var sourceHint = new Label("Leave empty to use auto-created source");
                sourceHint.style.color = new Color(0.5f, 0.5f, 0.5f);
                sourceHint.style.fontSize = 10;
                sourceHint.style.marginBottom = 5;
                Container.Add(sourceHint);

                // Loop toggle (only for PlayOnSource)
                CreateToggle("Loop", _node.loop, v => _node.loop = v);
            }

            CreateSeparator();

            // Settings
            CreateLabel("Settings", true);
            
            var volumeSlider = new Slider("Volume", 0f, 1f) { value = _node.volume };
            volumeSlider.RegisterValueChangedCallback(evt =>
            {
                _node.volume = evt.newValue;
                MarkDirty();
            });
            volumeSlider.style.marginBottom = 5;
            Container.Add(volumeSlider);

            var pitchSlider = new Slider("Pitch", 0.5f, 2f) { value = _node.pitch };
            pitchSlider.RegisterValueChangedCallback(evt =>
            {
                _node.pitch = evt.newValue;
                MarkDirty();
            });
            pitchSlider.style.marginBottom = 5;
            Container.Add(pitchSlider);

            CreateToggle("Wait For Completion", _node.waitForCompletion, v => _node.waitForCompletion = v);
        }
    }
}
#endif
