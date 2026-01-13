#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Factory for creating inline node content renderers
    /// </summary>
    public static class NodeInlineContentFactory
    {
        private static Dictionary<Type, Type> _contentTypes = new Dictionary<Type, Type>();
        
        // Nodes that should NOT have inline content (simple flow nodes)
        private static HashSet<Type> _excludedTypes = new HashSet<Type>();

        static NodeInlineContentFactory()
        {
            // Exclude simple flow nodes that have no editable properties
            _excludedTypes.Add(typeof(Nodes.StartNode));
            _excludedTypes.Add(typeof(Nodes.EndNode));
            _excludedTypes.Add(typeof(Nodes.SequenceNode));   // Flow only
            _excludedTypes.Add(typeof(Nodes.ParallelNode));   // Flow only
            _excludedTypes.Add(typeof(Nodes.WaitForAllNode)); // Flow only
            _excludedTypes.Add(typeof(Nodes.CommentNode));    // Uses CommentNodeView
            
            // === Register all custom inline content ===
            
            // Variable & Logic nodes
            Register<Nodes.SetVariableNode, SetVariableNodeInlineContent>();
            Register<Nodes.ConditionalNode, ConditionalNodeInlineContent>();
            
            // UI nodes
            Register<Nodes.AnimationNode, AnimationNodeInlineContent>();
            Register<Nodes.AnimationSequencerNode, AnimationSequencerNodeInlineContent>();
            Register<Nodes.SetTextNode, SetTextNodeInlineContent>();
            Register<Nodes.ButtonActivationNode, ButtonActivationNodeInlineContent>();
            Register<Nodes.ButtonActionNode, ButtonActionNodeInlineContent>();
            
            // Flow nodes
            Register<Nodes.DelayNode, DelayNodeInlineContent>();
            Register<Nodes.LoopNode, LoopNodeInlineContent>();
            Register<Nodes.RandomBranchNode, RandomBranchNodeInlineContent>();
            Register<Nodes.SubGraphNode, SubGraphNodeInlineContent>();
            
            // Animation/Tween nodes
            Register<Nodes.TweenPropertyNode, TweenPropertyNodeInlineContent>();
            
            // Scene & Events
            Register<Nodes.SceneNode, SceneNodeInlineContent>();
            Register<Nodes.UnityEventNode, UnityEventNodeInlineContent>();
            
            // Audio
            Register<Nodes.PlaySoundNode, PlaySoundNodeInlineContent>();
            
            // Debug
            Register<Nodes.DebugLogNode, DebugLogNodeInlineContent>();
            
            // Quiz nodes
            Register<Nodes.Quiz.ShowQuestionNode, ShowQuestionNodeInlineContent>();
            Register<Nodes.Quiz.CheckAnswerNode, CheckAnswerNodeInlineContent>();
        }

        public static void Register<TNode, TContent>() 
            where TNode : NodeData 
            where TContent : NodeInlineContentBase
        {
            _contentTypes[typeof(TNode)] = typeof(TContent);
        }

        public static void Exclude<TNode>() where TNode : NodeData
        {
            _excludedTypes.Add(typeof(TNode));
        }

        /// <summary>
        /// Get inline content renderer for a node type
        /// </summary>
        public static NodeInlineContentBase GetContent(NodeData node)
        {
            if (node == null) return null;

            var nodeType = node.GetType();
            
            // Check for custom content first
            if (_contentTypes.TryGetValue(nodeType, out Type contentType))
            {
                return (NodeInlineContentBase)Activator.CreateInstance(contentType);
            }

            // Return full content for all other nodes
            return new FullNodeInlineContent();
        }

        /// <summary>
        /// Check if a node type has inline content
        /// </summary>
        public static bool HasInlineContent(NodeData node)
        {
            if (node == null) return false;
            
            // Excluded nodes don't have inline content
            if (_excludedTypes.Contains(node.GetType())) return false;
            
            // All other nodes have inline content
            return true;
        }
    }
}
#endif

