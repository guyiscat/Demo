using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Seaside;

public static class SeasidePart1Backgrounds
{
    [Serializable] sealed class Layout { public float width,height; public Layer[] children; }
    [Serializable] sealed class Layer { public string name; public float x,y,width,height; public TextureInfo texture; }
    [Serializable] sealed class TextureInfo { public string uri; }
    static string Work => Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/20261030-part1-backgrounds");

    public static void ApplyBatch()
    {
        EditorSceneManager.OpenScene(SeasideArtSetup.PreviewScene);
        AddBackgrounds();
        Capture();
    }

    [MenuItem("Seaside/Art/Add remaining Part1 backgrounds")]
    public static void AddBackgrounds()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=SeasideArtSetup.PreviewScene)throw new InvalidOperationException("Open the saved SeasideArtPreview scene.");
        var art=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SeasideArtRoot>(true)).Single();
        var anchor=art.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.sprite!=null&&Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(r.sprite))=="bg3");
        if(anchor.flipX||anchor.flipY)throw new InvalidOperationException("Unflip bg3 before using its layout as an anchor.");
        // Read the layout beside the actual sprites; Figures only stores editable sources.
        string folder=Path.GetDirectoryName(AssetDatabase.GetAssetPath(anchor.sprite)).Replace('\\','/');
        var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Path.Combine(folder,"part1.json.ps")));
        var reference=layout.children.Single(l=>l.name=="bg3");
        var names=new[]{"bg2","bg1","bg0"};
        foreach(var name in names)
            if(art.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.sprite!=null&&Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(r.sprite))==name))
                throw new InvalidOperationException(name+" is already placed; preserve manual changes instead of duplicating it.");

        string geometry=SeasideSceneGeometry.Signature(scene);
        string anchorState=EditorJsonUtility.ToJson(anchor.transform)+EditorJsonUtility.ToJson(anchor);
        var settings=new TextureImporterSettings();
        var anchorImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(anchor.sprite));
        anchorImporter.ReadTextureSettings(settings);
        if(settings.spriteMeshType!=SpriteMeshType.FullRect)throw new InvalidOperationException("The bg3 anchor must use Full Rect for canvas alignment.");
        var assets=names.Select(name=>folder+"/"+layout.children.Single(l=>l.name==name).texture.uri).ToArray();
        foreach(var path in assets)if(!File.Exists(path))throw new FileNotFoundException(path);

        Directory.CreateDirectory(Work);
        var rows=new System.Collections.Generic.List<string>{"source\tobject\tparent\tx\ty\twidth\theight\tsortingOrder"};
        for(int i=0;i<names.Length;i++)
        {
            var layer=layout.children.Single(l=>l.name==names[i]);
            var importer=(TextureImporter)AssetImporter.GetAtPath(assets[i]);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=anchorImporter.spritePixelsPerUnit;
            importer.maxTextureSize=8192;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            var importSettings=new TextureImporterSettings();importer.ReadTextureSettings(importSettings);
            importSettings.spriteMeshType=SpriteMeshType.FullRect;importSettings.spriteGenerateFallbackPhysicsShape=false;
            importer.SetTextureSettings(importSettings);importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(assets[i]);
            if(sprite==null)throw new Exception("Sprite import failed: "+assets[i]);
            string objectName=names[i]=="bg2"?"Background_Distant":names[i]=="bg1"?"Background_Midground":"Terrain_Artwork";
            var go=new GameObject(objectName);SceneManager.MoveGameObjectToScene(go,scene);
            Undo.RegisterCreatedObjectUndo(go,"Place "+objectName);
            go.transform.SetParent(anchor.transform.parent,false);
            float sx=anchor.transform.localScale.x*anchor.sprite.bounds.size.x/reference.width;
            float sy=anchor.transform.localScale.y*anchor.sprite.bounds.size.y/reference.height;
            // Work in bg3's parent space, including its rotation, scale and off-centre pivot.
            var topLeft=anchor.transform.localPosition+anchor.transform.localRotation*Vector3.Scale(
                new Vector3(anchor.sprite.bounds.min.x,anchor.sprite.bounds.max.y,0),anchor.transform.localScale);
            float cx=layer.x+layer.width*.5f-reference.x,cy=layer.y+layer.height*.5f-reference.y;
            go.transform.localPosition=topLeft+anchor.transform.localRotation*new Vector3(cx*sx,-cy*sy,0);
            go.transform.localRotation=anchor.transform.localRotation;
            go.transform.localScale=new Vector3(layer.width*sx/sprite.bounds.size.x,layer.height*sy/sprite.bounds.size.y,anchor.transform.localScale.z);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=anchor.sharedMaterial;
            renderer.sortingLayerID=anchor.sortingLayerID;renderer.sortingOrder=anchor.sortingOrder+10*(i+1);
            if(names[i]=="bg0")
            {
                var foreground=art.transform.Find("Foreground");
                if(foreground==null){var group=new GameObject("Foreground");SceneManager.MoveGameObjectToScene(group,scene);group.transform.SetParent(art.transform,false);Undo.RegisterCreatedObjectUndo(group,"Create foreground");foreground=group.transform;}
                go.transform.SetParent(foreground,true);
            }
            rows.Add(names[i]+"\t"+objectName+"\t"+go.transform.parent.name+"\t"+go.transform.position.x+"\t"+go.transform.position.y+"\t"+renderer.bounds.size.x+"\t"+renderer.bounds.size.y+"\t"+renderer.sortingOrder);
        }
        if(geometry!=SeasideSceneGeometry.Signature(scene))throw new Exception("Gameplay geometry changed.");
        if(anchorState!=EditorJsonUtility.ToJson(anchor.transform)+EditorJsonUtility.ToJson(anchor))throw new Exception("Existing bg3 changed.");
        if(art.GetComponentsInChildren<Collider2D>(true).Length!=0)throw new Exception("Artwork acquired a collider.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllLines(Path.Combine(Work,"background-layout.tsv"),rows);
        File.WriteAllText(Path.Combine(Work,"background-checks.txt"),"PASS All three remaining bg images placed exactly once\nPASS Existing bg3 renderer and transform unchanged\nPASS Gameplay world geometry and checkpoint data unchanged\nPASS Artwork has no colliders\nPASS Scene saved\n");
        Selection.activeGameObject=art.gameObject;
        Debug.Log("SEASIDE_PART1_BACKGROUNDS_PASS");
    }

    static void Capture()
    {
        var follow=UnityEngine.Object.FindAnyObjectByType<SeasideCamera>();var camera=follow.GetComponent<Camera>();
        camera.aspect=16f/9;follow.overview=true;follow.Snap();
        var target=new RenderTexture(1920,1080,24);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Work,"backgrounds-preview.png"),pixels.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
