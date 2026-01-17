using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    public enum TimerAction
    {
        Start,
        Stop,
        Pause,
        Resume,
        WaitForExpiry
    }

    /// <summary>
    /// Controls quiz timer functionality.
    /// Can start, stop, pause timers and wait for expiry.
    /// </summary>
    [Serializable]
    public class QuizTimerNode : NodeData
    {
        [SerializeField]
        public TimerAction action = TimerAction.Start;

        [SerializeField]
        public float duration = 60f; // seconds

        [SerializeField]
        public bool branchOnExpiry = false;

        public override string Name => "Quiz Timer";
        public override Color Color => new Color(0.8f, 0.4f, 0.4f); // Red
        public override string Category => "Quiz";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            var ports = new List<PortData>();

            if (action == TimerAction.WaitForExpiry || branchOnExpiry)
            {
                ports.Add(new PortData("expired", "Timer Expired", PortDirection.Output));
                ports.Add(new PortData("running", "Still Running", PortDirection.Output));
            }
            else
            {
                ports.Add(new PortData("output", "Next", PortDirection.Output));
            }

            return ports;
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;

            switch (action)
            {
                case TimerAction.Start:
                    state.StartTimer(duration);
                    Complete();
                    break;

                case TimerAction.Stop:
                    state.StopTimer();
                    Complete();
                    break;

                case TimerAction.Pause:
                    state.PauseTimer();
                    Complete();
                    break;

                case TimerAction.Resume:
                    state.ResumeTimer();
                    Complete();
                    break;

                case TimerAction.WaitForExpiry:
                    if (state.TimerExpired)
                    {
                        State = NodeState.Completed; // Expired
                        Complete();
                    }
                    else
                    {
                        Runner?.StartCoroutine(WaitForTimerExpiry());
                    }
                    break;
            }
        }

        private IEnumerator WaitForTimerExpiry()
        {
            var state = QuizState.Instance;

            while (!state.TimerExpired && state.timerActive && Runner != null && Runner.IsRunning)
            {
                yield return null;
            }

            State = state.TimerExpired ? NodeState.Completed : NodeState.Failed;
            Complete();
        }
    }
}
