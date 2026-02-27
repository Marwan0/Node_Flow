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

        public override string Name => "Log Message";
        public override Color Color => new Color(0.3f, 0.6f, 0.9f); // Blue
        public override string Category => "Utility";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Single, "Triggers the logging action.")
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output, PortCapacity.Multi, "Fires after the message is logged.")
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


