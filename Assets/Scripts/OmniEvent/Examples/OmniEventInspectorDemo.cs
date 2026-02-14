using System.Collections.Generic;
using UnityEngine;
using OmniEvent;

namespace OmniEvent.Examples
{
    /// <summary>
    /// Demonstration of the enhanced OmniEvent Inspector.
    /// This demo shows all the inspector features including parameter display and editing.
    /// </summary>
    public class OmniEventInspectorDemo : MonoBehaviour
    {
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        [Header("=== Single Parameter Events ===")]
        [Space]
        public OmniEvent<int> onScoreChanged = new OmniEvent<int>();
        public OmniEvent<float> onHealthChanged = new OmniEvent<float>();
        public OmniEvent<string> onMessageReceived = new OmniEvent<string>();
        public OmniEvent<Vector3> onPositionChanged = new OmniEvent<Vector3>();
        public OmniEvent<Color> onColorChanged = new OmniEvent<Color>();
        public OmniEvent<GameState> onGameStateChanged = new OmniEvent<GameState>();
        public OmniEvent<LayerMask> onLayerChanged = new OmniEvent<LayerMask>();

        [Header("=== Multi-Parameter Events ===")]
        [Space]
        public OmniEvent<Vector3, Quaternion> onTransformUpdated = new OmniEvent<Vector3, Quaternion>();
        public OmniEvent<string, int, float> onPlayerDataUpdated = new OmniEvent<string, int, float>();
        public OmniEvent<GameState, Vector3, Color, float> onComplexEvent = new OmniEvent<GameState, Vector3, Color, float>();

        [Header("=== Complex Type Events ===")]
        [Space]
        public OmniEvent<List<int>> onIntListEvent = new OmniEvent<List<int>>();
        public OmniEvent<List<string>> onStringListEvent = new OmniEvent<List<string>>();
        public OmniEvent<List<Vector3>> onVector3ListEvent = new OmniEvent<List<Vector3>>();

        [Header("=== Test Data ===")]
        [Space]
        [SerializeField] private int testScore = 0;
        [SerializeField] private float testHealth = 100f;
        [SerializeField] private string testMessage = "Hello, OmniEvent!";
        [SerializeField] private Vector3 testPosition = Vector3.zero;
        [SerializeField] private Color testColor = Color.white;
        [SerializeField] private GameState testState = GameState.Menu;

        [Header("=== Test Buttons ===")]
        [Space]
        [SerializeField] private bool triggerScore = false;
        [SerializeField] private bool triggerHealth = false;
        [SerializeField] private bool triggerMessage = false;
        [SerializeField] private bool triggerPosition = false;
        [SerializeField] private bool triggerColor = false;
        [SerializeField] private bool triggerState = false;
        [SerializeField] private bool triggerTransform = false;
        [SerializeField] private bool triggerPlayerData = false;
        [SerializeField] private bool triggerComplex = false;
        [SerializeField] private bool triggerIntList = false;
        [SerializeField] private bool triggerStringList = false;
        [SerializeField] private bool triggerVector3List = false;

        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            if (triggerScore) { triggerScore = false; TestScore(); }
            if (triggerHealth) { triggerHealth = false; TestHealth(); }
            if (triggerMessage) { triggerMessage = false; TestMessage(); }
            if (triggerPosition) { triggerPosition = false; TestPosition(); }
            if (triggerColor) { triggerColor = false; TestColor(); }
            if (triggerState) { triggerState = false; TestState(); }
            if (triggerTransform) { triggerTransform = false; TestTransform(); }
            if (triggerPlayerData) { triggerPlayerData = false; TestPlayerData(); }
            if (triggerComplex) { triggerComplex = false; TestComplex(); }
            if (triggerIntList) { triggerIntList = false; TestIntList(); }
            if (triggerStringList) { triggerStringList = false; TestStringList(); }
            if (triggerVector3List) { triggerVector3List = false; TestVector3List(); }
        }

        public void TestScore()
        {
            testScore++;
            Debug.Log($"[Demo] Triggering onScoreChanged: {testScore}");
            onScoreChanged?.Invoke(testScore);
        }

