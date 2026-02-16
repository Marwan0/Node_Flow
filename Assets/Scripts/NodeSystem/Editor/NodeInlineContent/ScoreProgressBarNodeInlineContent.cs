#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class ScoreProgressBarNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ScoreProgressBarNode;
            if (node == null) return;

            CreateLabel("Target (drag Slider, Image, or GameObject)");
            CreateObjectField<UnityEngine.Object>("Target", node.targetRef, v =>
            {
                node.targetRef = v;
                if (v != null)
                {
                    Transform t = v is GameObject go ? go.transform : (v as Component)?.transform;
                    if (t != null)
                    {
                        node.targetPath = GetHierarchyPath(t);
                        // Save the graph to persist the path
                        var graph = GetNodeGraph();
                        if (graph != null)
                        {
                            graph.SaveToJson();
                            UnityEditor.EditorUtility.SetDirty(graph);
                        }
                    }
                }
                // Don't clear targetPath when v is null - keep it for restoration
                MarkDirty();
            });

            CreateEnumField("Value from", node.valueSource, v =>
            {
                node.valueSource = v;
                RequestRefresh();
            });

            if (node.valueSource == ScoreProgressBarNode.ValueSource.Variable)
            {
                CreateVariableSelector("Value var", node.valueVariableName, v => node.valueVariableName = v);
            }

            if (node.valueSource == ScoreProgressBarNode.ValueSource.QuizScore)
            {
                CreateToggle("Use quiz range (0 to Start Quiz max)", node.useQuizRange, v =>
                {
                    node.useQuizRange = v;
                    RequestRefresh();
                });
            }

            if (node.valueSource != ScoreProgressBarNode.ValueSource.QuizScore || !node.useQuizRange)
            {
                CreateLabel("Min");
                CreateFloatField("", node.minLiteral, v => node.minLiteral = v);
                CreateVariableSelector("Min var (optional)", node.minVariableName, v => node.minVariableName = v);

                CreateLabel("Max");
                CreateFloatField("", node.maxLiteral, v => node.maxLiteral = v);
                CreateVariableSelector("Max var (optional)", node.maxVariableName, v => node.maxVariableName = v);
            }

            CreateToggle("Animate fill (lerp)", node.animateFill, v =>
            {
                node.animateFill = v;
                RequestRefresh();
            });
            if (node.animateFill)
                CreateFloatField("Duration (s)", node.animationDuration, v => node.animationDuration = Mathf.Clamp(v, 0.05f, 2f));
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private NodeGraph GetNodeGraph()
        {
            if (Node == null) return null;
            
            // Search all NodeGraph assets to find which one contains this node
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:NodeGraph");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var graph = UnityEditor.AssetDatabase.LoadAssetAtPath<NodeGraph>(path);
                if (graph != null && graph.Nodes != null)
                {
                    foreach (var node in graph.Nodes)
                    {
                        if (node != null && node.Guid == Node.Guid)
                        {
                            return graph;
                        }
                    }
                }
            }
            return null;
        }
    }
}
#endif
