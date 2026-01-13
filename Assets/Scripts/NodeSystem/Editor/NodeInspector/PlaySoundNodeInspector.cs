#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
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

            // Audio Source picker
            CreateLabel("Audio Source", true);
            
            var sourceField = new ObjectField("AudioSource (Optional)")
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

            var sourceHint = new Label("Leave empty to use any AudioSource in scene");
            sourceHint.style.color = new Color(0.5f, 0.5f, 0.5f);
            sourceHint.style.fontSize = 10;
            sourceHint.style.marginBottom = 10;
            Container.Add(sourceHint);

            CreateSeparator();

            // Audio Clip
            CreateLabel("Audio Clip", true);

            var clipField = new ObjectField("Clip")
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };

            // Try to load clip from Resources
            if (!string.IsNullOrEmpty(_node.clipName))
            {
                clipField.value = Resources.Load<AudioClip>(_node.clipName);
            }

            clipField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is AudioClip clip)
                {
                    // Store the asset name (for Resources.Load)
                    _node.clipName = clip.name;
                }
                else
                {
                    _node.clipName = "";
                }
                MarkDirty();
            });
            clipField.style.marginBottom = 5;
            Container.Add(clipField);

            var clipHint = new Label("Clip must be in a Resources folder");
            clipHint.style.color = new Color(0.5f, 0.5f, 0.5f);
            clipHint.style.fontSize = 10;
            clipHint.style.marginBottom = 10;
            Container.Add(clipHint);

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

            CreateToggle("Wait For Completion", _node.waitForCompletion, v => _node.waitForCompletion = v);
        }
    }
}
#endif

