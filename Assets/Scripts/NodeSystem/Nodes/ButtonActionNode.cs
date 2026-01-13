using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Waits for a button to be clicked before proceeding
    /// </summary>
    [Serializable]
    public class ButtonActionNode : NodeData
    {
        [SerializeField]
        public string buttonPath = "";
        
        [SerializeField]
        public bool disableAfterClick = true;

        [NonSerialized]
        private Button _button;
        
        [NonSerialized]
        private bool _clicked;

        public override string Name => "Button Action";
        public override Color Color => new Color(0.3f, 0.5f, 0.8f); // Dark Blue
        public override string Category => "UI";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Wait For Click", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "On Click", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            _clicked = false;
            
            if (string.IsNullOrEmpty(buttonPath))
            {
                Debug.LogWarning("[ButtonActionNode] No button path specified");
                Complete();
                return;
            }

            var buttonObj = GameObject.Find(buttonPath);
            if (buttonObj == null)
            {
                Debug.LogWarning($"[ButtonActionNode] Button not found: {buttonPath}");
                Complete();
                return;
            }

            _button = buttonObj.GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogWarning($"[ButtonActionNode] No Button component on: {buttonPath}");
                Complete();
                return;
            }

            Debug.Log($"[ButtonActionNode] Waiting for click on: {buttonPath}");
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            if (_clicked) return;
            _clicked = true;
            
            Debug.Log($"[ButtonActionNode] Button clicked: {buttonPath}");
            
            // Clean up listener
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
                
                if (disableAfterClick)
                {
                    _button.interactable = false;
                }
            }
            
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            
            // Clean up listener if still attached
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
                _button = null;
            }
            _clicked = false;
        }
    }
}

