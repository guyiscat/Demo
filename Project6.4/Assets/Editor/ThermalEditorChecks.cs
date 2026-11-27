using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expedition;
using Forest;
using Seaside;
using UnityEditor;
using UnityEngine;
public static class ThermalEditorChecks
{
    public static void Run(bool snow)
    {
        var log=new List<string>();Action<bool,string> check=(ok,label)=>log.Add((ok?"PASS ":"FAIL ")+label);
        string key=snow?"Snow":"Lighthouse";
        var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>(key+"Map").text);
        var level=UnityEngine.Object.FindAnyObjectByType<ThermalLevel>();Physics2D.SyncTransforms();
        check(level!=null&&level.sourceSha256==map.sourceSha256,"Saved scene retains read-only workbook source hash");
        check(level.cameraRig.mapSize==new Vector2(map.width,map.height),"Full-map camera uses exact source bounds");
        var expected=map.cells.Where(c=>ThermalLevelBuilder.IsSolid(c.kind)).ToDictionary(c=>new Vector2Int(c.x,c.y),c=>c.kind);
        var actual=new Dictionary<Vector2Int,string>();bool duplicate=false,aligned=true;
        var cells=UnityEngine.Object.FindObjectsByType<SeasideCell>();
        foreach(var cell in cells.Where(c=>c.kind==CellKind.Ground))
        {
            var collider=cell.GetComponent<BoxCollider2D>();var b=collider.bounds;var thermal=cell.GetComponent<ThermalSurface>();
            string kind=thermal!=null?(thermal.requiredProtection==ProtectionMode.Cold?"coldSurface":"hotSurface"):cell.attachable?"ground":"noGrip";
            aligned&=Mathf.Abs(b.min.x-Mathf.Round(b.min.x))<.001f&&Mathf.Abs(b.min.y-Mathf.Round(b.min.y))<.001f;
            for(int y=Mathf.RoundToInt(b.min.y);y<Mathf.RoundToInt(b.max.y);y++)for(int x=Mathf.RoundToInt(b.min.x);x<Mathf.RoundToInt(b.max.x);x++)
            {var p=new Vector2Int(x,y);if(actual.ContainsKey(p))duplicate=true;actual[p]=kind;}
            check(cell.gameObject.layer==6&&!collider.isTrigger,"Physical solid "+cell.excelAddress);
        }
        check(aligned&&!duplicate&&actual.Count==expected.Count&&expected.All(e=>actual.TryGetValue(e.Key,out var k)&&k==e.Value),"Every source solid has exact nonduplicated geometry and protection type");
        var spikes=cells.Where(c=>c.kind==CellKind.Spike).ToArray();
        check(spikes.Select(c=>c.excelAddress).OrderBy(s=>s).SequenceEqual(map.cells.Where(c=>c.kind=="spike").Select(c=>c.address).OrderBy(s=>s)),"Every authored red cell exists exactly once");
        foreach(var c in spikes)check(c.gameObject.layer==7&&c.GetComponent<BoxCollider2D>().isTrigger,"Spike trigger "+c.excelAddress);
        foreach(var kind in new[]{"coldPoint","hotPoint"})
        {
            var mode=kind=="coldPoint"?ProtectionMode.Cold:ProtectionMode.Heat;
            var groups=ForestMapRevision.Groups(map,kind);var points=level.thermal.points.Where(p=>p.requiredProtection==mode).ToArray();
            check(points.Length==groups.Count,"All "+kind+" groups are distinct aerial jump anchors");
            foreach(var group in groups)
            {
                var point=points.SingleOrDefault(p=>p.sourceAddress.Split(',').Contains(group[0].address));
                var center=new Vector2((group.Min(c=>c.x)+group.Max(c=>c.x)+1)*.5f,(group.Min(c=>c.y)+group.Max(c=>c.y)+1)*.5f);
                check(point!=null&&(Vector2)point.transform.position==center&&point.GetComponent<Collider2D>()==null&&point.rangeRing!=null,"Climb ring preserves authored coordinates and is not a solid: "+group[0].address);
            }
        }
        check(level.player.environment==level.thermal&&level.thermal.settings==level.settings,"Optional environment rules wired only to this independent player");
        check(level.player.wheel==null&&level.player.waterfall==null,"No unrequested Forest wheel or Mine lift instantiated");
        var railGroups=ForestMapRevision.Groups(map,"coldZipline").Concat(ForestMapRevision.Groups(map,"hotZipline")).ToArray();
        check(level.player.ziplines.Length==railGroups.Length,"Every cold/hot horizontal cable is a genuine zipline");
        foreach(var group in railGroups)
        {
            var rail=level.player.ziplines.SingleOrDefault(r=>r.excelAddress.Split(',').Contains(group[0].address));
            check(rail!=null&&Mathf.Abs(rail.StartPoint.x-group.Min(c=>c.x))<.001f&&Mathf.Abs(rail.EndPoint.x-group.Max(c=>c.x)-1)<.001f&&Mathf.Abs(rail.StartPoint.y-group[0].y-.5f)<.001f,"Cable coordinates match source: "+group[0].address);
            check(rail!=null&&rail.GetComponent<ThermalZipline>().requiredProtection==(group[0].kind=="coldZipline"?ProtectionMode.Cold:ProtectionMode.Heat),"Cable requires authored protection: "+group[0].address);
        }
        check(level.player.motor.settings==GameSystemSettings.Current.movement,"Common movement parameters and 2x2 body reused");
        check(level.player.settings.teleportInterruptsDash&&level.player.settings.teleportHoverDuration>=0,"Current configured teleport interruption and adjustable hover inherited");
        foreach(var cp in cells.Where(c=>c.kind==CellKind.Checkpoint))
        {
            Vector2 p=cp.respawnAnchor.position;
            check(!Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(),"Clear checkpoint respawn: "+cp.name);
            var floor=Physics2D.Raycast(p+Vector2.down,Vector2.down,.2f,1<<6).collider;
            check(floor!=null&&floor.GetComponent<ThermalSurface>()==null,"Checkpoint rests on neutral static ground: "+cp.name);
        }
        var start=level.session.startPoint;check(!Physics2D.OverlapBoxAll(start,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(),"Safe independent entry");
        check(level.goal!=null&&level.goal.isTrigger&&level.goal.GetComponent<SeasideCell>()==null,"Goal requires authored E interaction, not incidental overlap");
        check(snow?level is Snow.SnowLevel:level is Lighthouse.LighthouseLevel,"Scene has its independent level controller");
        if(!snow){var collapse=((Lighthouse.LighthouseLevel)level).collapse;check(collapse!=null&&collapse.mapRoot!=null&&collapse.front!=null&&collapse.speed>0&&collapse.segmentTimeLimit>0,"Tower timed road collapse is wired with adjustable chase parameters");}
        check(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ThermalLevelBuilder.ScenePath(snow)),"Thermal scene included in sequential campaign");
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing component: "+t.name);
        string output=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/"+key.ToLowerInvariant()+"-editor-checks.txt");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,log);
        if(log.Any(s=>s.StartsWith("FAIL")))throw new Exception(key+" scene checks failed: "+output);
        Debug.Log(key.ToUpperInvariant()+"_EDITOR_CHECKS_PASS assertions="+log.Count);
    }
}
