#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NodeSystem.Editor
{
    [CustomEditor(typeof(NodeGraphRunner))]
    public class NodeGraphRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all properties except _sceneEvents
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                // Skip script field and _sceneEvents
                if (prop.name == "m_Script" || prop.name == "_sceneEvents")
                    continue;

                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();

            // Add cleanup button
            EditorGUILayout.Space();
            var runner = target as NodeGraphRunner;
            if (runner != null)
            {
                var sceneEventsProp = serializedObject.FindProperty("_sceneEvents");
                int eventCount = sceneEventsProp != null ? sceneEventsProp.arraySize : 0;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Scene Events: {eventCount}", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Clean Up Orphaned Events", GUILayout.Width(200)))
                {
                    runner.CleanupSceneEvents();
                }
                EditorGUILayout.EndHorizontal();
                
                if (eventCount > 0)
                {
                    EditorGUILayout.HelpBox("Scene Events are managed automatically by Unity Event nodes. Use the cleanup button to remove events for deleted nodes.", MessageType.Info);
                }
            }
        }
    }
}
#endif

