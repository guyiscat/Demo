using System.Collections;
using UnityEngine;
namespace Seaside
{
    public sealed class SeasideSession:MonoBehaviour
    {
        public SeasideMotor player;
        public Vector2 startPoint;
        public Vector2 RespawnPoint{get;private set;}
        public int Deaths{get;private set;}
        public int CheckpointId{get;private set;}
        public bool Paused{get;private set;}
        public bool Won{get;private set;}
        public bool Respawning{get;private set;}
        public bool ShowRoute=true,ShowHelp;
        public bool InputBlocked=>Paused||Won||Respawning;
        public float Timer{get;private set;}
        void Awake(){Application.targetFrameRate=120;}
        void Start()
        {
            SeasideLog.Begin();
            RespawnPoint=startPoint;
            var args=System.Environment.GetCommandLineArgs();
            if(System.Array.IndexOf(args,"-trailPreview")>=0)gameObject.AddComponent<TrailVisualPreview>();
            if(System.Array.IndexOf(args,"-cameraPreview")>=0)gameObject.AddComponent<CameraPreview>();
            if(System.Array.IndexOf(args,"-regression")>=0)gameObject.AddComponent<PlaytestRuntimeChecks>();
            if(System.Array.IndexOf(args,"-calibrate")>=0)gameObject.AddComponent<StandaloneCalibration>();
            if(System.Array.IndexOf(args,"-smokeTest")>=0)StartCoroutine(Smoke());
            int capture=System.Array.IndexOf(args,"-capture");
            if(capture>=0&&capture+1<args.Length)StartCoroutine(Capture(args[capture+1],System.Array.IndexOf(args,"-overview")>=0));
        }
        void Update()
        {
            if(!InputBlocked)Timer+=Time.deltaTime;
            if(Input.GetKeyDown(KeyCode.Escape)){Paused=!Paused;Time.timeScale=Paused?0:1;}
            if(Input.GetKeyDown(KeyCode.H))ShowHelp=!ShowHelp;
            if(Input.GetKeyDown(KeyCode.Tab))FindFirstObjectByType<SeasideCamera>().overview^=true;
            if(Input.GetKeyDown(KeyCode.B))ShowRoute=!ShowRoute;
            if(Input.GetKeyDown(KeyCode.R)){if(Input.GetKey(KeyCode.LeftControl)||Won)Restart();else Respawn();}
        }
        public void Checkpoint(SeasideCell cell)
        {
            // Checkpoint IDs represent progression; revisiting old ones must never overwrite the spawn.
            if(cell.checkpointId<=CheckpointId)return;
            CheckpointId=cell.checkpointId;RespawnPoint=cell.respawnAnchor==null?cell.respawn:(Vector2)cell.respawnAnchor.position;
            SeasideLog.Event("CHECKPOINT","id="+CheckpointId+" spawn="+RespawnPoint);
            foreach(var marker in FindObjectsByType<SeasideCell>(FindObjectsSortMode.None))if(marker.kind==CellKind.Checkpoint)
            {
                var sr=marker.GetComponent<SpriteRenderer>();if(sr!=null)sr.color=marker==cell?new Color(.3f,1,.65f):new Color(1,.78f,.2f);
            }
        }
        public void Die(){if(InputBlocked)return;Deaths++;SeasideLog.Event("DEATH","count="+Deaths+" position="+player.Body.position);Respawning=true;player.frozen=true;StartCoroutine(Revive());}
        IEnumerator Revive(){yield return new WaitForSeconds(.2f);Respawn();}
        public void Respawn(){StopAllCoroutines();Respawning=false;player.ResetAt(RespawnPoint);FindFirstObjectByType<SeasideCamera>()?.Snap();}
        public void Restart()
        {
            Won=Paused=Respawning=false;Time.timeScale=1;Timer=0;Deaths=CheckpointId=0;RespawnPoint=startPoint;
            foreach(var marker in FindObjectsByType<SeasideCell>(FindObjectsSortMode.None))if(marker.kind==CellKind.Checkpoint)
            {var sr=marker.GetComponent<SpriteRenderer>();if(sr!=null)sr.color=new Color(1,.78f,.2f);}
            Respawn();
        }
        public void Complete(){Won=true;player.frozen=true;}
        public void Resume(){Paused=false;Time.timeScale=1;}
        IEnumerator Smoke(){yield return new WaitForSeconds(3);Debug.Log("SEASIDE_SMOKE_PASS player="+player.State+" ground="+player.Grounded);Application.Quit(0);}
        IEnumerator Capture(string path,bool map)
        {
            yield return new WaitForSeconds(.5f);player.frozen=true;FindFirstObjectByType<SeasideCamera>().overview=map;
            yield return new WaitForSeconds(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(.5f);Application.Quit();
        }
        void OnDestroy(){SeasideLog.End();Time.timeScale=1;}
    }
}
