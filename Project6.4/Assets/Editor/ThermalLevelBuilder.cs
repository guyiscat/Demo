using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Expedition;
using Forest;
using Seaside;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ThermalLevelBuilder
{
    static Sprite square,spike,hero,flag;
    static PhysicsMaterial2D friction;
    static Material lines;
    static ForestMapData map;
    public static string ScenePath(bool snow)=>"Assets/Scenes/"+(snow?"Snow":"Lighthouse")+"Whitebox.unity";
    static Transform Child(string name,Transform parent)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static SpriteRenderer Picture(string name,Vector2 p,Vector2 size,Color color,Transform parent,Sprite sprite=null,int order=0)
    {
        var t=Child(name,parent);t.position=p;var sr=t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite=sprite==null?square:sprite;sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;sr.color=color;sr.sortingOrder=order;return sr;
    }
    static BoxCollider2D Box(SpriteRenderer sr,Vector2 size,bool trigger=false)
    {var b=sr.gameObject.AddComponent<BoxCollider2D>();b.size=size;b.isTrigger=trigger;b.sharedMaterial=friction;return b;}
    static List<List<ForestMapCell>> Groups(string kind)=>ForestMapRevision.Groups(map,kind);
    static void Bounds(List<ForestMapCell> group,out Vector2 p,out Vector2 size)
    {var min=new Vector2(group.Min(c=>c.x),group.Min(c=>c.y));size=new Vector2(group.Max(c=>c.x)+1,group.Max(c=>c.y)+1)-min;p=min+size*.5f;}
    static void Label(string text,Vector2 p,Transform parent)
    {
        var t=Child("Label_"+text,parent);t.position=p;var label=t.gameObject.AddComponent<TextMesh>();
        label.text=text;label.fontSize=48;label.characterSize=.12f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.13f,.25f,.17f);GameTypography.Apply(label);
    }
    public static void Create(bool snow)
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Create new maps only in an isolated batch project.");
        string path=ScenePath(snow),key=snow?"Snow":"Lighthouse";
        if(File.Exists(path))throw new InvalidOperationException("Existing authored scene will not be overwritten: "+path);
        map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>(key+"Map").text);
        square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ForestWhitebox/Block.png");
        spike=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Spike.png");hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player2x2.png");flag=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");
        friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/NoFriction.physicsMaterial2D");lines=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForestWhitebox/Lines.mat");
        var settings=AssetDatabase.LoadAssetAtPath<ThermalSettings>("Assets/Settings/"+key+".asset");
        if(settings==null){settings=ScriptableObject.CreateInstance<ThermalSettings>();AssetDatabase.CreateAsset(settings,"Assets/Settings/"+key+".asset");}
        var traversal=AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/"+key+"Traversal.asset");
        if(traversal==null)
        {
            traversal=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/Forest.asset"));traversal.name=key+"Traversal";
            AssetDatabase.CreateAsset(traversal,"Assets/Settings/"+key+"Traversal.asset");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Child(key,null);var gameplay=Child("Gameplay",root);var systems=Child("Systems",root);Child("Art",root);
        var geometry=Child("Map",gameplay);var terrain=Child("Terrain",geometry);var hazards=Child("Hazards",geometry);var nodes=Child("ClimbPoints",geometry);var checkpoints=Child("Checkpoints",geometry);
        var solids=new HashSet<Vector2Int>(map.cells.Where(c=>IsSolid(c.kind)).Select(c=>new Vector2Int(c.x,c.y)));
        foreach(var kind in new[]{"ground","noGrip","coldSurface","hotSurface"})foreach(var row in map.cells.Where(c=>c.kind==kind).GroupBy(c=>c.y))
        {
            var cells=row.OrderBy(c=>c.x).ToArray();
            for(int first=0;first<cells.Length;)
            {
                int last=first;while(last+1<cells.Length&&cells[last+1].x==cells[last].x+1)last++;
                int width=last-first+1;var size=new Vector2(width,1);bool cold=kind=="coldSurface",hot=kind=="hotSurface";
                var color=cold?new Color(0,.69f,.94f):hot?new Color(.94f,.56f,.62f):kind=="ground"?new Color(.07f,.1f,.09f):new Color(.52f,.55f,.54f);
                var sr=Picture(kind+"_"+cells[first].address+"_"+cells[last].address,new Vector2(cells[first].x+width*.5f,row.Key+.5f),size,color,terrain);
                sr.gameObject.layer=6;Box(sr,size);var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.attachable=kind!="noGrip";cell.excelAddress=cells[first].address+":"+cells[last].address;
                if(cold||hot){var surface=sr.gameObject.AddComponent<ThermalSurface>();surface.requiredProtection=cold?ProtectionMode.Cold:ProtectionMode.Heat;surface.sourceAddress=cell.excelAddress;}
                first=last+1;
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
        var pointList=new List<ClimbPoint>();
        var rails=Child("Ziplines",geometry);var railList=new List<ForestZipline>();
        foreach(var kind in new[]{"coldZipline","hotZipline"})foreach(var group in Groups(kind))
        {
            Bounds(group,out var p,out var size);bool cold=kind=="coldZipline";
            var rail=Child("Zipline_"+group[0].address,rails).gameObject.AddComponent<ForestZipline>();
            rail.excelAddress=string.Join(",",group.Select(c=>c.address));rail.start=Child("Start",rail.transform);rail.end=Child("End",rail.transform);
            rail.start.position=p-Vector2.right*size.x*.5f;rail.end.position=p+Vector2.right*size.x*.5f;
            var line=Child("Cable",rail.transform).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=lines;line.positionCount=2;
            line.SetPosition(0,rail.StartPoint);line.SetPosition(1,rail.EndPoint);line.startWidth=line.endWidth=.2f;line.sortingOrder=4;
            line.startColor=line.endColor=cold?new Color(0,.69f,.94f):new Color(.94f,.56f,.62f);
            var rule=rail.gameObject.AddComponent<ThermalZipline>();rule.requiredProtection=cold?ProtectionMode.Cold:ProtectionMode.Heat;
            Picture("StartGrip",rail.StartPoint,Vector2.one*.45f,line.startColor,rail.transform,null,4);
            Picture("EndGrip",rail.EndPoint,Vector2.one*.45f,line.startColor,rail.transform,null,4);railList.Add(rail);
        }
        foreach(var kind in new[]{"coldPoint","hotPoint"})foreach(var group in Groups(kind))
        {
            Bounds(group,out var p,out var size);bool cold=kind=="coldPoint";
            var sr=Picture("Climb_"+group[0].address,p,size,cold?new Color(0,.69f,.94f):new Color(.94f,.56f,.62f),nodes,null,4);
            var node=sr.gameObject.AddComponent<ClimbPoint>();node.requiredProtection=cold?ProtectionMode.Cold:ProtectionMode.Heat;node.sourceAddress=string.Join(",",group.Select(c=>c.address));
            var ring=Child("Range",sr.transform).gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.sharedMaterial=lines;ring.startWidth=ring.endWidth=.055f;ring.sortingOrder=3;
            node.rangeRing=ring;node.ShowState(cold,Vector2.one*(1+settings.climbPadding));pointList.Add(node);Label(cold?"寒":"热",p,sr.transform);
        }
        var authoredPoints=Groups("checkpoint").OrderBy(g=>g.Min(c=>c.y)).ToArray();
        Bounds(authoredPoints[0],out var entry,out _);Physics2D.SyncTransforms();entry=SafePoint(entry,18);
        var character=Picture("Player",entry,new Vector2(2,2),Color.white,gameplay,hero,31000).gameObject;character.layer=8;
        var body=character.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var playerBox=character.AddComponent<BoxCollider2D>();playerBox.size=new Vector2(2,2);playerBox.sharedMaterial=friction;
        var motor=character.AddComponent<SeasideMotor>();motor.settings=GameSystemSettings.Current.movement;motor.externalInput=true;character.AddComponent<DashAfterimage>();
        var session=Child("GameSession",systems).gameObject.AddComponent<SeasideSession>();session.player=motor;session.startPoint=entry;session.latestCheckpointWins=true;
        var camObject=Child("MainCamera",systems).gameObject;camObject.tag="MainCamera";var camera=camObject.AddComponent<Camera>();camera.orthographic=true;camera.backgroundColor=new Color(.87f,.91f,.84f);camera.clearFlags=CameraClearFlags.SolidColor;camObject.AddComponent<AudioListener>();
        var rig=camObject.AddComponent<SeasideCamera>();rig.player=motor;rig.mapSize=new Vector2(map.width,map.height);rig.constrainToBackground=false;rig.ApplySystemSettings();
        var actor=character.AddComponent<ForestPlayer>();actor.motor=motor;actor.session=session;actor.settings=traversal;actor.ziplines=railList.ToArray();
        actor.beacon=Picture("TeleportBeacon",entry,Vector2.one*traversal.beaconRadius*2,new Color(.85f,.28f,1),systems,null,12);actor.beacon.enabled=false;
        actor.beaconTrail=Child("BeaconTrail",systems).gameObject.AddComponent<LineRenderer>();actor.beaconTrail.sharedMaterial=lines;actor.beaconTrail.positionCount=2;actor.beaconTrail.startWidth=actor.beaconTrail.endWidth=.06f;actor.beaconTrail.startColor=actor.beaconTrail.endColor=new Color(.7f,.25f,1,.7f);actor.beaconTrail.enabled=false;
        var thermal=character.AddComponent<ThermalTraversal>();thermal.player=actor;thermal.settings=settings;thermal.points=pointList.ToArray();actor.environment=thermal;
        ThermalLevel level=snow?(ThermalLevel)Child("SnowLevel",systems).gameObject.AddComponent<Snow.SnowLevel>():Child("LighthouseLevel",systems).gameObject.AddComponent<Lighthouse.LighthouseLevel>();
        level.player=actor;level.session=session;level.cameraRig=rig;level.thermal=thermal;level.settings=settings;level.sourceSha256=map.sourceSha256;level.mapResource=key+"Map";
        int id=0;
        foreach(var group in authoredPoints)
        {
            Bounds(group,out var p,out var size);
            if(!snow&&group==authoredPoints.Last())continue;
            CreateCheckpoint(checkpoints,p,size,SafePoint(p,18),++id,string.Join(",",group.Select(c=>c.address)));
        }
        if(snow)
        {
            // The authored summit and return route gain safe rest points without adding terrain.
            AddRestPoint(checkpoints,new Vector2(179,152),ref id,"SummitRest");
            AddRestPoint(checkpoints,new Vector2(184,103),ref id,"ReturnRest");
            var g=Groups("fragment").Single();Bounds(g,out var p,out var size);
            var sr=Picture("Fragment_"+g[0].address,p,size,new Color(.19f,.75f,.7f),geometry,null,5);level.goal=Box(sr,size,true);Label("残缺的策划案",p+Vector2.up*3,geometry);
            Child("Source_"+g[0].address,sr.transform);
            var hud=Child("HUD",systems).gameObject.AddComponent<Snow.SnowHUD>();hud.level=(Snow.SnowLevel)level;
        }
        else
        {
            AddRestPoint(checkpoints,new Vector2(15,62),ref id,"LowerRest");
            AddRestPoint(checkpoints,new Vector2(15,102),ref id,"MiddleRest");
            AddRestPoint(checkpoints,new Vector2(18,168),ref id,"UpperRest");
            Bounds(authoredPoints.Last(),out var p,out var size);var sr=Picture("LighthouseLamp_"+authoredPoints.Last()[0].address,p,size,new Color(.52f,.62f,.57f),geometry,null,6);
            level.goal=Box(sr,size,true);((Lighthouse.LighthouseLevel)level).lamp=sr;Label("塔顶灯台",p+Vector2.up*3,geometry);
            var collapse=Child("RoadCollapse",systems).gameObject.AddComponent<Lighthouse.LighthouseCollapse>();collapse.level=(Lighthouse.LighthouseLevel)level;collapse.mapRoot=geometry;
            collapse.front=Child("CollapseFront",systems).gameObject.AddComponent<LineRenderer>();collapse.front.sharedMaterial=lines;collapse.front.startWidth=collapse.front.endWidth=.35f;
            collapse.front.startColor=collapse.front.endColor=new Color(.95f,.14f,.53f);collapse.front.sortingOrder=9;((Lighthouse.LighthouseLevel)level).collapse=collapse;
            var hud=Child("HUD",systems).gameObject.AddComponent<Lighthouse.LighthouseHUD>();hud.level=(Lighthouse.LighthouseLevel)level;
        }
        Label(snow?"雪山入口":"灯塔入口",entry+Vector2.up*4,geometry);
        rig.Snap();EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(path);ThermalEditorChecks.Run(snow);
        Debug.Log(key.ToUpperInvariant()+"_SCENE_CREATED source="+map.sourceSha256);
    }
    public static bool IsSolid(string kind)=>kind=="ground"||kind=="noGrip"||kind=="coldSurface"||kind=="hotSurface";
    static void CreateCheckpoint(Transform parent,Vector2 p,Vector2 size,Vector2 respawn,int id,string source)
    {
        var sr=Picture("Checkpoint_"+id.ToString("00"),p,size,new Color(.3f,.46f,.8f),parent,flag,6);Box(sr,size,true);
        var cell=sr.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Checkpoint;cell.checkpointId=id;cell.excelAddress=source;cell.respawn=respawn;cell.respawnAnchor=Child("RespawnAnchor",sr.transform);cell.respawnAnchor.position=respawn;
    }
    static void AddRestPoint(Transform parent,Vector2 near,ref int id,string label)
    {
        var safe=SafePoint(near,35);CreateCheckpoint(parent,safe,new Vector2(1.3f,1.8f),safe,++id,"DesignedRest_"+label);Label("休整点",safe+Vector2.up*2,parent);
    }
    static Vector2 SafePoint(Vector2 near,float maxDistance)
    {
        var candidates=new List<Vector2>();
        foreach(var b in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(b=>b.gameObject.layer==6&&!b.isTrigger&&b.GetComponent<ThermalSurface>()==null))
            for(float x=b.bounds.min.x+1.04f;x<=b.bounds.max.x-1.04f;x+=.5f)
            {
                var p=new Vector2(x,b.bounds.max.y+1.04f);if(Vector2.Distance(p,near)>maxDistance)continue;
                if(!Physics2D.OverlapBoxAll(p,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any())candidates.Add(p);
            }
        if(candidates.Count==0)throw new Exception("No clear neutral landing near "+near);
        return candidates.OrderBy(p=>(p-near).sqrMagnitude).First();
    }
    public static void Build(bool snow)
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use Tools/BuildAndTest.ps1 in an isolated project.");
        if(!File.Exists(ScenePath(snow)))Create(snow);
        SystemContractChecks.Run();EditorSceneManager.OpenScene(ScenePath(snow));ThermalEditorChecks.Run(snow);
        SeasideTestScenes.RunMovementRegression();
        EditorSceneManager.OpenScene(ScenePath(snow));string key=snow?"Snow":"Lighthouse";
        string output=Path.Combine(SeasidePaths.ReleaseRoot,"Play64/"+key+"/"+key+"Whitebox.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=CampaignBuild.ScenesFrom(ScenePath(snow)),locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception(key+" build failed: "+report.summary.result);
        Debug.Log(key.ToUpperInvariant()+"_BUILD_PASS bytes="+report.summary.totalSize+" errors="+report.summary.totalErrors);
    }
}
