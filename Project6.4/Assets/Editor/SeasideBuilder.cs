using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Seaside;

public static class SeasideBuilder
{
    public static string Root => SeasidePaths.ReleaseRoot;
    static ExcelMapData map;
    static HashSet<Vector2Int> terrain;
    static Sprite ground,spike,playerSprite,checkpointSprite,routeSprite;
    [MenuItem("Seaside/Import Excel JSON - rebuild scene (overwrites scene edits)")]
    public static void CreateScene()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(string dir in new[]{"Assets/Art","Assets/Settings","Assets/Scenes","Assets/Prefabs"})Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        map=JsonUtility.FromJson<ExcelMapData>(Resources.Load<TextAsset>("SeasideMap").text);
        terrain=new HashSet<Vector2Int>(map.cells.Where(c=>c.kind=="ground").Select(c=>new Vector2Int(c.x,c.y)));
        ground=SpriteAsset("Ground",0);spike=SpriteAsset("Spike",1);playerSprite=SpriteAsset("Player",2);checkpointSprite=SpriteAsset("Checkpoint",3);routeSprite=SpriteAsset("Route",4);
        var cfg=AssetDatabase.LoadAssetAtPath<SeasideSettings>("Assets/Settings/Movement.asset");
        if(cfg==null){cfg=ScriptableObject.CreateInstance<SeasideSettings>();AssetDatabase.CreateAsset(cfg,"Assets/Settings/Movement.asset");}
        var grip=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/NoFriction.physicsMaterial2D");
        if(grip==null){grip=new PhysicsMaterial2D("NoFriction"){friction=0,bounciness=0};AssetDatabase.CreateAsset(grip,"Assets/Settings/NoFriction.physicsMaterial2D");}
        var root=new GameObject("EXCEL MAP - Sheet1 - 1 CELL = 1 UNIT").transform;
        var solidRoot=Child("01 TERRAIN - editable row modules",root);
        int groundCount=0;
        foreach(var row in map.cells.Where(c=>c.kind=="ground").GroupBy(c=>c.y))
        {
            var sorted=row.OrderBy(c=>c.x).ToArray();int begin=0;
            while(begin<sorted.Length)
            {
                int end=begin;while(end+1<sorted.Length&&sorted[end+1].x==sorted[end].x+1)end++;
                int width=end-begin+1;
                var go=Picture("Ground "+sorted[begin].address+"-"+sorted[end].address,new Vector2(sorted[begin].x+width*.5f,row.Key+.5f),new Vector2(width,1),ground,Color.white,solidRoot);
                go.layer=6;go.AddComponent<BoxCollider2D>().sharedMaterial=grip;
                var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.excelAddress=sorted[begin].address+":"+sorted[end].address;
                groundCount+=width;begin=end+1;
            }
        }
        var hazardRoot=Child("02 SPIKES - exact red cell positions",root);int spikes=0,decorative=0;
        foreach(var c in map.cells.Where(c=>c.kind=="spike"))
        {
            if(c.x>=141){decorative++;continue;} // Literal END lettering beyond the final deck, not a gameplay hazard.
            var go=Picture("Spike "+c.address,new Vector2(c.x+.5f,c.y+.5f),Vector2.one,spike,Color.white,hazardRoot);go.layer=7;
            Vector2 normal=SpikeNormal(c.x,c.y);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,normal);
            var col=go.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(.84f,.84f);
            var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Spike;cell.excelAddress=c.address;spikes++;
        }
        var cpRoot=Child("03 CHECKPOINTS - move parent to move spawn",root);
        var groups=Groups("checkpoint").OrderBy(g=>g.Min(c=>c.x)).ThenBy(g=>g.Min(c=>c.y)).ToList();
        int id=0;var report=new List<string>{"id,cells,world_x,world_y,spawn_x,spawn_y"};
        foreach(var group in groups)
        {
            id++;float x=(float)group.Average(c=>c.x)+.5f,y=(float)group.Average(c=>c.y)+.5f;
            var go=Picture("Checkpoint "+id.ToString("00")+" "+group[0].address,new Vector2(x,y),new Vector2(2,2),checkpointSprite,new Color(1,.78f,.2f),cpRoot,3);
            var col=go.AddComponent<BoxCollider2D>();col.isTrigger=true;
            var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Checkpoint;cell.checkpointId=id;cell.excelAddress=string.Join(",",group.Select(c=>c.address));
            var spawn=FindSpawn(x,group.Min(c=>c.y));var anchor=new GameObject("Respawn - drag this safe point").transform;anchor.SetParent(go.transform);anchor.position=spawn;
            cell.respawn=spawn;cell.respawnAnchor=anchor;
            report.Add(id+",\""+cell.excelAddress+"\","+x+","+y+","+spawn.x+","+spawn.y);
        }
        var routeRoot=Child("04 BLUE ROUTE - visual only, B to toggle",root);routeRoot.gameObject.AddComponent<RouteDisplay>();
        foreach(var group in Groups("route"))
        {
            float minX=group.Min(c=>c.x),minY=group.Min(c=>c.y),maxX=group.Max(c=>c.x)+1,maxY=group.Max(c=>c.y)+1;
            Picture("Route "+group[0].address,new Vector2((minX+maxX)*.5f,(minY+maxY)*.5f),new Vector2(maxX-minX,maxY-minY),routeSprite,new Color(.25f,.53f,1,.65f),routeRoot,2);
        }
        var start=new Vector2(5,3.2f);
        var character=Picture("Player",start,new Vector2(2,2),playerSprite,Color.white,null,10);character.layer=8;
        var body=character.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var box=character.AddComponent<BoxCollider2D>();box.sharedMaterial=grip;
        // Keep root unscaled; motor contact probes use these world dimensions.
        character.transform.localScale=Vector3.one;box.size=new Vector2(2,2);var visual=character.GetComponent<SpriteRenderer>();visual.drawMode=SpriteDrawMode.Sliced;visual.size=box.size;
        var motor=character.AddComponent<SeasideMotor>();motor.settings=cfg;
        var game=new GameObject("Game Session").AddComponent<SeasideSession>();game.player=motor;game.startPoint=start;game.gameObject.AddComponent<SeasideHUD>();
        var cam=new GameObject("Main Camera");cam.tag="MainCamera";
        var camera=cam.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=9;camera.backgroundColor=new Color(.78f,.89f,.93f);camera.clearFlags=CameraClearFlags.SolidColor;
        cam.transform.position=new Vector3(10,6,-10);cam.AddComponent<AudioListener>();var follow=cam.AddComponent<SeasideCamera>();follow.player=motor;follow.mapSize=new Vector2(map.width,map.height);
        var finish=Picture("Finish - END deck",new Vector2(139,43),new Vector2(1,2),checkpointSprite,new Color(.3f,1,.7f),root,4);
        var fin=finish.AddComponent<BoxCollider2D>();fin.isTrigger=true;finish.AddComponent<SeasideCell>().kind=CellKind.Finish;
        Label("START",new Vector2(5,5.5f),root);Label("END",new Vector2(146,44),root);
        Prefabs(grip);
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(6).stringValue="Ground";layers.GetArrayElementAtIndex(7).stringValue="Hazard";layers.GetArrayElementAtIndex(8).stringValue="Player";tags.ApplyModifiedProperties();
        Physics2D.IgnoreLayerCollision(8,8);Time.fixedDeltaTime=.01f;
        PlayerSettings.companyName="Friends Studio";PlayerSettings.productName="Seaside Whitebox";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        EditorSettings.serializationMode=SerializationMode.ForceText;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Seaside01.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Seaside01.unity",true)};AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Root+"/Docs");File.WriteAllLines(Root+"/Docs/checkpoints.csv",report);
        File.WriteAllText(Root+"/Docs/import-summary.txt","SOURCE Sheet1 155x73\nGround cells="+groundCount+" (expected 1619)\nSpike cells="+spikes+"; red END text="+decorative+"; total red="+(spikes+decorative)+" (expected 224)\nCheckpoints="+id+" (48 yellow cells)\nBlue route markers="+Groups("route").Count+" (84 blue cells)\nOne cell = one logical map pixel / 1 world unit; display scaling does not alter map distances.");
        if(groundCount!=1619||spikes+decorative!=224||id!=12)throw new Exception("Import counts mismatch");
        Debug.Log("SEASIDE_IMPORT_PASS terrain="+groundCount+" spikes="+spikes+" checkpoints="+id);
    }
    static Transform Child(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent);return go.transform;}
    static GameObject Picture(string name,Vector2 p,Vector2 size,Sprite sprite,Color color,Transform parent,int order=0)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=p;go.transform.localScale=new Vector3(size.x,size.y,1);
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;return go;
    }
    static void Label(string text,Vector2 p,Transform root){var go=new GameObject(text);go.transform.SetParent(root);go.transform.position=p;var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=.12f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.08f,.3f,.4f);}
    static Vector2 FindSpawn(float x,int bottom)
    {
        for(int y=bottom-1;y>=0;y--)if(terrain.Contains(new Vector2Int(Mathf.FloorToInt(x),y)))return new Vector2(x,y+2.2f);
        throw new Exception("Checkpoint has no floor beneath at "+x+","+bottom);
    }
    static Vector2 SpikeNormal(int x,int y)
    {
        var directions=new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right};
        for(int distance=1;distance<6;distance++)foreach(var d in directions)if(terrain.Contains(new Vector2Int(x,y)+d*distance))return -(Vector2)d;
        return Vector2.up;
    }
    static List<List<ExcelCell>> Groups(string kind)
    {
        var source=map.cells.Where(c=>c.kind==kind).ToDictionary(c=>new Vector2Int(c.x,c.y));var result=new List<List<ExcelCell>>();
        var dirs=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(source.Count>0){var first=source.First();var q=new Queue<Vector2Int>();q.Enqueue(first.Key);var group=new List<ExcelCell>();while(q.Count>0){var p=q.Dequeue();if(!source.TryGetValue(p,out var cell))continue;source.Remove(p);group.Add(cell);foreach(var d in dirs)if(source.ContainsKey(p+d))q.Enqueue(p+d);}result.Add(group);}return result;
    }
    static Sprite SpriteAsset(string name,int kind)
    {
        string path="Assets/Art/"+name+".png";var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)
        {
            Color color=Color.clear;
            if(kind==0)color=y>=14?new Color(.25f,.37f,.4f):(x+y)%7==0?new Color(.14f,.23f,.27f):new Color(.10f,.17f,.21f);
            if(kind==1&&Mathf.Abs(x-7.5f)<=(15-y)*.5f)color=x<7?new Color(.98f,.31f,.27f):new Color(.73f,.12f,.16f);
            if(kind==2)color=x>=10&&x<=12&&y>=10&&y<=12?new Color(.02f,.14f,.2f):(x==7||y==7?new Color(.65f,.72f,.75f):Color.white);
            if(kind==3&&(x<=2||(y>=8&&y<=13&&x<=12)))color=Color.white;
            if(kind==4&&(x<=1||x>=14||y<=1||y>=14))color=Color.white;
            texture.SetPixel(x,y,color);
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Prefabs(PhysicsMaterial2D material)
    {
        foreach(var kind in new[]{CellKind.Ground,CellKind.Spike,CellKind.Checkpoint})
        {
            var go=Picture(kind.ToString(),Vector2.zero,Vector2.one,kind==CellKind.Ground?ground:kind==CellKind.Spike?spike:checkpointSprite,Color.white,null);
            var cell=go.AddComponent<SeasideCell>();cell.kind=kind;
            var col=go.AddComponent<BoxCollider2D>();col.isTrigger=kind!=CellKind.Ground;col.sharedMaterial=material;go.layer=kind==CellKind.Ground?6:kind==CellKind.Spike?7:0;
            if(kind==CellKind.Checkpoint){var anchor=new GameObject("Respawn - drag to safe ground").transform;anchor.SetParent(go.transform);anchor.localPosition=Vector3.up*1.2f;cell.respawnAnchor=anchor;}
            PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/"+kind+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
    }
    public static void BuildAll()
    {
        CreateScene();SeasideTests.Run();EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");Directory.CreateDirectory(Root+"/Play");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Seaside01.unity"},locationPathName=Root+"/Play/SeasideWhitebox.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed "+result.summary.result);
        Debug.Log("SEASIDE_BUILD_SUCCESS bytes="+result.summary.totalSize);
    }
    // Targeted migration: preserves the user's existing map geometry and Scene edits.
    public static void UpdateCharacterAndBuild()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        var motor=UnityEngine.Object.FindFirstObjectByType<SeasideMotor>();
        var collider=motor.GetComponent<BoxCollider2D>();float previousHalf=collider.size.y*.5f;
        var cfg=motor.settings;cfg.bodySide=2;cfg.shortJumpHeight=6;cfg.longJumpHeight=10;cfg.dashDistance=10;
        EditorUtility.SetDirty(cfg);
        float lift=1f-previousHalf;
        motor.transform.localScale=Vector3.one;collider.size=new Vector2(2,2);
        var picture=motor.GetComponent<SpriteRenderer>();picture.sprite=SpriteAsset("Player2x2",2);picture.drawMode=SpriteDrawMode.Sliced;picture.size=collider.size;
        motor.transform.position+=Vector3.up*lift;
        var game=UnityEngine.Object.FindFirstObjectByType<SeasideSession>();game.startPoint+=Vector2.up*lift;
        foreach(var cp in UnityEngine.Object.FindObjectsByType<SeasideCell>(FindObjectsSortMode.None))if(cp.kind==CellKind.Checkpoint)
        {
            cp.respawn+=Vector2.up*lift;
            if(cp.respawnAnchor!=null)cp.respawnAnchor.position+=Vector3.up*lift;
            EditorUtility.SetDirty(cp);
        }
        EditorUtility.SetDirty(game);EditorUtility.SetDirty(motor);EditorUtility.SetDirty(collider);EditorUtility.SetDirty(picture);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        SeasideTests.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");Directory.CreateDirectory(Root+"/Play");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Seaside01.unity"},locationPathName=Root+"/Play/SeasideWhitebox.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed "+result.summary.result);
        Debug.Log("SEASIDE_2X2_BUILD_SUCCESS bytes="+result.summary.totalSize);
    }
}
