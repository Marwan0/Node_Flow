#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class UnityEventNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as UnityEventNode;
            if (node == null) return;
            
            var graph = GetGraph(); // Restore missing graph variable
            if (graph == null) return;

            // Fix width
            Container.style.minWidth = 350;

            // Determine context (Runner vs Graph)
            SerializedObject so = null;
            SerializedProperty eventsProp = null;
            bool isSceneContext = false;

            var window = Resources.FindObjectsOfTypeAll<NodeGraphEditorWindow>();
            NodeGraphRunner activeRunner = null;
            
            // Try to get runner from window first
            if (window.Length > 0 && window[0].ActiveRunner != null && window[0].ActiveRunner.Graph == graph)
            {
                activeRunner = window[0].ActiveRunner;
            }
            
            // Fallback: search scene directly if window doesn't have a runner
            if (activeRunner == null)
            {
                var runners = UnityEngine.Object.FindObjectsOfType<NodeGraphRunner>();
                foreach (var runner in runners)
                {
                    if (runner.SourceGraph == graph)
                    {
                        activeRunner = runner;
                        break;
                    }
                }
            }

            if (activeRunner != null)
            {
                // SCENE CONTEXT
                isSceneContext = true;
                
                // Ensure event exists in runner
                activeRunner.GetUnityEvent(node.Guid);
                
                so = new SerializedObject(activeRunner);
                eventsProp = so.FindProperty("_sceneEvents");
                
                var label = new Label($"Scene Context ({activeRunner.name})");
                label.style.color = new Color(0.5f, 1f, 0.5f);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                Container.Add(label);
            }
            else
            {
                // ASSET CONTEXT
                graph.GetUnityEvent(node.Guid);
                so = new SerializedObject(graph);
                eventsProp = so.FindProperty("_nodeEvents");
                
                var label = new Label("Asset Context (Scene Selection Required)");
                label.style.color = new Color(1f, 0.6f, 0.4f);
                label.style.fontSize = 10;
                Container.Add(label);
            }

            if (eventsProp != null)
            {
                int index = -1;
                for (int i = 0; i < eventsProp.arraySize; i++)
                {
                    var element = eventsProp.GetArrayElementAtIndex(i);
                    var guidProp = element.FindPropertyRelative("nodeGuid");
                    if (guidProp.stringValue == node.Guid)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    var element = eventsProp.GetArrayElementAtIndex(index);
                    var eventProp = element.FindPropertyRelative("onEvent");
                    
                    if (eventProp != null)
                    {
                        var field = new PropertyField(eventProp, "Action");
                        field.Bind(so);
                        Container.Add(field);
                    }
                }
                else
                {
                    // Event should exist but wasn't found - show helpful message
                    var helpLabel = new Label("Event configured. Select the Runner in hierarchy to edit.");
                    helpLabel.style.fontSize = 10;
                    helpLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                    helpLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    Container.Add(helpLabel);
                }
            }
        }
    }
}
#endif
