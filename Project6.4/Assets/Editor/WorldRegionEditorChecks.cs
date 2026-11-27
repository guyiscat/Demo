using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Mine;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class WorldRegionEditorChecks
{
    public static void Run()
    {
        var lines=new List<string>();Action<bool,string> check=(ok,label)=>lines.Add((ok?"PASS ":"FAIL ")+label);
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        check(CampaignFlow.Scenes.Length==6&&EditorBuildSettings.scenes.Count(s=>s.enabled)==7,"Six regions plus menu included");
        var settings=GameSystemSettings.Current;
        check(settings.respawnDelay==.5f&&settings.regionBlackDuration==1.5f&&settings.regionEntrySpeed>0,"Shared death lock, black duration, exposed entry speed configured");
        foreach(var path in CampaignFlow.Scenes)
        {
            EditorSceneManager.OpenScene(path);Physics2D.SyncTransforms();var session=Object.FindAnyObjectByType<SeasideSession>();var region=session.GetComponent<WorldRegion>();
            check(region!=null&&region.session==session&&region.entry!=null&&region.fromRight!=null&&region.fromMine!=null,"Region anchors/references "+path);
            check(session.GetComponent<TravelPointMenu>()?.session==session&&Object.FindObjectsByType<TravelPoint>().Length==1,"One entrance travel marker and menu "+path);
            check(Object.FindObjectsByType<CheckpointVisual>(FindObjectsInactive.Include).Length==0,"No old checkpoint toggle components "+path);
            foreach(var cp in Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint))
                check(cp.GetComponentsInChildren<Transform>(true).All(t=>t.name!="StateOff"&&t.name!="StateOn"),"No old state children on "+cp.name);
            foreach(var anchor in new[]{region.entry,region.fromRight,region.fromMine})
            {
                Vector2 p=anchor.position;
                check(!Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,(1<<6)|(1<<7)).Any(c=>!c.isTrigger||c.gameObject.layer==7),"Arrival clear of solids/spikes "+path+" "+anchor.name);
                check(Physics2D.Raycast(p+Vector2.down,Vector2.down,.25f,1<<6).collider!=null,"Arrival has static support "+path+" "+anchor.name);
            }
            foreach(var exit in region.exits)
            {
                check(exit.session==session&&CampaignFlow.Scenes.Contains(exit.destination)&&exit.GetComponent<Collider2D>().isTrigger,"Physical region exit wired "+exit.name);
                if(!exit.requiresInteraction&&exit.destination!=CampaignFlow.Scenes[2])
                {
                    var center=(Vector2)exit.GetComponent<Collider2D>().bounds.center;
                    var anchor=center.x>region.entry.position.x+40?(Vector2)region.fromRight.position:(Vector2)region.entry.position;
                    var delta=new Vector2(center.x-anchor.x,0);
                    var blockers=Physics2D.BoxCastAll(anchor,new Vector2(1.98f,1.98f),0,delta.normalized,delta.magnitude,1<<6).Where(h=>!h.collider.isTrigger&&h.normal.sqrMagnitude>.1f&&Vector2.Dot(h.normal,delta)<-.01f).ToArray();
                    check(blockers.Length==0,"Walkable connector corridor "+path+"/"+exit.name+" blockers="+string.Join(",",blockers.Select(h=>h.collider.name)));
                }
            }
            if(path==CampaignFlow.Scenes[0])check(region.exits.Single(e=>e.destination==CampaignFlow.Scenes[4]).requiresLighthouse,"Seaside west Lighthouse exit requires Snow continue");
            if(path==CampaignFlow.Scenes[5])
            {
                check(region.exits.Any(e=>e.destination==CampaignFlow.Scenes[2])&&region.exits.Any(e=>e.destination==CampaignFlow.Scenes[1])&&region.exits.Any(e=>e.destination==CampaignFlow.Scenes[3]),"Village pit/Mine and left/Forest right/Snow topology");
                var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("VillageMap").text);
                check(Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Ground).Sum(c=>Mathf.RoundToInt(c.GetComponent<BoxCollider2D>().size.x*c.GetComponent<BoxCollider2D>().size.y))==map.cells.Count(c=>c.kind=="ground"),"Village neutral geometry matches all 452 authored cells");
            }
            var actor=session.player.GetComponent<ForestPlayer>();if(actor!=null)check(actor.settings.teleportHoverDuration>=0&&actor.settings.teleportHoverDuration<=10,"Valid exposed teleport hover "+path);
            var mine=Object.FindAnyObjectByType<MineLevel>();if(mine!=null)
            {
                check(mine.lift!=null&&mine.deepLift!=null&&mine.gate!=null,"Mine two physical lifts and gate");
                check(Object.FindObjectsByType<MineTouchSwitch>().Count()==2,"Mine yellow/purple touch switches");
                check(mine.interactions.Count(i=>i.completesLevel)==1&&mine.interactions.Single(i=>i.completesLevel).GetComponent<SpriteRenderer>().color.r>.9f,"Golden collectible replaces misread blue stop");
                check(mine.cameraRig.GetComponent<MineDarkness>()?.maskMaterial!=null&&mine.cameraRig.GetComponent<MineCorruption>()!=null,"Mine circular darkness and corruption render components");
                check(mine.deepLift.upperStop==new Vector2(120,85.5f)&&mine.deepLift.lowerStop==new Vector2(120,1.5f),"Deep blue-marker lift endpoints");
            }
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script "+path+"/"+t.name);
        }
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        string output=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/world-region-editor-checks.txt");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,lines);
        if(lines.Any(l=>l.StartsWith("FAIL")))throw new Exception("World region editor validation failed: "+output);
        Debug.Log("WORLD_REGION_EDITOR_PASS assertions="+lines.Count);
    }
}
