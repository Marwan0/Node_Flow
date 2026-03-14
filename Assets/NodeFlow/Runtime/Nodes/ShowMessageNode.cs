using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Shows a short-lived message (toast). Completes after duration seconds so flow can continue.
    /// Uses Debug.Log if no UI target is set; assign a target with a Text or similar to show in UI.
    /// </summary>
    [Serializable]
    public class ShowMessageNode : NodeData
    {
        [SerializeField]
        public string message = "Message";

        [SerializeField]
        [Range(0.1f, 10f)]
        public float duration = 2f;

        [SerializeField]
        [Tooltip("Optional: assign a GameObject with Text/TextMeshPro to show message. If null, logs to console.")]
        public UnityEngine.Object targetRef;

        [SerializeField]
        public string targetPath = "";

        public override string Name => "Show Message";
        public override Color Color => new Color(0.4f, 0.65f, 0.85f);
        public override string Category => "UI";
        public override string Description => "Overwrites the text of a specific UI component.";

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
            if (!string.IsNullOrEmpty(message))
                Debug.Log($"[ShowMessage] {message}");

            var textComponent = ResolveTextComponent();
            if (textComponent != null)
            {
                SetText(textComponent, message);
                if (Runner != null)
                    Runner.StartCoroutine(HideAfterDelay(textComponent));
                else
                    Complete();
            }
            else if (duration > 0 && Runner != null)
                Runner.StartCoroutine(CompleteAfterDelay());
            else
                Complete();
        }

        private void SetText(Component textComponent, string text)
        {
            if (textComponent is UnityEngine.UI.Text uiText)
                uiText.text = text;
        }

        private void ClearText(Component textComponent)
        {
            if (textComponent is UnityEngine.UI.Text uiText)
                uiText.text = "";
        }

        private IEnumerator HideAfterDelay(Component textComponent)
        {
            yield return new WaitForSeconds(duration);
            ClearText(textComponent);
            Complete();
        }

        private IEnumerator CompleteAfterDelay()
        {
            yield return new WaitForSeconds(duration);
            Complete();
        }

        private Component ResolveTextComponent()
        {
            GameObject go = null;
            if (targetRef is GameObject targetGo)
                go = targetGo;
            else if (targetRef is Component c)
                go = c.gameObject;
            if (go == null && !string.IsNullOrEmpty(targetPath))
                go = GameObject.Find(targetPath);
            if (go == null) return null;
            return go.GetComponent<UnityEngine.UI.Text>() ?? go.GetComponentInChildren<UnityEngine.UI.Text>();
        }
    }
}
