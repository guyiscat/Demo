using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Explicit workbook-difference update of the saved scene. Unchanged source
// objects retain their IDs and manual edits. Never calls CreateScene/NewScene.
public static class ForestMapRevision
{
    sealed class Segment
    {
        public string key,kind,address,name;
        public Vector2 center,size;
    }
    static readonly List<string> report=new List<string>();
    static string Argument(string name)
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);
        if(i<0||i+1>=args.Length)throw new ArgumentException("Missing "+name);
        return args[i+1];
    }
    public static void RunPlayModeChecks()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch-only Play Mode validation.");
        EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        EditorApplication.EnterPlaymode();
    }
    public static void Capture()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch-only saved-scene capture.");
        EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        var level=Object.FindAnyObjectByType<ForestLevel>();var water=level.player.waterfall;
        string folder=Argument("-forestMapCapture");Directory.CreateDirectory(folder);
        foreach(int side in new[]{-1,1})foreach(bool overview in new[]{true,false})
        {
            var b=water.curtainBounds.bounds;
            level.player.transform.position=new Vector2(side<0?b.min.x-2:b.max.x+2,60);
            level.cameraRig.overview=overview;Physics2D.SyncTransforms();
            level.cameraRig.Snap();water.ResetSide(level.player.transform.position);
            var camera=level.cameraRig.GetComponent<Camera>();var target=new RenderTexture(1565,610,24);
            var pixels=new Texture2D(1565,610,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try
            {
                camera.targetTexture=target;level.cameraRig.Snap();water.Refresh();camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1565,610),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,(overview?"overview":"near")+(side<0?"-left":"-right")+".png"),pixels.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);}
        }
        Debug.Log("FOREST_MAP_CAPTURE_PASS saved scene only; no scene was saved.");
    }
    public static void Apply()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project for a saved-scene update.");
        report.Clear();
        var previous=JsonUtility.FromJson<ForestMapData>(File.ReadAllText(Argument("-forestPreviousMap")));
        var next=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("ForestMap").text);
        if(previous.width!=next.width||previous.height!=next.height||previous.minColumn!=next.minColumn||previous.bottomRow!=next.bottomRow
            ||previous.drawings.Length!=next.drawings.Length
            ||previous.drawings.Where((d,i)=>d.kind!=next.drawings[i].kind||d.start!=next.drawings[i].start||d.end!=next.drawings[i].end).Any())
            throw new InvalidOperationException("Coordinate frame/drawings changed; this patch only updates cell layout.");
        var scene=EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        var level=Object.FindAnyObjectByType<ForestLevel>();
        if(level.sourceSha256!=previous.sourceSha256)throw new InvalidOperationException("Scene does not match the supplied previous workbook snapshot.");
        var imported=Object.FindObjectsByType<SeasideCell>().Where(c=>!string.IsNullOrEmpty(c.excelAddress)).ToArray();
        PatchTerrain(previous,next,imported);
        PatchSpikes(next,imported);
        PatchRoutes(previous,next);
        PatchRails(previous,next,level.player);
        Physics2D.SyncTransforms();
        foreach(var point in Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint))
        {
            Vector2 old=point.respawnAnchor.position;
            if(SafeSpawn(old)){report.Add("KEEP checkpoint="+point.checkpointId+" position="+old);continue;}
            Vector2 safe=NearestSpawn(old);
            point.transform.position+=(Vector3)(safe-old);point.respawnAnchor.position=safe;point.respawn=safe;
            Physics2D.SyncTransforms();report.Add("MOVE checkpoint="+point.checkpointId+" from="+old+" to="+safe);
        }
        if(!SafeSpawn(level.session.startPoint))throw new InvalidOperationException("Original spawn is no longer safe; inspect new entrance before saving.");
        level.sourceSha256=next.sourceSha256;
        // Validate before saving; a failed patch never replaces the saved scene.
        ForestEditorChecks.Run();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string path=Path.GetFullPath(Argument("-forestMapReport"));Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path,report);
        Debug.Log("FOREST_MAP_REVISION_PASS source="+next.sourceSha256+" changes="+report.Count+" scene="+ForestBuilder.ScenePath);
    }
    static List<Segment> Segments(ForestMapData map)
    {
        var result=new List<Segment>();
        foreach(string kind in new[]{"ground","noGrip"})foreach(var row in map.cells.Where(c=>c.kind==kind).GroupBy(c=>c.y))
        {
            var cells=row.OrderBy(c=>c.x).ToArray();
            for(int first=0;first<cells.Length;)
            {
                int last=first;while(last+1<cells.Length&&cells[last+1].x==cells[last].x+1)last++;
                int width=last-first+1;string address=cells[first].address+":"+cells[last].address;
                result.Add(new Segment{kind=kind,address=address,key=kind+":"+address,
                    name=(kind=="ground"?"Terrain_":"NoGrip_")+cells[first].address+"_"+cells[last].address,
                    center=new Vector2(cells[first].x+width*.5f,row.Key+.5f),size=new Vector2(width,1)});
                first=last+1;
            }
        }
        return result;
    }
    static void PatchTerrain(ForestMapData previous,ForestMapData next,SeasideCell[] imported)
    {
        var before=Segments(previous).ToDictionary(s=>s.key);var after=Segments(next).ToDictionary(s=>s.key);
        var existing=imported.Where(c=>c.kind==CellKind.Ground).ToDictionary(c=>(c.attachable?"ground:":"noGrip:")+c.excelAddress);
        if(before.Keys.Any(k=>!existing.ContainsKey(k)))throw new InvalidOperationException("Missing previous terrain row; refusing unrelated geometry replacement.");
        var template=existing.Values.First();
        foreach(var entry in after.Where(p=>!before.ContainsKey(p.Key)))
        {
            var s=entry.Value;var cell=Object.Instantiate(template,template.transform.parent);cell.name=s.name;
            cell.transform.SetPositionAndRotation(s.center,Quaternion.identity);cell.transform.localScale=Vector3.one;
            cell.excelAddress=s.address;cell.attachable=s.kind=="ground";
            cell.GetComponent<BoxCollider2D>().size=s.size;
            var sprite=cell.GetComponent<SpriteRenderer>();sprite.size=s.size;
            sprite.color=cell.attachable?new Color(.07f,.1f,.09f):new Color(.52f,.54f,.54f);
            report.Add("ADD terrain="+s.key+" position="+s.center+" size="+s.size);
        }
        foreach(var key in before.Keys.Where(k=>!after.ContainsKey(k)))
        {Object.DestroyImmediate(existing[key].gameObject);report.Add("REMOVE terrain="+key);}
        report.Add("KEEP terrainRows="+before.Keys.Count(after.ContainsKey));
    }
    static void PatchSpikes(ForestMapData next,SeasideCell[] imported)
    {
        var existing=imported.Where(c=>c.kind==CellKind.Spike).ToDictionary(c=>c.excelAddress);
        var after=next.cells.Where(c=>c.kind=="spike").ToDictionary(c=>c.address);
        var solids=new HashSet<Vector2Int>(next.cells.Where(c=>c.kind=="ground"||c.kind=="noGrip").Select(c=>new Vector2Int(c.x,c.y)));
        var template=existing.Values.First();
        foreach(var entry in after)
        {
            bool added=!existing.TryGetValue(entry.Key,out var cell);var c=entry.Value;
            if(added)
            {
                cell=Object.Instantiate(template,template.transform.parent);cell.name="Spike_"+c.address;
                cell.transform.position=new Vector2(c.x+.5f,c.y+.5f);cell.transform.localScale=Vector3.one;cell.excelAddress=c.address;
                report.Add("ADD spike="+entry.Key);
            }
            Quaternion rotation=Quaternion.FromToRotation(Vector3.up,SpikeNormal(c,solids));
            if(Quaternion.Angle(cell.transform.rotation,rotation)>.01f)
            {cell.transform.rotation=rotation;if(!added)report.Add("ROTATE spike="+entry.Key+" to new terrain normal");}
        }
        foreach(var entry in existing.Where(p=>!after.ContainsKey(p.Key)))
        {Object.DestroyImmediate(entry.Value.gameObject);report.Add("REMOVE spike="+entry.Key);}
        report.Add("KEEP spikes="+existing.Keys.Count(after.ContainsKey));
    }
    static Vector2 SpikeNormal(ForestMapCell c,HashSet<Vector2Int> solids)
    {
        for(int distance=1;distance<=5;distance++)foreach(var d in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
            if(solids.Contains(new Vector2Int(c.x,c.y)+d*distance))return -(Vector2)d;
        return Vector2.up;
    }
    public static List<List<ForestMapCell>> Groups(ForestMapData map,string kind)
    {
        var source=map.cells.Where(c=>c.kind==kind).ToDictionary(c=>new Vector2Int(c.x,c.y));var result=new List<List<ForestMapCell>>();
        while(source.Count>0)
        {
            var first=source.Values.OrderBy(c=>c.row).ThenBy(c=>c.col).First();var queue=new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(first.x,first.y));var group=new List<ForestMapCell>();
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(!source.TryGetValue(p,out var c))continue;source.Remove(p);group.Add(c);
                foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(source.ContainsKey(p+d))queue.Enqueue(p+d);
            }
            result.Add(group);
        }
        return result;
    }
    static string GroupKey(IEnumerable<ForestMapCell> cells)=>string.Join(",",cells.Select(c=>c.address).OrderBy(a=>a,StringComparer.Ordinal));
    static void BoundsFor(List<ForestMapCell> group,out Vector2 center,out Vector2 size)
    {
        float x=group.Min(c=>c.x),y=group.Min(c=>c.y);size=new Vector2(group.Max(c=>c.x)+1-x,group.Max(c=>c.y)+1-y);center=new Vector2(x,y)+size*.5f;
    }
    static void PatchRoutes(ForestMapData previous,ForestMapData next)
    {
        var display=Object.FindAnyObjectByType<RouteDisplay>();var sprites=display.GetComponentsInChildren<SpriteRenderer>();
        var before=Groups(previous,"route").ToDictionary(GroupKey);var after=Groups(next,"route").ToDictionary(GroupKey);
        var template=sprites.First();
        foreach(var p in after.Where(p=>!before.ContainsKey(p.Key)))
        {
            BoundsFor(p.Value,out var center,out var size);var sr=Object.Instantiate(template,display.transform);
            sr.name="Route_"+p.Value[0].address;sr.transform.position=center;sr.transform.rotation=Quaternion.identity;sr.transform.localScale=Vector3.one;sr.size=size;
            report.Add("ADD route="+p.Key);
        }
        foreach(var p in before.Where(p=>!after.ContainsKey(p.Key)))
        {
            var sr=sprites.Single(s=>s!=null&&s.name=="Route_"+p.Value[0].address);Object.DestroyImmediate(sr.gameObject);report.Add("REMOVE route="+p.Key);
        }
        report.Add("KEEP routes="+before.Keys.Count(after.ContainsKey));
    }
    static void PatchRails(ForestMapData previous,ForestMapData next,ForestPlayer player)
    {
        var before=Groups(previous,"zipline").ToDictionary(GroupKey);var after=Groups(next,"zipline");
        var existing=player.ziplines.ToDictionary(r=>string.Join(",",r.excelAddress.Split(',').OrderBy(a=>a,StringComparer.Ordinal)));
        if(before.Keys.Any(k=>!existing.ContainsKey(k)))throw new InvalidOperationException("Previous zipline metadata mismatch.");
        var updated=new List<ForestZipline>();var template=player.ziplines[0];
        foreach(var group in after)
        {
            string key=GroupKey(group);
            if(before.ContainsKey(key)){updated.Add(existing[key]);continue;}
            var rail=Object.Instantiate(template,template.transform.parent);rail.name="Zipline_"+group[0].address;rail.excelAddress=string.Join(",",group.Select(c=>c.address));
            BoundsFor(group,out var center,out var size);
            rail.start.position=new Vector2(center.x-size.x*.5f,center.y);rail.end.position=new Vector2(center.x+size.x*.5f,center.y);
            var line=rail.GetComponentInChildren<LineRenderer>();line.SetPosition(0,rail.StartPoint);line.SetPosition(1,rail.EndPoint);
            // Grip markers are renderers (not endpoint references) in the existing whitebox.
            foreach(var sr in rail.GetComponentsInChildren<SpriteRenderer>())
                sr.transform.position=sr.name=="StartGrip"?rail.StartPoint:rail.EndPoint;
            updated.Add(rail);report.Add("ADD zipline="+key+" start="+rail.StartPoint+" end="+rail.EndPoint);
        }
        var keep=new HashSet<ForestZipline>(updated);
        foreach(var rail in player.ziplines.Where(r=>!keep.Contains(r)))
        {report.Add("REMOVE zipline="+rail.excelAddress);Object.DestroyImmediate(rail.gameObject);}
        player.ziplines=updated.ToArray();report.Add("ZIPLINES count="+updated.Count);
    }
    static bool SafeSpawn(Vector2 p)
    {
        if(Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0).Any(c=>(c.gameObject.layer==6&&!c.isTrigger&&c.GetComponent<ForestWheelPlatform>()==null)||c.gameObject.layer==7))return false;
        var hit=Physics2D.Raycast(p+Vector2.down,Vector2.down,.2f,1<<6);
        return hit.collider!=null&&hit.collider.GetComponent<ForestWheelPlatform>()==null;
    }
    static Vector2 NearestSpawn(Vector2 original)
    {
        var options=new List<Vector2>();
        foreach(var collider in Object.FindObjectsByType<BoxCollider2D>().Where(c=>c.gameObject.layer==6&&!c.isTrigger&&c.GetComponent<ForestWheelPlatform>()==null))
        {
            var b=collider.bounds;
            if(Mathf.Abs(b.max.y+1.05f-original.y)>12)continue;
            for(float x=Mathf.Max(b.min.x+1.15f,original.x-12);x<=Mathf.Min(b.max.x-1.15f,original.x+12);x+=.5f)
                options.Add(new Vector2(x,b.max.y+1.05f));
        }
        foreach(var p in options.OrderBy(p=>(p-original).sqrMagnitude))if(SafeSpawn(p))return p;
        throw new InvalidOperationException("No safe static respawn near "+original);
    }
}
