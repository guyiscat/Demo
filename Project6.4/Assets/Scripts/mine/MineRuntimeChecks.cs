using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Seaside;
using UnityEngine;

namespace Mine
{
    public sealed class MineRuntimeChecks : MonoBehaviour
    {
        const float Dt=.01f;
        MineLevel level;ForestPlayer actor;SeasideMotor motor;SeasideSession session;
        readonly List<string> log=new List<string>();
        void Check(bool ok,string label){log.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(log[log.Count-1]);}
        void Step(ForestInput input=default,int count=1)
        {
            for(int i=0;i<count;i++)
            {
                level.lift.Advance(Dt);actor.Tick(Dt,input);
                if(!session.Paused)Physics2D.Simulate(Dt);
            }
        }
        void Place(Vector2 point){level.CloseDialogue();motor.ResetAt(point);Physics2D.SyncTransforms();}
        static ForestInput Input(float horizontal=0,bool jump=false,bool dash=false,bool teleport=false,bool ride=false,Vector2 aim=default)
            =>new ForestInput{movement=new SeasideInput{horizontal=horizontal,jumpDown=jump,jumpHeld=jump,dashDown=dash,aim=aim},teleportDown=teleport,rideHeld=ride};
        IEnumerator Start()
        {
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            level=GetComponent<MineLevel>();actor=level.player;motor=actor.motor;session=level.session;
            actor.manualInput=true;level.lift.manualSimulation=true;
            var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            GameObject fixture=null;
            try
            {
                SystemRuntimeChecks.Run(session,Check);
                Place(session.startPoint);Step(count:20);
                Check(motor.Grounded&&!session.Respawning,"Actual Mine entrance is safe and grounded on static terrain");
                Check(actor.wheel==null&&actor.waterfall==null,"Mine runs without Forest wheel or waterfall instances");
                foreach(var rail in actor.ziplines)
                {
                    Vector2 target=Vector2.zero;bool free=false;
                    for(int i=1;i<10;i++)
                    {
                        var p=rail.Point(rail.Length*i/10f)-Vector2.up*1.12f;
                        if(!actor.IsFree(p)||Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,1<<7).Any())continue;
                        target=p;free=true;break;
                    }
                    Check(free,"Real authored cable has a clear body catch point: "+rail.name);
                    if(!free)continue;
                    Place(target);Step(Input(ride:true));
                    Check(actor.RidingRail==rail,"W catches authored Mine cable: "+rail.name);
                    if(actor.RidingRail!=rail)continue;
                    float x=motor.Body.position.x;Step(Input(horizontal:1),count:3);
                    Check(motor.Body.position.x>x,"D slides right on "+rail.name);
                    x=motor.Body.position.x;Step(Input(horizontal:-1),count:3);
                    Check(motor.Body.position.x<x,"A reverses the same cable: "+rail.name);
                    Step(Input(jump:true));Check(actor.RidingRail==null&&motor.Body.linearVelocity.y>20,"SPACE leaves authored cable: "+rail.name);
                }
                fixture=new GameObject("MineRuntimeFixtures");
                var floor=new GameObject("Floor");floor.transform.SetParent(fixture.transform);floor.layer=6;floor.transform.position=new Vector2(-110,264.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(50,1);
                var wall=new GameObject("Wall");wall.transform.SetParent(fixture.transform);wall.layer=6;wall.transform.position=new Vector2(-90,280);wall.AddComponent<BoxCollider2D>().size=new Vector2(1,30);
                Physics2D.SyncTransforms();Place(new Vector2(-110,266.04f));Step(count:20);float y=motor.Body.position.y;
                Step(Input(jump:true));float apex=y;
                for(int i=0;i<100;i++){Step(Input(jump:false));apex=Mathf.Max(apex,motor.Body.position.y);if(motor.Body.linearVelocity.y<=0)break;}
                Check(apex>y+3.7f&&apex<y+4.2f,"Mine tap jump shares four-unit height");
                Place(new Vector2(-90.51f-motor.BodySize.x*.5f,280));Step(Input(horizontal:1));Check(motor.Attached,"Mine shares wall attachment");
                Step(Input(horizontal:-1,jump:true));Check(motor.WallKicks==1&&motor.Body.linearVelocity.x<0&&motor.Body.linearVelocity.y>30,"Mine shares wall kick");
                Place(new Vector2(-110,280));Step(Input(dash:true,aim:Vector2.right));
                Check(motor.Dashing,"Mine keeps original dash");Step(Input(teleport:true,aim:Vector2.up));
                Check(actor.BeaconActive&&!motor.Dashing&&motor.Body.linearVelocity.sqrMagnitude<.0001f,"Mine reuses teleport dash interruption without residual burst");
                Step(Input(teleport:true));float hoverY=motor.Body.position.y;
                Step(count:Mathf.FloorToInt(actor.settings.teleportHoverDuration*.5f/Dt));
                Check(!actor.BeaconActive&&actor.PostTeleportHoverRemaining>0&&Mathf.Abs(motor.Body.position.y-hoverY)<.005f,"Mine teleport landing shares adjustable configured hover");
                Step(count:Mathf.CeilToInt(actor.PostTeleportHoverRemaining/Dt)+10);Check(motor.Body.position.y<hoverY-.02f,"Mine hover expires and gravity resumes");
                var workbench=level.interactions.Single(i=>i.kind==MineInteractionKind.Workbench);
                Place(new Vector2(-110,280));Check(!level.BeginInteraction(workbench),"Out-of-range E cannot activate remote interaction");
                Place(workbench.transform.position);Check(level.BeginInteraction(workbench)&&motor.frozen,"Workbench starts frozen narrative interaction");
                Check(level.Dialogue.Contains("资金"),"Workbench first page matches supplied design");level.AdvanceDialogue();
                Check(level.Dialogue.Contains("离职"),"Workbench second page matches supplied design");level.AdvanceDialogue();
                Check(level.Dialogue.Contains("停工"),"Workbench last page matches supplied design");level.AdvanceDialogue();
                Check(level.WorkbenchRead&&workbench.Used&&level.Dialogue==null&&!motor.frozen,"Finishing dialogue restores player control");
                var liftSwitch=FindObjectsByType<MineTouchSwitch>().Single(i=>i.lift==level.lift);
                Check(!level.lift.Activated,"Lift remains locked before touching switch");
                Place(liftSwitch.transform.position);liftSwitch.Touch(motor);
                Check(level.lift.Activated&&!level.lift.Moving,"Touch unlocks lift; no automatic departure");
                TestLift();
                foreach(var cp in FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderByDescending(c=>c.checkpointId))
                {
                    session.Checkpoint(cp);session.Respawn();
                    Check(Vector2.Distance(motor.Body.position,cp.respawnAnchor.position)<.01f,"Checkpoint supports latest-contact respawn: "+cp.name);
                    Step(count:10);Check(motor.Grounded&&!session.Respawning,"Checkpoint is safe after physical settling: "+cp.name);
                }
                var spike=FindObjectsByType<SeasideCell>().First(c=>c.kind==CellKind.Spike);
                int deaths=session.Deaths;Place(spike.transform.position);Step(count:3);
                Check(session.Deaths==deaths+1&&session.Respawning,"Actual workbook spike kills the Mine player");session.Respawn();
                var relic=level.interactions.Single(i=>i.completesLevel);
                Place(relic.transform.position);Check(level.BeginInteraction(relic),"Deepest authored relic is interactable");
                Check(level.Dialogue.Contains("比特币"),"Relic first page grants Bitcoin");level.AdvanceDialogue();
                Check(level.Dialogue.Contains("求而不得"),"Relic second page matches supplied design");level.AdvanceDialogue();
                Check(level.HasBitcoin&&!session.Won&&!motor.frozen,"Deepest relic is collected without automatic region change");
                level.Restart();Step(count:10);
                Check(!session.Won&&!level.HasBitcoin&&!level.WorkbenchRead&&!level.lift.Activated&&level.interactions.All(i=>!i.Used),"Restart clears narrative, collectible and lift state");
                Check(motor.Grounded&&actor.TeleportCharged&&motor.DashReady,"Restart returns to safe entrance with shared abilities ready");
            }
            catch(Exception e){Check(false,e.ToString());Debug.LogException(e);}
            finally
            {
                Physics2D.simulationMode=previous;if(fixture!=null)Destroy(fixture);
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-mineReport");
                string path=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"mine-runtime-checks.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllLines(path,log);
            }
            bool failed=log.Any(s=>s.StartsWith("FAIL"));Debug.Log(failed?"MINE_RUNTIME_FAIL":"MINE_RUNTIME_PASS assertions="+log.Count);Application.Quit(failed?1:0);
        }
        void TestLift()
        {
            var lift=level.lift;lift.ResetLift();Physics2D.SyncTransforms();
            Place(lift.body.position+Vector2.up*(lift.Collider.bounds.extents.y+1.012f));Step(count:5);
            Check(actor.RidingSurface==lift,"Stationary lift is recognized as shared physical support");
            lift.Activate();Check(lift.Use(motor),"E dispatches unlocked lift");float y=motor.Body.position.y;Step(count:30);
            Check(actor.RidingSurface==lift&&motor.Body.position.y<y-1,"Active descending lift carries a standing player");
            Check(Mathf.Abs(motor.Body.position.y-lift.body.position.y-lift.Collider.bounds.extents.y-1.012f)<.05f,"Lift preserves feet contact without parenting player");
            Check(motor.transform.parent!=lift.transform,"Lift does not parent-lock the player");
            Step(Input(jump:true));Check(actor.RidingSurface==null&&motor.Body.linearVelocity.y>20,"Jump leaves lift using ordinary jump rules");
            Vector2 before=lift.body.position;
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Step(count:20);
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,false);
            Check(Vector2.Distance(before,lift.body.position)<.001f,"Pause freezes lift progression");
            Place(new Vector2(-110,280));bool bottom=false,top=false;float minimum=lift.body.position.y,maximum=lift.body.position.y;
            for(int i=0;i<3000&&!top;i++)
            {
                Step();minimum=Mathf.Min(minimum,lift.body.position.y);maximum=Mathf.Max(maximum,lift.body.position.y);
                if(Vector2.Distance(lift.body.position,lift.lowerStop)<.01f&&!bottom){bottom=true;
                    Place(lift.lowerStop+Vector2.up*1.52f);Check(!lift.Moving&&lift.Use(motor),"E starts return from lower stop");Place(new Vector2(-110,280));}
                if(bottom&&Vector2.Distance(lift.body.position,lift.upperStop)<.01f)top=true;
            }
            Check(bottom&&top&&minimum>=lift.lowerStop.y-.01f&&maximum<=lift.upperStop.y+.01f,"Lift travels both actual workbook stops without overshoot");
        }
    }
}
