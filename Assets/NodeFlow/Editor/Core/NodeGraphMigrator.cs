using UnityEditor;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Editor
{
    public class NodeGraphMigrator : EditorWindow
    {
        [MenuItem("Tools/Node System/Migrate All Graphs to SerializeReference")]
        public static void MigrateAllGraphs()
        {
            string[] guids = AssetDatabase.FindAssets("t:NodeGraph");
            int count = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NodeGraph graph = AssetDatabase.LoadAssetAtPath<NodeGraph>(path);
                
                if (graph != null)
                {
                    // Forces the graph to run EnsureLoaded(). 
                    // This will hit the fallback path and parse the old JSON since _serializedNodes is empty.
                    var forceLoad = graph.Nodes; 
                    
                    // Forces the graph to save its newly parsed _runtimeNodes into the native _serializedNodes list.
                    graph.SaveToJson(); 
                    
                    EditorUtility.SetDirty(graph);
                    count++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"[Migration] Successfully migrated {count} NodeGraphs to use native SerializeReference list.");
        }
    }
}
