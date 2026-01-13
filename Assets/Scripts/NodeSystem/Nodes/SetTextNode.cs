using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Sets text on UI Text or TextMeshPro components
    /// </summary>
    [Serializable]
    public class SetTextNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";
        
        [SerializeField]
        public string text = "";
        
        [SerializeField]
        public bool typewriterEffect = false;
        
        [SerializeField]
        public float typewriterSpeed = 0.05f;

        public override string Name => "Set Text";
        public override Color Color => new Color(0.4f, 0.7f, 0.4f); // Green
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
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogWarning("[SetTextNode] No target path specified");
                Complete();
                return;
            }

            var target = GameObject.Find(targetPath);
            if (target == null)
            {
                Debug.LogWarning($"[SetTextNode] Target not found: {targetPath}");
                Complete();
                return;
            }

            // Try TextMeshPro first
            var tmpText = target.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                if (typewriterEffect)
                {
                    Runner?.StartCoroutine(TypewriterEffectTMP(tmpText));
                }
                else
                {
                    tmpText.text = text;
                    Debug.Log($"[SetTextNode] Set TMP text on {targetPath}");
                    Complete();
                }
                return;
            }

            // Try legacy Text
            var legacyText = target.GetComponent<Text>();
            if (legacyText != null)
            {
                if (typewriterEffect)
                {
                    Runner?.StartCoroutine(TypewriterEffectLegacy(legacyText));
                }
                else
                {
                    legacyText.text = text;
                    Debug.Log($"[SetTextNode] Set legacy text on {targetPath}");
                    Complete();
                }
                return;
            }

            Debug.LogWarning($"[SetTextNode] No Text component found on: {targetPath}");
            Complete();
        }

        private IEnumerator TypewriterEffectTMP(TextMeshProUGUI textComponent)
        {
            textComponent.text = "";
            foreach (char c in text)
            {
                textComponent.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            Debug.Log($"[SetTextNode] Typewriter complete on {targetPath}");
            Complete();
        }

        private IEnumerator TypewriterEffectLegacy(Text textComponent)
        {
            textComponent.text = "";
            foreach (char c in text)
            {
                textComponent.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            Debug.Log($"[SetTextNode] Typewriter complete on {targetPath}");
            Complete();
        }
    }
}

