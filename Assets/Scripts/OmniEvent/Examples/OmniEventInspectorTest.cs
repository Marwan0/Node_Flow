using UnityEngine;
using OmniEvent;

namespace OmniEvent.Examples
{
    /// <summary>
    /// Test component to verify OmniEvent Inspector shows argument fields when you assign
    /// a method with parameters (like the "Unity Events 2" reference images).
    /// 
    /// HOW TO TEST:
    /// 1. Add this component to a GameObject.
    /// 2. Expand an event below (e.g. "On Float"), click + to add a listener.
    /// 3. Drag the same GameObject into the object field, select "OmniEventInspectorTest" and
    ///    pick the matching method (e.g. HandlerFloat).
    /// 4. You should see the parameter field(s) appear (e.g. "value:" for float).
    /// 5. Set the value in the Inspector, enter Play mode, click "Invoke All" or use the buttons
    ///    to fire events and check the Console for the logged values.
    /// </summary>
    public class OmniEventInspectorTest : MonoBehaviour
    {
        public enum ExampleEnum
        {
            FIRST,
            SECOND,
            THIRD
        }

        [Header("One-parameter events (argument should show in Inspector when you add a listener)")]
        public OmniEvent<float> onFloat = new OmniEvent<float>();
        public OmniEvent<string> onString = new OmniEvent<string>();
        public OmniEvent<int> onInt = new OmniEvent<int>();
        public OmniEvent<bool> onBool = new OmniEvent<bool>();
        public OmniEvent<Vector2> onVector2 = new OmniEvent<Vector2>();
        public OmniEvent<Vector3> onVector3 = new OmniEvent<Vector3>();
        public OmniEvent<Vector4> onVector4 = new OmniEvent<Vector4>();
        public OmniEvent<ExampleEnum> onEnum = new OmniEvent<ExampleEnum>();
        public OmniEvent<LayerMask> onLayerMask = new OmniEvent<LayerMask>();
        public OmniEvent<GameObject> onGameObject = new OmniEvent<GameObject>();
        public OmniEvent<Color> onColor = new OmniEvent<Color>();

        [Header("Multi-parameter (e.g. string + float + enum + GameObject)")]
        public OmniEvent<string, float, ExampleEnum, GameObject> onFourParams = new OmniEvent<string, float, ExampleEnum, GameObject>();

        [Header("Invoke from Inspector (Play mode)")]
        [Tooltip("Click to invoke all single-arg events with sample values")]
        [SerializeField] private bool invokeAllSingleParam;
        [Tooltip("Click to invoke the 4-param event")]
        [SerializeField] private bool invokeFourParam;

        // --- Handler methods (assign these as listeners in the Inspector to see argument fields) ---

        public void HandlerFloat(float value) => Debug.Log($"[OmniEvent Test] HandlerFloat received: {value}");
        public void HandlerString(string value) => Debug.Log($"[OmniEvent Test] HandlerString received: {value}");
        public void HandlerInt(int value) => Debug.Log($"[OmniEvent Test] HandlerInt received: {value}");
        public void HandlerBool(bool value) => Debug.Log($"[OmniEvent Test] HandlerBool received: {value}");
        public void HandlerVector2(Vector2 value) => Debug.Log($"[OmniEvent Test] HandlerVector2 received: {value}");
        public void HandlerVector3(Vector3 value) => Debug.Log($"[OmniEvent Test] HandlerVector3 received: {value}");
        public void HandlerVector4(Vector4 value) => Debug.Log($"[OmniEvent Test] HandlerVector4 received: {value}");
        public void HandlerEnum(ExampleEnum value) => Debug.Log($"[OmniEvent Test] HandlerEnum received: {value}");
        public void HandlerLayerMask(LayerMask value) => Debug.Log($"[OmniEvent Test] HandlerLayerMask received: {value}");
        public void HandlerGameObject(GameObject value) => Debug.Log($"[OmniEvent Test] HandlerGameObject received: {(value != null ? value.name : "null")}");
        public void HandlerColor(Color value) => Debug.Log($"[OmniEvent Test] HandlerColor received: {value}");

        /// <summary>Dynamic: receives event args at invoke time. Shown under "Dynamic" in the dropdown.</summary>
        public void HandlerFour(string s, float f, ExampleEnum e, GameObject go)
        {
            Debug.Log($"[OmniEvent Test] HandlerFour received: s={s}, f={f}, e={e}, go={(go != null ? go.name : "null")}");
        }

        /// <summary>Static Parameters: use this in the dropdown to get 4 editable fields (string, float, enum as int, GameObject).</summary>
        public void HandlerFour(string s, float f, int enumAsInt, GameObject go)
        {
            ExampleEnum e = (ExampleEnum)Mathf.Clamp(enumAsInt, 0, 2);
            HandlerFour(s, f, e, go);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (invokeAllSingleParam) { invokeAllSingleParam = false; InvokeAllSingleParam(); }
            if (invokeFourParam) { invokeFourParam = false; InvokeFourParam(); }
        }

        [ContextMenu("Invoke all single-param events (Play mode)")]
        private void InvokeAllSingleParam()
        {
            onFloat?.Invoke(2.5f);
            onString?.Invoke("Events");
            onInt?.Invoke(42);
            onBool?.Invoke(true);
            onVector2?.Invoke(new Vector2(10, 20));
            onVector3?.Invoke(new Vector3(10, 20, 30));
            onVector4?.Invoke(new Vector4(10, 20, 30, 40));
            onEnum?.Invoke(ExampleEnum.SECOND);
            onLayerMask?.Invoke(0);
            onGameObject?.Invoke(gameObject);
            onColor?.Invoke(Color.cyan);
            Debug.Log("[OmniEvent Test] Invoked all single-param events. Check above for handler logs.");
        }

        [ContextMenu("Invoke 4-param event (Play mode)")]
        private void InvokeFourParam()
        {
            onFourParams?.Invoke("TEST", 2f, ExampleEnum.THIRD, gameObject);
            Debug.Log("[OmniEvent Test] Invoked 4-param event.");
        }
    }
}