        public void TestHealth()
        {
            testHealth -= 10f;
            Debug.Log($"[Demo] Triggering onHealthChanged: {testHealth}");
            onHealthChanged?.Invoke(testHealth);
        }

        public void TestMessage()
        {
            Debug.Log($"[Demo] Triggering onMessageReceived: {testMessage}");
            onMessageReceived?.Invoke(testMessage);
        }

        public void TestPosition()
        {
            testPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(0f, 5f), Random.Range(-5f, 5f));
            Debug.Log($"[Demo] Triggering onPositionChanged: {testPosition}");
            onPositionChanged?.Invoke(testPosition);
        }

        public void TestColor()
        {
            testColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            Debug.Log($"[Demo] Triggering onColorChanged: {testColor}");
            onColorChanged?.Invoke(testColor);
        }

        public void TestState()
        {
            var states = (GameState[])System.Enum.GetValues(typeof(GameState));
            testState = states[(int)(testState + 1) % states.Length];
            Debug.Log($"[Demo] Triggering onGameStateChanged: {testState}");
            onGameStateChanged?.Invoke(testState);
        }

        public void TestTransform()
        {
            testPosition = transform.position;
            var rotation = transform.rotation;
            Debug.Log($"[Demo] Triggering onTransformUpdated");
            onTransformUpdated?.Invoke(testPosition, rotation);
        }

        public void TestPlayerData()
        {
            string playerName = "Player";
            int score = testScore;
            float time = Time.time;
            Debug.Log($"[Demo] Triggering onPlayerDataUpdated");
            onPlayerDataUpdated?.Invoke(playerName, score, time);
        }

        public void TestComplex()
        {
            Debug.Log($"[Demo] Triggering onComplexEvent");
            onComplexEvent?.Invoke(testState, testPosition, testColor, (float)testScore);
        }

        public void TestIntList()
        {
            var list = new List<int> { testScore, testScore * 2, testScore * 3 };
            Debug.Log($"[Demo] Triggering onIntListEvent with {list.Count} items");
            onIntListEvent?.Invoke(list);
        }

        public void TestStringList()
        {
            var list = new List<string> { "Item 1", "Item 2", "Item 3", testMessage };
            Debug.Log($"[Demo] Triggering onStringListEvent with {list.Count} items");
            onStringListEvent?.Invoke(list);
        }

        public void TestVector3List()
        {
            var list = new List<Vector3> { Vector3.zero, Vector3.up, Vector3.right, testPosition };
            Debug.Log($"[Demo] Triggering onVector3ListEvent with {list.Count} items");
            onVector3ListEvent?.Invoke(list);
        }

        // Handler methods
        public void HandleScore(int score) { Debug.Log($"[Handler] Score: {score}"); }
        public void HandleHealth(float health) { Debug.Log($"[Handler] Health: {health:F2}"); }
        public void HandleMessage(string message) { Debug.Log($"[Handler] Message: {message}"); }
        public void HandlePosition(Vector3 position) { transform.position = position; }
        public void HandleColor(Color color)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
                renderer.material.color = color;
        }
        public void HandleState(GameState state) { testState = state; }
        public void HandleTransform(Vector3 position, Quaternion rotation) { transform.SetPositionAndRotation(position, rotation); }
        public void HandlePlayerData(string name, int score, float time) { Debug.Log($"[Handler] {name}, {score}, {time:F2}"); }
        public void HandleComplex(GameState state, Vector3 position, Color color, float value)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
                renderer.material.color = color;
            testState = state;
            testPosition = position;
            testColor = color;
        }
        public void HandleIntList(List<int> numbers)
        {
            Debug.Log($"[Handler] Int list with {numbers.Count} items");
            for (int i = 0; i < numbers.Count; i++)
                Debug.Log($"  [{i}] = {numbers[i]}");
        }
        public void HandleStringList(List<string> items)
        {
            Debug.Log($"[Handler] String list with {items.Count} items");
            for (int i = 0; i < items.Count; i++)
                Debug.Log($"  [{i}] = {items[i]}");
        }
        public void HandleVector3List(List<Vector3> positions)
        {
            Debug.Log($"[Handler] Vector3 list with {positions.Count} items");
            for (int i = 0; i < positions.Count; i++)
                Debug.Log($"  [{i}] = {positions[i]}");
        }
    }
}
