#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Factory for creating node inspectors
    /// </summary>
    public static class NodeInspectorFactory
    {
        private static Dictionary<Type, Type> _inspectorTypes = new Dictionary<Type, Type>();

        static NodeInspectorFactory()
        {
            // Register custom inspectors
            Register<Nodes.SetVariableNode, SetVariableNodeInspector>();
            Register<Nodes.ConditionalNode, ConditionalNodeInspector>();
            Register<Nodes.AnimationNode, AnimationNodeInspector>();
            Register<Nodes.ButtonActivationNode, ButtonNodeInspector>();
            Register<Nodes.ButtonActionNode, ButtonActionNodeInspector>();
            Register<Nodes.SetTextNode, SetTextNodeInspector>();
            Register<Nodes.PlaySoundNode, PlaySoundNodeInspector>();
            Register<Nodes.DelayNode, DelayNodeInspector>();
        }

        public static void Register<TNode, TInspector>() 
            where TNode : NodeData 
            where TInspector : NodeInspectorBase
        {
            _inspectorTypes[typeof(TNode)] = typeof(TInspector);
        }

        /// <summary>
        /// Get an inspector for the given node type
        /// </summary>
        public static NodeInspectorBase GetInspector(NodeData node)
        {
            if (node == null) return null;

            var nodeType = node.GetType();
            
            if (_inspectorTypes.TryGetValue(nodeType, out Type inspectorType))
            {
                return (NodeInspectorBase)Activator.CreateInstance(inspectorType);
            }

            // Return default inspector
            return new DefaultNodeInspector();
        }
    }
}
#endif

