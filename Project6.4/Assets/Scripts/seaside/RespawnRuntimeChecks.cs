using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class RespawnRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();bool failed;string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-respawnChecks")){var go=new GameObject("RespawnRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<RespawnRuntimeChecks>();}}
        void Check(bool ok,string label){failed|=!ok;lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"-respawnReport")+1];Directory.CreateDirectory(folder);
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[0]);yield return null;yield return new WaitForFixedUpdate();
            var session=FindAnyObjectByType<SeasideSession>();var motor=session.player;motor.externalInput=true;
            var thorn=FindObjectsByType<SpriteRenderer>().Single(r=>r.name=="thorn1");
            var hazard=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Spike).Select(c=>c.GetComponent<Collider2D>()).OrderBy(c=>Vector2.Distance(c.bounds.center,thorn.bounds.center)).First();
            lines.Add("INFO thorn1="+thorn.bounds+" hazard="+hazard.name+" "+hazard.bounds);
            var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            foreach(var cp in FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId))
            {
                session.Respawn();motor.ResetAt(cp.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();Physics2D.Simulate(.01f);
                Check(session.CheckpointId==cp.checkpointId,"Real trigger selected #"+cp.checkpointId);
                Vector2 intended=cp.GetComponent<Collider2D>().bounds.center;
                lines.Add("INFO cp="+cp.checkpointId+" trigger="+intended+" anchor="+session.RespawnPoint);
                if(cp.checkpointId==9)
                {
                    Check(Mathf.Abs(session.RespawnPoint.y-intended.y)<motor.BodySize.y,"Checkpoint 9 uses its own platform, not the upper floor");
                    foreach(float y in new[]{session.RespawnPoint.y,intended.y})
                        lines.Add("INFO support y="+y+" "+string.Join("; ",Physics2D.RaycastAll(new Vector2(session.RespawnPoint.x,y),Vector2.down,12,1<<6).Select(h=>h.collider.name+" at "+h.point+" normal="+h.normal)));
                }
                motor.ResetAt(hazard.bounds.center);Physics2D.SyncTransforms();Physics2D.Simulate(.01f);
                Check(session.Respawning&&motor.frozen,"Actual thorn1-area collision starts death lock #"+cp.checkpointId);
                yield return new WaitForSecondsRealtime(GameSystemSettings.Current.respawnDelay+.1f);
                Check(!session.Respawning&&!motor.frozen&&motor.Body.simulated,"Timed death releases physics/control #"+cp.checkpointId);
                Check(Vector2.Distance(motor.Body.position,session.RespawnPoint)<.01f,"Timed revival uses active anchor #"+cp.checkpointId);
                for(int i=0;i<50;i++){motor.Tick(.01f,default);Physics2D.Simulate(.01f);}
                Check(motor.Grounded&&!session.Respawning&&Mathf.Abs(motor.Body.linearVelocity.y)<.1f,"Revival settles on solid ground #"+cp.checkpointId);
                if(cp.checkpointId==9)
                {
                    Check(Mathf.Abs(motor.Body.position.y-intended.y)<motor.BodySize.y,"thorn1 revival stays beside checkpoint 9 without high spawn");
                    FindAnyObjectByType<SeasideCamera>()?.Snap();
                    if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"thorn1-after-respawn.png"));yield return null;}
                    motor.ResetAt(new Vector2(83.3f,66.05f));Physics2D.SyncTransforms();
                    Check(ManualSaveSafety.SafeGround(motor,motor.Body.position),"Checkpoint 9 lower-platform candidate is safe");
                }
            }
            Physics2D.simulationMode=previous;File.WriteAllLines(Path.Combine(folder,"respawn-runtime-checks.txt"),lines);Application.Quit(failed?1:0);
        }
    }
}
