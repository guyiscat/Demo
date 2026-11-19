using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Seaside;
using UnityEngine;
namespace Expedition
{
    public sealed class ThermalRuntimeChecks : MonoBehaviour
    {
        const float Dt=.01f;
        ThermalLevel level;ForestPlayer actor;SeasideMotor motor;SeasideSession session;
        readonly List<string> log=new List<string>();
        void Check(bool ok,string label){log.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(log[log.Count-1]);}
        static ForestInput Input(float horizontal=0,bool jump=false,bool held=false,bool dash=false,bool teleport=false,bool ride=false,Vector2 aim=default)
            =>new ForestInput{movement=new SeasideInput{horizontal=horizontal,jumpDown=jump,jumpHeld=held,dashDown=dash,aim=aim},teleportDown=teleport,rideHeld=ride};
        void Step(ForestInput input=default,int count=1)
        {
            for(int i=0;i<count;i++){if(level is Lighthouse.LighthouseLevel tower)tower.collapse.Advance(Dt);actor.Tick(Dt,input);if(!session.Paused)Physics2D.Simulate(Dt);}
        }
        void Place(Vector2 point){level.CloseDialogue();session.Respawn();motor.ResetAt(point);Physics2D.SyncTransforms();}
        void Mode(ProtectionMode mode){if(level.thermal.Mode!=mode)level.thermal.TrySwitchMode();}
        IEnumerator Start()
        {
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            level=GetComponent<ThermalLevel>();actor=level.player;motor=actor.motor;session=level.session;actor.manualInput=true;
            if(level is Lighthouse.LighthouseLevel towerLevel)towerLevel.collapse.manualSimulation=true;
            var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            GameObject fixture=null;
            try
            {
                SystemRuntimeChecks.Run(session,Check);
                Place(session.startPoint);Step(count:20);
                Check(motor.Grounded&&!session.Respawning,"Actual authored entry settles safely on neutral ground");
                Check(actor.environment==level.thermal&&level.thermal.Mode==ProtectionMode.Cold,"Independent entry grants core with cold protection");
                Check(level.thermal.TrySwitchMode()&&level.thermal.Mode==ProtectionMode.Heat,"S switches to heat protection");
                Check(level.thermal.TrySwitchMode()&&level.thermal.Mode==ProtectionMode.Cold,"S switches back without charge consumption");
                Check(actor.TeleportCharged&&motor.DashReady,"Protection mode does not consume dash or teleport");
                typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);
                Check(!level.thermal.TrySwitchMode()&&level.thermal.Mode==ProtectionMode.Cold,"Pause blocks protection switching");session.Resume();
                fixture=new GameObject("ThermalRuntimeFixtures");
                var floor=new GameObject("NeutralFloor");floor.transform.SetParent(fixture.transform);floor.layer=6;floor.transform.position=new Vector2(-100,39.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(50,1);
                var wall=new GameObject("NeutralWall");wall.transform.SetParent(fixture.transform);wall.layer=6;wall.transform.position=new Vector2(-80,55);wall.AddComponent<BoxCollider2D>().size=new Vector2(1,30);wall.AddComponent<SeasideCell>().kind=CellKind.Ground;
                Physics2D.SyncTransforms();
                Place(new Vector2(-100,41.04f));Step(count:15);float y=motor.Body.position.y;
                Step(Input(jump:true));float apex=y;
                for(int i=0;i<120;i++){Step();apex=Mathf.Max(apex,motor.Body.position.y);if(motor.Body.linearVelocity.y<=0)break;}
                Check(apex>y+3.7f&&apex<y+4.2f,"Shared tap jump remains four units");
                Place(new Vector2(-81.51f,55));Step(Input(horizontal:1));Check(motor.Attached,"Shared neutral wall suspension remains available");
                Step(Input(horizontal:-1,jump:true,held:true));Check(motor.WallKicks==1&&motor.Body.linearVelocity.y>30,"Shared wall kick remains available");
                Place(new Vector2(-100,60));Step(Input(dash:true,aim:Vector2.right));Check(motor.Dashing,"Shared dash starts in independent map");
                Step(Input(teleport:true,aim:Vector2.up));Check(actor.BeaconActive&&!motor.Dashing&&motor.Body.linearVelocity.sqrMagnitude<.001f,"Teleport interrupts dash with no residual burst");
                Step(Input(teleport:true));float hoverY=motor.Body.position.y;Step(count:10);
                Check(actor.PostTeleportHoverRemaining>0&&Mathf.Abs(motor.Body.position.y-hoverY)<.005f,"Configured teleport hover suspends character");
                Step(count:Mathf.CeilToInt(actor.PostTeleportHoverRemaining/Dt)+10);Check(motor.Body.position.y<hoverY-.02f,"Gravity resumes after hover expires");
                TestSurfaces();TestPoints();TestRails();
                foreach(var cp in FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderByDescending(c=>c.checkpointId))
                {
                    session.Checkpoint(cp);session.Respawn();Check(Vector2.Distance(motor.Body.position,cp.respawnAnchor.position)<.01f,"Latest-contact respawn uses saved safe anchor: "+cp.name);
                    Step(count:10);Check(motor.Grounded&&!session.Respawning,"Saved checkpoint is physically safe: "+cp.name);
                }
                var spike=FindObjectsByType<SeasideCell>().First(c=>c.kind==CellKind.Spike);
                int deaths=session.Deaths;Place(spike.transform.position);Step(count:3);Check(session.Deaths==deaths+1&&session.Respawning,"Actual source spike kills player");session.Respawn();
                if(level is Snow.SnowLevel snow)TestSnow(snow);else TestLighthouse((Lighthouse.LighthouseLevel)level);
                level.Restart();Step(count:10);
                Check(!session.Won&&motor.Grounded&&actor.TeleportCharged&&motor.DashReady,"Full restart restores safe entrance and all abilities");
                Check(level.thermal.Mode==ProtectionMode.Cold&&level.thermal.AirJumps==0,"Restart restores protection and clears consumed climb points");
            }
            catch(Exception e){Check(false,e.ToString());Debug.LogException(e);}
            finally
            {
                Physics2D.simulationMode=previous;if(fixture!=null)Destroy(fixture);
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-thermalReport");
                string path=i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"thermal-runtime-checks.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllLines(path,log);
            }
            bool failed=log.Any(s=>s.StartsWith("FAIL"));Debug.Log(failed?"THERMAL_RUNTIME_FAIL":"THERMAL_RUNTIME_PASS assertions="+log.Count);Application.Quit(failed?1:0);
        }
        void TestSurfaces()
        {
            var surfaces=FindObjectsByType<ThermalSurface>();
            foreach(var mode in new[]{ProtectionMode.Cold,ProtectionMode.Heat})
            {
                bool tested=false;int candidates=0,freeCandidates=0,contactRejected=0;
                foreach(var surface in surfaces.Where(s=>s.requiredProtection==mode))
                {
                    var b=surface.GetComponent<BoxCollider2D>().bounds;
                    // Unity expands proximity queries by its contact offset. Keep
                    // the body clear while remaining within the contact probe.
                    foreach(var p in new[]{new Vector2(b.center.x,b.max.y+1.04f),new Vector2(b.min.x-1.04f,b.center.y),new Vector2(b.max.x+1.04f,b.center.y)})
                    {
                        candidates++;
                        if(!actor.IsFree(p)||Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,1<<7).Any())continue;
                        freeCandidates++;
                        Place(new Vector2(-100,60));Mode(mode);motor.WarpForTraversal(p);Physics2D.SyncTransforms();int deaths=session.Deaths;
                        if(!level.thermal.ValidateContact()||session.Respawning){contactRejected++;Debug.Log("CONTACT_REJECT "+mode+" actual="+level.thermal.Mode+" frozen="+motor.frozen+" pos="+p+" hazard="+level.thermal.LastHazard);continue;}
                        Check(session.Deaths==deaths,"Matching mode safely touches actual "+mode+" surface "+surface.sourceAddress);
                        level.thermal.TrySwitchMode();Check(session.Deaths==deaths+1&&session.Respawning,"Switching to wrong protection at contact causes death: "+mode);
                        session.Respawn();tested=true;break;
                    }
                    if(tested)break;
                }
                Check(tested,"Authored "+mode+" terrain has a tested contact position; candidates="+candidates+" free="+freeCandidates+" rejected="+contactRejected);
            }
        }
        void TestPoints()
        {
            foreach(var point in level.thermal.points)
            {
                var p=(Vector2)point.transform.position;
                Place(new Vector2(-100,60));Mode(point.requiredProtection);
                Step(Input(dash:true,aim:Vector2.right));Step(Input(teleport:true,aim:Vector2.up));Step(Input(teleport:true));
                Check(!actor.TeleportCharged&&!motor.DashReady,"Climb setup consumes both abilities before refill test");
                actor.CancelSpecial();motor.WarpForTraversal(p);Physics2D.SyncTransforms();
                Check(level.thermal.NearestReadyPoint()==point,"Matching mode recognizes authored climb ring: "+point.sourceAddress);
                Step(Input(jump:true,held:true));Check(level.thermal.AirJumps==1&&motor.Body.linearVelocity.y>30,"SPACE performs aerial jump at actual point: "+point.sourceAddress);
                Check(actor.TeleportCharged&&motor.DashReady,"Authored climb jump restores dash and teleport as full design specifies");
                motor.WarpForTraversal(p);Step(Input(jump:true,held:true));Check(level.thermal.AirJumps==1&&motor.Body.linearVelocity.y<=0,"Same point cannot be farmed within one aerial sequence");
                Place(new Vector2(-100,60));Mode(point.requiredProtection==ProtectionMode.Cold?ProtectionMode.Heat:ProtectionMode.Cold);motor.WarpForTraversal(p);Step(Input(jump:true));
                Check(level.thermal.AirJumps==0,"Mismatched protection cannot use climb point");
            }
            Place(new Vector2(-100,60));Step(Input(jump:true));Check(level.thermal.AirJumps==0&&motor.Body.linearVelocity.y<0,"No free second jump outside every ring");
            var first=level.thermal.points[0];Place(new Vector2(-100,60));Mode(first.requiredProtection);
            motor.WarpForTraversal((Vector2)first.transform.position+Vector2.up*(level.settings.climbRadius+.15f));Step(Input(jump:true));
            Check(level.thermal.AirJumps==0,"Climb radius boundary rejects out-of-range jump");
        }
        void TestRails()
        {
            foreach(var rail in actor.ziplines)
            {
                var mode=rail.GetComponent<ThermalZipline>().requiredProtection;
                Vector2 target=Vector2.zero;bool free=false;
                for(int i=2;i<9;i++)
                {
                    var p=rail.Point(rail.Length*i/10f)-Vector2.up*1.12f;
                    if(!actor.IsFree(p)||Physics2D.OverlapBoxAll(p,new Vector2(1.98f,1.98f),0,1<<7).Any())continue;
                    target=p;free=true;break;
                }
                Check(free,"Authored thermal cable has clear catch location: "+rail.name);if(!free)continue;
                Place(new Vector2(-100,60));Mode(mode);motor.WarpForTraversal(target);Physics2D.SyncTransforms();Step(Input(ride:true));
                Check(actor.RidingRail==rail,"Matching protection W catches actual thermal cable: "+rail.name);
                float x=motor.Body.position.x;Step(Input(horizontal:1),count:3);Check(motor.Body.position.x>x,"D moves right on thermal cable");
                x=motor.Body.position.x;Step(Input(horizontal:-1),count:3);Check(motor.Body.position.x<x,"A reverses thermal cable");
                Step(Input(jump:true,held:true));Check(actor.RidingRail==null&&motor.Body.linearVelocity.y>20,"SPACE leaves thermal cable");
                Place(new Vector2(-100,60));Mode(mode);motor.WarpForTraversal(target);Step(Input(ride:true));
                int deaths=session.Deaths;level.thermal.TrySwitchMode();Check(session.Respawning&&session.Deaths==deaths+1,"Switching to wrong protection while riding causes death");session.Respawn();
                Place(new Vector2(-100,60));Mode(mode==ProtectionMode.Cold?ProtectionMode.Heat:ProtectionMode.Cold);motor.WarpForTraversal(target);Step(Input(ride:true));
                Check(session.Respawning&&actor.RidingRail==null,"Wrong protection cannot catch thermal cable safely");session.Respawn();
            }
        }
        void ApproachGoal(){Place(level.goal.transform.position);}
        void TestSnow(Snow.SnowLevel snow)
        {
            Place(new Vector2(-100,60));Check(!snow.BeginInteraction(),"Remote E cannot read fragment");
            ApproachGoal();Check(snow.BeginInteraction()&&motor.frozen,"Fragment E starts dialogue and freezes movement");
            Check(snow.Dialogue.Contains("策划案"),"Fragment first page matches supplied PDF");snow.AdvanceDialogue();
            Check(snow.Dialogue.Contains("设定描述"),"Fragment second page matches supplied PDF");snow.AdvanceDialogue();
            Check(snow.Choosing&&!session.Won,"Both ending choices await player selection");
            Check(!level.thermal.TrySwitchMode(),"Dialogue blocks mode switching");snow.Choose(0);
            Check(session.Won&&snow.Outcome==Snow.SnowOutcome.Stayed&&snow.CompletionTitle.Contains("END"),"Stay choice completes ending with explicit ending title");
            snow.Restart();ApproachGoal();snow.BeginInteraction();snow.AdvanceDialogue();snow.AdvanceDialogue();snow.Choose(1);
            Check(!session.Won&&snow.Outcome==Snow.SnowOutcome.CarriedFragment&&snow.Dialogue.Contains("也许还有用"),"Continue choice preserves fragment and original last line");snow.AdvanceDialogue();
            Check(session.Won&&snow.CompletionTitle.Contains("CONTINUE"),"Continue completes independent snow exploration without ending world");
            snow.Restart();Check(snow.Outcome==Snow.SnowOutcome.Exploring&&!snow.Choosing&&snow.Dialogue==null,"Restart clears both ending branches");
        }
        void TestLighthouse(Lighthouse.LighthouseLevel lighthouse)
        {
            TestCollapse(lighthouse);
            Place(session.startPoint);Check(!lighthouse.BeginInteraction(),"Bottom spawn cannot remotely activate tower-top lamp");
            ApproachGoal();Check(lighthouse.BeginInteraction()&&motor.frozen,"Tower-top E starts lamp interaction");
            lighthouse.AdvanceDialogue();Check(!session.Won&&!lighthouse.Lit,"Lamp waits for completed interaction");lighthouse.AdvanceDialogue();
            Check(session.Won&&lighthouse.Lit&&lighthouse.lamp.color.r>.9f,"Finishing interaction lights lamp and completes tower");
            lighthouse.Restart();Check(!lighthouse.Lit&&!session.Won,"Restart resets tower-top lamp");
        }
        void TestCollapse(Lighthouse.LighthouseLevel tower)
        {
            tower.Restart();var collapse=tower.collapse;collapse.Advance(10);
            Check(!collapse.Started&&collapse.Elapsed==0,"Tower entrance waits for player departure before countdown");
            motor.WarpForTraversal(session.startPoint+Vector2.up*140);collapse.Advance(collapse.graceSeconds+12);
            Check(collapse.Started&&collapse.Height>session.startPoint.y&&collapse.CollapsedCount>0,"Timed collapse physically removes lower authored roads behind player");
            float y=collapse.Height,time=collapse.Elapsed;typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);collapse.Advance(20);
            Check(collapse.Height==y&&collapse.Elapsed==time,"Pause stops timed chase and road collapse");session.Resume();
            session.ShowHelp=true;collapse.Advance(10);Check(collapse.Elapsed==time,"Reading help stops collapse countdown");session.ShowHelp=false;
            motor.WarpForTraversal(new Vector2(session.startPoint.x,collapse.Height+.5f));int deaths=session.Deaths;collapse.Advance(Dt);
            Check(session.Respawning&&session.Deaths==deaths+1,"Collapse frontier reaching player causes death");session.Respawn();
            Check(collapse.CollapsedCount==0&&!collapse.Started&&collapse.Elapsed==0,"Respawn restores collapsed roads and segment timer");
            motor.WarpForTraversal(session.startPoint+Vector2.up*350);collapse.Advance(collapse.segmentTimeLimit+.01f);
            Check(session.Respawning,"Segment time limit forces safe restart even above chase frontier");session.Respawn();
            var cp=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).Last();
            session.Checkpoint(cp);collapse.Advance(Dt);
            Check(collapse.Height==cp.respawnAnchor.position.y-collapse.initialOffset&&!collapse.Started,"New rest point anchors a fresh recoverable chase segment");
            tower.Restart();
        }
    }
}
