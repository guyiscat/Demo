using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expedition;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampaignSetup
{
    static readonly List<string> report=new List<string>();
    public static void Install()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Install in an isolated batch project; never replace an open authored scene.");
        report.Clear();
        PatchCheckpoints("Assets/Scenes/MineWhitebox.unity",new[]{new Vector2(104,258),new Vector2(73,239),new Vector2(76,185),new Vector2(118,102),new Vector2(119,10)});
        PatchCheckpoints("Assets/Scenes/SnowWhitebox.unity",new[]{new Vector2(72,76),new Vector2(135,140),new Vector2(175,64)});
        PatchCheckpoints("Assets/Scenes/LighthouseWhitebox.unity",new[]{new Vector2(32,142),new Vector2(49,228)});
        UpgradeMenu();
        EditorBuildSettings.scenes=new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes).Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        var settings=GameSystemSettings.Current;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        CampaignBuild.ValidateSceneList();
        string folder=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261120-campaign-saves");Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,"setup-report.txt"),report);
        Debug.Log("CAMPAIGN_SETUP_PASS "+string.Join("; ",report));
    }
    static void PatchCheckpoints(string path,Vector2[] targets)
    {
        var scene=EditorSceneManager.OpenScene(path);Physics2D.SyncTransforms();
        var session=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();
        var existing=UnityEngine.Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).ToList();
        var parent=existing[0].transform.parent;int id=existing.Max(c=>c.checkpointId);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");
        int added=0;
        foreach(var target in targets)
        {
            string key="CampaignRest_"+target.x+"_"+target.y;
            if(existing.Any(c=>c.excelAddress==key)){report.Add(path+" existing "+key);continue;}
            var candidates=new List<Vector2>();
            foreach(var b in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(b=>b.gameObject.layer==6&&!b.isTrigger&&b.GetComponent<ThermalSurface>()==null&&b.GetComponent<MovingSurface>()==null))
                for(float x=b.bounds.min.x+1.05f;x<=b.bounds.max.x-1.05f;x+=.5f)
                {
                    var p=new Vector2(x,b.bounds.max.y+1.05f);if(Vector2.Distance(p,target)>32)continue;
                    if(existing.Any(c=>Vector2.Distance(p,c.respawnAnchor.position)<9))continue;
                    if(Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any())continue;
                    candidates.Add(p);
                }
            if(candidates.Count==0){report.Add(path+" no extra safe neutral ground near "+target+"; existing checkpoints preserved");continue;}
            Vector2 safe=candidates.OrderBy(p=>(p-target).sqrMagnitude).First();
            var go=new GameObject("CampaignCheckpoint_"+(++id).ToString("00"));go.transform.SetParent(parent,false);go.transform.position=safe;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.drawMode=SpriteDrawMode.Sliced;sr.size=new Vector2(1.5f,1.9f);sr.sortingOrder=6;sr.color=new Color(.6f,.35f,.85f);
            var trigger=go.AddComponent<BoxCollider2D>();trigger.size=new Vector2(2.4f,2.2f);trigger.isTrigger=true;
            var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Checkpoint;cell.checkpointId=id;cell.excelAddress=key;cell.respawn=safe;
            cell.respawnAnchor=new GameObject("RespawnAnchor").transform;cell.respawnAnchor.SetParent(go.transform,false);
            var label=new GameObject("SaveLabel").AddComponent<TextMesh>();label.transform.SetParent(go.transform,false);label.transform.localPosition=Vector3.up*2;
            label.text="存档点 "+id;label.fontSize=48;label.characterSize=.1f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.12f,.28f,.16f);GameTypography.Apply(label);
            existing.Add(cell);added++;report.Add(path+" added #"+id+" near "+target+" -> safe "+safe);
        }
        EditorSceneManager.SaveScene(scene);report.Add(path+" checkpoints="+existing.Count+" added="+added);
    }
    static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    public static void UpgradeMenu()
    {
        var scene=EditorSceneManager.OpenScene(CampaignFlow.MenuScene);var menu=UnityEngine.Object.FindAnyObjectByType<StartGameMenu>();
        var parent=menu.buttons[0].transform.parent;
        foreach(var button in menu.buttons)if(button!=null)UnityEngine.Object.DestroyImmediate(button.gameObject);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ForestWhitebox/Block.png");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForestWhitebox/Lines.mat");
        var labels=new[]{"开始游戏","继续游戏","游戏选项","退出游戏"};
        menu.buttons=new StartGameButton[4];
        for(int i=0;i<4;i++)
        {
            var root=Child("Menu_"+(StartMenuAction)i,parent);root.localPosition=new Vector3(.015f,-.85f-i*.45f,0);
            var button=root.gameObject.AddComponent<StartGameButton>();button.action=(StartMenuAction)i;button.hitSize=new Vector2(1.8f,.4f);menu.buttons[i]=button;
            for(int state=0;state<2;state++)
            {
                var visual=Child(state==0?"Normal":"Highlighted",root);
                var bg=visual.gameObject.AddComponent<SpriteRenderer>();bg.sprite=sprite;bg.drawMode=SpriteDrawMode.Sliced;bg.size=new Vector2(1.8f,.39f);bg.sortingOrder=20;bg.color=new Color(.035f,.065f,.055f,state==0?.3f:.85f);
                var label=Child("Label",visual).gameObject.AddComponent<TextMesh>();label.fontSize=64;label.characterSize=.032f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.text=labels[i];label.color=Color.white;GameTypography.Apply(label);label.GetComponent<MeshRenderer>().sortingOrder=22;
                if(state==1)
                {
                    var line=Child("Selection",visual).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=2;
                    line.SetPosition(0,new Vector3(-.82f,-.185f));line.SetPosition(1,new Vector3(.82f,-.185f));line.startWidth=line.endWidth=.014f;
                    line.startColor=line.endColor=new Color(.86f,1,.91f);line.sortingOrder=23;
                }
                if(state==0)button.normalVisual=visual.gameObject;else button.highlightedVisual=visual.gameObject;
            }
            button.SetHighlighted(i==0);
        }
        EditorSceneManager.SaveScene(scene);report.Add("StartGame: four action buttons use shared Alibaba font; eight original theme layers preserved");
    }
}
