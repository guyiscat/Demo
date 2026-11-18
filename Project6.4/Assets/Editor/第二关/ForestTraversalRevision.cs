using System;
using System.Linq;
using Forest;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestTraversalRevision
{
    public static void CaptureTransition()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch-only visual check.");
        EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<ForestLevel>();var water=level.player.waterfall;
        var camera=level.cameraRig.GetComponent<Camera>();
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-forestVisualFolder");
        if(index<0)throw new ArgumentException("Missing -forestVisualFolder");
        string folder=args[index+1];System.IO.Directory.CreateDirectory(folder);
        var target=new RenderTexture(1440,900,24);var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        Action<string> capture=name=>
        {
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
        };
        Action<Vector2> place=pos=>{level.player.transform.position=pos;level.player.GetComponent<Rigidbody2D>().position=pos;Physics2D.SyncTransforms();};
        try
        {
            camera.targetTexture=target;
            level.cameraRig.overview=false;var b=water.curtainBounds.bounds;
            place(new Vector2(b.min.x-2,60));level.cameraRig.Snap();water.ResetSide(level.player.transform.position);capture("near-before-crossing");
            place(new Vector2(b.max.x+1.02f,60));level.cameraRig.UpdateView(.01f);water.Refresh(.01f);
            for(int i=0;i<35;i++){level.cameraRig.UpdateView(.01f);water.Refresh(.01f);}
            if(!water.IsTransitioning)throw new InvalidOperationException("Expected an actual intermediate camera frame.");
            capture("near-during-crossing");
            for(int i=0;i<1600&&water.IsTransitioning;i++){level.cameraRig.UpdateView(.01f);water.Refresh(.01f);}
            capture("near-after-crossing");
            level.cameraRig.overview=true;level.cameraRig.Snap();water.Refresh();capture("overview-after-crossing");
            place(new Vector2(b.min.x-1.02f,60));level.cameraRig.UpdateView(.01f);water.Refresh(.01f);capture("overview-during-crossing");
            for(int i=0;i<1600&&water.IsTransitioning;i++){level.cameraRig.UpdateView(.01f);water.Refresh(.01f);}
            capture("overview-settled-left");
            level.cameraRig.overview=false;place(new Vector2(244,66.05f));level.cameraRig.Snap();water.ResetSide(level.player.transform.position);capture("checkpoint-right-upper-lip");
            Debug.Log("FOREST_TRAVERSAL_CAPTURE_PASS actual camera renders; saved scene not modified");
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
    public static void Apply()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Patch the saved existing scene in an isolated batch project.");
        var scene=EditorSceneManager.OpenScene(ForestBuilder.ScenePath);
        var level=UnityEngine.Object.FindAnyObjectByType<ForestLevel>();
        level.session.latestCheckpointWins=true;
        var wheel=level.wheel;
        foreach(var platform in wheel.platforms.Skip(12))UnityEngine.Object.DestroyImmediate(platform.gameObject);
        wheel.platforms=wheel.platforms.Take(12).ToArray();
        for(int i=0;i<wheel.platforms.Length;i++)
        {
            var p=wheel.platforms[i];float angle=i*30;
            p.initialAngle=p.angle=p.previousAngle=angle;p.NextPosition=wheel.Orbit(angle);
            p.transform.position=p.NextPosition;p.body.position=p.NextPosition;p.Velocity=Vector2.zero;
        }
        var points=UnityEngine.Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).ToArray();
        if(!points.Any(c=>c.checkpointId==6))
        {
            // Screenshot: the safe lip on the right upper corridor, before the
            // drop to the two long cables. Use the current workbook's lip.
            Vector2 position=new Vector2(242,66.05f);
            var point=UnityEngine.Object.Instantiate(points[0],points[0].transform.parent);
            point.name="Checkpoint_06_RightUpperLip";point.checkpointId=6;
            point.transform.position=position;point.respawnAnchor.position=position;point.respawn=position;
            point.GetComponent<SpriteRenderer>().color=new Color(1,.78f,.2f);
        }
        level.player.waterfall.cameraTransitionSpeed=40;
        Physics2D.SyncTransforms();ForestEditorChecks.Run();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("FOREST_TRAVERSAL_REVISION_PASS sameScene="+ForestBuilder.ScenePath+" platforms=12 checkpoints=6 latestCheckpointWins=true");
    }
}
