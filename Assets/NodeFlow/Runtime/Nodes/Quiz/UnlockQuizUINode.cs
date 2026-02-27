using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Unlocks the quiz UI after answer feedback has finished playing.
    /// Place this at the END of your feedback chain (after Delay, PlaySound, Animation, etc.)
    /// so the user can interact with the question again after the feedback completes.
    /// </summary>
    [Serializable]
    public class UnlockQuizUINode : NodeData
    {
        public override string Name => "Unlock Quiz UI";
        public override Color Color => new Color(0.2f, 0.6f, 0.9f); // Blue
        public override string Category => "Quiz";
        public override string Description =>
            "Unlocks the quiz UI so the user can interact again. " +
            "Place at the end of an On Wrong Feedback or On Correct Feedback chain.";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Single, "Triggers the UI unlock logic.")
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Done", PortDirection.Output, PortCapacity.Multi, "Fires after the UI unlock request is sent.")
            };
        }

        protected override void OnExecute()
        {
            QuizState.RequestUIUnlock();
            Complete();
        }
    }
}
