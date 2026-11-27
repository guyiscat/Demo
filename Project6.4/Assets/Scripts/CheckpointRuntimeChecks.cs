using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class CheckpointRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(!Environment.GetCommandLineArgs().Contains("-checkpointChecks"))return;var go=new GameObject("CheckpointRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<CheckpointRuntimeChecks>();}
        void Check(bool ok,string label){failed|=!ok;lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());}
        IEnumerator Start()
        {
            foreach(var path in CampaignFlow.Scenes)
            {
                yield return SceneManager.LoadSceneAsync(path);yield return null;yield return new WaitForFixedUpdate();yield return null;
                var session=FindAnyObjectByType<SeasideSession>();var motor=session.player;var actor=motor.GetComponent<Forest.ForestPlayer>();if(actor!=null)actor.manualInput=true;motor.externalInput=true;
                FindAnyObjectByType<Forest.ForestIntroCamera>()?.Cancel(true);
                if(FindAnyObjectByType<Lighthouse.LighthouseLevel>() is Lighthouse.LighthouseLevel tower)tower.collapse.manualSimulation=true;
                var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
                var checkpoints=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
                Check(checkpoints.Select(c=>c.checkpointId).Distinct().Count()==checkpoints.Length,"Unique checkpoint IDs in "+path);
                foreach(var cp in checkpoints)
                {
                    session.Respawn();motor.ResetAt(cp.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();Physics2D.Simulate(.01f);
                    Check(session.CheckpointId==cp.checkpointId,"Actual trigger selects "+CampaignFlow.RegionLabel(path)+"/"+cp.name+" #"+cp.checkpointId);
                    session.Respawn();Check(Vector2.Distance(motor.Body.position,cp.respawnAnchor.position)<.01f,"Respawn uses repaired anchor "+cp.name);
                    for(int i=0;i<40;i++){if(actor!=null)actor.Tick(.01f,default);else motor.Tick(.01f,default);Physics2D.Simulate(.01f);}
                    Check(motor.Grounded&&!session.Respawning,"Repaired checkpoint settles on safe ground "+cp.name);
                    Check(cp.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),"Checkpoint remains invisible after activation "+cp.name);
                }
                Physics2D.simulationMode=previous;
            }
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-checkpointReport");string report=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(report));File.WriteAllLines(report,lines);Application.Quit(failed?1:0);
        }
    }
}
