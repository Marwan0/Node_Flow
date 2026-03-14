using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Generates a random float value and stores it in a variable
    /// </summary>
    [Serializable]
    public class RandomFloatNode : NodeData
    {
        [SerializeField]
        [GraphVariable(true)]
        public string variableName = "RandomResult";

        [SerializeField]
        public float minValue = 0f;

        [SerializeField]
        public float maxValue = 1f;

        public override string Name => "Random Float";
        public override Color Color => new Color(0.4f, 0.7f, 0.5f); // Greenish
        public override string Category => "Math";
        public override string Description => "Generates a random float value between min and max, storing it in a variable.";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[RandomFloatNode] No graph runner assigned!");
                Complete();
                return;
            }

            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("[RandomFloatNode] No variable name specified");
                Complete();
                return;
            }

            var graph = Runner.Graph;
            
            // Generate random value
            float randomValue = UnityEngine.Random.Range(minValue, maxValue);
            
            // Store in variable
            var variable = graph.GetOrCreateVariable(variableName, VariableType.Float, randomValue.ToString());
            variable.SetFloatValue(randomValue);
            
            Debug.Log($"[RandomFloatNode] Generated {randomValue} (Range: {minValue}-{maxValue}) -> {variableName}");

            graph.Save();
            Complete();
        }
    }
}
