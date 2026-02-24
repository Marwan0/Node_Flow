using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Enables or disables a UI Button
    /// </summary>
    [Serializable]
    public class ButtonActivationNode : NodeData
    {
        [SerializeField]
        public string buttonPath = "";
        
        [SerializeField]
        public bool setInteractable = true;

        public override string Name => "Button Activation";
        public override Color Color => new Color(0.3f, 0.6f, 0.9f); // Blue
        public override string Category => "UI";

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
            if (string.IsNullOrEmpty(buttonPath))
            {
                Debug.LogWarning("[ButtonActivationNode] No button path specified");
                Complete();
                return;
            }

            var buttonObj = GameObject.Find(buttonPath);
            if (buttonObj == null)
            {
                Debug.LogWarning($"[ButtonActivationNode] Button not found: {buttonPath}");
                Complete();
                return;
            }

            var button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[ButtonActivationNode] No Button component on: {buttonPath}");
                Complete();
                return;
            }

            button.interactable = setInteractable;
            Debug.Log($"[ButtonActivationNode] Set {buttonPath} interactable = {setInteractable}");
            
            Complete();
        }
    }
}

