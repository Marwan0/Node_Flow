using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Logs a message to the console
    /// </summary>
    [Serializable]
    public class DebugLogNode : NodeData
    {
        public enum LogType { Info, Warning, Error }

        [SerializeField]
        public LogType logType = LogType.Info;

        [SerializeField]
        public string message = "Debug message";

        public override string Name => "Debug Log";
        public override Color Color => new Color(0.3f, 0.6f, 0.9f); // Blue
        public override string Category => "Debug";

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
            switch (logType)
            {
                case LogType.Info:
                    Debug.Log($"[DebugLog] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[DebugLog] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[DebugLog] {message}");
                    break;
            }
            Complete();
        }
    }
}


