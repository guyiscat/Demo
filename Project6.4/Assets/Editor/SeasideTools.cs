using UnityEditor;
using UnityEngine;
using Seaside;
public sealed class SeasideTools:EditorWindow
{
    [MenuItem("Seaside/Map editing palette")]
    static void Open(){GetWindow<SeasideTools>("Seaside palette");}
    void OnGUI()
    {
        EditorGUILayout.LabelField("Map modules",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Stop Play mode before editing. Pick a module to place at the Scene camera centre; move with W, resize with R. Hold Ctrl for grid snap. Excel rebuilding overwrites scene edits.",MessageType.Info);
        foreach(string name in new[]{"Ground","Spike","Checkpoint"})if(GUILayout.Button("Add "+name,GUILayout.Height(30)))
        {
            if(EditorApplication.isPlaying){ShowNotification(new GUIContent("Stop Play mode first"));return;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(go,"Add map module");
            Vector3 p=SceneView.lastActiveSceneView==null?Vector3.zero:SceneView.lastActiveSceneView.pivot;go.transform.position=new Vector3(Mathf.Round(p.x),Mathf.Round(p.y),0);Selection.activeGameObject=go;
        }
        if(GUILayout.Button("Find movement settings"))Selection.activeObject=AssetDatabase.LoadAssetAtPath<SeasideSettings>("Assets/Settings/Movement.asset");
    }
    [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
    static void CheckpointGizmo(SeasideCell cell,GizmoType type)
    {
        if(cell.kind!=CellKind.Checkpoint||cell.respawnAnchor==null)return;Gizmos.color=Color.green;Gizmos.DrawWireCube(cell.respawnAnchor.position,new Vector3(2,2,0));Handles.Label(cell.respawnAnchor.position+Vector3.up*1.2f,"CP "+cell.checkpointId+" SPAWN");
    }
}
