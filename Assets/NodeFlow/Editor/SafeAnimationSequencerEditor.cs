#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Safe wrapper for AnimationSequencerController editor that prevents NullReferenceException
    /// from the buggy package editor. This editor takes priority over the package's custom editor.
    /// </summary>
    [CustomEditor(typeof(BrunoMikoski.AnimationSequencer.AnimationSequencerController), true)]
    [CanEditMultipleObjects]
    public class SafeAnimationSequencerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _internalEditor;
        private Type _originalEditorType;
        private bool _initFailed = false;

        private void OnEnable()
        {
            if (target == null) 
            {
                _initFailed = true;
                return;
            }

            try
            {
                // Find the original custom editor type from the package
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    _originalEditorType = assembly.GetType("BrunoMikoski.AnimationSequencer.AnimationSequencerControllerCustomEditor");
                    if (_originalEditorType != null) break;
                }

                // Don't create the internal editor here - let OnInspectorGUI handle it lazily
                _initFailed = false;
            }
            catch
            {
                _initFailed = true;
            }
        }

        private void OnDisable()
        {
            SafeDestroyInternalEditor();
        }

        private void OnDestroy()
        {
            SafeDestroyInternalEditor();
        }

        private void SafeDestroyInternalEditor()
        {
            if (_internalEditor != null)
            {
                try
                {
                    // Use delayed destruction to avoid OnDisable issues
                    var editor = _internalEditor;
                    _internalEditor = null;
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            if (editor != null)
                                DestroyImmediate(editor);
                        }
                        catch { }
                    };
                }
                catch { }
            }
        }

        public override void OnInspectorGUI()
        {
            if (_initFailed || target == null)
            {
                EditorGUILayout.HelpBox("Animation Sequencer target is null or initialization failed.", MessageType.Warning);
                return;
            }

            // Create internal editor lazily if needed
            if (_internalEditor == null && _originalEditorType != null && target != null)
            {
                try
                {
                    _internalEditor = CreateEditor(targets, _originalEditorType);
                }
                catch
                {
                    _internalEditor = null;
                }
            }

            // Try to draw with internal editor, fall back to default if it fails
            if (_internalEditor != null)
            {
                try
                {
                    _internalEditor.OnInspectorGUI();
                }
                catch
                {
                    // If the internal editor fails, fall back to default property drawing
                    DrawDefaultInspectorSafe();
                }
            }
            else
            {
                // Fall back to default property drawing
                DrawDefaultInspectorSafe();
            }
        }

        private void DrawDefaultInspectorSafe()
        {
            try
            {
                serializedObject.Update();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Sequencer", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // Draw all serialized properties
                SerializedProperty prop = serializedObject.GetIterator();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script") continue; // Skip script field
                        EditorGUILayout.PropertyField(prop, true);
                    }
                    while (prop.NextVisible(false));
                }

                serializedObject.ApplyModifiedProperties();
            }
            catch
            {
                EditorGUILayout.HelpBox("Could not draw Animation Sequencer properties.", MessageType.Warning);
            }
        }

        public override bool HasPreviewGUI()
        {
            if (_internalEditor != null)
            {
                try { return _internalEditor.HasPreviewGUI(); } catch { }
            }
            return false;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (_internalEditor != null)
            {
                try { _internalEditor.OnPreviewGUI(r, background); } catch { }
            }
        }
    }
}
#endif
