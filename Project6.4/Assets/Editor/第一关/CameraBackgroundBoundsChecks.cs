using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Seaside;

// Batch-only: these checks open scenes and must run in an isolated project copy.
public static class CameraBackgroundBoundsChecks
{
    static readonly List<string> results=new List<string>();
    static int assertions;
    static string Output => Environment.GetEnvironmentVariable("SEASIDE_CAMERA_CHECK_DIR") ??
        Path.Combine(SeasidePaths.ReleaseRoot,"Docs/Logs/Work/camera-background-checks");

    static void Check(bool condition,string message)
    {
        assertions++;
        if(!condition){results.Add("FAIL "+message);throw new Exception(message);}
    }

    static void CheckProjection(Camera camera,Bounds bounds)
    {
        var low=camera.ViewportToWorldPoint(new Vector3(0,0,-camera.transform.position.z));
        var high=camera.ViewportToWorldPoint(new Vector3(1,1,-camera.transform.position.z));
        const float tolerance=.0003f;
        Check(low.x>=bounds.min.x-tolerance&&low.y>=bounds.min.y-tolerance&&
              high.x<=bounds.max.x+tolerance&&high.y<=bounds.max.y+tolerance,
              "Camera viewport escaped the background: "+low.ToString("F7")+" / "+high.ToString("F7")+
              " bounds="+bounds.min.ToString("F7")+" / "+bounds.max.ToString("F7")+" viewport="+camera.rect.ToString("F7"));
        Check(camera.rect.x>=0&&camera.rect.y>=0&&camera.rect.xMax<=1.0001f&&camera.rect.yMax<=1.0001f,
              "Invalid letterbox viewport "+camera.rect);
    }

    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch checkout; these checks open scenes.");
        Directory.CreateDirectory(Output);results.Clear();assertions=0;
        try
        {
            var comfort=CameraComfortChecks.Run((pass,message)=>Check(pass,message));
            File.WriteAllLines(Path.Combine(Output,"camera-comfort.csv"),comfort);
            results.Add("PASS Existing camera comfort checks");
            EditorSceneManager.OpenScene("Assets/Scenes/SeasideArtPreview.unity");
            var follow=UnityEngine.Object.FindAnyObjectByType<SeasideCamera>();
            var camera=follow.GetComponent<Camera>();
            follow.overview=false;follow.Snap();
            Check(follow.backgroundBoundary!=null&&follow.backgroundBoundary.sprite.name=="bg3","Automatic bg3 boundary lookup failed");
            var background=follow.backgroundBoundary;
            Vector3 oldPosition=background.transform.position,oldScale=background.transform.localScale;
            var sizes=new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(2520,1080),new Vector2Int(900,1600),new Vector2Int(1000,1000)};
            foreach(var dimensions in sizes)
            {
                var target=new RenderTexture(dimensions.x,dimensions.y,24);camera.targetTexture=target;
                try
                {
                    foreach(int variant in new[]{0,1,2})
                    {
                        background.transform.position=oldPosition+new Vector3(variant*7,-variant*3,0);
                        background.transform.localScale=Vector3.Scale(oldScale,variant==0?Vector3.one:variant==1?new Vector3(.7f,.8f,1):new Vector3(.03f,.03f,1));
                        var bounds=background.bounds;
                        var points=new[]{bounds.center,bounds.min,bounds.max,
                            bounds.min-new Vector3(20,20,0),bounds.max+new Vector3(20,20,0)};
                        foreach(var point in points)
                        {
                            follow.player.transform.position=new Vector3(point.x,point.y,0);
                            foreach(bool overview in new[]{false,true})
                            {
                                follow.overview=overview;follow.Snap();CheckProjection(camera,bounds);
                                if(overview)
                                {
                                    Check(Mathf.Abs(camera.orthographicSize*2-bounds.size.y)<.001f,"Overview does not show full background height: actual="+(camera.orthographicSize*2).ToString("F7")+" expected="+bounds.size.y.ToString("F7")+" rect="+camera.rect.ToString("F7"));
                                    Check(Mathf.Abs(camera.orthographicSize*2*camera.aspect-bounds.size.x)<.001f,"Overview does not show full background width");
                                }
                            }
                            // Check the actual camera every frame through both mode transitions.
                            foreach(bool overview in new[]{false,true,false})
                            {
                                follow.overview=overview;
                                for(int i=0;i<90;i++){follow.UpdateView(1f/60);CheckProjection(camera,bounds);}
                            }
                            Time.timeScale=0;follow.UpdateView(0);CheckProjection(camera,bounds);Time.timeScale=1;
                        }
                    }
                    results.Add("PASS Projection at "+dimensions+", corners/outside targets, translated/scaled/tiny background, transitions and pause");
                }
                finally{camera.targetTexture=null;follow.Snap();target.Release();UnityEngine.Object.DestroyImmediate(target);}
            }
            background.transform.position=oldPosition;background.transform.localScale=oldScale;
            Capture(follow,camera,false,"follow-edge.png");
            Capture(follow,camera,true,"overview-letterbox.png");
            follow.overview=true;follow.Snap();
            Check(Resources.FindObjectsOfTypeAll<Camera>().Any(c=>c.name=="Background boundary letterbox"&&c.enabled),"Letterbox clear camera missing");
            follow.constrainToBackground=false;follow.Snap();
            Check(camera.rect==new Rect(0,0,1,1),"Disabling confinement did not restore viewport");
            results.Add("PASS Feature can be disabled and full viewport is restored");
            EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
            follow=UnityEngine.Object.FindAnyObjectByType<SeasideCamera>();follow.Snap();
            Check(follow.backgroundBoundary==null,"Original scene unexpectedly acquired an art boundary");
            Check(follow.GetComponent<Camera>().rect==new Rect(0,0,1,1),"Original scene viewport changed");
            results.Add("PASS Scene without bg3 keeps original camera behavior");
            results.Add("PASS Assertions="+assertions);
            File.WriteAllLines(Path.Combine(Output,"camera-bounds-checks.txt"),results);
            Debug.Log("CAMERA_BACKGROUND_BOUNDS_PASS assertions="+assertions);
        }
        catch(Exception ex)
        {
            results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(Output,"camera-bounds-checks.txt"),results);throw;
        }
        finally{Time.timeScale=1;}
    }

    static void Capture(SeasideCamera follow,Camera camera,bool overview,string name)
    {
        var target=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;
            var bounds=follow.backgroundBoundary.bounds;
            follow.player.transform.position=overview?bounds.center:bounds.min+new Vector3(2,2,0);
            follow.overview=overview;follow.Snap();
            RenderTexture.active=target;GL.Clear(true,true,Color.magenta);
            // Manual Camera.Render does not automatically render lower-depth cameras.
            foreach(var clear in Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.name=="Background boundary letterbox"&&c.enabled))clear.Render();
            camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
            if(overview)Check(pixels.GetPixel(2,2).maxColorComponent<.01f,"Letterbox area was not cleared to black");
            File.WriteAllBytes(Path.Combine(Output,name),pixels.EncodeToPNG());
            CheckProjection(camera,bounds);
        }
        finally
        {
            camera.targetTexture=null;follow.Snap();RenderTexture.active=previous;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    public static void RunAndBuildSavedScenes()
    {
        Run();
        // Build authored scene files, without requiring art-preview geometry to equal the old whitebox.
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        string play=Path.Combine(SeasidePaths.ReleaseRoot,"Play64");Directory.CreateDirectory(play);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/Seaside01.unity","Assets/Scenes/SeasideArtPreview.unity"},
            locationPathName=Path.Combine(play,"SeasideWhitebox.exe"),
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Camera bounds saved-scene build failed: "+report.summary.result);
        File.Copy(Path.Combine(Application.dataPath,"../Tools/StartWithLogs.ps1"),Path.Combine(play,"StartWithLogs.ps1"),true);
        Debug.Log("CAMERA_BOUNDS_SAVED_SCENES_BUILD_PASS");
    }
}
