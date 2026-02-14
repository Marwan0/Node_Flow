using System.Collections.Generic;
using UnityEngine;
using OmniEvent;

namespace OmniEvent.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating all supported complex types in OmniEvent:
    /// - Vector3, Quaternion, LayerMask, Color
    /// - Enums and Lists/Arrays
    /// - Multiple parameter combinations
    /// </summary>
    public class ComplexTypesExample : MonoBehaviour
    {
        // Define a test enum
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver
        }

        [Header("Single Parameter Events")]
        public OmniEvent<Vector3> onVector3Event = new OmniEvent<Vector3>();
        public OmniEvent<Quaternion> onQuaternionEvent = new OmniEvent<Quaternion>();
        public OmniEvent<Color> onColorEvent = new OmniEvent<Color>();
        public OmniEvent<LayerMask> onLayerMaskEvent = new OmniEvent<LayerMask>();
        public OmniEvent<GameState> onEnumEvent = new OmniEvent<GameState>();
        public OmniEvent<List<int>> onListEvent = new OmniEvent<List<int>>();

        [Header("Multi-Parameter Events")]
        public OmniEvent<Vector3, Quaternion> onTransformEvent = new OmniEvent<Vector3, Quaternion>();
        public OmniEvent<Color, float, string> onColorWithDataEvent = new OmniEvent<Color, float, string>();
        public OmniEvent<GameState, Vector3, Color, float> onComplexEvent = new OmniEvent<GameState, Vector3, Color, float>();

        [Header("Test Buttons")]
        [SerializeField] private bool testVector3 = false;
        [SerializeField] private bool testQuaternion = false;
        [SerializeField] private bool testColor = false;
        [SerializeField] private bool testLayerMask = false;
        [SerializeField] private bool testEnum = false;
        [SerializeField] private bool testList = false;
        [SerializeField] private bool testTransform = false;
        [SerializeField] private bool testColorWithData = false;
        [SerializeField] private bool testComplex = false;

        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            if (testVector3) { testVector3 = false; TestVector3(); }
            if (testQuaternion) { testQuaternion = false; TestQuaternion(); }
            if (testColor) { testColor = false; TestColor(); }
            if (testLayerMask) { testLayerMask = false; TestLayerMask(); }
            if (testEnum) { testEnum = false; TestEnum(); }
            if (testList) { testList = false; TestList(); }
            if (testTransform) { testTransform = false; TestTransform(); }
            if (testColorWithData) { testColorWithData = false; TestColorWithData(); }
            if (testComplex) { testComplex = false; TestComplex(); }
        }

        // ==================== Test Methods ====================

        public void TestVector3()
        {
            Vector3 position = new Vector3(10f, 5f, 3f);
            Debug.Log($"[ComplexTypes] Triggering Vector3 event: {position}");
            onVector3Event?.Invoke(position);
        }

        public void TestQuaternion()
        {
            Quaternion rotation = Quaternion.Euler(45f, 90f, 0f);
            Debug.Log($"[ComplexTypes] Triggering Quaternion event: {rotation.eulerAngles}");
            onQuaternionEvent?.Invoke(rotation);
        }

        public void TestColor()
        {
            Color color = new Color(0.5f, 0.8f, 0.2f, 1f);
            Debug.Log($"[ComplexTypes] Triggering Color event: {color}");
            onColorEvent?.Invoke(color);
        }

        public void TestLayerMask()
        {
            LayerMask mask = LayerMask.GetMask("Default", "UI");
            Debug.Log($"[ComplexTypes] Triggering LayerMask event: {mask.value}");
            onLayerMaskEvent?.Invoke(mask);
        }

        public void TestEnum()
        {
            GameState state = GameState.Playing;
            Debug.Log($"[ComplexTypes] Triggering Enum event: {state}");
            onEnumEvent?.Invoke(state);
        }

        public void TestList()
        {
            List<int> numbers = new List<int> { 1, 2, 3, 5, 8, 13 };
            Debug.Log($"[ComplexTypes] Triggering List event with {numbers.Count} items");
            onListEvent?.Invoke(numbers);
        }

        public void TestTransform()
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            Debug.Log($"[ComplexTypes] Triggering Transform event: Pos={position}, Rot={rotation.eulerAngles}");
            onTransformEvent?.Invoke(position, rotation);
        }

        public void TestColorWithData()
        {
            Color color = Color.cyan;
            float intensity = 0.75f;
            string label = "Cyan Light";
            Debug.Log($"[ComplexTypes] Triggering ColorWithData event: {label}, {color}, {intensity}");
            onColorWithDataEvent?.Invoke(color, intensity, label);
        }

        public void TestComplex()
        {
            GameState state = GameState.Playing;
            Vector3 position = Vector3.up * 10f;
            Color color = Color.yellow;
            float score = 1250.5f;
            Debug.Log($"[ComplexTypes] Triggering Complex event: {state}, {position}, {color}, {score}");
            onComplexEvent?.Invoke(state, position, color, score);
        }

        // ==================== Handler Methods ====================

        public void HandleVector3(Vector3 position)
        {
            Debug.Log($"[ComplexTypes] Received Vector3: {position}");
            transform.position = position;
        }

        public void HandleQuaternion(Quaternion rotation)
        {
            Debug.Log($"[ComplexTypes] Received Quaternion: {rotation.eulerAngles}");
            transform.rotation = rotation;
        }

        public void HandleColor(Color color)
        {
            Debug.Log($"[ComplexTypes] Received Color: {color}");
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        public void HandleLayerMask(LayerMask mask)
        {
            Debug.Log($"[ComplexTypes] Received LayerMask: {mask.value}");
            gameObject.layer = (int)Mathf.Log(mask.value, 2);
        }

        public void HandleEnum(GameState state)
        {
            Debug.Log($"[ComplexTypes] Received GameState: {state}");
            
            switch (state)
            {
                case GameState.Menu:
                    Debug.Log("  → Showing menu");
                    break;
                case GameState.Playing:
                    Debug.Log("  → Game is running");
                    break;
                case GameState.Paused:
                    Debug.Log("  → Game paused");
                    break;
                case GameState.GameOver:
                    Debug.Log("  → Game over!");
                    break;
            }
        }

        public void HandleList(List<int> numbers)
        {
            Debug.Log($"[ComplexTypes] Received List with {numbers.Count} items:");
            for (int i = 0; i < numbers.Count; i++)
            {
                Debug.Log($"  [{i}] = {numbers[i]}");
            }
        }

        public void HandleTransform(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"[ComplexTypes] Received Transform: Pos={position}, Rot={rotation.eulerAngles}");
            transform.SetPositionAndRotation(position, rotation);
        }

        public void HandleColorWithData(Color color, float intensity, string label)
        {
            Debug.Log($"[ComplexTypes] Received ColorWithData:");
            Debug.Log($"  Label: {label}");
            Debug.Log($"  Color: {color}");
            Debug.Log($"  Intensity: {intensity}");
        }

        public void HandleComplex(GameState state, Vector3 position, Color color, float score)
        {
            Debug.Log($"[ComplexTypes] Received Complex event:");
            Debug.Log($"  State: {state}");
            Debug.Log($"  Position: {position}");
            Debug.Log($"  Color: {color}");
            Debug.Log($"  Score: {score}");
        }
    }
}

