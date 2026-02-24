using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Plays an Animation Sequencer on a target GameObject
    /// </summary>
    [Serializable]
    public class AnimationSequencerNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";
        
        [SerializeField]
        public bool autoCreateIfMissing = false;

        public override string Name => "Animation Sequencer";
        public override Color Color => new Color(0.7f, 0.5f, 0.9f); // Purple
        public override string Category => "Animation";

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
                new PortData("output", "On Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogError("[AnimationSequencerNode] ⚠️ No target configured! Drag a GameObject to the Target field.");
                Complete();
                return;
            }

            var target = GameObject.Find(targetPath);
            if (target == null)
            {
                Debug.LogError($"[AnimationSequencerNode] ⚠️ Target not found in scene: {targetPath}");
                Complete();
                return;
            }

            Debug.Log($"[AnimationSequencerNode] Playing Animation Sequencer on {targetPath}");
            
            // Fire visual event
            NodeGraphRunner.BroadcastNodeStarted(Runner, this);
            
            Runner?.StartCoroutine(PlayAnimationSequencer(target));
        }

        private IEnumerator PlayAnimationSequencer(GameObject target)
        {
            // Try to get AnimationSequencerController using reflection
            Component sequencerComponent = GetAnimationSequencerComponent(target);
            
            if (sequencerComponent == null)
            {
                if (autoCreateIfMissing)
                {
                    sequencerComponent = CreateAnimationSequencerComponent(target);
                    if (sequencerComponent == null)
                    {
                        Debug.LogError("[AnimationSequencerNode] ⚠️ Animation Sequencer is not available in this project. " +
                            "Please ensure:\n" +
                            "1. Animation Sequencer package is installed\n" +
                            "2. DOTWEEN_ENABLED is defined (check DOTween setup)\n" +
                            "3. The package is properly imported and compiled");
                        NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
                        Complete();
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"[AnimationSequencerNode] ⚠️ No Animation Sequencer component found on {targetPath}. Enable 'Auto Create If Missing' to create one automatically.");
                    NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
                    Complete();
                    yield break;
                }
            }

            // Try to use PlayEnumerator() for cleaner waiting
            var playEnumeratorMethod = sequencerComponent.GetType().GetMethod("PlayEnumerator", 
                BindingFlags.Public | BindingFlags.Instance);
            
            if (playEnumeratorMethod != null)
            {
                // Use PlayEnumerator() - it returns IEnumerator and waits for completion
                var enumerator = playEnumeratorMethod.Invoke(sequencerComponent, null) as IEnumerator;
                if (enumerator != null)
                {
                    // Execute the enumerator
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }
                else
                {
                    // Fallback to callback approach
                    yield return WaitForAnimationComplete(sequencerComponent);
                }
            }
            else
            {
                // Fallback to callback approach
                yield return WaitForAnimationComplete(sequencerComponent);
            }

            Debug.Log($"[AnimationSequencerNode] Animation Sequencer complete on {targetPath}");
            
            // Fire completion event
            NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
            
            Complete();
        }

        private Component GetAnimationSequencerComponent(GameObject target)
        {
            // Try to find AnimationSequencerController using reflection
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Type sequencerType = null;

            foreach (var assembly in assemblies)
            {
                // Try correct capitalization first (BrunoMikoski)
                sequencerType = assembly.GetType("BrunoMikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
                    
                // Fallback to lowercase (Brunomikoski)
                sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
            }

            if (sequencerType == null)
            {
                // Log available assemblies for debugging
                Debug.LogWarning("[AnimationSequencerNode] Animation Sequencer Controller type not found. " +
                    "Make sure DOTWEEN_ENABLED is defined and the Animation Sequencer package is properly installed.");
                return null;
            }

            // Get component from target
            return target.GetComponent(sequencerType);
        }

        private Component CreateAnimationSequencerComponent(GameObject target)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Type sequencerType = null;

            foreach (var assembly in assemblies)
            {
                // Try correct capitalization first
                sequencerType = assembly.GetType("BrunoMikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
                    
                // Fallback to lowercase
                sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
            }

            if (sequencerType == null)
                return null;

            // Add component to target
            return target.AddComponent(sequencerType);
        }

        private IEnumerator WaitForAnimationComplete(Component sequencerComponent)
        {
            if (sequencerComponent == null)
                yield break;

            // Try to use Play(Action) method first
            var playMethod = sequencerComponent.GetType().GetMethod("Play", 
                BindingFlags.Public | BindingFlags.Instance, 
                null, 
                new Type[] { typeof(System.Action) }, 
                null);

            bool animationComplete = false;
            System.Action onComplete = () => { animationComplete = true; };

            if (playMethod != null)
            {
                // Call Play(onComplete)
                playMethod.Invoke(sequencerComponent, new object[] { onComplete });
            }
            else
            {
                // Fallback: try Play() without callback and use OnFinishedEvent
                var playMethodNoCallback = sequencerComponent.GetType().GetMethod("Play", 
                    BindingFlags.Public | BindingFlags.Instance, 
                    null, 
                    new Type[] { }, 
                    null);

                if (playMethodNoCallback != null)
                {
                    playMethodNoCallback.Invoke(sequencerComponent, null);
                    
                    // Try to subscribe to OnFinishedEvent
                    var onFinishedEventProperty = sequencerComponent.GetType().GetProperty("OnFinishedEvent");
                    if (onFinishedEventProperty != null)
                    {
                        var unityEvent = onFinishedEventProperty.GetValue(sequencerComponent);
                        if (unityEvent != null)
                        {
                            var addListenerMethod = unityEvent.GetType().GetMethod("AddListener");
                            if (addListenerMethod != null)
                            {
                                // Create a UnityAction wrapper for our callback
                                var unityActionType = typeof(UnityEngine.Events.UnityAction);
                                var actionDelegate = System.Delegate.CreateDelegate(unityActionType, onComplete.Target, onComplete.Method);
                                addListenerMethod.Invoke(unityEvent, new object[] { actionDelegate });
                            }
                        }
                    }
                    else
                    {
                        // If we can't subscribe, check PlayingSequence
                        var playingSequenceProperty = sequencerComponent.GetType().GetProperty("PlayingSequence");
                        if (playingSequenceProperty != null)
                        {
                            var sequence = playingSequenceProperty.GetValue(sequencerComponent);
                            if (sequence != null)
                            {
                                // Wait for sequence to complete
                                var waitMethod = sequence.GetType().GetMethod("WaitForCompletion");
                                if (waitMethod != null)
                                {
                                    var waitEnumerator = waitMethod.Invoke(sequence, null) as IEnumerator;
                                    if (waitEnumerator != null)
                                    {
                                        // Execute the wait enumerator
                                        while (waitEnumerator.MoveNext())
                                        {
                                            yield return waitEnumerator.Current;
                                        }
                                        animationComplete = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("[AnimationSequencerNode] Could not find Play() method on Animation Sequencer component.");
                    animationComplete = true;
                }
            }

            // Wait for animation to complete
            while (!animationComplete)
            {
                yield return null;
            }
        }

    }
}

