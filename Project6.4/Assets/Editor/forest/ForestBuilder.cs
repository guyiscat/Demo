using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Forest;
using Seaside;

public static class ForestBuilder
{
    public const string ScenePath="Assets/Scenes/ForestWhitebox.unity";
    const string Art="Assets/Art/ForestWhitebox";
    static Sprite square,spike,hero,route,flag;
    static Material lineMaterial;
    static PhysicsMaterial2D friction;
    static ForestMapData map;
    static HashSet<Vector2Int> solidCells;

    [MenuItem("Forest/Create independent whitebox (never overwrites)")]
    public static void CreateScene()
    {
        if(File.Exists(ScenePath))throw new InvalidOperationException("ForestWhitebox already exists. Edit the saved scene; regeneration is refused.");
        map=JsonUtility.FromJson<ForestMapData>(Resources.Load<TextAsset>("ForestMap").text);
        solidCells=new HashSet<Vector2Int>(map.cells.Where(c=>c.kind=="ground"||c.kind=="noGrip").Select(c=>new Vector2Int(c.x,c.y)));
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        square=CreateSquare();
        spike=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Spike.png");
        hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player2x2.png");
        route=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Route.png");
        flag=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Checkpoint.png");
        friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/NoFriction.physicsMaterial2D");
        lineMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Lines.mat");
        if(lineMaterial==null){lineMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(lineMaterial,Art+"/Lines.mat");}
        var settings=AssetDatabase.LoadAssetAtPath<ForestSettings>("Assets/Settings/Forest.asset");
        if(settings==null)
        {
            settings=ScriptableObject.CreateInstance<ForestSettings>();settings.teleportDistance=map.teleportDistance;
            AssetDatabase.CreateAsset(settings,"Assets/Settings/Forest.asset");
        }
        // Additive creation does not save or discard any currently open user scene.
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root=new GameObject("Forest").transform;
        var gameplay=Child("Gameplay",root);var systems=Child("Systems",root);
        var mapRoot=Child("Map",gameplay);mapRoot.gameObject.AddComponent<SeasideMapRoot>();
        var terrain=Child("Terrain",mapRoot);var hazards=Child("Hazards",mapRoot);
        var rails=Child("Ziplines",mapRoot);var unlocks=Child("UnlockPoints",mapRoot);
        var guides=Child("RouteGuides",mapRoot);guides.gameObject.AddComponent<RouteDisplay>();
        Child("Art",root); // Intentionally empty: this deliverable is the whitebox stage.
        foreach(var kind in new[]{"ground","noGrip"})
            foreach(var row in map.cells.Where(c=>c.kind==kind).GroupBy(c=>c.y))
            {
                var cells=row.OrderBy(c=>c.x).ToArray();
                for(int first=0;first<cells.Length;)
                {
                    int last=first;while(last+1<cells.Length&&cells[last+1].x==cells[last].x+1)last++;
                    int width=last-first+1;
                    var item=Picture((kind=="ground"?"Terrain_":"NoGrip_")+cells[first].address+"_"+cells[last].address,
                        new Vector2(cells[first].x+width*.5f,row.Key+.5f),new Vector2(width,1),square,kind=="ground"?new Color(.07f,.1f,.09f):new Color(.52f,.54f,.54f),terrain);
                    item.gameObject.layer=6;var collider=item.gameObject.AddComponent<BoxCollider2D>();
                    collider.size=new Vector2(width,1);collider.sharedMaterial=friction;
                    var cell=item.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.attachable=kind=="ground";
                    cell.excelAddress=cells[first].address+":"+cells[last].address;first=last+1;
                }
            }
        foreach(var c in map.cells.Where(c=>c.kind=="spike"))
        {
            var item=Picture("Spike_"+c.address,new Vector2(c.x+.5f,c.y+.5f),Vector2.one,spike,Color.white,hazards,2);
            item.transform.rotation=Quaternion.FromToRotation(Vector3.up,SpikeNormal(c));item.gameObject.layer=7;
            var box=item.gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=Vector2.one*.84f;
            var cell=item.gameObject.AddComponent<SeasideCell>();cell.kind=CellKind.Spike;cell.excelAddress=c.address;
        }
        foreach(var group in Groups("route"))
        {
            BoundsFor(group,out var center,out var size);
            Picture("Route_"+group[0].address,center,size,route,new Color(.22f,.46f,1,.85f),guides,3);
        }
        var railList=new List<ForestZipline>();
        foreach(var group in Groups("zipline"))
        {
            BoundsFor(group,out var center,out var size);
            var rail=Child("Zipline_"+group[0].address,rails).gameObject.AddComponent<ForestZipline>();
            rail.excelAddress=string.Join(",",group.Select(c=>c.address));
            rail.start=Child("Start",rail.transform);rail.end=Child("End",rail.transform);
            rail.start.position=new Vector2(center.x-size.x*.5f,center.y);
            rail.end.position=new Vector2(center.x+size.x*.5f,center.y);
            Line("Cable",new[]{(Vector3)rail.StartPoint,(Vector3)rail.EndPoint},new Color(.14f,.78f,.28f),.19f,rail.transform,4);
            Picture("StartGrip",rail.StartPoint,Vector2.one*.5f,square,new Color(.1f,.55f,.2f),rail.transform,4);
            Picture("EndGrip",rail.EndPoint,Vector2.one*.5f,square,new Color(.1f,.55f,.2f),rail.transform,4);
            railList.Add(rail);
        }
        Vector2 startPoint=new Vector2(3,5.2f);
        var character=Picture("Player",startPoint,new Vector2(2,2),hero,Color.white,gameplay,31000).gameObject;
        character.layer=8;
        var body=character.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;
        body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var playerCollider=character.AddComponent<BoxCollider2D>();playerCollider.size=new Vector2(2,2);playerCollider.sharedMaterial=friction;
        var motor=character.AddComponent<SeasideMotor>();motor.settings=AssetDatabase.LoadAssetAtPath<SeasideSettings>("Assets/Settings/Movement.asset");motor.externalInput=true;
        character.AddComponent<DashAfterimage>();
        var session=Child("GameSession",systems).gameObject.AddComponent<SeasideSession>();session.player=motor;session.startPoint=startPoint;session.latestCheckpointWins=true;
        var cameraObject=Child("MainCamera",systems).gameObject;cameraObject.tag="MainCamera";cameraObject.transform.position=new Vector3(12,10,-10);
        var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=10;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.87f,.91f,.84f);cameraObject.AddComponent<AudioListener>();
        var rig=cameraObject.AddComponent<SeasideCamera>();rig.player=motor;rig.mapSize=new Vector2(map.width,122);rig.constrainToBackground=false;
        var wheel=CreateWheel(mapRoot,settings);
        var lockList=new List<ForestUnlock>();
        foreach(var group in Groups("unlock").OrderBy(g=>g.Min(c=>c.x)))
        {
            BoundsFor(group,out var center,out var size);
            var item=Picture("Unlock_"+(lockList.Count==0?"Left_":"Right_")+group[0].address,center,size,square,new Color(.35f,.8f,1),unlocks,5);
            var trigger=item.gameObject.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=size;
            var state=item.gameObject.AddComponent<ForestUnlock>();state.index=lockList.Count;state.wheel=wheel;state.picture=item;
            state.excelAddress=string.Join(",",group.Select(c=>c.address));lockList.Add(state);
            Label(lockList.Count==1?"LEFT LOCK":"RIGHT LOCK",center+Vector2.up*4,unlocks,.15f);
        }
        wheel.locks=lockList.ToArray();
        var waterfall=CreateWaterfall(mapRoot,motor);
        var actor=character.AddComponent<ForestPlayer>();actor.motor=motor;actor.settings=settings;actor.session=session;actor.wheel=wheel;
        actor.waterfall=waterfall;actor.ziplines=railList.ToArray();
        actor.beacon=Picture("TeleportBeacon",startPoint,Vector2.one*settings.beaconRadius*2,square,new Color(.85f,.28f,1),systems,12);actor.beacon.enabled=false;
        actor.beaconTrail=Line("BeaconTrail",new[]{(Vector3)startPoint,(Vector3)startPoint},new Color(.7f,.25f,1,.7f),.06f,systems,11);actor.beaconTrail.enabled=false;
        var goal=Picture("FinishTrigger",new Vector2(179,100),new Vector2(3,3.5f),flag,new Color(.22f,.85f,.42f),mapRoot,6).gameObject;
        var goalCollider=goal.AddComponent<BoxCollider2D>();goalCollider.isTrigger=true;goalCollider.size=new Vector2(3,3.5f);
        var finish=goal.AddComponent<ForestGoal>();finish.session=session;finish.wheel=wheel;
        Label("START",new Vector2(6,8),mapRoot,.18f);Label("FOREST FINISH",new Vector2(188,103),mapRoot,.18f);
        var level=Child("ForestLevel",systems).gameObject.AddComponent<ForestLevel>();level.player=actor;level.session=session;level.wheel=wheel;level.cameraRig=rig;level.sourceSha256=map.sourceSha256;
        level.gameObject.AddComponent<ForestHUD>().level=level;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log("FOREST_CREATE_PASS scene="+ScenePath+" source="+map.sourceSha256);
    }
    static ForestWaterwheel CreateWheel(Transform parent,ForestSettings settings)
    {
        var shape=map.drawings.Single(d=>d.kind=="ellipse");
        var wheel=Child("Waterwheel",parent).gameObject.AddComponent<ForestWaterwheel>();wheel.settings=settings;
        wheel.center=(shape.start+shape.end)*.5f;wheel.radius=new Vector2((shape.end.x-shape.start.x)*.5f,(shape.start.y-shape.end.y)*.5f);
        wheel.rim=Child("RotatingRim",wheel.transform);wheel.rim.position=wheel.center;
        var points=Enumerable.Range(0,129).Select(i=>(Vector3)(wheel.Orbit(i*360f/128)-wheel.center)).ToArray();
        var line=Line("Rim",points,new Color(.37f,.25f,.1f),.32f,wheel.rim,-1);line.useWorldSpace=false;
        for(int i=0;i<8;i++)
        {
            var spoke=Line("Spoke_"+i,new[]{Vector3.zero,(Vector3)(wheel.Orbit(i*45)-wheel.center)},new Color(.46f,.34f,.18f,.6f),.12f,wheel.rim,-2);spoke.useWorldSpace=false;
        }
        Picture("Hub",wheel.center,new Vector2(3,3),square,new Color(.37f,.25f,.1f),wheel.transform);
        var list=new List<ForestWheelPlatform>();
        for(int i=0;i<12;i++)
        {
            float angle=i*30f;Vector2 p=wheel.Orbit(angle);
            var item=Picture("WheelPlatform_"+i.ToString("00"),p,new Vector2(8,.36f),square,new Color(.57f,.4f,.17f),wheel.transform,1).gameObject;item.layer=6;
            var box=item.AddComponent<BoxCollider2D>();box.size=new Vector2(8,.36f);box.sharedMaterial=friction;
            var body=item.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var platform=item.AddComponent<ForestWheelPlatform>();platform.wheel=wheel;platform.body=body;
            platform.angle=platform.previousAngle=platform.initialAngle=angle;platform.NextPosition=p;list.Add(platform);
            var surface=item.AddComponent<SeasideCell>();surface.kind=CellKind.Ground;surface.attachable=false;
        }
        wheel.platforms=list.ToArray();wheel.deliveryAnchor=Child("TopPlatformArrival",wheel.transform);wheel.deliveryAnchor.position=new Vector2(176.5f,99.25f);
        wheel.deliveryClearance=Child("TopPlatformClearance",wheel.transform);wheel.deliveryClearance.position=new Vector2(172.75f,99.25f);
        wheel.transferBasket=Picture("TopTransferBasket",Vector2.zero,new Vector2(3,.36f),square,new Color(.72f,.49f,.18f),wheel.transform,3);
        wheel.transferBasket.enabled=false;
        return wheel;
    }
    static ForestWaterfall CreateWaterfall(Transform parent,SeasideMotor motor)
    {
        var shape=map.drawings.Single(d=>d.kind=="rect");Vector2 center=(shape.start+shape.end)*.5f;
        Vector2 size=new Vector2(shape.end.x-shape.start.x,shape.start.y-shape.end.y);
        var water=Picture("Waterfall",center,size,square,new Color(.36f,.73f,.9f,.82f),parent,8);
        var volume=water.gameObject.AddComponent<BoxCollider2D>();volume.isTrigger=true;volume.size=size;
        var waterfall=water.gameObject.AddComponent<ForestWaterfall>();waterfall.player=motor;waterfall.curtainBounds=volume;
        waterfall.occlusion=Picture("FarSideOcclusion",new Vector2(2232,40),new Vector2(4096,4096),square,new Color(.10f,.15f,.16f),parent,20000);
        for(int i=0;i<5;i++)Picture("Stream_"+i,new Vector2(shape.start.x+(i+.5f)*size.x/5,center.y),new Vector2(.14f,size.y),square,new Color(.7f,.9f,1,.6f),water.transform,9);
        return waterfall;
    }
    static Sprite CreateSquare()
    {
        string path=Art+"/Block.png";
        if(!File.Exists(path))
        {
            var tex=new Texture2D(8,8);tex.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());tex.Apply();
            File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spritePixelsPerUnit=8;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;
            var spriteSettings=new TextureImporterSettings();importer.ReadTextureSettings(spriteSettings);spriteSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(spriteSettings);
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static SpriteRenderer Picture(string name,Vector2 point,Vector2 size,Sprite sprite,Color color,Transform parent,int order=0)
    {
        var t=Child(name,parent);t.position=point;var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite;
        sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;sr.color=color;sr.sortingOrder=order;return sr;
    }
    static LineRenderer Line(string name,Vector3[] points,Color color,float width,Transform parent,int order)
    {
        var line=Child(name,parent).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.useWorldSpace=true;
        line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;line.sortingOrder=order;return line;
    }
    static void Label(string value,Vector2 position,Transform parent,float size)
    {
        var t=Child(value.Replace(' ','_'),parent);t.position=position;var text=t.gameObject.AddComponent<TextMesh>();text.text=value;
        text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.14f,.29f,.18f);
    }
    static Vector2 SpikeNormal(ForestMapCell cell)
    {
        foreach(int distance in Enumerable.Range(1,5))foreach(var direction in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
            if(solidCells.Contains(new Vector2Int(cell.x,cell.y)+direction*distance))return -(Vector2)direction;
        return Vector2.up;
    }
    static void BoundsFor(List<ForestMapCell> group,out Vector2 center,out Vector2 size)
    {
        float x=group.Min(c=>c.x),y=group.Min(c=>c.y);size=new Vector2(group.Max(c=>c.x)+1-x,group.Max(c=>c.y)+1-y);center=new Vector2(x,y)+size*.5f;
    }
    static List<List<ForestMapCell>> Groups(string kind)
    {
        var source=map.cells.Where(c=>c.kind==kind).ToDictionary(c=>new Vector2Int(c.x,c.y));var result=new List<List<ForestMapCell>>();
        while(source.Count>0)
        {
            var queue=new Queue<Vector2Int>();queue.Enqueue(source.First().Key);var group=new List<ForestMapCell>();
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(!source.TryGetValue(p,out var c))continue;source.Remove(p);group.Add(c);
                foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(source.ContainsKey(p+d))queue.Enqueue(p+d);
            }
            result.Add(group);
        }
        return result;
    }
    public static void Build()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run Tools/BuildAndTest.ps1 -ForestWhitebox in a closed or isolated project.");
        if(!File.Exists(ScenePath))CreateScene();
        SystemContractChecks.Run();
        EditorSceneManager.OpenScene(ScenePath);ForestEditorChecks.Run();
        // Shared motor extension must preserve the complete existing movement regression suite.
        SeasideTestScenes.RunMovementRegression();
        EditorSceneManager.OpenScene(ScenePath);
        string output=Path.Combine(SeasidePaths.ReleaseRoot,"Play64/Forest/ForestWhitebox.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=CampaignBuild.ScenesFrom(ScenePath),locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Forest build failed: "+report.summary.result);
        Debug.Log("FOREST_BUILD_PASS bytes="+report.summary.totalSize+" errors="+report.summary.totalErrors);
    }
}
