using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Mine;
using Seaside;
using UnityEditor;
using UnityEngine;

public static class MineEditorChecks
{
    public static void Run()
    {
        var log=new List<string>();Action<bool,string> check=(ok,s)=>log.Add((ok?"PASS ":"FAIL ")+s);
        var level=UnityEngine.Object.FindAnyObjectByType<MineLevel>();
        var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("MineMap").text);
        Physics2D.SyncTransforms();
        check(level!=null&&level.sourceSha256==map.sourceSha256,"Saved mine matches source workbook hash");
        check(level.cameraRig.mapSize==new Vector2(map.width,map.height)&&map.width==170&&map.height==272,"Camera covers authored 170x272 map frame");
        var cells=UnityEngine.Object.FindObjectsByType<SeasideCell>();
        var expected=map.cells.Where(c=>c.kind=="ground"||c.kind=="noGrip").ToDictionary(c=>new Vector2Int(c.x,c.y),c=>c.kind);
        var actual=new Dictionary<Vector2Int,string>();bool duplicates=false,aligned=true;
        foreach(var cell in cells.Where(c=>c.kind==CellKind.Ground))
        {
            var b=cell.GetComponent<BoxCollider2D>().bounds;aligned&=Mathf.Abs(b.min.x-Mathf.Round(b.min.x))<.001f&&Mathf.Abs(b.min.y-Mathf.Round(b.min.y))<.001f;
            for(int y=Mathf.RoundToInt(b.min.y);y<Mathf.RoundToInt(b.max.y);y++)for(int x=Mathf.RoundToInt(b.min.x);x<Mathf.RoundToInt(b.max.x);x++)
            {var key=new Vector2Int(x,y);if(actual.ContainsKey(key))duplicates=true;actual[key]=cell.attachable?"ground":"noGrip";}
            check(cell.gameObject.layer==6&&!cell.GetComponent<BoxCollider2D>().isTrigger,"Solid Ground layer: "+cell.excelAddress);
        }
        check(aligned&&!duplicates&&actual.Count==expected.Count&&expected.All(e=>actual.TryGetValue(e.Key,out var k)&&k==e.Value),"Every black/gray source cell has exact nonduplicated collision coverage");
        var spikes=UnityEngine.Object.FindObjectsByType<SeasideCell>(FindObjectsInactive.Include).Where(c=>c.kind==CellKind.Spike).ToArray();
        check(spikes.Select(c=>c.excelAddress).OrderBy(s=>s).SequenceEqual(map.cells.Where(c=>c.kind=="spike").Select(c=>c.address).OrderBy(s=>s)),"Every red source hazard exists once, including authored inactive objects");
        var inactive=spikes.Where(c=>!c.gameObject.activeInHierarchy).Select(c=>c.excelAddress).OrderBy(s=>s).ToArray();
        if(inactive.Length>0)log.Add("WARN Authored inactive hazards preserved (not re-enabled): "+inactive.Length+" / "+string.Join(",",inactive));
        foreach(var c in spikes)check(c.gameObject.layer==7&&c.GetComponent<BoxCollider2D>().isTrigger,"Spike trigger "+c.excelAddress);
        var sourceRails=ForestMapRevision.Groups(map,"zipline");check(level.player.ziplines.Length==sourceRails.Count&&sourceRails.Count==12,"Twelve independent authored ziplines");
        check(Mathf.Abs(level.player.ziplines.Sum(r=>r.Length)-map.cells.Count(c=>c.kind=="zipline"))<.001f,"Zipline length matches green source cells");
        foreach(var group in sourceRails)
        {
            string key=string.Join(",",group.Select(c=>c.address).OrderBy(s=>s));
            var rail=level.player.ziplines.SingleOrDefault(r=>string.Join(",",r.excelAddress.Split(',').OrderBy(s=>s))==key);
            check(rail!=null&&Mathf.Abs(rail.StartPoint.x-group.Min(c=>c.x))<.001f&&Mathf.Abs(rail.EndPoint.x-group.Max(c=>c.x)-1)<.001f&&Mathf.Abs(rail.StartPoint.y-group[0].y-.5f)<.001f,"Rail anchors match workbook: "+group[0].address);
        }
        check(level.player.motor.settings==GameSystemSettings.Current.movement&&level.player.motor.BodySize.x>0&&level.player.motor.BodySize.y>0,"Mine reuses shared motor with authored collider dimensions");
        check(level.player.settings!=null&&level.player.wheel==null&&level.player.waterfall==null,"Mine traversal is independent of Forest wheel and waterfall");
        check(level.interactions.Count(i=>i.kind==MineInteractionKind.Workbench)==1&&UnityEngine.Object.FindObjectsByType<MineTouchSwitch>().Count(i=>i.lift!=null)==1,"One authored workbench and one lift switch");
        check(level.interactions.Count(i=>i.kind==MineInteractionKind.Relic)==1&&level.interactions.Count(i=>i.completesLevel)==1,"Two cyan points; only deepest relic completes Mine");
        check(level.lift.lowerStop==new Vector2(86,161.5f)&&level.lift.upperStop==new Vector2(86,254.5f),"Lift stops match orange workbook bars");
        check(level.lift.body.bodyType==RigidbodyType2D.Kinematic&&level.lift.Collider.gameObject.layer==6,"Lift is a physical kinematic Ground support");
        var checkpoints=cells.Where(c=>c.kind==CellKind.Checkpoint).ToArray();check(checkpoints.Length>=5,"Authored and added safe checkpoints cover Mine route");
        foreach(var cp in checkpoints)
        {
            var p=(Vector2)cp.respawnAnchor.position;
            check(!Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(),"Checkpoint has clear respawn body: "+cp.name);
            var hit=Physics2D.Raycast(p+Vector2.down,Vector2.down,.2f,1<<6);check(hit.collider!=null&&hit.collider.GetComponent<MovingSurface>()==null,"Checkpoint has static floor: "+cp.name);
        }
        var start=level.session.startPoint;
        check(!Physics2D.OverlapBoxAll(start,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(),"Entrance spawn is clear of terrain and spikes");
        check(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==MineBuilder.ScenePath),"Mine included in sequential campaign");
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script: "+t.name);
        string path=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/mine-editor-checks.txt");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,log);
        if(log.Any(s=>s.StartsWith("FAIL")))throw new Exception("Mine editor checks failed; see "+path);
        Debug.Log("MINE_EDITOR_CHECKS_PASS assertions="+log.Count);
    }
}
