using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Forest;
using Seaside;

// An explicit, one-time patch of the SAVED existing scene, run in an isolated
// copy. It never calls ForestBuilder.CreateScene or rebuilds map geometry.
public static class ForestRevisionSetup
{
    public static void Apply()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Apply the saved-scene patch in an isolated batch project.");
        var scene=EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<ForestLevel>();
        Physics2D.SyncTransforms();
        var wheel=level.wheel;var water=level.player.waterfall;
        // Preserve the user's current Active=>true preview as a serialized toggle.
        wheel.forceRotation=true;
        if(wheel.transferBasket!=null)wheel.transferBasket.enabled=false;
        water.cameraRig=level.cameraRig;
        if(water.occlusion!=null)water.occlusion.enabled=false;
        var existing=UnityEngine.Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).ToArray();
        if(existing.Length==0)
        {
            var parent=new GameObject("Checkpoints").transform;parent.SetParent(wheel.transform.parent,false);
            Vector2[] positions={new Vector2(5.15f,50.05f),new Vector2(64,68.05f),new Vector2(202,5.05f),
                new Vector2(247.15f,37.05f),new Vector2(202,66.05f)};
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");
            for(int i=0;i<positions.Length;i++)
            {
                Vector2 point=positions[i];
                if(Physics2D.OverlapBoxAll(point,new Vector2(1.98f,1.98f),0).Any(c=>!c.isTrigger&&(c.gameObject.layer==6||c.gameObject.layer==7)&&c.GetComponent<ForestWheelPlatform>()==null))
                    throw new InvalidOperationException("Checkpoint intersects saved geometry at "+point+" hits="+string.Join(",",Physics2D.OverlapBoxAll(point,new Vector2(1.98f,1.98f),0).Select(c=>c.name+" bounds="+c.bounds)));
                var go=new GameObject("Checkpoint_"+(i+1).ToString("00"));go.transform.SetParent(parent,false);go.transform.position=point;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.drawMode=SpriteDrawMode.Simple;
                sr.color=new Color(1,.78f,.2f);sr.sortingOrder=6;go.transform.localScale=new Vector3(2,2,1);
                var box=go.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=Vector2.one;
                var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Checkpoint;cell.checkpointId=i+1;
                var anchor=new GameObject("RespawnAnchor").transform;anchor.SetParent(go.transform,false);anchor.position=point;
                cell.respawnAnchor=anchor;cell.respawn=point;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("FOREST_REVISION_APPLIED sameScene="+ForestBuilder.ScenePath+" checkpoints="+UnityEngine.Object.FindObjectsByType<SeasideCell>().Count(c=>c.kind==CellKind.Checkpoint));
    }
}
