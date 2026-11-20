using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Seaside;
using UnityEditor;
using UnityEngine;

public static class ForestEditorChecks
{
    static void CheckMapCoverage(ForestMapData map,ForestLevel level,SeasideCell[] imported,Action<bool,string> check)
    {
        var actual=new Dictionary<Vector2Int,string>();bool duplicates=false,aligned=true;
        foreach(var cell in imported.Where(c=>c.kind==CellKind.Ground))
        {
            var b=cell.GetComponent<BoxCollider2D>().bounds;
            aligned&=Mathf.Abs(b.min.x-Mathf.Round(b.min.x))<.001f&&Mathf.Abs(b.min.y-Mathf.Round(b.min.y))<.001f;
            for(int y=Mathf.RoundToInt(b.min.y);y<Mathf.RoundToInt(b.max.y);y++)
                for(int x=Mathf.RoundToInt(b.min.x);x<Mathf.RoundToInt(b.max.x);x++)
                {var p=new Vector2Int(x,y);if(actual.ContainsKey(p))duplicates=true;actual[p]=cell.attachable?"ground":"noGrip";}
        }
        var expected=map.cells.Where(c=>c.kind=="ground"||c.kind=="noGrip").ToDictionary(c=>new Vector2Int(c.x,c.y),c=>c.kind);
        check(aligned&&!duplicates&&actual.Count==expected.Count&&expected.All(p=>actual.TryGetValue(p.Key,out var kind)&&kind==p.Value),"Exact world collider coverage matches every black/gray workbook cell without duplicate/removed terrain");
        var hazards=imported.Where(c=>c.kind==CellKind.Spike).ToDictionary(c=>c.excelAddress);
        check(map.cells.Where(c=>c.kind=="spike").All(c=>hazards.TryGetValue(c.address,out var spike)&&Vector2.Distance(spike.transform.position,new Vector2(c.x+.5f,c.y+.5f))<.001f&&spike.GetComponent<BoxCollider2D>().isTrigger),"Every new red cell has its actual hazard trigger at the correct position");
        var routes=new HashSet<Vector2Int>();bool routeAligned=true;
        foreach(var sr in UnityEngine.Object.FindAnyObjectByType<RouteDisplay>().GetComponentsInChildren<SpriteRenderer>())
        {
            var b=sr.bounds;routeAligned&=Mathf.Abs(b.min.x-Mathf.Round(b.min.x))<.001f&&Mathf.Abs(b.min.y-Mathf.Round(b.min.y))<.001f;
            for(int y=Mathf.RoundToInt(b.min.y);y<Mathf.RoundToInt(b.max.y);y++)for(int x=Mathf.RoundToInt(b.min.x);x<Mathf.RoundToInt(b.max.x);x++)routes.Add(new Vector2Int(x,y));
        }
        check(routeAligned&&routes.SetEquals(map.cells.Where(c=>c.kind=="route").Select(c=>new Vector2Int(c.x,c.y))),"Blue route renderers cover exactly the current workbook guides");
        var sourceRails=map.cells.Where(c=>c.kind=="zipline").ToDictionary(c=>c.address);var covered=new List<string>();bool railAligned=true;
        foreach(var rail in level.player.ziplines)
        {
            var addresses=rail.excelAddress.Split(',');covered.AddRange(addresses);
            if(addresses.Any(a=>!sourceRails.ContainsKey(a))){railAligned=false;continue;}
            var cells=addresses.Select(a=>sourceRails[a]).ToArray();
            railAligned&=Vector2.Distance(rail.StartPoint,new Vector2(cells.Min(c=>c.x),cells[0].y+.5f))<.001f
                &&Vector2.Distance(rail.EndPoint,new Vector2(cells.Max(c=>c.x)+1,cells[0].y+.5f))<.001f;
            var line=rail.GetComponentInChildren<LineRenderer>();
            railAligned&=Vector2.Distance(line.GetPosition(0),rail.StartPoint)<.001f&&Vector2.Distance(line.GetPosition(1),rail.EndPoint)<.001f;
        }
        check(railAligned&&covered.Count==sourceRails.Count&&covered.Distinct().Count()==covered.Count&&covered.All(sourceRails.ContainsKey),"Actual zipline endpoints/cables match every current source cell without stale rails");
    }
    public static void Run()
    {
        var log=new List<string>();
        Action<bool,string> check=(ok,message)=>{log.Add((ok?"PASS ":"FAIL ")+message);};
        var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("ForestMap").text);
        var level=UnityEngine.Object.FindAnyObjectByType<ForestLevel>();
        check(level!=null,"Independent ForestLevel exists");
        var cells=UnityEngine.Object.FindObjectsByType<SeasideCell>();
        var imported=cells.Where(c=>!string.IsNullOrEmpty(c.excelAddress)).ToArray();
        check(map.width==313&&map.height==106,"Workbook coordinate frame remains 313 x 106 cells");
        check(level.sourceSha256==map.sourceSha256,"Saved scene records current workbook source hash");
        check(map.teleportDistance==10&&level.player.settings.teleportDistance==10,"Teleport ruler is 10 map cells, separate from dash 8");
        check(level.player.motor.settings==AssetDatabase.LoadAssetAtPath<SeasideSettings>("Assets/Settings/Movement.asset"),"Exact shared Seaside Movement asset reused");
        check(level.player.motor.settings.shortJumpHeight==4&&level.player.motor.settings.longJumpHeight==8&&level.player.motor.settings.dashDistance==8,"Existing movement values remain 4 / 8 / 8");
        check(level.player.motor.externalInput,"Forest-only driver owns input and delegates ordinary motion to Seaside Tick");
        check(level.player.motor.GetComponent<BoxCollider2D>().size==new Vector2(2,2),"Character is 2 x 2 cells with unit transform");
        check(level.player.motor.transform.lossyScale==Vector3.one,"Character scale does not distort contact probes");
        int terrain=(int)imported.Where(c=>c.kind==CellKind.Ground&&c.attachable).Sum(c=>c.GetComponent<BoxCollider2D>().size.x);
        int gray=(int)imported.Where(c=>c.kind==CellKind.Ground&&!c.attachable).Sum(c=>c.GetComponent<BoxCollider2D>().size.x);
        check(terrain==map.cells.Count(c=>c.kind=="ground"),"Black terrain area matches current workbook");
        check(gray==map.cells.Count(c=>c.kind=="noGrip"),"Gray terrain area matches workbook and remains non-attachable");
        check(imported.Count(c=>c.kind==CellKind.Spike)==map.cells.Count(c=>c.kind=="spike"),"Red hazard count matches current workbook");
        var railCells=map.cells.Where(c=>c.kind=="zipline").Select(c=>new Vector2Int(c.x,c.y)).ToHashSet();
        check(level.player.ziplines.Length==railCells.Count(p=>!railCells.Contains(p+Vector2Int.left)),"Every current horizontal source rail exists");
        check(Mathf.Abs(level.player.ziplines.Sum(r=>r.Length)-railCells.Count)<.001f,"Cable length matches source green cell coverage");
        CheckMapCoverage(map,level,imported,check);
        check(level.wheel.locks.Length==2&&level.wheel.locks.Select(l=>l.index).Distinct().Count()==2,"Two independent waterwheel unlock points");
        check(level.wheel.platforms.Length==12,"Twelve horizontal kinematic wheel platforms");
        check(level.wheel.platforms.Select((p,i)=>Mathf.Abs(Mathf.DeltaAngle(p.initialAngle,i*30))<.001f).All(ok=>ok),"Twelve wheel platforms spaced evenly at 30 degrees");
        check(UnityEngine.Object.FindObjectsByType<ForestGoal>().Length==1,"Single final platform goal completes on player contact");
        check(!level.cameraRig.constrainToBackground,"Forest does not accidentally use Seaside art boundaries");
        check(level.player.waterfall.occlusion!=null&&level.player.waterfall.occlusion.sprite!=null,"Existing waterfall mask is available for overview");
        check(level.player.waterfall.cameraRig==level.cameraRig,"Waterfall bounds the Forest near camera");
        var checkpoints=cells.Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
        check(checkpoints.Length==6,"Six Forest checkpoints including right upper lip");
        check(checkpoints.Select(c=>c.checkpointId).SequenceEqual(Enumerable.Range(1,6)),"Checkpoint IDs remain unique labels");
        check(level.session.latestCheckpointWins,"Forest checkpoints use last contact regardless of ID");
        check(level.player.waterfall.cameraTransitionSpeed>0,"Waterfall camera transition speed is editable and positive");
        foreach(var checkpoint in checkpoints)
        {
            check(checkpoint.respawnAnchor!=null,"Checkpoint has editable respawn anchor "+checkpoint.name);
            var p=(Vector2)checkpoint.respawnAnchor.position;
            check(!Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0).Any(c=>!c.isTrigger&&c.gameObject.layer==6),"Checkpoint body clears solid terrain "+checkpoint.name);
            check(!Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,1<<7).Any(),"Checkpoint body clears spikes "+checkpoint.name);
            check(Physics2D.Raycast(p+Vector2.down,Vector2.down,.2f,1<<6).collider!=null,"Checkpoint has static floor "+checkpoint.name);
        }
        check(level.player.waterfall.curtainBounds.isTrigger,"Waterfall is passable; occlusion is visual only");
        check(UnityEngine.Object.FindObjectsByType<SeasideHUD>().Length==0,"Forest owns its help and completion UI");
        check(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ForestBuilder.ScenePath),"Forest included in sequential campaign");
        foreach(var c in imported.Where(c=>c.kind==CellKind.Ground))check(c.gameObject.layer==6,"Ground layer "+c.excelAddress);
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script: "+t.name);
        string path=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/forest-editor-checks.txt");File.WriteAllLines(path,log);
        if(log.Any(s=>s.StartsWith("FAIL")))throw new Exception("Forest editor validation failed; see "+path);
        Debug.Log("FOREST_EDITOR_CHECKS_PASS assertions="+log.Count);
    }
}