/* 
 * ============================================================================
 * INSPECTOR SETUP EXAMPLES
 * ============================================================================
 * 
 * EXAMPLE 1: Vector3 Event (Static)
 * ----------------------------------
 * 1. Add listener to "On Vector3 Event"
 * 2. Select HandleVector3 method
 * 3. Set Vector3 parameter to (10, 5, 3) using the X/Y/Z fields
 * 
 * EXAMPLE 2: Vector3 Event (Dynamic)
 * -----------------------------------
 * 1. Add listener to "On Vector3 Event"
 * 2. Select HandleVector3 method
 * 3. For the Vector3 parameter, click the property selector
 * 4. Choose another GameObject's Transform > position
 * 
 * EXAMPLE 3: Color Event (Static)
 * --------------------------------
 * 1. Add listener to "On Color Event"
 * 2. Select HandleColor method
 * 3. Use the color picker to choose a color
 * 
 * EXAMPLE 4: Enum Event (Static)
 * -------------------------------
 * 1. Add listener to "On Enum Event"
 * 2. Select HandleEnum method
 * 3. Choose a value from the GameState dropdown (Menu/Playing/Paused/GameOver)
 * 
 * EXAMPLE 5: Multi-Parameter Event (Transform)
 * ---------------------------------------------
 * 1. Add listener to "On Transform Event"
 * 2. Select HandleTransform method
 * 3. Set Vector3 parameter (position)
 * 4. Set Quaternion parameter (rotation) - you can use Euler angles
 * 
 * EXAMPLE 6: Complex 4-Parameter Event
 * -------------------------------------
 * 1. Add listener to "On Complex Event"
 * 2. Select HandleComplex method
 * 3. Configure all four parameters:
 *    - GameState (enum dropdown)
 *    - Vector3 (X/Y/Z fields)
 *    - Color (color picker)
 *    - Float (score value)
 * 
 * ============================================================================
 */
