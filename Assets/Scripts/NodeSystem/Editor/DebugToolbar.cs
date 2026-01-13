#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Debug toolbar for controlling graph execution
    /// </summary>
    public class DebugToolbar : VisualElement
    {
        private Button _pauseButton;
        private Button _resumeButton;
        private Button _stepButton;
        private Button _stopButton;
        private Label _statusLabel;
        private NodeGraphRunner _currentRunner;

        public DebugToolbar()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 10;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            CreateButtons();
            
            // Subscribe to runner events
            NodeGraphRunner.OnGraphStarted += OnGraphStarted;
            NodeGraphRunner.OnGraphEnded += OnGraphEnded;
            NodeGraphRunner.OnPaused += OnPaused;
            NodeGraphRunner.OnResumed += OnResumed;
        }

        private void CreateButtons()
        {
            // Pause button
            _pauseButton = new Button(() => _currentRunner?.Pause())
            {
                text = "⏸ Pause"
            };
            _pauseButton.style.marginRight = 5;
            Add(_pauseButton);

            // Resume button
            _resumeButton = new Button(() => _currentRunner?.Resume())
            {
                text = "▶ Resume"
            };
            _resumeButton.style.marginRight = 5;
            _resumeButton.style.display = DisplayStyle.None;
            Add(_resumeButton);

            // Step button
            _stepButton = new Button(() => _currentRunner?.Step())
            {
                text = "⏭ Step"
            };
            _stepButton.style.marginRight = 5;
            _stepButton.style.display = DisplayStyle.None;
            Add(_stepButton);

            // Stop button
            _stopButton = new Button(() => _currentRunner?.Stop())
            {
                text = "⏹ Stop"
            };
            _stopButton.style.marginRight = 5;
            Add(_stopButton);

            // Status label
            _statusLabel = new Label("No graph running");
            _statusLabel.style.marginLeft = 10;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            Add(_statusLabel);

            UpdateButtonStates();
        }

        private void OnGraphStarted(NodeGraphRunner runner)
        {
            _currentRunner = runner;
            UpdateButtonStates();
        }

        private void OnGraphEnded(NodeGraphRunner runner)
        {
            if (runner == _currentRunner)
            {
                _currentRunner = null;
                UpdateButtonStates();
            }
        }

        private void OnPaused(NodeGraphRunner runner)
        {
            if (runner == _currentRunner)
            {
                UpdateButtonStates();
            }
        }

        private void OnResumed(NodeGraphRunner runner)
        {
            if (runner == _currentRunner)
            {
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            bool hasRunner = _currentRunner != null;
            bool isRunning = hasRunner && _currentRunner.IsRunning;
            bool isPaused = hasRunner && _currentRunner.IsPaused;

            _pauseButton.SetEnabled(isRunning && !isPaused);
            _resumeButton.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Step button: always show when runner exists, but only enable when paused
            _stepButton.style.display = hasRunner ? DisplayStyle.Flex : DisplayStyle.None;
            _stepButton.SetEnabled(isPaused);
            
            if (isPaused)
            {
                _stepButton.tooltip = "Step to next node";
                _stepButton.style.opacity = 1f;
            }
            else if (hasRunner)
            {
                _stepButton.tooltip = "Pause first, then use Step to debug";
                _stepButton.style.opacity = 0.5f; // Dimmed when not paused
            }
            
            _stopButton.SetEnabled(hasRunner);
            
            // Update status label
            if (_statusLabel != null)
            {
                if (!hasRunner)
                {
                    _statusLabel.text = "No graph running - Enter Play mode";
                    _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else if (isPaused)
                {
                    _statusLabel.text = "Paused - Click Step to continue";
                    _statusLabel.style.color = new Color(1f, 0.8f, 0.4f);
                }
                else if (isRunning)
                {
                    _statusLabel.text = "Running - Click Pause to debug";
                    _statusLabel.style.color = new Color(0.4f, 1f, 0.4f);
                }
                else
                {
                    _statusLabel.text = "Stopped";
                    _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
        }
    }
}
#endif

