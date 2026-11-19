using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Seaside;

namespace Forest
{
    [DefaultExecutionOrder(-150)]
    public sealed class ForestLevel : MonoBehaviour
    {
        public ForestPlayer player;
        public SeasideSession session;
        public ForestWaterwheel wheel;
        public SeasideCamera cameraRig;
        public string sourceSha256;
        public ForestIntroCamera Intro{get;private set;}
        void Awake()
        {
            Intro=gameObject.AddComponent<ForestIntroCamera>();Intro.level=this;
        }
        void Start()
        {
            string[] args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-forestChecks")>=0)gameObject.AddComponent<ForestRuntimeChecks>();
            int i=Array.IndexOf(args,"-forestCapture");
            if(i>=0&&i+1<args.Length)StartCoroutine(Capture(args[i+1]));
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.R)&&(Input.GetKey(KeyCode.LeftControl)||session.Won))wheel.ResetWheel();
        }
        public void Restart(){wheel.ResetWheel();session.Restart();}
        IEnumerator Capture(string directory)
        {
            Directory.CreateDirectory(directory);yield return new WaitForSeconds(.4f);
            player.manualInput=true;
            player.motor.Body.constraints=RigidbodyConstraints2D.FreezeAll;
            player.Tick(.01f,new ForestInput{movement=new SeasideInput{aim=new Vector2(1,1).normalized}});
            player.AimIndicator.Refresh();
            cameraRig.overview=false;cameraRig.Snap();player.waterfall.Refresh();
            yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(directory,"forest-body-direction-arrow.png"));
            float trigger=player.settings.introWalkDistance;player.settings.introWalkDistance=0;
            Intro.Cancel(true);
            for(int frame=0;frame<900&&!Intro.Completed;frame++)
            {
                cameraRig.UpdateView(1f/60);player.waterfall.Refresh(1f/60);Intro.Tick(1f/60);
                if(frame==60||Intro.Phase==2)
                {
                    CaptureCamera(Path.Combine(directory,Intro.Phase==2?"forest-intro-full-map.png":"forest-intro-zooming.png"));
                    if(Intro.Phase==2)break;
                }
            }
            player.settings.introWalkDistance=trigger;Intro.Cancel();
            player.motor.frozen=true;
            yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(directory,"forest-start.png"));
            cameraRig.overview=true;yield return new WaitForSeconds(3);
            yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(directory,"forest-overview-left.png"));
            player.motor.WarpForTraversal(new Vector2(202,5.2f));player.waterfall.ResetSide(player.motor.Body.position);cameraRig.Snap();
            yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();
            CaptureCamera(Path.Combine(directory,"forest-overview-right.png"));
            cameraRig.overview=false;
            foreach(float side in new[]{-1f,1f})
            {
                var bounds=player.waterfall.curtainBounds.bounds;
                player.motor.WarpForTraversal(new Vector2(side<0?bounds.min.x-2:bounds.max.x+2,60));
                player.waterfall.ResetSide(player.motor.Body.position);cameraRig.Snap();player.waterfall.Refresh();
                yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(directory,side<0?"forest-near-waterfall-left.png":"forest-near-waterfall-right.png"));
            }
            yield return new WaitForSeconds(.5f);Application.Quit(0);
        }
        void CaptureCamera(string path)
        {
            // A hidden Windows player has no reliable screen back buffer.
            // Render the actual game camera explicitly, as the startup smoke test does.
            var camera=cameraRig.GetComponent<Camera>();var target=new RenderTexture(1440,900,24);
            var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                if(Intro.IsPlaying)
                {
                    if(Intro.Phase==2){Intro.GetFullMapPose(camera,out var position,out var size);camera.transform.position=position;camera.orthographicSize=size;}
                }
                else player.waterfall.Refresh();
                camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(pixels);
            }
        }
    }
}
