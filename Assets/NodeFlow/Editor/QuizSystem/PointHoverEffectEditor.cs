using UnityEditor;
using UnityEngine;

namespace QuizSystem
{
    [CustomEditor(typeof(PointHoverEffect))]
    public class PointHoverEffectEditor : Editor
    {
        private SerializedProperty _configMode;
        private SerializedProperty _config;

        // Inline fields
        private SerializedProperty _inlineIdleColor;
        private SerializedProperty _inlineIdleSprite;
        private SerializedProperty _inlineHoverColor;
        private SerializedProperty _inlineHoverSprite;
        private SerializedProperty _inlineHoverSFX;
        private SerializedProperty _inlineTransitionDuration;
        private SerializedProperty _inlineTransitionEase;
        private SerializedProperty _inlineEnableScalePunch;
        private SerializedProperty _inlineScalePunchAmount;
        private SerializedProperty _inlineScalePunchDuration;
        private SerializedProperty _inlineSfxVolume;

        // Shared fields
        private SerializedProperty _targetImage;
        private SerializedProperty _standaloneAudioSource;

        private void OnEnable()
        {
            _configMode = serializedObject.FindProperty("configMode");
            _config = serializedObject.FindProperty("config");

            _inlineIdleColor = serializedObject.FindProperty("inlineIdleColor");
            _inlineIdleSprite = serializedObject.FindProperty("inlineIdleSprite");
            _inlineHoverColor = serializedObject.FindProperty("inlineHoverColor");
            _inlineHoverSprite = serializedObject.FindProperty("inlineHoverSprite");
            _inlineHoverSFX = serializedObject.FindProperty("inlineHoverSFX");
            _inlineTransitionDuration = serializedObject.FindProperty("inlineTransitionDuration");
            _inlineTransitionEase = serializedObject.FindProperty("inlineTransitionEase");
            _inlineEnableScalePunch = serializedObject.FindProperty("inlineEnableScalePunch");
            _inlineScalePunchAmount = serializedObject.FindProperty("inlineScalePunchAmount");
            _inlineScalePunchDuration = serializedObject.FindProperty("inlineScalePunchDuration");
            _inlineSfxVolume = serializedObject.FindProperty("inlineSfxVolume");

            _targetImage = serializedObject.FindProperty("targetImage");
            _standaloneAudioSource = serializedObject.FindProperty("standaloneAudioSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_configMode, new GUIContent("Config Mode"));

            bool isAsset = (HoverConfigMode)_configMode.enumValueIndex == HoverConfigMode.Asset;

            if (isAsset)
            {
                EditorGUILayout.PropertyField(_config, new GUIContent("Config Asset"));
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Idle State", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_inlineIdleColor, new GUIContent("Idle Color"));
                EditorGUILayout.PropertyField(_inlineIdleSprite, new GUIContent("Idle Sprite"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Hover State", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_inlineHoverColor, new GUIContent("Hover Color"));
                EditorGUILayout.PropertyField(_inlineHoverSprite, new GUIContent("Hover Sprite"));
                EditorGUILayout.PropertyField(_inlineHoverSFX, new GUIContent("Hover SFX"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_inlineTransitionDuration, new GUIContent("Duration"));
                EditorGUILayout.PropertyField(_inlineTransitionEase, new GUIContent("Ease"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Scale Punch", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_inlineEnableScalePunch, new GUIContent("Enable"));
                if (_inlineEnableScalePunch.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_inlineScalePunchAmount, new GUIContent("Amount"));
                    EditorGUILayout.PropertyField(_inlineScalePunchDuration, new GUIContent("Duration"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("SFX Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_inlineSfxVolume, new GUIContent("Volume"));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Optional", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_targetImage, new GUIContent("Target Image Override"));
            EditorGUILayout.PropertyField(_standaloneAudioSource, new GUIContent("Standalone Audio Source"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
