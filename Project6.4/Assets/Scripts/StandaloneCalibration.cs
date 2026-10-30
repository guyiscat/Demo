using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Seaside
{
    public sealed class StandaloneCalibration:MonoBehaviour
    {
        SeasideMotor motor;SeasideSession game;readonly List<string> results=new List<string>();GameObject fixture;
        SimulationMode2D oldMode;
        void Start()
        {
            game=GetComponent<SeasideSession>();motor=game.player;motor.externalInput=true;
            oldMode=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            fixture=new GameObject("Runtime calibration floor");fixture.layer=6;fixture.transform.position=new Vector3(-50,-.5f,0);fixture.AddComponent<BoxCollider2D>().size=new Vector2(30,1);
            try
            {
                Check(motor.BodySize==new Vector2(2,2),"Player is a four-cell 2x2 square");
                Check(Mathf.Abs(Jump(false)-4)<.08f,"Tap jump to four map pixels / two bodies");
                Check(Mathf.Abs(Jump(true)-8)<.08f,"Held jump to eight map pixels / four bodies");
                motor.ResetAt(new Vector2(-100,40));Physics2D.SyncTransforms();Vector2 p=motor.Body.position;
                for(int i=0;i<32;i++)Step(new SeasideInput{dashDown=i==0,aim=new Vector2(1,1)});
                float distance=Vector2.Distance(p,motor.Body.position);Check(Mathf.Abs(distance-8)<.02f,"Mouse-aim diagonal dash distance="+distance.ToString("F4")+" map pixels / "+(distance/2).ToString("F4")+" bodies");
                int uses=motor.DashUses;Step(new SeasideInput{dashDown=true,aim=Vector2.left});Check(motor.DashUses==uses,"Second air dash rejected");
                var cells=FindObjectsByType<SeasideCell>(FindObjectsSortMode.None);
                var checkpoint=Array.Find(cells,c=>c.kind==CellKind.Checkpoint);
                motor.ResetAt(checkpoint.transform.position);Physics2D.SyncTransforms();Step(new SeasideInput());
                Check(game.CheckpointId==checkpoint.checkpointId,"Actual yellow trigger activates checkpoint");
                game.Restart();
                var spike=Array.Find(cells,c=>c.kind==CellKind.Spike);
                motor.ResetAt(spike.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();Step(new SeasideInput());
                Check(game.Deaths==1&&game.Respawning,"Actual red-cell trigger kills player");
            }
            catch(Exception e){results.Add("FAIL "+e);}
            finally
            {
                Physics2D.simulationMode=oldMode;Destroy(fixture);motor.externalInput=false;game.Restart();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../..","Docs"));Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,"standalone-calibration.txt"),results);
            }
            bool failed=results.Exists(s=>s.StartsWith("FAIL"));Debug.Log(failed?"SEASIDE_RUNTIME_CALIBRATION_FAIL":"SEASIDE_RUNTIME_CALIBRATION_PASS");Application.Quit(failed?1:0);
        }
        void Check(bool passed,string text){results.Add((passed?"PASS ":"FAIL ")+text);}
        void Step(SeasideInput input){motor.Tick(.01f,input);Physics2D.Simulate(.01f);}
        float Jump(bool held)
        {
            motor.ResetAt(new Vector2(-50,1.2f));Physics2D.SyncTransforms();for(int i=0;i<40;i++)Step(new SeasideInput());
            float p=motor.Body.position.y,apex=p;Step(new SeasideInput{jumpDown=true,jumpHeld=true});
            for(int i=0;i<200;i++){Step(new SeasideInput{jumpHeld=held});apex=Mathf.Max(apex,motor.Body.position.y);if(motor.Body.linearVelocity.y<=0)break;}
            float height=apex-p;results.Add("MEASURE "+(held?"long":"short")+" jump="+height.ToString("F4"));return height;
        }
    }
}
