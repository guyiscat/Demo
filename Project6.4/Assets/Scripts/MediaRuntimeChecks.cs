using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class MediaRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();string report;bool failed;
        string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i<0||i+1>=a.Length?null:a[i+1];}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mediaChecks"))return;
            var go=new GameObject("MediaRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<MediaRuntimeChecks>();
        }
        void Check(bool ok,string label){failed|=!ok;lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());}
        void OnLog(string value,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){failed=true;lines.Add("FAIL "+value+" "+stack);}}
        IEnumerator Capture(string name)
        {
            if(Arg("-mediaScreens") is string directory)
            {Directory.CreateDirectory(directory);FindAnyObjectByType<SeasideCamera>()?.Snap();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}
        }
        IEnumerator Start()
        {
            report=Arg("-mediaReport")??Path.Combine(Application.persistentDataPath,"media-checks.txt");Application.logMessageReceived+=OnLog;
            yield return null;var music=RegionMusic.Current;var config=GameMediaSettings.Current;
            Check(music!=null&&config!=null,"Global media and persistent music initialize from menu");
            foreach(var path in new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes))
            {
                yield return SceneManager.LoadSceneAsync(path);yield return null;yield return new WaitForSecondsRealtime(1.5f);
                Check(RegionMusic.Current==music&&FindObjectsByType<RegionMusic>().Length==1,"Exactly one persistent music service in "+path);
                Check(music.Track==config.Track(path)&&music.Track.length>100,"Correct imported music clip in "+path);
                var sources=music.GetComponents<AudioSource>();Check(sources.Length==2&&sources.All(s=>s.loop&&s.spatialBlend==0),"Stereo 2D looping sources in "+path);
                Check(Mathf.Abs(sources.Sum(s=>s.volume)-config.musicVolume)<.02f,"Crossfade reaches configured music level in "+path);
                if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {var current=sources.Single(s=>s.clip==music.Track&&s.volume>.01f);Check(current.isPlaying&&current.time>0,"Actual audio playback advances in "+path);}
                int requests=music.PlayRequests;music.Select(path);Check(music.PlayRequests==requests,"Same track selection does not restart playback in "+path);
                if(path==CampaignFlow.MenuScene)continue;
                var session=FindAnyObjectByType<SeasideSession>();var motor=session.player;var art=motor.GetComponent<PlayerArtwork>();
                if(path==CampaignFlow.Scenes[2]&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)TestMouseLight();
                Check(art!=null&&art.Picture!=null&&art.Picture.sprite!=null,"Imported character is visible in "+path);
                Check(motor.GetComponent<BoxCollider2D>().size==art.ColliderSizeAtInstall&&motor.transform.localScale==art.ScaleAtInstall&&motor.GetComponent<BoxCollider2D>().enabled,"Artwork preserves authored collider and scale in "+path);
                Check(motor.GetComponentInChildren<PlayerAimIndicator>()!=null&&motor.transform.Find("AimArrowArtwork")!=null,"Imported directional indicator in "+path);
                var intro=FindAnyObjectByType<Forest.ForestIntroCamera>();intro?.Cancel(true);
                var actor=motor.GetComponent<Forest.ForestPlayer>();if(actor!=null)actor.manualInput=true;
                motor.externalInput=true;
                var floor=Physics2D.Raycast(session.startPoint,Vector2.down,10,1<<6);
                Vector2 grounded=new Vector2(session.startPoint.x,floor.point.y+1.02f);
                motor.ResetAt(grounded);Physics2D.SyncTransforms();motor.Tick(.02f,new SeasideInput());
                motor.Body.simulated=false;yield return null;yield return Capture(CampaignFlow.RegionLabel(path)+"-idle");
                if(path==CampaignFlow.Scenes[2])
                {
                    var mine=FindAnyObjectByType<Mine.MineLevel>();var item=mine.interactions.Single(i=>i.completesLevel);
                    motor.ResetAt((Vector2)item.transform.position+new Vector2(2,1));motor.Body.simulated=false;yield return null;yield return Capture("Mine-bitcoin-glow");
                }
                if(path==CampaignFlow.Scenes[3])
                {
                    CampaignFlow.GrantCore(session);var thermal=motor.GetComponent<Expedition.ThermalTraversal>();var point=thermal.points[0];
                    motor.ResetAt(point.transform.position);motor.Body.simulated=false;if(thermal.Mode!=point.requiredProtection)thermal.TrySwitchMode();yield return null;yield return Capture("Snow-rectangular-climb");
                }
                if(path==CampaignFlow.Scenes[1])
                {
                    motor.Tick(.1f,new SeasideInput{horizontal=1});yield return null;Check(art.Pose=="Run"&&config.run.Contains(art.Picture.sprite),"Walking selects isolated PSD run frames");
                    var first=art.Picture.sprite;yield return new WaitForSeconds(.16f);Check(art.Picture.sprite!=first,"Run animation advances at configured frame rate");yield return Capture("Forest-run");
                    motor.ResetAt(new Vector2(20,10));motor.Body.simulated=false;motor.Body.linearVelocity=Vector2.up*12;yield return null;
                    Check(art.Pose=="JumpRise"&&art.Picture.sprite==config.jumpRise,"Unique raised-arm jump PNG supplements PSD");yield return Capture("Forest-jump");
                    motor.Body.linearVelocity=Vector2.down*12;yield return null;Check(art.Pose=="JumpFall"&&art.Picture.sprite==config.jumpFall,"Falling selects PSD pose");
                    motor.Tick(.01f,new SeasideInput{dashDown=true,aim=Vector2.right});yield return null;Check(art.Pose=="Dash"&&art.Picture.sprite==config.dash,"Air dash uses lossless PSD rather than matching JPG");yield return Capture("Forest-dash");
                    motor.ResetAt(new Vector2(20,8));motor.Body.simulated=false;yield return null;
                    actor.Tick(.01f,new Forest.ForestInput{teleportDown=true,movement=new SeasideInput{aim=Vector2.right}});
                    for(int i=0;i<8;i++)actor.Tick(.01f,new Forest.ForestInput());yield return null;
                    actor.Tick(.01f,new Forest.ForestInput{teleportDown=true});yield return null;
                    Check(art.TeleportVisible,"Teleport displays imported transparent departure/arrival effects; count="+actor.Teleports+" result="+actor.LastTeleportResult);yield return Capture("Forest-teleport");
                    motor.ResetAt(session.startPoint);motor.Body.simulated=false;yield return null;motor.Body.position+=Vector2.left*.2f;motor.transform.position=motor.Body.position;yield return null;
                    Check(art.Picture.flipX,"Character faces left without flipping collider or hierarchy");
                    float volume=AudioListener.volume;AudioListener.volume=0;Check(music.EffectiveVolume==0,"Master volume mutes music");AudioListener.volume=volume;
                    int before=music.PlayRequests;music.Select(CampaignFlow.Scenes[5]);int started=music.PlayRequests;music.Select(CampaignFlow.Scenes[2]);Check(started==before+1&&music.PlayRequests==started,"Village and Mine share uninterrupted music");music.Select(path);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));File.WriteAllLines(report,lines);Application.logMessageReceived-=OnLog;Application.Quit(failed?1:0);
        }
        void TestMouseLight()
        {
            var mine=FindAnyObjectByType<Mine.MineLevel>();var mask=mine.cameraRig.GetComponent<Mine.MineDarkness>();
            var material=new Material(mask.maskMaterial);var target=new RenderTexture(128,128,0);var pixels=new Texture2D(128,128,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                material.SetVector("_Player",new Vector4(.2f,.5f,0,0));material.SetVector("_ViewSize",new Vector4(36,36,0,0));material.SetFloat("_Radius",3);material.SetFloat("_Feather",1);
                material.SetVector("_Mouse",new Vector4(.8f,.5f,0,0));material.SetFloat("_MouseRadius",4);material.SetFloat("_MouseFeather",1);
                Graphics.Blit(Texture2D.whiteTexture,target,material);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();
                Check(pixels.GetPixel(102,64).r>.95f&&pixels.GetPixel(26,64).r>.95f&&pixels.GetPixel(64,64).r<.01f,"Mine shader illuminates cursor and player independently, preserving darkness elsewhere");
                material.SetFloat("_MouseRadius",0);Graphics.Blit(Texture2D.whiteTexture,target,material);pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();
                Check(pixels.GetPixel(102,64).r<.01f,"Disabling mouse light leaves that distant area black");
            }
            finally{RenderTexture.active=previous;target.Release();Destroy(target);Destroy(pixels);Destroy(material);}
        }
    }
}
