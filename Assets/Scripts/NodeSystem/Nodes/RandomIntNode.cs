using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Generates a random integer value and stores it in a variable
    /// </summary>
    [Serializable]
    public class RandomIntNode : NodeData
    {
        [SerializeField]
        public string variableName = "RandomResult";

        [SerializeField]
        public int minValue = 0;

        [SerializeField]
        public int maxValue = 100;

        public override string Name => "Random Int";
        public override Color Color => new Color(0.35f, 0.65f, 0.45f); // Darker Greenish
        public override string Category => "Math";

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
                Debug.LogError("[RandomIntNode] No graph runner assigned!");
                Complete();
                return;
            }

            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("[RandomIntNode] No variable name specified");
                Complete();
                return;
            }

            var graph = Runner.Graph;
            
            // Generate random value (Random.Range for int is exclusive for max, so we add 1 to include it)
            int randomValue = UnityEngine.Random.Range(minValue, maxValue + 1);
            
            // Store in variable
            var variable = graph.GetOrCreateVariable(variableName, VariableType.Int, randomValue.ToString());
            variable.SetIntValue(randomValue);
            
            Debug.Log($"[RandomIntNode] Generated {randomValue} (Range: {minValue}-{maxValue}) -> {variableName}");

            graph.Save();
            Complete();
        }
    }
}
