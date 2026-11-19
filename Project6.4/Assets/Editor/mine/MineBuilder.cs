using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Mine;
using Seaside;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MineBuilder
{
    public const string ScenePath="Assets/Scenes/MineWhitebox.unity";
    static Sprite square,spike,hero,flag,route;
    static PhysicsMaterial2D friction;
    static Material lines;
    static ForestMapData map;
    static Transform Child(string name,Transform parent)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    static SpriteRenderer Picture(string name,Vector2 p,Vector2 size,Color color,Transform parent,Sprite sprite=null,int order=0)
    {
        var t=Child(name,parent);t.position=p;
        var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite==null?square:sprite;
        sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;sr.color=color;sr.sortingOrder=order;return sr;
    }
    static BoxCollider2D Box(SpriteRenderer picture,Vector2 size,bool trigger=false)
    {
        var b=picture.gameObject.AddComponent<BoxCollider2D>();b.size=size;b.isTrigger=trigger;b.sharedMaterial=friction;return b;
    }
    static List<List<ForestMapCell>> Groups(string kind)=>ForestMapRevision.Groups(map,kind);
    static void Bounds(List<ForestMapCell> group,out Vector2 center,out Vector2 size)
    {
        var min=new Vector2(group.Min(c=>c.x),group.Min(c=>c.y));
        size=new Vector2(group.Max(c=>c.x)+1,group.Max(c=>c.y)+1)-min;center=min+size*.5f;
    }
    static void Label(string value,Vector2 p,Transform parent)
    {
        var t=Child("Label_"+value,parent);t.position=p;
        var text=t.gameObject.AddComponent<TextMesh>();text.text=value;text.fontSize=48;text.characterSize=.12f;
        text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.14f,.29f,.18f);GameTypography.Apply(text);
    }
    static LineRenderer Line(string name,Vector2 a,Vector2 b,Color color,Transform parent,float width=.19f)
    {
        var l=Child(name,parent).gameObject.AddComponent<LineRenderer>();l.sharedMaterial=lines;l.useWorldSpace=true;
        l.positionCount=2;l.SetPosition(0,a);l.SetPosition(1,b);l.startWidth=l.endWidth=width;
        l.startColor=l.endColor=color;l.sortingOrder=4;return l;
    }
    [MenuItem("Mine/Create independent whitebox (never overwrites)")]
    public static void CreateScene()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Create in an isolated batch project.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("Mine scene already exists; authored geometry will not be overwritten.");
        map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("MineMap").text);
        square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ForestWhitebox/Block.png");
        if(square==null)throw new Exception("Shared whitebox square sprite is missing.");
        spike=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Spike.png");hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player2x2.png");
        flag=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");route=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Route.png");
        lines=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForestWhitebox/Lines.mat");
        friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/NoFriction.physicsMaterial2D");
        var settings=AssetDatabase.LoadAssetAtPath<MineSettings>("Assets/Settings/Mine.asset");
        if(settings==null){settings=ScriptableObject.CreateInstance<MineSettings>();AssetDatabase.CreateAsset(settings,"Assets/Settings/Mine.asset");}
        var traversal=AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/MineTraversal.asset");
        if(traversal==null)
        {
            traversal=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/Forest.asset"));
            traversal.name="MineTraversal";AssetDatabase.CreateAsset(traversal,"Assets/Settings/MineTraversal.asset");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Child("Mine",null);var gameplay=Child("Gameplay",root);var systems=Child("Systems",root);Child("Art",root);
        var geometry=Child("Map",gameplay);var terrain=Child("Terrain",geometry);var hazards=Child("Hazards",geometry);
        var rails=Child("Ziplines",geometry);var points=Child("Interactions",geometry);var checkpoints=Child("Checkpoints",geometry);
        var guides=Child("RouteGuides",geometry);guides.gameObject.AddComponent<RouteDisplay>();
        var solids=new HashSet<Vector2Int>(map.cells.Where(c=>c.kind=="ground"||c.kind=="noGrip").Select(c=>new Vector2Int(c.x,c.y)));
        foreach(string kind in new[]{"ground","noGrip"})foreach(var row in map.cells.Where(c=>c.kind==kind).GroupBy(c=>c.y))
        {
            var cells=row.OrderBy(c=>c.x).ToArray();
            for(int first=0;first<cells.Length;)
            {
                int last=first;while(last+1<cells.Length&&cells[last+1].x==cells[last].x+1)last++;
                int width=last-first+1;var size=new Vector2(width,1);
                var sr=Picture((kind=="ground"?"Terrain_":"NoGrip_")+cells[first].address+"_"+cells[last].address,
                    new Vector2(cells[first].x+width*.5f,row.Key+.5f),size,kind=="ground"?new Color(.07f,.1f,.09f):new Color(.52f,.54f,.54f),terrain);
                sr.gameObject.layer=6;Box(sr,size);
                var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.attachable=kind=="ground";
                cell.excelAddress=cells[first].address+":"+cells[last].address;first=last+1;
            }
        }
        foreach(var c in map.cells.Where(c=>c.kind=="spike"))
        {
            var sr=Picture("Spike_"+c.address,new Vector2(c.x+.5f,c.y+.5f),Vector2.one,Color.white,hazards,spike,2);
            Vector2 normal=Vector2.up;bool found=false;
            for(int distance=1;distance<=5&&!found;distance++)foreach(var d in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
                if(solids.Contains(new Vector2Int(c.x,c.y)+d*distance)){normal=-(Vector2)d;found=true;break;}
            sr.transform.rotation=Quaternion.FromToRotation(Vector3.up,normal);sr.gameObject.layer=7;Box(sr,Vector2.one*.84f,true);
            var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Spike;cell.excelAddress=c.address;
        }
        foreach(var group in Groups("route")){Bounds(group,out var p,out var size);Picture("Route_"+group[0].address,p,size,new Color(.22f,.46f,1,.85f),guides,route,3);}
        foreach(var group in Groups("marker")){Bounds(group,out var p,out var size);Picture("Marker_"+group[0].address,p,size,new Color(.1f,.5f,.8f,.7f),guides,route,3);}
        var railList=new List<ForestZipline>();
        foreach(var group in Groups("zipline"))
        {
            Bounds(group,out var p,out var size);var r=Child("Zipline_"+group[0].address,rails).gameObject.AddComponent<ForestZipline>();
            r.excelAddress=string.Join(",",group.Select(c=>c.address));r.start=Child("Start",r.transform);r.end=Child("End",r.transform);
            r.start.position=p-Vector2.right*size.x*.5f;r.end.position=p+Vector2.right*size.x*.5f;
            Line("Cable",r.StartPoint,r.EndPoint,new Color(.14f,.78f,.28f),r.transform);
            Picture("StartGrip",r.StartPoint,Vector2.one*.5f,new Color(.1f,.55f,.2f),r.transform,null,4);
            Picture("EndGrip",r.EndPoint,Vector2.one*.5f,new Color(.1f,.55f,.2f),r.transform,null,4);railList.Add(r);
        }
        var liftGroups=Groups("lift");var stops=liftGroups.Where(g=>g.Max(c=>c.x)-g.Min(c=>c.x)>g.Max(c=>c.y)-g.Min(c=>c.y)).ToArray();
        if(stops.Length!=2)throw new Exception("Expected two horizontal lift stops.");
        Bounds(stops.OrderBy(g=>g.Min(c=>c.y)).First(),out var lower,out var liftSize);
        Bounds(stops.OrderByDescending(g=>g.Min(c=>c.y)).First(),out var upper,out _);
        var liftVisual=Picture("MineLift",upper,liftSize,new Color(1,.7f,.12f),geometry,null,5);liftVisual.gameObject.layer=6;Box(liftVisual,liftSize);
        var liftBody=liftVisual.gameObject.AddComponent<Rigidbody2D>();liftBody.bodyType=RigidbodyType2D.Kinematic;liftBody.interpolation=RigidbodyInterpolation2D.Interpolate;
        var lift=liftVisual.gameObject.AddComponent<MineLift>();lift.body=liftBody;lift.lowerStop=lower;lift.upperStop=upper;lift.settings=settings;lift.ResetLift();
        Line("LiftShaft",lower,upper,new Color(.35f,.43f,.36f,.35f),geometry,.06f);
        Physics2D.SyncTransforms();
        // Keep the entrance respawn on static ground even when the lift is away.
        Vector2 startPoint=SafeCheckpoint(upper+Vector2.up*(liftSize.y*.5f+1.05f));
        var character=Picture("Player",startPoint,new Vector2(2,2),Color.white,gameplay,hero,31000).gameObject;character.layer=8;
        var body=character.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;
        body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var playerBox=character.AddComponent<BoxCollider2D>();playerBox.size=new Vector2(2,2);playerBox.sharedMaterial=friction;
        var motor=character.AddComponent<SeasideMotor>();motor.settings=GameSystemSettings.Current.movement;motor.externalInput=true;character.AddComponent<DashAfterimage>();
        var session=Child("GameSession",systems).gameObject.AddComponent<SeasideSession>();session.player=motor;session.startPoint=startPoint;session.latestCheckpointWins=true;
        var cameraObject=Child("MainCamera",systems).gameObject;cameraObject.tag="MainCamera";
        var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.backgroundColor=new Color(.87f,.91f,.84f);camera.clearFlags=CameraClearFlags.SolidColor;cameraObject.AddComponent<AudioListener>();
        var rig=cameraObject.AddComponent<SeasideCamera>();rig.player=motor;rig.mapSize=new Vector2(map.width,map.height);rig.constrainToBackground=false;rig.ApplySystemSettings();
        var actor=character.AddComponent<ForestPlayer>();actor.motor=motor;actor.session=session;actor.settings=traversal;actor.ziplines=railList.ToArray();
        actor.beacon=Picture("TeleportBeacon",startPoint,Vector2.one*traversal.beaconRadius*2,new Color(.85f,.28f,1),systems,null,12);actor.beacon.enabled=false;
        actor.beaconTrail=Line("BeaconTrail",startPoint,startPoint,new Color(.7f,.25f,1,.7f),systems,.06f);actor.beaconTrail.enabled=false;
        var level=Child("MineLevel",systems).gameObject.AddComponent<MineLevel>();level.player=actor;level.session=session;level.cameraRig=rig;level.lift=lift;level.settings=settings;level.sourceSha256=map.sourceSha256;
        var interactions=new List<MineInteraction>();
        foreach(string kind in new[]{"workbench","lift","relic"})foreach(var group in Groups(kind))
        {
            if(kind=="lift"&&stops.Contains(group))continue;
            Bounds(group,out var p,out var size);
            if(kind=="lift"&&size.x>size.y)continue;
            var sr=Picture(kind+"_"+group[0].address,p,size,kind=="workbench"?new Color(.2f,.76f,.65f):kind=="lift"?new Color(1,.75f,.05f):new Color(0,.73f,.94f),points,null,5);
            Box(sr,size,true);var point=sr.gameObject.AddComponent<MineInteraction>();point.sourceAddress=string.Join(",",group.Select(c=>c.address));
            point.kind=kind=="workbench"?MineInteractionKind.Workbench:kind=="lift"?MineInteractionKind.LiftSwitch:MineInteractionKind.Relic;
            point.completesLevel=kind=="relic"&&group.Min(c=>c.y)==map.cells.Where(c=>c.kind=="relic").Min(c=>c.y);interactions.Add(point);
        }
        level.interactions=interactions.ToArray();Physics2D.SyncTransforms();
        int checkpointId=0;
        foreach(var group in Groups("checkpoint").OrderByDescending(g=>g.Min(c=>c.y)))
        {
            Bounds(group,out var p,out var size);var safe=SafeCheckpoint(p);
            var sr=Picture("Checkpoint_"+(++checkpointId).ToString("00"),p,size,new Color(.68f,.35f,.9f),checkpoints,flag,6);Box(sr,size,true);
            var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Checkpoint;cell.checkpointId=checkpointId;
            cell.excelAddress=string.Join(",",group.Select(c=>c.address));cell.respawn=safe;cell.respawnAnchor=Child("RespawnAnchor",sr.transform);cell.respawnAnchor.position=safe;
        }
        Label("矿洞入口",startPoint+new Vector2(0,4),geometry);Label("工作台",new Vector2(110,260),points);
        Label("电梯控制台",new Vector2(77,236),points);Label("矿洞深处",new Vector2(120,7),points);
        var hud=Child("HUD",systems).gameObject.AddComponent<MineHUD>();hud.level=level;
        rig.Snap();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath);MineEditorChecks.Run();
        Debug.Log("MINE_SCENE_CREATED source="+map.sourceSha256+" size="+map.width+"x"+map.height);
    }
    static Vector2 SafeCheckpoint(Vector2 original)
    {
        var candidates=new List<Vector2>();
        foreach(var b in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(b=>b.gameObject.layer==6&&!b.isTrigger&&b.GetComponent<MovingSurface>()==null))
            for(float x=b.bounds.min.x+1.05f;x<=b.bounds.max.x-1.05f;x+=.5f)
            {
                var p=new Vector2(x,b.bounds.max.y+1.05f);if(Vector2.Distance(p,original)>14)continue;
                if(!Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any())candidates.Add(p);
            }
        if(candidates.Count==0)throw new Exception("No safe checkpoint near "+original);
        return candidates.OrderBy(p=>(p-original).sqrMagnitude).First();
    }
    public static void Build()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use Tools/BuildAndTest.ps1 -MineWhitebox in an isolated project.");
        if(!File.Exists(ScenePath))CreateScene();
        SystemContractChecks.Run();EditorSceneManager.OpenScene(ScenePath);MineEditorChecks.Run();
        SeasideTestScenes.RunMovementRegression();
        EditorSceneManager.OpenScene(ScenePath);string output=Path.Combine(SeasidePaths.ReleaseRoot,"Play64/Mine/MineWhitebox.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Mine build failed: "+report.summary.result);
        Debug.Log("MINE_BUILD_PASS bytes="+report.summary.totalSize+" errors="+report.summary.totalErrors);
    }
}
