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

            // Display mode selector at the top
            CreateEnumField("Mode", node.displayMode, v =>
            {
                node.displayMode = v;
                RequestRefresh();
            });

            bool isSlots = node.displayMode == ScoreProgressBarNode.DisplayMode.Slots;

            // Target field (label changes per mode)
            UnityEngine.Object currentTarget = node.targetRef;
            if (currentTarget == null && !string.IsNullOrEmpty(node.targetPath))
            {
                var restored = FindGameObjectByPath(node.targetPath);
                if (restored != null)
                {
                    currentTarget = restored;
                    node.targetRef = restored;

                    var graph = GetNodeGraph();
                    if (graph != null)
                    {
                        graph.SaveToJson();
                        UnityEditor.EditorUtility.SetDirty(graph);
                    }
                }
            }

            CreateLabel(isSlots
                ? "Target (drag slot container)"
                : "Target (drag Slider, Image, or GameObject)");
            CreateObjectField<UnityEngine.Object>("Target", currentTarget, v =>
            {
                node.targetRef = v;
                if (v != null)
                {
                    Transform t = v is GameObject go ? go.transform : (v as Component)?.transform;
                    if (t != null)
                    {
                        node.targetPath = GetHierarchyPath(t);
                        var graph = GetNodeGraph();
                        if (graph != null)
                        {
                            graph.SaveToJson();
                            UnityEditor.EditorUtility.SetDirty(graph);
                        }
                    }
                }
                MarkDirty();
            });
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                CreateLabel($"Path: {node.targetPath}", new Color(0.55f, 0.55f, 0.55f));
            }

            if (isSlots)
            {
                // === Slots-specific fields ===
                CreateLabel("Slot Colors");
                CreateColorField("Default", node.slotDefaultColor, v => node.slotDefaultColor = v);
                CreateColorField("Correct", node.slotCorrectColor, v => node.slotCorrectColor = v);
                CreateColorField("Wrong", node.slotWrongColor, v => node.slotWrongColor = v);

                CreateLabel("Slot Sprites (optional)");
                CreateObjectField<Sprite>("Default", node.slotDefaultSprite, v => node.slotDefaultSprite = v);
                CreateObjectField<Sprite>("Correct", node.slotCorrectSprite, v => node.slotCorrectSprite = v);
                CreateObjectField<Sprite>("Wrong", node.slotWrongSprite, v => node.slotWrongSprite = v);

                CreateToggle("Animate pop on fill", node.slotAnimateOnFill, v =>
                {
                    node.slotAnimateOnFill = v;
                    RequestRefresh();
                });
                if (node.slotAnimateOnFill)
                    CreateFloatField("Duration (s)", node.slotAnimationDuration, v => node.slotAnimationDuration = Mathf.Clamp(v, 0.05f, 2f));

                CreateToggle("Count wrong attempts", node.slotCountWrongAttempts, v =>
                {
                    node.slotCountWrongAttempts = v;
                    RequestRefresh();
                });
            }
            else
            {
                // === Slider / FilledImage fields ===
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

        private static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var found = GameObject.Find(path);
            if (found != null) return found;

            var parts = path.Split('/');
            if (parts.Length > 0)
            {
                string rootName = parts[0];
                string relativePath = parts.Length > 1 ? string.Join("/", parts, 1, parts.Length - 1) : "";

                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    foreach (var rootGo in scene.GetRootGameObjects())
                    {
                        if (rootGo.name != rootName) continue;
                        if (parts.Length == 1) return rootGo;

                        var t = rootGo.transform.Find(relativePath);
                        if (t != null) return t.gameObject;
                    }
                }
            }

            string leafName = parts.Length > 0 ? parts[parts.Length - 1] : path;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var byName = FindInHierarchyByName(rootGo.transform, leafName);
                    if (byName != null) return byName;
                }
            }

            return null;
        }

        private static GameObject FindInHierarchyByName(Transform parent, string targetName)
        {
            if (parent == null || string.IsNullOrEmpty(targetName)) return null;
            if (parent.name == targetName) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInHierarchyByName(parent.GetChild(i), targetName);
                if (result != null) return result;
            }

            return null;
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
