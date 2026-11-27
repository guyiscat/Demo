using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expedition;
using Forest;
using Mine;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class WorldRegionSetup
{
    static Sprite square,flag,hero,offSprite,onSprite;
    static Material lineMaterial;
    static PhysicsMaterial2D friction;
    static readonly List<string> report=new List<string>();
    const string Work="Docs/Logs/Work/20261124-world-regions";
    static Transform Child(string name,Transform parent,Vector2 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;return t;}
    static SpriteRenderer Picture(string name,Transform parent,Vector2 p,Vector2 size,Color color,Sprite sprite=null)
    {var t=Child(name,parent,p);var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite??square;sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;sr.color=color;sr.sortingOrder=6;return sr;}
    static BoxCollider2D Box(GameObject go,Vector2 size,bool trigger=false)
    {var b=go.AddComponent<BoxCollider2D>();b.size=size;b.isTrigger=trigger;b.sharedMaterial=friction;return b;}
    static void Label(Transform parent,string text,Vector2 p)
    {var t=Child("Label",parent,p);var label=t.gameObject.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=.11f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.75f,.85f,.6f);GameTypography.Apply(label);label.GetComponent<MeshRenderer>().sortingOrder=7;}
    public static void Install()
    {
        if(!Application.isBatchMode)throw new Exception("Only isolated batch installation is allowed.");
        square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ForestWhitebox/Block.png");flag=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player2x2.png");
        lineMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForestWhitebox/Lines.mat");friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/NoFriction.physicsMaterial2D");
        var common=GameSystemSettings.Current;common.respawnDelay=.5f;common.regionBlackDuration=1.5f;common.regionFadeDuration=.25f;common.regionEntrySpeed=12;EditorUtility.SetDirty(common);
        foreach(string guid in AssetDatabase.FindAssets("t:ForestSettings")) {var s=AssetDatabase.LoadAssetAtPath<ForestSettings>(AssetDatabase.GUIDToAssetPath(guid));s.teleportHoverDuration=.5f;EditorUtility.SetDirty(s);}
        foreach(var path in CampaignFlow.Scenes.Take(5))
        {
            var scene=EditorSceneManager.OpenScene(path);RemoveCheckpointVisuals();
            if(path==CampaignFlow.Scenes[3])PatchSnow();
            if(path==CampaignFlow.Scenes[2])PatchMine();
            InstallRegion(path);EditorSceneManager.SaveScene(scene);
        }
        CreateVillage();
        EditorBuildSettings.scenes=new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes).Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();AssetDatabase.SaveAssets();
        string folder=Path.Combine(SeasidePaths.ReleaseRoot,Work);Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,"setup-report.txt"),report);
        WorldRegionEditorChecks.Run();
    }
    public static void RepairDeepLift()
    {
        if(!Application.isBatchMode)throw new Exception("Batch only.");
        var scene=EditorSceneManager.OpenScene(CampaignFlow.Scenes[2]);Object.FindAnyObjectByType<MineLevel>().deepLift.startsUnlocked=true;EditorSceneManager.SaveScene(scene);
    }
    static void RemoveCheckpointVisuals()
    {
        foreach(var visual in Object.FindObjectsByType<CheckpointVisual>(FindObjectsInactive.Include))
        {
            var serialized=new SerializedObject(visual);
            foreach(var field in new[]{"offVisual","onVisual"})
            {
                var go=serialized.FindProperty(field).objectReferenceValue as GameObject;if(go==null)continue;
                var sr=go.GetComponentInChildren<SpriteRenderer>(true);if(sr!=null){if(field=="offVisual")offSprite=sr.sprite;else onSprite=sr.sprite;}
                report.Add("REMOVE checkpoint visual "+go.name+" from "+visual.name);Object.DestroyImmediate(go);
            }
            Object.DestroyImmediate(visual);
        }
    }
    static void PatchSnow()
    {
        var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("SnowMap").text);var allowed=new HashSet<string>(map.cells.Where(c=>c.kind=="spike").Select(c=>c.address));
        foreach(var cell in Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Spike&&!allowed.Contains(c.excelAddress)).ToArray())
        {report.Add("SNOW REMOVE authored deleted spike "+cell.excelAddress);Object.DestroyImmediate(cell.gameObject);}
        Object.FindAnyObjectByType<Snow.SnowLevel>().sourceSha256=map.sourceSha256;
    }
    static void PatchMine()
    {
        var level=Object.FindAnyObjectByType<MineLevel>();var parent=level.lift.transform.parent;
        foreach(var old in Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint&&!string.IsNullOrEmpty(c.excelAddress)&&!c.excelAddress.StartsWith("CampaignRest_")).ToArray())
        {report.Add("MINE remove former purple-as-checkpoint "+old.excelAddress);Object.DestroyImmediate(old.gameObject);}
        var gatePicture=Picture("PurpleGate_DQ120",parent,new Vector2(90,165),new Vector2(2,6),new Color(.44f,.15f,.65f));gatePicture.gameObject.layer=6;
        var gate=gatePicture.gameObject.AddComponent<MineGate>();gate.barrier=Box(gatePicture.gameObject,new Vector2(2,6));gate.visual=gatePicture;level.gate=gate;
        var button=Picture("PurpleGateSwitch_AP69",parent,new Vector2(11,217.5f),new Vector2(2,3),new Color(.44f,.15f,.65f));Box(button.gameObject,new Vector2(2,3),true);
        var touch=button.gameObject.AddComponent<MineTouchSwitch>();touch.level=level;touch.gate=gate;Label(button.transform,"触摸开启大门",new Vector2(11,221));
        var switchPoint=level.interactions.Single(i=>i.kind==MineInteractionKind.LiftSwitch);var yellow=switchPoint.gameObject.AddComponent<MineTouchSwitch>();yellow.level=level;yellow.lift=level.lift;
        level.interactions=level.interactions.Where(i=>i!=switchPoint).ToArray();Object.DestroyImmediate(switchPoint);
        // Blue inverted-T source markers indicate a second physical lift, not relics.
        foreach(var item in level.interactions.Where(i=>i.kind==MineInteractionKind.Relic).ToArray())Object.DestroyImmediate(item.gameObject);
        level.interactions=level.interactions.Where(i=>i.kind!=MineInteractionKind.Relic).ToArray();
        var deep=Picture("DeepShaftLift",parent,new Vector2(120,85.5f),new Vector2(5.5f,.55f),new Color(.05f,.7f,1));deep.gameObject.layer=6;Box(deep.gameObject,new Vector2(5.5f,.55f));
        var body=deep.gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var lift=deep.gameObject.AddComponent<MineLift>();lift.body=body;lift.settings=level.settings;lift.upperStop=new Vector2(120,85.5f);lift.lowerStop=new Vector2(120,1.5f);lift.startsUnlocked=true;lift.Activate();level.deepLift=lift;
        foreach(var y in new[]{85.5f,1.5f}){var sr=Picture("BlueLiftStop",parent,new Vector2(120,y),new Vector2(6,.15f),new Color(0,.6f,.9f));Label(sr.transform,"电梯 / E",new Vector2(120,y+3));}
        // The horizontal red line outside the shaft is a corruption threshold; no spike changes required.
        var itemVisual=Picture("GoldenBitcoin",parent,new Vector2(115.7f,3.2f),new Vector2(.8f,.8f),new Color(1,.8f,.08f));Box(itemVisual.gameObject,Vector2.one,true);
        var relic=itemVisual.gameObject.AddComponent<MineInteraction>();relic.kind=MineInteractionKind.Relic;relic.completesLevel=true;relic.sourceAddress="DeepShaftGoldenItem";
        level.interactions=level.interactions.Concat(new[]{relic}).ToArray();Label(itemVisual.transform,"道具 / E",new Vector2(115.7f,5.5f));
        var darkness=level.cameraRig.gameObject.AddComponent<MineDarkness>();darkness.player=level.player.motor;
        const string materialPath="Assets/Art/MineVisibility.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(mat==null){mat=new Material(Shader.Find("Hidden/Demo/MineVisibility"));AssetDatabase.CreateAsset(mat,materialPath);}darkness.maskMaterial=mat;
        var corruption=level.cameraRig.gameObject.AddComponent<MineCorruption>();corruption.level=level;corruption.thresholdY=43;
        report.Add("MINE yellow: touch unlock + E dispatch; purple button/gate at source cells; blue shaft 120/85.5 ->120/1.5; golden item115.7/3.2; darkness+corruption");
    }
    static Transform Anchor(Transform parent,string key,Vector2 p)=>Child(key,parent,p);
    static WorldRegion BaseRegion(SeasideSession session)
    {
        if(session.GetComponent<WorldRegion>()!=null)throw new Exception("Already configured; do not rerun installation over manual changes.");
        var region=session.gameObject.AddComponent<WorldRegion>();region.session=session;
        var menu=session.gameObject.AddComponent<TravelPointMenu>();menu.session=session;return region;
    }
    static RegionExit Exit(WorldRegion region,string name,Rect rect,string destination,string arrival,bool interaction=false,bool lighthouse=false,bool story=false)
    {
        var go=Child(name,region.transform,rect.center).gameObject;Box(go,rect.size,true);
        var exit=go.AddComponent<RegionExit>();exit.session=region.session;exit.destination=destination;exit.arrival=arrival;exit.requiresInteraction=interaction;exit.requiresLighthouse=lighthouse;exit.requiresSeasideStory=story;
        region.exits=(region.exits??new RegionExit[0]).Concat(new[]{exit}).ToArray();return exit;
    }
    static void Bridge(WorldRegion region,string name,float left,float right,float playerY)
    {
        var picture=Picture(name,region.transform,new Vector2((left+right)*.5f,playerY-1.54f),new Vector2(right-left,1),new Color(.13f,.2f,.19f));picture.gameObject.layer=6;Box(picture.gameObject,new Vector2(right-left,1));
        report.Add("ADD connector "+region.gameObject.scene.name+" "+left+".."+right+" top="+(playerY-1.04f));
    }
    static void Portal(SeasideSession session,Transform parent,Vector2 p,string name="TravelPoint")
    {
        var root=Child(name,parent,p);var point=root.gameObject.AddComponent<TravelPoint>();point.session=session;
        // Reuse the two source sprites as static portal art; no checkpoint-state toggling.
        foreach(var pair in new[]{(offSprite,"PortalFrame",new Vector2(-.5f,0)),(onSprite,"PortalCore",new Vector2(.5f,0))})
        {var sr=Picture(pair.Item2,root,p+pair.Item3,new Vector2(1.2f,1.8f),new Color(.5f,.9f,1),pair.Item1??flag);sr.sortingOrder=10;}
        Label(root,"传送点 / E",p+Vector2.up*2.5f);
    }
    static void InstallRegion(string path)
    {
        var session=Object.FindAnyObjectByType<SeasideSession>();var region=BaseRegion(session);Vector2 entry=session.startPoint;
        region.entry=Anchor(region.transform,"Entry",entry);region.fromRight=Anchor(region.transform,"Right",entry);region.fromMine=Anchor(region.transform,"MineReturn",entry);
        if(path==CampaignFlow.Scenes[0])
        {
            Vector2 right=SafeNear(new Vector2(149,43),20);region.fromRight.position=right;
            Bridge(region,"EastCoastalPath",right.x-2,right.x+13,right.y);
            Exit(region,"EastToForest",new Rect(right.x+9,right.y-2,3,7),CampaignFlow.Scenes[1],"Entry",story:true);
            Bridge(region,"WestCoastalPath",entry.x-12,entry.x,entry.y);
            Exit(region,"WestToLighthouse",new Rect(entry.x-8,entry.y-2,3,6),CampaignFlow.Scenes[4],"Entry",lighthouse:true);
            Label(region.transform,"← 雪山选择继续后前往灯塔",new Vector2(entry.x-5,entry.y+3));
        }
        else if(path==CampaignFlow.Scenes[1])
        {
            Vector2 right=SafeNear(new Vector2(179,100),15);region.fromRight.position=right+Vector2.right*6;
            Bridge(region,"EastForestPath",right.x-2,right.x+15,right.y);Exit(region,"EastToVillage",new Rect(right.x+10,right.y-2,3,7),CampaignFlow.Scenes[5],"Entry");
            Bridge(region,"WestForestPath",entry.x-10,entry.x,entry.y);Exit(region,"WestToSeaside",new Rect(entry.x-7,entry.y-2,3,6),CampaignFlow.Scenes[0],"Right");
            Label(region.transform,"→ 村庄",right+new Vector2(7,3));
        }
        else if(path==CampaignFlow.Scenes[3])
        {
            Bridge(region,"WestSnowPath",entry.x-20,entry.x,entry.y);Exit(region,"WestToVillage",new Rect(entry.x-17,entry.y-2,3,6),CampaignFlow.Scenes[5],"Right");
        }
        else if(path==CampaignFlow.Scenes[4])
        {
            Bridge(region,"ReturnToSeasidePath",entry.x-12,entry.x,entry.y);Exit(region,"WestToSeaside",new Rect(entry.x-8,entry.y-2,3,6),CampaignFlow.Scenes[0],"Entry");
        }
        else if(path==CampaignFlow.Scenes[2])
        {
            // Return marker sits on the upper deep-lift landing, away from the shaft.
            Vector2 p=SafeNear(new Vector2(113,88),18);var exit=Exit(region,"DeepLiftReturnToVillage",new Rect(p.x-2,p.y-2,4,5),CampaignFlow.Scenes[5],"Mine",true);
            Picture("VillageReturnSign",exit.transform,p,new Vector2(1.2f,2),new Color(.6f,.3f,1),offSprite??flag);Label(exit.transform,"返回村庄 / E",p+Vector2.up*3);
        }
        Portal(session,region.transform,entry+Vector2.right*3.5f);
    }
    static Vector2 SafeNear(Vector2 target,float maxDistance)
    {
        Physics2D.SyncTransforms();var candidates=new List<Vector2>();
        foreach(var b in Object.FindObjectsByType<BoxCollider2D>().Where(b=>b.gameObject.layer==6&&!b.isTrigger&&b.GetComponent<ThermalSurface>()==null&&b.GetComponent<MovingSurface>()==null&&b.GetComponent<MineGate>()==null))
            for(float x=b.bounds.min.x+1.05f;x<=b.bounds.max.x-1.05f;x+=.5f)
            {
                var p=new Vector2(x,b.bounds.max.y+1.05f);if(Vector2.Distance(p,target)>maxDistance)continue;
                if(!Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any())candidates.Add(p);
            }
        if(candidates.Count==0)throw new Exception("No safe region landing near "+target);
        return candidates.OrderBy(p=>(p-target).sqrMagnitude).First();
    }
    static void CreateVillage()
    {
        string path=CampaignFlow.Scenes[5];if(File.Exists(path))throw new Exception("Existing Village scene must not be replaced.");
        var map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("VillageMap").text);var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Child("Village",null,Vector2.zero);var gameplay=Child("Gameplay",root,Vector2.zero);var geometry=Child("Map",gameplay,Vector2.zero);var systems=Child("Systems",root,Vector2.zero);
        foreach(var row in map.cells.Where(c=>c.kind=="ground").GroupBy(c=>c.y))
        {
            var cells=row.OrderBy(c=>c.x).ToArray();for(int first=0;first<cells.Length;)
            {
                int last=first;while(last+1<cells.Length&&cells[last+1].x==cells[last].x+1)last++;
                int width=last-first+1;var sr=Picture("Terrain_"+cells[first].address+"_"+cells[last].address,geometry,new Vector2(cells[first].x+width*.5f,row.Key+.5f),new Vector2(width,1),new Color(.12f,.16f,.14f));sr.gameObject.layer=6;Box(sr.gameObject,new Vector2(width,1));
                var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.excelAddress=cells[first].address+":"+cells[last].address;first=last+1;
            }
        }
        Vector2 entry=new Vector2(9.5f,5.05f);var character=Picture("Player",gameplay,entry,new Vector2(2,2),Color.white,hero).gameObject;character.layer=8;character.GetComponent<SpriteRenderer>().sortingOrder=31000;
        var body=character.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;Box(character,new Vector2(2,2));
        var motor=character.AddComponent<SeasideMotor>();motor.settings=GameSystemSettings.Current.movement;motor.externalInput=true;character.AddComponent<DashAfterimage>();
        var session=Child("GameSession",systems,Vector2.zero).gameObject.AddComponent<SeasideSession>();session.player=motor;session.startPoint=entry;
        var cam=Child("MainCamera",systems,Vector2.zero).gameObject;cam.tag="MainCamera";var camera=cam.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.38f,.34f);cam.AddComponent<AudioListener>();
        var rig=cam.AddComponent<SeasideCamera>();rig.player=motor;rig.mapSize=new Vector2(117,24);rig.constrainToBackground=false;rig.ApplySystemSettings();
        var actor=character.AddComponent<ForestPlayer>();actor.motor=motor;actor.session=session;actor.settings=AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/Forest.asset");actor.ziplines=new ForestZipline[0];
        actor.beacon=Picture("Beacon",systems,entry,Vector2.one*.25f,new Color(.7f,.2f,1));actor.beacon.enabled=false;
        actor.beaconTrail=Child("BeaconTrail",systems,entry).gameObject.AddComponent<LineRenderer>();actor.beaconTrail.sharedMaterial=lineMaterial;actor.beaconTrail.positionCount=2;actor.beaconTrail.startWidth=actor.beaconTrail.endWidth=.06f;actor.beaconTrail.enabled=false;
        var hud=session.gameObject.AddComponent<Village.VillageHUD>();hud.session=session;
        var cp=Picture("VillageCheckpoint",geometry,entry,new Vector2(1.5f,2),new Color(.6f,.8f,.4f),flag);Box(cp.gameObject,new Vector2(2,2),true);var checkpoint=cp.gameObject.AddComponent<SeasideCell>();checkpoint.kind=CellKind.Checkpoint;checkpoint.checkpointId=1;checkpoint.respawn=entry;checkpoint.respawnAnchor=Child("RespawnAnchor",cp.transform,entry);checkpoint.excelAddress="Y27,Z27,Y28,Z28";
        var npc=Picture("VillageRobot_CP26",geometry,new Vector2(79,5.5f),new Vector2(2,3),new Color(.15f,.6f,.8f));Label(npc.transform,"机器人 / E",new Vector2(79,9));
        var robot=npc.gameObject.AddComponent<Village.VillageRobot>();robot.session=session;hud.robot=robot;
        var region=BaseRegion(session);region.entry=Anchor(region.transform,"Entry",entry);region.fromRight=Anchor(region.transform,"Right",new Vector2(111,5.05f));region.fromMine=Anchor(region.transform,"MineReturn",new Vector2(24,5.05f));
        Exit(region,"WestToForest",new Rect(.5f,3,2,7),CampaignFlow.Scenes[1],"Right");Exit(region,"EastToSnow",new Rect(114,3,2,7),CampaignFlow.Scenes[3],"Entry");
        Exit(region,"PitToMine",new Rect(15,-3,4,5),CampaignFlow.Scenes[2],"Entry");Label(region.transform,"↓ 矿洞",new Vector2(17,9));Label(region.transform,"← 森林       村庄       雪山 →",new Vector2(58,13));
        Portal(session,region.transform,entry+Vector2.left*3.5f);
        rig.Snap();EditorSceneManager.SaveScene(scene,path);report.Add("VILLAGE created from workbook 452 black cells, 4-cell pit x15..19, robotCP26; route Forest/Village/Snow; pit->Mine");
    }
}
