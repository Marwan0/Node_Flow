#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Minimap for graph navigation
    /// </summary>
    public class MinimapView : MiniMap
    {
        public MinimapView()
        {
            anchored = true;
            SetPosition(new Rect(10, 30, 200, 150));
            
            // Style
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        }
    }
}
#endif

