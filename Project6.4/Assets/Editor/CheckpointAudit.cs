using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Seaside;
public static class CheckpointAudit
{
    public static void Repair()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch only.");
        var changes=new List<string>();
        foreach(var path in CampaignFlow.Scenes)
        {
            var scene=EditorSceneManager.OpenScene(path);Physics2D.SyncTransforms();
            var checkpoints=UnityEngine.Object.FindObjectsByType<SeasideCell>(FindObjectsInactive.Include).Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ThenBy(c=>c.name).ToArray();
            int next=checkpoints.Max(c=>c.checkpointId);var ids=new HashSet<int>();
            foreach(var cell in checkpoints)
            {
                int oldId=cell.checkpointId;
                if(oldId<=0||!ids.Add(oldId)){cell.checkpointId=++next;ids.Add(next);changes.Add(path+" "+cell.name+" ID "+oldId+" -> "+next);}
                if(cell.respawnAnchor==null){var go=new GameObject("RespawnAnchor");go.transform.SetParent(cell.transform,false);cell.respawnAnchor=go.transform;cell.respawnAnchor.position=cell.respawn;}
                Vector2 before=cell.respawnAnchor.position;
                if(!Safe(before))
                {
                    var candidates=new List<Vector2>();
                    foreach(var floor in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(c=>c.gameObject.layer==6&&!c.isTrigger&&c.attachedRigidbody==null&&c.GetComponent<MovingSurface>()==null&&c.GetComponent<Expedition.ThermalSurface>()==null&&c.GetComponent<Mine.MineGate>()==null))
                    {
                        var b=floor.bounds;if(b.size.x<2.1f)continue;
                        float center=Mathf.Clamp(before.x,b.min.x+1.06f,b.max.x-1.06f);
                        for(float x=Mathf.Max(b.min.x+1.06f,center-4);x<=Mathf.Min(b.max.x-1.06f,center+4);x+=.25f)
                        {var p=new Vector2(x,b.max.y+1.05f);if(Vector2.Distance(p,before)<=32&&Safe(p))candidates.Add(p);}
                    }
                    if(candidates.Count==0)throw new Exception("No safe nearby position for "+path+"/"+cell.name);
                    var point=candidates.OrderBy(p=>(p-before).sqrMagnitude).First();cell.respawnAnchor.position=point;cell.respawn=point;
                    changes.Add(path+" "+cell.name+" #"+cell.checkpointId+" anchor "+before.ToString("F2")+" -> "+point.ToString("F2")+" distance="+Vector2.Distance(before,point).ToString("F2"));
                }
                var trigger=cell.GetComponent<BoxCollider2D>();if(trigger==null)trigger=cell.gameObject.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.enabled=true;
                foreach(var label in cell.GetComponentsInChildren<TextMesh>(true))if(label.text.Contains("存档点"))label.text="存档点 "+cell.checkpointId;
            }
            EditorSceneManager.SaveScene(scene);
        }
        string root=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261127-region-mechanics");Directory.CreateDirectory(root);
        File.WriteAllLines(Path.Combine(root,"checkpoint-repairs.txt"),changes);Run();
    }
    static bool Safe(Vector2 p)
    {
        if(Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(c=>!c.isTrigger||c.gameObject.layer==7))return false;
        foreach(float dx in new[]{-.85f,0,.85f})
        {
            var floor=Physics2D.Raycast(p+new Vector2(dx,-.95f),Vector2.down,.25f,1<<6);
            if(floor.collider==null||floor.collider.isTrigger||floor.normal.y<.95f||floor.collider.attachedRigidbody!=null||floor.collider.GetComponent<MovingSurface>()!=null||floor.collider.GetComponent<Expedition.ThermalSurface>()!=null||floor.collider.GetComponent<Mine.MineGate>()!=null)return false;
        }
        return true;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Read-only audit in isolated batch project only.");
        var lines=new List<string>();var context=new List<string>();int count=0,issues=0;
        foreach(var path in CampaignFlow.Scenes)
        {
            EditorSceneManager.OpenScene(path);Physics2D.SyncTransforms();var session=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();
            var cells=UnityEngine.Object.FindObjectsByType<SeasideCell>(FindObjectsInactive.Include).Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
            foreach(var c in cells)
            {
                count++;var problems=new List<string>();Vector2 p=c.respawnAnchor!=null?(Vector2)c.respawnAnchor.position:c.respawn;
                var trigger=c.GetComponent<Collider2D>();
                if(c.checkpointId<=0||cells.Count(o=>o.checkpointId==c.checkpointId)>1)problems.Add("编号无效或重复");
                if(c.respawnAnchor==null)problems.Add("未绑定RespawnAnchor，使用备用坐标");
                if(!c.isActiveAndEnabled||trigger==null||!trigger.enabled||!trigger.isTrigger)problems.Add("触发器/对象未启用或未设IsTrigger");
                var solid=Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,1<<6).Where(o=>!o.isTrigger).ToArray();
                if(solid.Length>0)problems.Add("复活身体与实体重叠:"+string.Join(",",solid.Select(o=>o.name)));
                if(Physics2D.OverlapBoxAll(p,new Vector2(2.1f,2.1f),0,1<<7).Any())problems.Add("复活位置接触/紧邻尖刺");
                foreach(float dx in new[]{-.85f,0,.85f})
                {
                    var floor=Physics2D.Raycast(p+new Vector2(dx,-.95f),Vector2.down,.35f,1<<6);
                    if(floor.collider==null||floor.normal.y<.8f){problems.Add("脚下无完整支撑/靠近平台边缘");break;}
                    if(floor.collider.GetComponent<MovingSurface>()!=null)problems.Add("依赖移动平台支撑");
                    if(floor.collider.GetComponent<Expedition.ThermalSurface>()!=null)problems.Add("复活支撑为属性地形，依赖冷热模式");
                }
                var id=CampaignFlow.RegionLabel(path)+" / "+c.name+" #"+c.checkpointId+" @ "+p.ToString("F2");
                if(problems.Count>0){issues++;lines.Add("ISSUE "+id+" : "+string.Join("；",problems.Distinct()));}else lines.Add("PASS "+id);
            }
            var region=session.GetComponent<WorldRegion>();context.Add(path+" start="+session.startPoint+" entry="+region.entry.position+" right="+region.fromRight.position+" mine="+region.fromMine.position);
            foreach(var exit in region.exits)context.Add("EXIT "+exit.name+" bounds="+exit.GetComponent<Collider2D>().bounds+" to="+exit.destination+" key="+exit.arrival);
            if(path==CampaignFlow.Scenes[2])
                foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>().Where(c=>Vector2.Distance(c.bounds.center,session.startPoint)<25))context.Add("MINE TOP "+c.name+" layer="+c.gameObject.layer+" trigger="+c.isTrigger+" bounds="+c.bounds);
        }
        var root=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261127-region-mechanics");Directory.CreateDirectory(root);
        lines.Insert(0,"CHECKPOINT_AUDIT total="+count+" problematic="+issues+" (read only; no changes)");
        File.WriteAllLines(Path.Combine(root,"checkpoint-audit.txt"),lines);File.WriteAllLines(Path.Combine(root,"region-context.txt"),context);
        Debug.Log(lines[0]);
    }
}
