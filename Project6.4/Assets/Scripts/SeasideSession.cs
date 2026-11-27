using System.Collections;
using UnityEngine;
namespace Seaside
{
    public sealed class SeasideSession:MonoBehaviour
    {
        public SeasideMotor player;
        public Vector2 startPoint;
        [Tooltip("Runtime default comes from Resources/GameSystems for both levels.")]
        public bool latestCheckpointWins;
        SeasideCell activeCheckpoint;
        public Vector2 RespawnPoint{get;private set;}
        public int Deaths{get;private set;}
        public int CheckpointId{get;private set;}
        public string RegionArrivalKey{get;private set;}="Entry";
        public bool Paused{get;private set;}
        public bool Won{get;private set;}
        public bool Respawning{get;private set;}
        public bool ShowRoute=true,ShowHelp;
        public bool Travelling{get;set;}
        public bool TravelMenuOpen{get;set;}
        public bool SaveMenuOpen{get;set;}
        public bool InputBlocked=>Paused||ShowHelp||Won||Respawning||Travelling||TravelMenuOpen||SaveMenuOpen||GameSettingsScreen.Opened;
        public float Timer{get;private set;}
        void Awake(){ApplySystemSettings();}
        public void ApplySystemSettings()
        {
            var common=GameSystemSettings.Current;
            if(common==null){Application.targetFrameRate=120;return;}
            Application.targetFrameRate=common.targetFrameRate;
            latestCheckpointWins=common.latestCheckpointWins;ShowRoute=common.showRoute;
        }
        void Start()
        {
            SeasideLog.Begin();
            RespawnPoint=startPoint;
            CampaignFlow.Attach(this);
            if(gameObject.scene.path==CampaignFlow.Scenes[5]&&GetComponent<Village.VillageArtwork>()==null)
                gameObject.AddComponent<Village.VillageArtwork>();
            if(CampaignFlow.ScenesContains(gameObject.scene.path)&&GameMediaSettings.Current!=null&&player.GetComponent<PlayerArtwork>()==null)
                player.gameObject.AddComponent<PlayerArtwork>();
            if(CampaignFlow.ScenesContains(gameObject.scene.path)&&player.GetComponent<ProtectionAura>()==null)
                player.gameObject.AddComponent<ProtectionAura>();
            if(!player.externalInput&&player.GetComponent<PlayerAimIndicator>()==null)
                player.gameObject.AddComponent<PlayerAimIndicator>();
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
            if(Respawning||Travelling||TravelMenuOpen||SaveMenuOpen||GameSettingsScreen.Opened)return;
            if(GameBindings.Pressed(GameAction.Pause))
            {if(ShowHelp)SetHelp(false);else if(Paused)Resume();else Pause();return;}
            if(Input.GetKeyDown(KeyCode.H)){SetHelp(!ShowHelp);return;}
            if(InputBlocked)return;
            if(Input.GetKeyDown(KeyCode.Tab))FindAnyObjectByType<SeasideCamera>().overview^=true;
            if(Input.GetKeyDown(KeyCode.B))ShowRoute=!ShowRoute;
            if(Input.GetKeyDown(KeyCode.R)){if(Input.GetKey(KeyCode.LeftControl)||Won)Restart();else Respawn();}
        }
        public void Checkpoint(SeasideCell cell)
        {
            if(latestCheckpointWins?cell==activeCheckpoint:cell.checkpointId<=CheckpointId)return;
            activeCheckpoint=cell;
            CheckpointId=cell.checkpointId;RespawnPoint=cell.respawnAnchor==null?cell.respawn:(Vector2)cell.respawnAnchor.position;
            SeasideLog.Event("CHECKPOINT","id="+CheckpointId+" spawn="+RespawnPoint);
            CampaignFlow.SaveCheckpoint(this);
            foreach(var marker in FindObjectsByType<SeasideCell>())if(marker.kind==CellKind.Checkpoint)
            {
                var sr=marker.GetComponent<SpriteRenderer>();if(sr!=null)sr.color=marker==cell?new Color(.3f,1,.65f):new Color(1,.78f,.2f);
            }
        }
        public void SetRegionArrival(string key,Vector2 point)
        {RegionArrivalKey=key;CheckpointId=0;activeCheckpoint=null;RespawnPoint=point;}
        public void Die(){if(InputBlocked)return;Deaths++;SeasideLog.Event("DEATH","count="+Deaths+" position="+player.Body.position);Respawning=true;player.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();player.BeginTraversal();player.frozen=true;StartCoroutine(Revive());}
        IEnumerator Revive(){yield return new WaitForSecondsRealtime(GameSystemSettings.Current!=null?GameSystemSettings.Current.respawnDelay:.5f);Respawn();}
        public void Respawn(){StopAllCoroutines();Respawning=false;player.ResetAt(RespawnPoint);FindAnyObjectByType<SeasideCamera>()?.Snap();}
        public void Restart()
        {
            Won=Paused=ShowHelp=Respawning=false;Time.timeScale=1;Timer=0;Deaths=CheckpointId=0;RegionArrivalKey="Entry";RespawnPoint=startPoint;
            activeCheckpoint=null;
            foreach(var marker in FindObjectsByType<SeasideCell>())if(marker.kind==CellKind.Checkpoint)
            {var sr=marker.GetComponent<SpriteRenderer>();if(sr!=null)sr.color=new Color(1,.78f,.2f);}
            Respawn();CampaignFlow.SaveCheckpoint(this);
        }
        public void Complete()
        {
            if(Won)return;
            if(!CampaignFlow.IsStoryEnding(this)){CampaignFlow.Complete(this);return;}
            Won=true;player.frozen=true;CampaignFlow.Complete(this);
        }
        public void RestoreCheckpoint(int checkpointId,float timer,int deaths)
        {
            Timer=Mathf.Max(0,timer);Deaths=Mathf.Max(0,deaths);
            foreach(var cell in FindObjectsByType<SeasideCell>(FindObjectsInactive.Include))
                if(cell.kind==CellKind.Checkpoint&&cell.checkpointId==checkpointId){Checkpoint(cell);break;}
            Respawn();
        }
        public void RestoreManualPosition(Vector2 position,float timer,int deaths)
        {
            Timer=Mathf.Max(0,timer);Deaths=Mathf.Max(0,deaths);
            SetRegionArrival("Entry",position);Respawn();
        }
        public void Pause(){Paused=true;ShowHelp=false;Time.timeScale=0;GameBindings.BlockFrame();}
        public void SetHelp(bool show){ShowHelp=show;Time.timeScale=show||Paused?0:1;GameBindings.BlockFrame();}
        public void Resume(){Paused=ShowHelp=false;Time.timeScale=1;GameBindings.BlockFrame();}
        IEnumerator Smoke(){yield return new WaitForSeconds(3);Debug.Log("SEASIDE_SMOKE_PASS player="+player.State+" ground="+player.Grounded);Application.Quit(0);}
        IEnumerator Capture(string path,bool map)
        {
            yield return new WaitForSeconds(.5f);player.frozen=true;FindAnyObjectByType<SeasideCamera>().overview=map;
            yield return new WaitForSeconds(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(.5f);Application.Quit();
        }
        void OnDestroy(){SeasideLog.End();Time.timeScale=1;}
    }
}
