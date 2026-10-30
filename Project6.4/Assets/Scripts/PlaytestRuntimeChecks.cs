using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Seaside
{
    public sealed class PlaytestRuntimeChecks:MonoBehaviour
    {
        readonly List<string> log=new List<string>();
        SeasideSession game;SeasideMotor player;GameObject fixtures;
        void Check(bool passed,string label){log.Add((passed?"PASS ":"FAIL ")+label);}
        void Step(SeasideInput c){player.Tick(.01f,c);Physics2D.Simulate(.01f);}
        void Reset(Vector2 p){player.ResetAt(p);Physics2D.SyncTransforms();}
        void Start()
        {
            game=GetComponent<SeasideSession>();player=game.player;player.externalInput=true;
            var old=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            fixtures=new GameObject("Runtime regression fixtures");var wall=new GameObject("Wall");
            wall.transform.SetParent(fixtures.transform);wall.transform.position=new Vector3(-20,25,0);wall.layer=6;
            wall.AddComponent<BoxCollider2D>().size=new Vector2(1,50);wall.AddComponent<SeasideCell>().kind=CellKind.Ground;
            try
            {
                Reset(new Vector2(-21.51f,25));Step(new SeasideInput{horizontal=1});
                Check(player.Attached,"Initial held key attaches");
                float start=player.Body.position.y;
                for(int i=0;i<160;i++)Step(new SeasideInput());
                Check(!player.Attached&&player.WallSliding&&start-player.Body.position.y>4,"Released key smoothly slides on wall");
                Check(player.Body.linearVelocity.y>-3.51f&&player.Body.linearVelocity.y< -3.4f,"Released-wall descent settles smoothly near 3.5 cells/s");
                Step(new SeasideInput{horizontal=1});float heldY=player.Body.position.y;
                for(int i=0;i<100;i++)Step(new SeasideInput{horizontal=1});
                Check(player.Attached&&Mathf.Abs(heldY-player.Body.position.y)<.01f,"Same-wall repress hovers immediately and stays still");
                Step(new SeasideInput());
                Check(player.WallSliding&&player.Body.linearVelocity.y>-.25f,"Second release restarts smooth slow slide");
                for(int i=0;i<25;i++)Step(new SeasideInput{horizontal=-1});
                for(int i=0;i<70&&!player.Attached;i++)Step(new SeasideInput{horizontal=1});
                Check(player.Attached,"Moving away and returning restores attachment");
                Reset(new Vector2(-25,20));int t=0;
                do{Step(new SeasideInput{dashDown=t==0,aim=new Vector2(1,1)});t++;}while(player.Dashing&&t<100);
                float impactY=player.Body.position.y;Step(new SeasideInput());
                Check(player.DashStoppedByTerrain&&!player.Dashing&&Mathf.Abs(player.Body.position.y-impactY)<.01f,"Diagonal dash stops at wall without vertical dash slide");
                MovementGraceChecks.Run(player,Check);
                WallJumpRegression.Run(player,Check);
                var comfort=CameraComfortChecks.Run(Check);
                string reportFolder=Path.GetFullPath(Path.Combine(Application.dataPath,"../..","Docs"));
                Directory.CreateDirectory(reportFolder);File.WriteAllLines(Path.Combine(reportFolder,"runtime-camera-comfort.csv"),comfort);
                AfterimageChecks.Run(player,Check);
                var cps=FindObjectsByType<SeasideCell>(FindObjectsSortMode.None).Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
                game.Restart();game.Checkpoint(cps[5]);var p=game.RespawnPoint;
                game.Checkpoint(cps[0]);game.Checkpoint(cps[2]);game.Respawn();
                Check(game.CheckpointId==cps[5].checkpointId&&Vector2.Distance(player.Body.position,p)<.01f,"Old checkpoints cannot replace latest respawn");
                game.Checkpoint(cps.Last());game.Checkpoint(cps[3]);
                Check(game.CheckpointId==cps.Last().checkpointId,"Higher progression retained after touching older checkpoint");
                var camera=FindFirstObjectByType<SeasideCamera>();
                // This assertion measures follow-mode framing, regardless of whether the
                // authored scene starts in overview mode (the art preview deliberately does).
                bool wasOverview=camera.overview;camera.overview=false;camera.Snap();Vector3 snap=camera.transform.position;
                Check(Mathf.Abs(snap.z+10)<.001f&&Mathf.Abs(snap.x-player.Body.position.x)<20,"Camera respawn framing finite and near player");
                camera.overview=wasOverview;
            }
            catch(Exception ex){log.Add("FAIL "+ex);}
            finally
            {
                Physics2D.simulationMode=old;Destroy(fixtures);player.externalInput=false;game.Restart();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../..","Docs"));Directory.CreateDirectory(folder);
                File.WriteAllLines(Path.Combine(folder,"standalone-playtest-regression.txt"),log);
            }
            bool failed=log.Any(s=>s.StartsWith("FAIL"));Debug.Log(failed?"PLAYTEST_RUNTIME_FAIL":"PLAYTEST_RUNTIME_PASS");Application.Quit(failed?1:0);
        }
    }
}

