using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Seaside;

public static class SeasideHierarchyRefactor
{
    static string Work => Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261029-hierarchy-refactor");
    public static void ApplyBatch()
    {
        EditorSceneManager.OpenScene(SeasideArtSetup.PreviewScene);
        Apply();
    }
    [MenuItem("Seaside/Scene/Normalize preview hierarchy")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before refactoring.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=SeasideArtSetup.PreviewScene)throw new InvalidOperationException("Open SeasideArtPreview first.");
        if(Objects<SeasideMapRoot>(scene).Any())throw new InvalidOperationException("Hierarchy is already normalized; manual edits are preserved.");
        var map=SeasideMapRoot.FindInScene(scene);
        var part=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Part1Art");
        if(map==null||part==null)throw new InvalidOperationException("Expected saved map and Part1Art roots.");
        if(part.GetComponentsInChildren<SpriteRenderer>(true).Length==0)throw new InvalidOperationException("Part1Art has no artwork.");
        var original=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
        var before=Snapshot(original);
        string geometry=SeasideSceneGeometry.Signature(scene);
        // Session-local identity survives object renaming and reparenting.
        var oldPaths=original.ToDictionary(o=>o.GetEntityId(),o=>PathOf(o.transform));
        Directory.CreateDirectory(Work);
        File.WriteAllLines(Path.Combine(Work,"before-world-state.txt"),before.OrderBy(k=>k.Key).Select(k=>k.Key+"|"+k.Value));
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Normalize Seaside preview hierarchy");
        try
        {
            var root=Group("Seaside",null,scene);
            var gameplay=Group("Gameplay",root,scene);
            var systems=Group("Systems",root,scene);
            var art=Group("Art",root,scene);
            Move(map.transform,gameplay);Rename(map,"Map");Undo.AddComponent<SeasideMapRoot>(map);
            foreach(var child in map.transform.Cast<Transform>().ToArray())
            {
                if(child.name.StartsWith("01 TERRAIN"))Rename(child.gameObject,"Terrain");
                else if(child.name.StartsWith("02 SPIKES"))Rename(child.gameObject,"Hazards");
                else if(child.name.StartsWith("03 CHECKPOINTS"))Rename(child.gameObject,"Checkpoints");
                else if(child.name.StartsWith("04 BLUE ROUTE"))Rename(child.gameObject,"RouteGuides");
            }
            var markers=Group("Markers",map.transform,scene);
            foreach(var label in map.GetComponentsInChildren<TextMesh>(true))
            {
                Move(label.transform,markers);
                Rename(label.gameObject,label.text.Trim().Equals("START",StringComparison.OrdinalIgnoreCase)?"Marker_StartText":"Marker_FinishText");
            }
            foreach(var cell in map.GetComponentsInChildren<SeasideCell>(true))
            {
                string address=Safe(cell.excelAddress);
                switch(cell.kind)
                {
                    case CellKind.Ground:Rename(cell.gameObject,"Terrain_"+address);break;
                    case CellKind.Spike:Rename(cell.gameObject,"Spike_"+address);break;
                    case CellKind.Checkpoint:
                        Rename(cell.gameObject,"Checkpoint_"+cell.checkpointId.ToString("D2")+"_"+address);
                        if(cell.respawnAnchor!=null)Rename(cell.respawnAnchor.gameObject,"RespawnAnchor");break;
                    case CellKind.Finish:Rename(cell.gameObject,"FinishTrigger");break;
                }
            }
            foreach(var route in map.GetComponentsInChildren<RouteDisplay>(true))
                foreach(Transform item in route.transform)
                    Rename(item.gameObject,"Route_"+Safe(item.name.StartsWith("Route ")?item.name.Substring(6):item.name));
            foreach(var player in Objects<SeasideMotor>(scene)){Move(player.transform,gameplay);Rename(player.gameObject,"Player");}
            foreach(var session in Objects<SeasideSession>(scene)){Move(session.transform,systems);Rename(session.gameObject,"GameSession");}
            foreach(var camera in Objects<SeasideCamera>(scene)){Move(camera.transform,systems);Rename(camera.gameObject,"MainCamera");}
            Move(part.transform,art);Rename(part,"Part1");Undo.AddComponent<SeasideArtRoot>(part);
            var backgrounds=Group("Backgrounds",part.transform,scene);
            foreach(var visual in part.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Move(visual.transform,backgrounds);
                Rename(visual.gameObject,visual.sprite!=null&&visual.sprite.name.StartsWith("bg3")?"Background_Far":"Background_"+Safe(visual.gameObject.name));
            }
            // Existing children of checkpoint/route/player are kept with their owners.
            var after=Snapshot(original);
            var changed=before.Where(k=>!after.ContainsKey(k.Key)||after[k.Key]!=k.Value).Select(k=>k.Key).ToArray();
            if(changed.Length!=0||geometry!=SeasideSceneGeometry.Signature(scene))
                throw new Exception("World state changed: "+string.Join(",",changed));
            if(SeasideMapRoot.FindInScene(scene)!=map)throw new Exception("Map resolver did not return the marked Map.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllLines(Path.Combine(Work,"after-world-state.txt"),after.OrderBy(k=>k.Key).Select(k=>k.Key+"|"+k.Value));
            File.WriteAllLines(Path.Combine(Work,"rename-map.tsv"),new[]{"Before\tAfter"}.Concat(original.Select(o=>oldPaths[o.GetEntityId()]+"\t"+PathOf(o.transform))));
            File.WriteAllLines(Path.Combine(Work,"hierarchy.txt"),root.GetComponentsInChildren<Transform>(true).Select(t=>PathOf(t)));
            File.WriteAllText(Path.Combine(Work,"refactor-result.txt"),"PASS Existing objects checked: "+original.Length+
                "\nPASS World transforms, tags/layers, active state, existing components and references unchanged\nPASS Cell geometry and checkpoint data unchanged\nPASS Name-independent map resolver\nPASS Scene saved\n");
            Undo.CollapseUndoOperations(undo);Selection.activeGameObject=root.gameObject;
            Debug.Log("SEASIDE_HIERARCHY_REFACTOR_PASS objects="+original.Length+" scene="+scene.path);
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }

    public static void ValidateScenes()
    {
        var original=EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        var expected=SeasideSceneGeometry.Signature(original);
        if(SeasideMapRoot.FindInScene(original)==null)throw new Exception("Legacy map resolver failed.");
        var preview=EditorSceneManager.OpenScene(SeasideArtSetup.PreviewScene);
        if(expected!=SeasideSceneGeometry.Signature(preview))throw new Exception("Map world geometry differs.");
        var marker=Objects<SeasideMapRoot>(preview).Single();
        var resolved=SeasideMapRoot.FindInScene(preview);
        if(resolved!=marker.gameObject)throw new Exception("Map marker resolution failed.");
        string name=marker.name;marker.name="ArbitraryRenamedMap";
        if(SeasideMapRoot.FindInScene(preview)!=resolved)throw new Exception("Map resolver still depends on name.");
        marker.name=name;
        var artwork=Objects<SeasideArtRoot>(preview).Single();
        if(artwork.GetComponentsInChildren<SpriteRenderer>(true).Length<1)throw new Exception("Layer artwork missing.");
        string report="PASS Original scene resolves without name lookup\nPASS Refactored scene resolves by marker\nPASS Map rename does not affect resolution\nPASS Original and preview world geometry match\nPASS User artwork remains present\n";
        File.WriteAllText(Path.Combine(SeasidePaths.ReleaseRoot,"Docs/hierarchy-checks.txt"),report);
        Debug.Log("SEASIDE_HIERARCHY_CHECKS_PASS");
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
    }
    public static void CapturePreview()
    {
        EditorSceneManager.OpenScene(SeasideArtSetup.PreviewScene);
        var follow=Objects<SeasideCamera>(SceneManager.GetActiveScene()).Single();
        var camera=follow.GetComponent<Camera>();camera.aspect=16f/9;follow.overview=true;follow.Snap();
        var target=new RenderTexture(1920,1080,24);
        var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(Work,"refactored-scene.png"),pixels.EncodeToPNG());
            Debug.Log("SEASIDE_REFACTOR_CAPTURE_PASS");
        }
        finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
    static IEnumerable<T> Objects<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true));
    static Transform Group(string name,Transform parent,Scene scene)
    {
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);Undo.RegisterCreatedObjectUndo(go,"Create "+name);
        if(parent!=null)go.transform.SetParent(parent,false);return go.transform;
    }
    static void Move(Transform item,Transform parent){Undo.SetTransformParent(item,parent,"Group "+item.name);}
    static void Rename(GameObject go,string name){Undo.RecordObject(go,"Rename "+go.name);go.name=name;}
    static string Safe(string value)=>string.IsNullOrEmpty(value)?"Unassigned":value.Replace(":","_").Replace(",","_").Replace(" ","_");
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static string Float(float n)=>Math.Round(n,4).ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
    static string Vec(Vector3 v)=>Float(v.x)+","+Float(v.y)+","+Float(v.z);
    static Dictionary<string,string> Snapshot(IEnumerable<GameObject> objects)
    {
        var rows=new Dictionary<string,string>();
        foreach(var go in objects)
        {
            string id=go.GetEntityId().ToString();var q=go.transform.rotation;
            rows[id+"/Object"]=Vec(go.transform.position)+"|"+Vec(go.transform.lossyScale)+"|"+Float(q.x)+","+Float(q.y)+","+Float(q.z)+","+Float(q.w)+"|"+go.layer+"|"+go.tag+"|"+go.activeInHierarchy;
            var components=go.GetComponents<Component>();
            for(int i=0;i<components.Length;i++)
            {
                var c=components[i];if(c==null)throw new Exception("Missing script on "+go.name);
                if(c is Transform||c is SeasideMapRoot||c is SeasideArtRoot)continue;
                rows[id+"/"+c.GetType().FullName+"/"+i]=EditorJsonUtility.ToJson(c);
            }
        }
        return rows;
    }
}
