using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelCopySetup
{
    public static void Install()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project to preserve open authored scenes.");
        var lines=new List<string>();
        foreach(var path in CampaignFlow.Scenes)
        {
            var scene=EditorSceneManager.OpenScene(path);
            var session=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();
            if(path==CampaignFlow.Scenes[0])
            {
                var story=session.GetComponent<SeasideStory>()??session.gameObject.AddComponent<SeasideStory>();story.session=session;
                var art=UnityEngine.Object.FindAnyObjectByType<SeasideArtRoot>();
                // Resolve the source sprite once, then serialize the collider reference. Runtime ignores names/paths.
                var landmark=art.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.sprite!=null&&r.sprite.name=="lighthouse");
                story.lighthouse=landmark.GetComponent<Collider2D>();
                if(story.lighthouse==null)throw new Exception("Authored lighthouse collision not found.");
                lines.Add("Seaside lighthouse artwork anchor: "+landmark.transform.position+"; interactionDistance="+story.interactionDistance);
                SetZones(session,new[]{
                    Zone("Dash",new Rect(7,1,20,15),"鼠标左键进行冲刺"),
                    Zone("Wall",new Rect(30,2,14,13),"部分墙壁紧贴按住方向键可攀附")});
            }
            else if(path==CampaignFlow.Scenes[1])SetZones(session,new[]{Zone("Zipline",new Rect(0,48,15,12),"按 w 在绳索上滑行")});
            else if(path==CampaignFlow.Scenes[3])SetZones(session,new[]{
                Zone("Cold",new Rect(2,21,22,13),"冻僵的地形需要防寒措施才能接触"),
                Zone("Heat",new Rect(184,116,28,19),"灼烧的地形需要隔热措施才能接触"),
                Zone("Climb",new Rect(94,100,26,20),"在攀岩点周围可以二次跳跃")});
            else continue;
            Physics2D.SyncTransforms();
            foreach(var zone in session.GetComponent<LevelTutorials>().zones)lines.Add(path+" "+zone.name+" "+zone.GetComponent<BoxCollider2D>().bounds+" -> "+zone.text);
            EditorSceneManager.SaveScene(scene);
        }
        LevelCopyEditorChecks.Run();
        string folder=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261120-level-copy");Directory.CreateDirectory(folder);
        File.WriteAllLines(Path.Combine(folder,"setup-report.txt"),lines);
    }
    struct ZoneData {public string name,text;public Rect rect;}
    static ZoneData Zone(string name,Rect rect,string text)=>new ZoneData{name=name,text=text,rect=rect};
    static void SetZones(SeasideSession session,ZoneData[] content)
    {
        var tutorials=session.GetComponent<LevelTutorials>()??session.gameObject.AddComponent<LevelTutorials>();tutorials.session=session;
        if(tutorials.zones!=null&&tutorials.zones.Any(z=>z!=null))throw new Exception("Tutorials already installed; adjust existing areas in place.");
        var parent=new GameObject("Tutorials").transform;parent.SetParent(session.transform,false);
        tutorials.zones=content.Select((data,index)=>{
            var go=new GameObject("Tutorial_"+data.name);go.transform.SetParent(parent,false);go.transform.position=data.rect.center;
            var area=go.AddComponent<BoxCollider2D>();area.isTrigger=true;area.size=data.rect.size;
            var zone=go.AddComponent<TutorialZone>();zone.text=data.text;zone.priority=index;return zone;
        }).ToArray();
    }
}

public static class LevelCopyEditorChecks
{
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch only.");
        var log=new List<string>();string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        Action<bool,string> check=(ok,label)=>log.Add((ok?"PASS ":"FAIL ")+label);
        foreach(var path in CampaignFlow.Scenes)
        {
            EditorSceneManager.OpenScene(path);Physics2D.SyncTransforms();
            var session=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();var tutorials=session.GetComponent<LevelTutorials>();
            int expected=path==CampaignFlow.Scenes[0]?2:path==CampaignFlow.Scenes[1]?1:path==CampaignFlow.Scenes[3]?3:0;
            check(expected==0?tutorials==null:tutorials!=null&&tutorials.session==session&&tutorials.zones.Length==expected,"Document tutorial count/references: "+path);
            if(tutorials!=null)
            {
                foreach(var zone in tutorials.zones)
                {
                    var area=zone.GetComponent<BoxCollider2D>();
                    check(area.isTrigger&&zone.gameObject.layer!=6&&zone.gameObject.layer!=7,"Tutorial adds no terrain/hazard: "+zone.name);
                    check(tutorials.At(area.bounds.center)==zone.text,"Inside zone shows original top text: "+zone.name);
                    check(!zone.Contains((Vector2)area.bounds.max+Vector2.one),"Outside zone clears: "+zone.name);
                }
                check(tutorials.At(new Vector2(-100,-100))==null,"No permanent tutorial outside marked areas: "+path);
            }
            if(path==CampaignFlow.Scenes[0])
            {
                var story=session.GetComponent<SeasideStory>();
                check(story!=null&&story.session==session&&story.lighthouse!=null&&story.lighthouse.GetComponent<SpriteRenderer>().sprite.name=="lighthouse","Seaside story bound to current lighthouse artwork");
                check(story.pages.Length==2&&story.pages[0]=="这里写着“这里曾是我最初的梦想”，似乎缺少什么"&&story.pages[1]=="获得传送：鼠标右键瞬移一段距离","Both seaside pages preserve PDF wording/order");
                check(UnityEngine.Object.FindObjectsByType<SeasideCell>().Count(c=>c.kind==CellKind.Finish)==1,"Existing seaside finish preserved");
            }
        }
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        var output=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/level-copy-editor-checks.txt");File.WriteAllLines(output,log);
        if(log.Any(s=>s.StartsWith("FAIL")))throw new Exception("Level copy checks failed: "+output);
        Debug.Log("LEVEL_COPY_EDITOR_PASS assertions="+log.Count);
    }
}
