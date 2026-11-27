using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Seaside;

namespace Forest
{
    // Runs only with -forestChecks in the independent Forest build. It drives
    // real Physics2D, input state and level triggers, not a second mechanic model.
    public sealed class ForestRuntimeChecks : MonoBehaviour
    {
        const float Dt=.01f;
        ForestLevel level;ForestPlayer actor;SeasideMotor motor;SeasideSession session;
        readonly List<string> log=new List<string>();
        GameObject fixture;SimulationMode2D oldMode;
        string Output
        {
            get
            {
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-forestReport");
                return i>=0&&i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"forest-runtime-checks.txt");
            }
        }
        void Check(bool ok,string label){log.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(log[log.Count-1]);}
        void Step(ForestInput input=default,int count=1)
        {
            for(int i=0;i<count;i++){actor.Tick(Dt,input);if(!session.Paused)Physics2D.Simulate(Dt);}
        }
        void Place(Vector2 position)
        {
            motor.ResetAt(position);Physics2D.SyncTransforms();
        }
        BoxCollider2D Box(string name,Vector2 point,Vector2 size,bool gray=false)
        {
            var go=new GameObject(name);go.transform.SetParent(fixture.transform);go.transform.position=point;go.layer=6;
            var box=go.AddComponent<BoxCollider2D>();box.size=size;
            var cell=go.AddComponent<SeasideCell>();cell.kind=CellKind.Ground;cell.attachable=!gray;return box;
        }
        ForestInput Input(bool right=false,bool jump=false,bool held=false,bool dash=false,bool teleport=false,bool ride=false,Vector2 aim=default)
        {
            return new ForestInput{movement=new SeasideInput{horizontal=right?1:0,jumpDown=jump,jumpHeld=held,dashDown=dash,aim=aim},teleportDown=teleport,rideHeld=ride};
        }
        IEnumerator Start()
        {
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            level=GetComponent<ForestLevel>();actor=level.player;motor=actor.motor;session=level.session;
            actor.manualInput=true;level.wheel.enabled=false;oldMode=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            fixture=new GameObject("RuntimeCheckFixtures");
            try
            {
                Check(actor.TeleportCharged,"Forest starts with independent teleport charge");
                SystemRuntimeChecks.Run(session,Check);
                session.Respawn();
                Check(Vector2.Distance(motor.Body.position,session.startPoint)<.02f,"Configured original spawn remains available regardless of editor preview player position");
                Box("FixtureFloor",new Vector2(-100,19.5f),new Vector2(100,1));
                var wall=Box("FixtureWall",new Vector2(-75,30),new Vector2(1,20));
                Physics2D.SyncTransforms();
                Place(new Vector2(-110,21.04f));Step(count:20);
                Check(motor.Grounded,"Forest ordinary locomotion uses original ground probe");
                float start=motor.Body.position.y,apex=start;Step(Input(jump:true,held:true));
                for(int i=0;i<180;i++){Step();apex=Mathf.Max(apex,motor.Body.position.y);if(motor.Body.linearVelocity.y<=0)break;}
                Check(Mathf.Abs(apex-start-4)<.1f,"Forest tap jump remains 4 cells: "+(apex-start));
                Place(new Vector2(-110,21.04f));Step(count:20);start=motor.Body.position.y;apex=start;
                Step(Input(jump:true,held:true));
                for(int i=0;i<180;i++){Step(Input(held:true));apex=Mathf.Max(apex,motor.Body.position.y);if(motor.Body.linearVelocity.y<=0)break;}
                Check(Mathf.Abs(apex-start-8)<.1f,"Forest hold jump remains 8 cells: "+(apex-start));
                Place(new Vector2(-110,35));start=motor.Body.position.x;Step(Input(dash:true,aim:Vector2.right));
                for(int i=0;i<40&&motor.Dashing;i++)Step();
                Check(Mathf.Abs(motor.Body.position.x-start-8)<.03f,"Original dash remains 8 cells");
                Check(actor.TeleportCharged,"Dash does not consume teleport charge");
                TestTeleportHover();
                TestDashInterrupt();
                Place(new Vector2(-110,21.04f));Step(count:20);Step(Input(teleport:true,aim:Vector2.up));Step(count:10);
                Check(motor.Grounded&&actor.BeaconActive&&!actor.TeleportCharged,"Remaining on the same ground cannot refill a launched teleport every physics tick");
                Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.right));
                Check(actor.BeaconActive&&!actor.TeleportCharged,"First right click emits small fast beacon and spends charge");
                Check(actor.settings.beaconSpeed>motor.settings.dashSpeed&&actor.settings.beaconRadius<.2f,"Beacon is smaller than player and faster than dash");
                Check(motor.DashReady,"Teleport launch does not consume dash");
                Vector2 mid=actor.BeaconPosition;Step(Input(teleport:true));
                Check(!actor.BeaconActive&&Vector2.Distance(motor.Body.position,mid)<.03f&&actor.LastTeleportResult=="MANUAL","Second right click teleports to current beacon");
                float landingY=motor.Body.position.y;Step(count:5);
                CheckLandingHover(landingY,"Manual");
                Step(Input(teleport:true,aim:Vector2.right));Check(!actor.BeaconActive,"No second teleport before renewed terrain contact");
                Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.right));
                for(int i=0;i<60&&actor.BeaconActive;i++)Step();
                Check(actor.LastTeleportResult=="RANGE"&&Mathf.Abs(actor.BeaconDistance-10)<.001f,"Range expiry automatically teleports at 10-cell ruler distance");
                Check(Mathf.Abs(motor.Body.position.x+100)<.03f&&!actor.TeleportCharged,"Maximum-distance landing and charge remain correct");
                landingY=motor.Body.position.y;Step(count:5);
                CheckLandingHover(landingY,"Range");
                Place(new Vector2(-80,30));Step(Input(teleport:true,aim:Vector2.right));
                for(int i=0;i<60&&actor.BeaconActive;i++)Step();
                Check(actor.LastTeleportResult=="TERRAIN"&&actor.IsFree(motor.Body.position),"Terrain impact resolves full-body safe landing");
                Check(motor.Body.position.x<=-75.49f-motor.BodySize.x*.5f,"Player never embeds in hit wall");
                landingY=motor.Body.position.y;Step(count:5);
                CheckLandingHover(landingY,"Terrain");
                // A beacon fits this one-cell passage, while the 2x2 body cannot.
                Box("NarrowCeiling",new Vector2(-94,39),new Vector2(12,1));
                Box("NarrowFloor",new Vector2(-94,37),new Vector2(12,1));Physics2D.SyncTransforms();
                Place(new Vector2(-103,38));Step(Input(teleport:true,aim:Vector2.right));
                for(int i=0;i<60&&actor.BeaconActive;i++)Step();
                Check(actor.IsFree(motor.Body.position)&&motor.Body.position.x< -99.99f-motor.BodySize.x*.5f,"Narrow-beacon passage cannot embed larger player; travelled samples fall back safely");
                Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.up));
                Vector2 pausedBeacon=actor.BeaconPosition,pausedBody=motor.Body.position;
                typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Time.timeScale=0;
                Step(Input(teleport:true),count:5);
                Check(actor.BeaconActive&&actor.BeaconPosition==pausedBeacon&&motor.Body.position==pausedBody,"Pause blocks beacon progress and second-click teleport");
                session.Resume();Step();Check(actor.BeaconPosition!=pausedBeacon,"Resume continues existing beacon flight");
                Check(Mathf.Abs(motor.Body.position.y-pausedBody.y)<.005f&&motor.Body.linearVelocity.y==0,"Pause/resume preserves beacon hover altitude");
                Place(new Vector2(-110,30));Step(Input(teleport:true,aim:Vector2.up));actor.CancelSpecial();
                motor.WarpForTraversal(new Vector2(-75.51f-motor.BodySize.x*.5f,30));Physics2D.SyncTransforms();Step(Input(right:true));
                Check(motor.Attached&&actor.TeleportCharged,"New black-wall contact restores teleport and original wall hover");
                wall.GetComponent<SeasideCell>().attachable=false;Place(new Vector2(-75.51f-motor.BodySize.x*.5f,30));Step(Input(right:true));
                Check(!motor.Attached,"Gray wall forbids attachment without changing Seaside wall behaviour");
                wall.GetComponent<SeasideCell>().attachable=true;
                var testRail=new GameObject("SlopedFixtureRail").AddComponent<ForestZipline>();testRail.transform.SetParent(fixture.transform);
                testRail.start=new GameObject("Start").transform;testRail.start.SetParent(testRail.transform);testRail.start.position=new Vector2(-130,40);
                testRail.end=new GameObject("End").transform;testRail.end.SetParent(testRail.transform);testRail.end.position=new Vector2(-120,45);
                var savedRails=actor.ziplines;actor.ziplines=new[]{testRail};
                Check(testRail.CanCatch(testRail.Point(5)+Vector2.up*.95f,motor.BodySize,.25f),"Full body may catch cable from above, without old center-height rejection");
                Check(testRail.CanCatch(testRail.StartPoint+Vector2.left*(motor.BodySize.x*.5f+.20f),motor.BodySize,.25f),"Body edge plus small padding reaches cable endpoint");
                Check(!testRail.CanCatch(testRail.StartPoint+Vector2.left*1.8f,motor.BodySize,.25f),"Expanded catch does not reach a distant cable");
                Place(testRail.Point(5)+Vector2.up*.85f);Step(Input(ride:true));
                Check(actor.RidingRail==testRail,"W grabs when feet/body overlap the cable from above");
                Place(testRail.StartPoint+Vector2.left*(motor.BodySize.x*.5f+.15f));Step(Input(ride:true));
                Check(actor.RidingRail==testRail,"W grabs when body side touches a cable endpoint");
                Place(new Vector2(-127,40.38f));Step(Input(dash:true,aim:Vector2.up));motor.BeginTraversal();
                Step(Input(teleport:true,aim:Vector2.up));actor.CancelSpecial();
                motor.Body.position=new Vector2(-127,40.38f);Physics2D.SyncTransforms();
                Step(Input(right:true,ride:true));
                Check(actor.RidingRail==testRail,"W catches an actually contacted cable");
                Check(motor.DashReady&&actor.TeleportCharged,"Zipline resets both dash and teleport");
                float hoverRailY=motor.Body.position.y;
                Step(Input(teleport:true,aim:Vector2.up));Step(Input(ride:true),count:8);
                Check(actor.BeaconActive&&actor.RidingRail==null&&Mathf.Abs(motor.Body.position.y-hoverRailY)<.005f,"Beacon launch detaches rail and W cannot override hover");
                Check(!actor.TeleportCharged,"Hover near former rail does not repeatedly refill teleport");
                Place(new Vector2(-127,40.38f));Step(Input(right:true,ride:true));
                float railY=motor.Body.position.y;Step(count:20);
                Check(actor.RidingRail==testRail&&motor.Body.position.y>railY+.8f,"Zipline moves uphill in facing direction without gravity");
                float speed=actor.settings.ziplineSpeed;actor.settings.ziplineSpeed=8;
                Vector2 speedFrom=motor.Body.position;Step(count:10);
                Check(Mathf.Abs(Vector2.Distance(speedFrom,motor.Body.position)-.8f)<.03f,"Editable ziplineSpeed controls real cable travel in units per second");actor.settings.ziplineSpeed=speed;
                float railX=motor.Body.position.x;Step(Input(jump:true,held:true));
                Check(actor.RidingRail==null&&motor.Body.linearVelocity.y>20,"Space detaches rail with original full jump launch");
                Step(Input(ride:true));Check(actor.RidingRail==null,"Jump detach cooldown prevents immediate W recatch");
                TestRailControlAndGrace(testRail);
                Place(new Vector2(-121,43.38f));
                Step(new ForestInput{rideHeld=true,movement=new SeasideInput{horizontal=-1}});
                railX=motor.Body.position.x;railY=motor.Body.position.y;Step(count:20);
                Check(actor.RidingRail==testRail&&motor.Body.position.x<railX-1&&motor.Body.position.y<railY-.5f,"Facing left reverses sloped rail travel and allows downhill riding");
                Place(testRail.EndPoint-Vector2.up*1.12f-testRail.Tangent*.1f);
                Step(Input(right:true,ride:true));Step(count:10);
                Check(actor.RidingRail==null,"Cable endpoint releases the character rather than trapping it pos="+motor.Body.position+" rail="+(actor.RidingRail!=null));
                actor.ziplines=savedRails;
                Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.right));session.Respawn();
                Check(!actor.BeaconActive&&actor.RidingRail==null&&actor.RidingWheel==null&&actor.TeleportCharged,"Respawn clears all Forest transient traversal state");
                Check(!actor.beacon.enabled&&!actor.beaconTrail.enabled,"Respawn removes beacon and trail");
                var danger=FindObjectsByType<SeasideCell>().First(c=>c.kind==CellKind.Spike);
                int deaths=session.Deaths;Place(danger.transform.position);Physics2D.Simulate(Dt);
                Check(session.Respawning&&session.Deaths==deaths+1,"Original red-cell hazard kills Forest player through the existing trigger");
                session.Respawn();
                TestWaterfall();
                fixture.SetActive(false);Physics2D.SyncTransforms();
                TestSavedRails();
                TestWheelAndGoal();
                TestCheckpoints();
                TestPostTeleportWindow();
                TestIntroAndArrow();
            }
            catch(Exception error){log.Add("FAIL "+error);Debug.LogException(error);}
            finally
            {
                Physics2D.simulationMode=oldMode;
                if(fixture!=null)Destroy(fixture);
                string path=Path.GetFullPath(Output);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,log);
            }
            bool failed=log.Any(s=>s.StartsWith("FAIL"));Debug.Log(failed?"FOREST_RUNTIME_FAIL":"FOREST_RUNTIME_PASS assertions="+log.Count);
#if UNITY_EDITOR
            // A batch Play Mode run uses the same physical scene checks when
            // standalone execution is unavailable; never claim it as a player run.
            if(Application.isBatchMode){UnityEditor.EditorApplication.Exit(failed?1:0);yield break;}
#endif
            Application.Quit(failed?1:0);
        }
        void TestTeleportHover()
        {
            foreach(float vertical in new[]{20f,-20f})
            {
                Place(new Vector2(-110,35));motor.Body.linearVelocity=new Vector2(3,vertical);
                float y=motor.Body.position.y,x=motor.Body.position.x;
                Step(Input(teleport:true,aim:Vector2.up));
                Check(actor.BeaconActive&&Mathf.Abs(motor.Body.position.y-y)<.005f&&motor.Body.linearVelocity.y==0,"Beacon immediately arrests vertical velocity "+vertical);
                Check(motor.Body.linearVelocity.x==0,"Launch locks horizontal momentum "+vertical);
                Step(Input(right:true,jump:true,held:true,dash:true,ride:true,aim:Vector2.up),count:12);
                Check(actor.BeaconActive&&Mathf.Abs(motor.Body.position.y-y)<.005f,"Hover holds altitude throughout real physics steps "+vertical);
                Check(Mathf.Abs(motor.Body.position.x-x)<.005f,"A/D cannot move during hover "+vertical);
                Check(!motor.Dashing&&motor.DashUses==0&&motor.DashReady,"Hover inputs do not spend dash or queue vertical motion "+vertical);
                actor.CancelSpecial();y=motor.Body.position.y;Step(count:5);
                Check(!actor.BeaconActive&&motor.Body.position.y<y-.02f,"Explicit cancellation restores falling "+vertical);
            }
            Place(new Vector2(-110,35));Step(Input(dash:true,aim:Vector2.up));
            Step(Input(teleport:true,aim:Vector2.right));float altitude=motor.Body.position.y;Step(count:8);
            Check(actor.BeaconActive&&!motor.Dashing&&Mathf.Abs(motor.Body.position.y-altitude)<.005f,"Beacon interrupts an ongoing vertical dash without consuming another dash");
            Place(new Vector2(-76.52f,30));Step(Input(teleport:true,aim:Vector2.up));Step(Input(right:true),count:12);
            Check(actor.BeaconActive&&actor.IsFree(motor.Body.position)&&motor.Body.position.x<=-75.49f-motor.BodySize.x*.5f,"Hover horizontal movement retains solid wall collision");
            Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.up));session.Die();Step();
            Check(!actor.BeaconActive&&session.Respawning&&!actor.beacon.enabled&&!actor.beaconTrail.enabled,"Death cancels hover and beacon visuals");
            session.Respawn();Place(new Vector2(-110,35));float yAfterRespawn=motor.Body.position.y;Step(count:5);
            Check(!actor.BeaconActive&&actor.TeleportCharged&&motor.Body.position.y<yAfterRespawn-.02f,"Respawn restores charge without leaving permanent hover");
            Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.up));
            actor.enabled=false;
            Check(!actor.BeaconActive&&!actor.beacon.enabled,"Disabling Forest driver clears hover state");
            actor.enabled=true;altitude=motor.Body.position.y;Step(count:5);
            Check(motor.Body.position.y<altitude-.02f,"Re-enabled Forest driver resumes normal falling");
        }
        void TestDashInterrupt()
        {
            foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down,new Vector2(1,1).normalized})
            foreach(int elapsed in new[]{0,8,25})
            {
                Place(new Vector2(-110,35));Step(Input(dash:true,aim:direction));Step(count:elapsed);
                Check(motor.Dashing,"Dash is active before interrupt: "+direction+" frame="+elapsed);
                Vector2 at=motor.Body.position;
                Step(Input(teleport:true,aim:Vector2.up));
                Check(actor.BeaconActive&&!motor.Dashing&&motor.Body.linearVelocity.sqrMagnitude<.0001f,
                    "Charged teleport cancels burst and residual velocity: "+direction+" frame="+elapsed);
                Check(Vector2.Distance(at,motor.Body.position)<.005f&&motor.DashUses==1&&!motor.DashReady&&!actor.TeleportCharged,
                    "Interrupt spends only teleport charge and preserves spent dash");
                Step(Input(teleport:true));
                Check(!actor.BeaconActive&&actor.PostTeleportHoverRemaining>0&&!motor.Dashing&&actor.IsFree(motor.Body.position),
                    "Manual commit after interrupted dash lands safely in hover");
            }
            bool enabled=actor.settings.teleportInterruptsDash;
            try
            {
                actor.settings.teleportInterruptsDash=false;
                Place(new Vector2(-110,35));Step(Input(dash:true,aim:Vector2.right));
                Step(Input(teleport:true,aim:Vector2.up));
                Check(motor.Dashing&&!actor.BeaconActive&&actor.TeleportCharged,"Optional disabled interrupt preserves dash and teleport charge");
            }
            finally{actor.settings.teleportInterruptsDash=enabled;}
        }
        void CheckLandingHover(float y,string reason)
        {
            if(actor.settings.teleportHoverDuration<=0)
            {
                Check(actor.PostTeleportHoverRemaining==0&&motor.Body.linearVelocity.y<0,reason+" zero duration skips landing hover");return;
            }
            Check(actor.PostTeleportHoverRemaining>0&&Mathf.Abs(motor.Body.position.y-y)<.005f,reason+" completion starts configured post-teleport hover");
            Step(count:Mathf.Max(0,Mathf.FloorToInt(actor.PostTeleportHoverRemaining*.65f/Dt)));
            Check(actor.PostTeleportHoverRemaining>0&&Mathf.Abs(motor.Body.position.y-y)<.005f,reason+" landing remains suspended before configured expiry");
            Step(count:Mathf.CeilToInt(actor.PostTeleportHoverRemaining/Dt)+8);
            Check(actor.PostTeleportHoverRemaining==0&&motor.Body.position.y<y-.02f,reason+" landing resumes falling after configured expiry");
        }
        void TestRailControlAndGrace(ForestZipline rail)
        {
            Place(rail.Point(5)-Vector2.up*1.12f);Step(Input(right:true,ride:true));
            float x=motor.Body.position.x;
            Step(new ForestInput{movement=new SeasideInput{horizontal=-1}},10);
            Check(actor.RidingRail==rail&&motor.Body.position.x<x-.5f,"A reverses an existing ride immediately");
            x=motor.Body.position.x;Step(Input(right:true),10);
            Check(actor.RidingRail==rail&&motor.Body.position.x>x+.5f,"D reverses the same ride again");
            Check(rail.CanCatch(rail.StartPoint+Vector2.left*1.5f,motor.BodySize,actor.settings.ziplineContactPadding,actor.settings.ziplineCatchDistance),"Configured enlarged catch includes previously missed endpoint margin");
            Place(rail.StartPoint+Vector2.left*1.5f);Step(Input(ride:true));
            Check(actor.RidingRail==rail,"Real W catch succeeds in the enlarged endpoint margin");
            foreach(bool rightEnd in new[]{true,false})
            {
                Place((rightEnd?rail.EndPoint-rail.Tangent*.05f:rail.StartPoint+rail.Tangent*.05f)-Vector2.up*1.12f);
                Step(new ForestInput{rideHeld=true,movement=new SeasideInput{horizontal=rightEnd?1:-1}});
                for(int i=0;i<20&&actor.RidingRail!=null;i++)Step();
                Check(actor.RidingRail==null&&actor.RailCoyoteRemaining>0,"Endpoint arms coyote jump at "+(rightEnd?"right":"left"));
                Step(count:5);Step(Input(jump:true,held:true));
                Check(motor.Body.linearVelocity.y>20&&actor.RailCoyoteRemaining==0,"Delayed endpoint jump consumes grace at "+rightEnd);
                Step();float before=motor.Body.linearVelocity.y;Step(Input(jump:true,held:true));
                Check(motor.Body.linearVelocity.y<before,"Grace cannot grant a second airborne jump at "+rightEnd);
            }
            Place(rail.EndPoint-rail.Tangent*.05f-Vector2.up*1.12f);Step(Input(right:true,ride:true));
            for(int i=0;i<20&&actor.RidingRail!=null;i++)Step();
            Step(count:Mathf.CeilToInt(actor.settings.ziplineCoyoteTime/Dt)+3);
            float velocity=motor.Body.linearVelocity.y;Step(Input(jump:true,held:true));
            Check(actor.RailCoyoteRemaining==0&&motor.Body.linearVelocity.y<velocity,"Expired coyote window cannot jump");
        }
        void FinishTeleport()
        {
            Step(Input(teleport:true,aim:Vector2.right));Step(Input(teleport:true));
        }
        void TestPostTeleportWindow()
        {
            fixture.SetActive(true);Physics2D.SyncTransforms();
            float duration=actor.settings.teleportHoverDuration;
            actor.settings.teleportHoverDuration=.12f;
            Place(new Vector2(-110,35));FinishTeleport();float y=motor.Body.position.y;
            Step(Input(right:true),5);
            Check(Mathf.Abs(motor.Body.position.y-y)<.005f&&motor.Body.linearVelocity.x==0,"Post-teleport hover rejects A/D motion");
            float remaining=actor.PostTeleportHoverRemaining;
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Time.timeScale=0;Step(count:20);
            Check(actor.PostTeleportHoverRemaining==remaining,"Pause preserves remaining post-teleport duration");
            session.Resume();Step(count:12);
            Check(actor.PostTeleportHoverRemaining==0&&motor.Body.position.y<y-.005f,"Editable short hover duration controls actual expiry");
            Place(new Vector2(-110,35));FinishTeleport();Step(Input(dash:true,aim:Vector2.up));
            Check(actor.PostTeleportHoverRemaining==0&&motor.Dashing&&motor.Body.linearVelocity.y>0,"Dash cancels post-teleport hover immediately");
            Place(new Vector2(-110,35));FinishTeleport();actor.enabled=false;actor.enabled=true;Step();
            Check(actor.PostTeleportHoverRemaining==0&&motor.Body.linearVelocity.y<0,"Disable clears post-teleport hover");
            Place(new Vector2(-110,35));FinishTeleport();session.Die();Step();session.Respawn();
            Check(actor.PostTeleportHoverRemaining==0&&actor.RailCoyoteRemaining==0,"Death and respawn clear both grace windows");
            actor.settings.teleportHoverDuration=0;Place(new Vector2(-110,35));FinishTeleport();Step();
            Check(actor.PostTeleportHoverRemaining==0&&motor.Body.linearVelocity.y<0,"Zero duration disables post-teleport hover");
            actor.settings.teleportHoverDuration=duration;
        }
        void IntroStep(float dt)
        {
            CameraStep(dt);level.Intro.Tick(dt);
        }
        void TestIntroAndArrow()
        {
            var intro=level.Intro;var camera=level.cameraRig.GetComponent<Camera>();
            Place(new Vector2(-110,21.04f));Step(count:20);level.cameraRig.overview=false;level.cameraRig.Snap();intro.Cancel(true);
            for(int i=0;i<10;i++)IntroStep(.1f);
            Check(intro.Phase==0,"Standing still does not trigger opening shot");
            session.Die();session.Respawn();
            Check(intro.Phase==0,"Death before first walking trigger keeps opening shot available");
            Place(new Vector2(-110,21.04f));Step(count:20);intro.Cancel(true);
            for(int i=0;i<100&&intro.Phase==0;i++){Step(Input(right:true));IntroStep(Dt);}
            Check(intro.IsPlaying&&actor.waterfall.FullMapReveal,"Walking a few units triggers the opening full-map reveal");
            float size=camera.orthographicSize;IntroStep(.1f);
            Check(camera.orthographicSize>size,"Opening zoom progresses gradually");
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Time.timeScale=0;
            size=camera.orthographicSize;IntroStep(.5f);
            Check(camera.orthographicSize==size,"Pause freezes opening camera progression");session.Resume();
            for(int i=0;i<300&&intro.Phase==1;i++)IntroStep(Dt);
            Check(intro.Phase==2&&!actor.waterfall.occlusion.enabled,"Full-map hold reveals both sides without waterfall mask");
            intro.GetFullMapPose(camera,out var fullPosition,out var fullSize);
            Check(Vector3.Distance(camera.transform.position,fullPosition)<.01f&&Mathf.Abs(camera.orthographicSize-fullSize)<.01f,"Full-map shot reaches its complete framing");
            var low=camera.WorldToViewportPoint(Vector3.zero);var high=camera.WorldToViewportPoint((Vector3)level.cameraRig.mapSize);
            Check(low.x>=0&&low.y>=.08f&&high.x<=1&&high.y<=.91f,"Entire map fits between HUD margins");
            var wheelLow=camera.WorldToViewportPoint((Vector3)(level.wheel.center-level.wheel.radius));
            var wheelHigh=camera.WorldToViewportPoint((Vector3)(level.wheel.center+level.wheel.radius));
            Check(wheelLow.x>=0&&wheelLow.y>=.08f&&wheelHigh.x<=1&&wheelHigh.y<=.91f,"Entire waterwheel fits in opening shot");
            int holdSteps=Mathf.Max(0,Mathf.FloorToInt((actor.settings.introHoldDuration-.1f)/Dt));
            for(int i=0;i<holdSteps;i++)IntroStep(Dt);
            Check(intro.Phase==2,"Full-map hold remains until the configured duration: "+actor.settings.introHoldDuration);
            for(int i=0;i<20&&intro.Phase==2;i++)IntroStep(Dt);
            Check(intro.Phase==3,"Full-map hold returns after the configured duration");
            for(int i=0;i<300&&!intro.Completed;i++)IntroStep(Dt);
            Check(intro.Completed&&!actor.waterfall.FullMapReveal&&camera.orthographicSize<fullSize*.5f,"Opening shot restores normal follow view and waterfall rules");
            for(int i=0;i<100;i++){Step(Input(right:true));IntroStep(Dt);}
            Check(intro.Completed,"Opening shot plays only once during ordinary walking");
            intro.Cancel(true);Check(intro.Phase==0,"Full restart can rearm the opening shot");intro.Cancel();
            float savedTrigger=actor.settings.introWalkDistance,savedSpeed=actor.settings.introZoomSpeed;
            actor.settings.introWalkDistance=0;actor.settings.introZoomSpeed=.25f;
            intro.Cancel(true);level.cameraRig.Snap();actor.waterfall.ResetSide(motor.Body.position);IntroStep(.1f);
            float slowSize=camera.orthographicSize;intro.Cancel();
            actor.settings.introZoomSpeed=1;
            intro.Cancel(true);level.cameraRig.Snap();actor.waterfall.ResetSide(motor.Body.position);IntroStep(.1f);
            Check(camera.orthographicSize>slowSize+.1f,"Exposed zoom speed changes actual camera progression");
            session.Respawn();Check(!intro.IsPlaying&&!actor.waterfall.FullMapReveal,"Respawn during reveal cancels camera and releases full-map mask override");
            level.Restart();Check(intro.Phase==0,"Real session restart rearms opening camera");intro.Cancel();
            actor.settings.introWalkDistance=savedTrigger;actor.settings.introZoomSpeed=savedSpeed;
            foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,new Vector2(1,-1).normalized})
            {
                Step(Input(aim:direction));actor.AimIndicator.Refresh();
                var arrow=actor.AimIndicator.Arrow;var root=(Vector2)arrow.GetPosition(0);var tip=(Vector2)arrow.GetPosition(1);
                Check(Vector2.Dot((tip-root).normalized,direction)>.999f,"Body arrow matches action direction "+direction);
                var local=root-(Vector2)actor.transform.position;var half=motor.BodySize*.5f;
                Check(Mathf.Abs(local.x)>half.x||Mathf.Abs(local.y)>half.y,"Body arrow stays outside the authored collider "+direction);
            }
            Place(new Vector2(-110,35));Step(Input(teleport:true,aim:Vector2.up));actor.AimIndicator.Refresh();
            Check(Vector2.Dot((actor.BeaconPosition-motor.Body.position).normalized,actor.AimIndicator.DisplayedDirection)>.999f,"Beacon launches along the displayed arrow");
            Place(new Vector2(-110,35));Step(Input(dash:true,aim:Vector2.up));actor.AimIndicator.Refresh();
            Check(Vector2.Dot(motor.Body.linearVelocity.normalized,actor.AimIndicator.DisplayedDirection)>.999f,"Dash travels along the displayed arrow");
        }
        void CameraStep(float dt)
        {
            level.cameraRig.UpdateView(dt);actor.waterfall.Refresh(dt);
        }
        void SettleCamera()
        {
            for(int i=0;i<2400&&actor.waterfall.IsTransitioning;i++)CameraStep(Dt);
            Check(!actor.waterfall.IsTransitioning,"Waterfall transition eventually arrives");
        }
        void TestWaterfall()
        {
            var water=actor.waterfall;var b=water.curtainBounds.bounds;var rig=level.cameraRig;
            var camera=rig.GetComponent<Camera>();rig.overview=false;
            Place(new Vector2(b.min.x-2,40));rig.Snap();water.ResetSide(motor.Body.position);
            Check(water.VisibleSide==-1&&!water.IsTransitioning,"Near left approach starts settled");
            Check(!water.occlusion.enabled,"Near mode uses camera bounds without black mask");
            Check(camera.ViewportToWorldPoint(new Vector3(1,0,10)).x<=b.max.x+.01f,"Left near view ends at waterfall");
            motor.Body.position=new Vector2(b.max.x+.5f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(water.VisibleSide==-1&&!water.IsTransitioning,"Partial body crossing keeps original region");
            Vector3 previous=camera.transform.position;
            motor.Body.position=new Vector2(b.max.x+1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(water.VisibleSide==1&&water.IsTransitioning,"Full crossing starts smooth transition instead of snapping");
            Check(Vector3.Distance(previous,camera.transform.position)<=water.cameraTransitionSpeed*Dt+.001f,"First camera step is limited by exposed speed");
            Check(!water.occlusion.enabled,"Both sides are unmasked during camera motion");
            previous=camera.transform.position;water.Refresh(0);
            Check(camera.transform.position==previous&&water.IsTransitioning,"Paused/zero-time refresh preserves camera pan instead of finishing instantly");
            float savedSpeed=water.cameraTransitionSpeed;water.cameraTransitionSpeed=10;
            previous=camera.transform.position;CameraStep(Dt);
            Check(Vector3.Distance(previous,camera.transform.position)<=.101f,"Changing exposed pan speed immediately controls actual displacement");water.cameraTransitionSpeed=savedSpeed;
            SettleCamera();
            Check(camera.ViewportToWorldPoint(new Vector3(0,0,10)).x>=b.min.x-.02f,"Right near view obeys waterfall boundary after arrival");
            previous=camera.transform.position;
            motor.Body.position=new Vector2(b.min.x-1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(water.VisibleSide==-1&&water.IsTransitioning&&Vector3.Distance(previous,camera.transform.position)<=water.cameraTransitionSpeed*Dt+.001f,"Reverse crossing pans gradually from previous displayed pose");
            SettleCamera();
            foreach(float aspect in new[]{4f/3,16f/9,21f/9})
            {
                camera.aspect=aspect;rig.Snap();water.Refresh();
                Check(camera.ViewportToWorldPoint(new Vector3(1,0,10)).x<=b.max.x+.01f,"Settled near boundary holds at aspect "+aspect);
            }
            camera.ResetAspect();rig.overview=true;rig.Snap();water.Refresh();
            CheckOverviewMask(-1,"Left overview hides right side when settled");
            motor.Body.position=new Vector2(b.max.x+.5f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            CheckOverviewMask(-1,"Overview partial crossing retains original mask");
            motor.Body.position=new Vector2(b.max.x+1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(water.VisibleSide==1&&water.IsTransitioning&&!water.occlusion.enabled,"Overview full crossing reveals both sides during transition");
            SettleCamera();CheckOverviewMask(1,"Overview masks left side only after arrival");
            motor.Body.position=new Vector2(b.min.x-1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(!water.occlusion.enabled&&water.IsTransitioning,"Reverse overview crossing also reveals both sides");
            SettleCamera();CheckOverviewMask(-1,"Reverse overview crossing masks right side after arrival");
            rig.overview=false;CameraStep(Dt);
            Check(!water.occlusion.enabled,"Tab return to near disables black mask");
            Check(camera.ViewportToWorldPoint(new Vector3(1,0,10)).x<=b.max.x+.01f,"Settled overview-to-near transition respects boundary");
            rig.Snap();water.ResetSide(motor.Body.position);
            // A return crossing during the pan must reverse from the current view.
            motor.Body.position=new Vector2(b.max.x+1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            rig.overview=true;CameraStep(Dt);
            Check(water.IsTransitioning&&!water.occlusion.enabled,"Switching Tab during a near pan keeps both sides unmasked");
            rig.overview=false;CameraStep(Dt);
            previous=camera.transform.position;
            motor.Body.position=new Vector2(b.min.x-1.02f,40);Physics2D.SyncTransforms();CameraStep(Dt);
            Check(Vector3.Distance(previous,camera.transform.position)<=water.cameraTransitionSpeed*Dt+.001f,"Reversing midway does not jump the camera");
            session.Respawn();rig.Snap();water.ResetSide(motor.Body.position);
            Check(!water.IsTransitioning,"Respawn cancels cross-waterfall transition");
        }
        void CheckOverviewMask(int side,string label)
        {
            var water=actor.waterfall;var b=water.curtainBounds.bounds;var mask=water.occlusion.bounds;
            bool covers=side<0?Mathf.Abs(mask.min.x-b.max.x)<.01f&&mask.max.x>=313:
                Mathf.Abs(mask.max.x-b.min.x)<.01f&&mask.min.x<=0;
            Check(water.occlusion.enabled&&water.occlusion.color==Color.black&&covers&&mask.min.y<=0&&mask.max.y>=106,label);
        }
        void WheelStep(ForestInput input=default)
        {
            level.wheel.Advance(Dt);Step(input);
        }
        void TestSavedRails()
        {
            foreach(var rail in actor.ziplines)
            {
                Place(rail.Point(rail.Length*.5f)-Vector2.up*1.12f);
                Step(Input(ride:true));
                Check(actor.RidingRail==rail,"Actual saved rail catches player: "+rail.name);
                Vector2 from=motor.Body.position;Step(count:10);
                Check(actor.RidingRail==rail&&Vector2.Dot(motor.Body.position-from,rail.Tangent)>1,
                    "Actual saved rail supports forward travel: "+rail.name);
            }
            session.Respawn();
        }
        void TestWheelAndGoal()
        {
            var wheel=level.wheel;bool preview=wheel.forceRotation;wheel.forceRotation=false;wheel.ResetWheel();
            float angle=wheel.platforms[0].angle;wheel.Advance(1);Physics2D.Simulate(Dt);
            Check(wheel.platforms[0].angle==angle&&!wheel.Active,"Normal wheel remains still before both locks");
            Place(wheel.locks[0].transform.position);Step();wheel.Advance(1);Physics2D.Simulate(Dt);
            Check(wheel.locks[0].Activated&&!wheel.Active,"Saved left unlock collider activates one puzzle lock");
            wheel.locks[0].Activate();Check(wheel.UnlockCount==1,"Same lock is not counted twice");
            var goal=FindAnyObjectByType<ForestGoal>();Place(goal.transform.position);Physics2D.Simulate(Dt);
            Check(!session.Won&&!motor.frozen,"Old goal permits continued walking with one lock");
            level.Restart();wheel.locks[0].Activate();
            Place(wheel.locks[1].transform.position);Step();
            Check(wheel.Active&&wheel.UnlockCount==2,"Saved right unlock completes puzzle");
            session.Respawn();Check(wheel.Active,"Checkpoint respawn preserves wheel unlock progress");
            float rotation=wheel.Rotation;
            for(int i=0;i<40;i++)WheelStep();
            Check(wheel.Rotation<rotation&&wheel.Direction==-1,"Waterwheel only turns clockwise");
            float savedSpeed=actor.settings.wheelDegreesPerSecond;actor.settings.wheelDegreesPerSecond=-savedSpeed;
            rotation=wheel.Rotation;WheelStep();Check(wheel.Rotation<rotation,"Negative speed input cannot reverse rotation");actor.settings.wheelDegreesPerSecond=savedSpeed;
            // Static real wheel platform: walking in both directions must never
            // be replaced by the old motor-frozen passenger state.
            wheel.ResetWheel();var platform=wheel.platforms[5];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);
            Check(actor.RidingWheel==platform,"Standing finds current physical platform support");
            Step(Input(teleport:true,aim:Vector2.up));
            Check(actor.BeaconActive&&actor.RidingWheel==null&&motor.Body.linearVelocity.y==0,"Beacon hover releases wheel support instead of applying wheel carry");
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);
            float fromX=motor.Body.position.x;Step(Input(right:true),count:20);
            Check(motor.Body.position.x>fromX+.8f,"A/D walking works on first platform landing");
            fromX=motor.Body.position.x;Step(new ForestInput{movement=new SeasideInput{horizontal=-1}},count:35);
            Check(motor.Body.position.x<fromX-.8f,"Player can reverse direction freely on wheel platform");
            Step(Input(jump:true,held:true));Check(actor.RidingWheel==null&&motor.Body.linearVelocity.y>20,"Platform jump uses normal motor instead of locked ride");
            bool landed=false;
            for(int i=0;i<240;i++){Step();if(actor.RidingWheel==platform){landed=true;break;}}
            Check(landed,"Jumping then landing re-acquires same platform automatically");
            wheel.locks[0].Activate();wheel.locks[1].Activate();
            float fromY=motor.Body.position.y;
            for(int i=0;i<15;i++)WheelStep();
            Check(actor.RidingWheel==platform&&motor.Body.position.y>fromY+.6f,"After a jump, platform still carries the player upwards");
            wheel.ResetWheel();wheel.locks[0].Activate();wheel.locks[1].Activate();platform=wheel.platforms[5];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);
            bool tookOff=false,reLanded=false;
            for(int i=0;i<180;i++)
            {
                float carry=actor.RidingWheel==platform?Mathf.Clamp(platform.Velocity.x,-actor.settings.wheelCarryMaxSpeed,actor.settings.wheelCarryMaxSpeed):0;
                float h=(platform.Velocity.x-carry+(platform.body.position.x-motor.Body.position.x)*3)/motor.settings.moveSpeed;
                WheelStep(new ForestInput{movement=new SeasideInput{horizontal=Mathf.Clamp(h,-1,1),jumpDown=i==20,jumpHeld=i>=20&&i<75}});
                if(i==20)tookOff=actor.RidingWheel==null&&motor.Body.linearVelocity.y>20;
                if(i>20&&tookOff&&actor.RidingWheel==platform){reLanded=true;break;}
            }
            Check(tookOff&&reLanded,"Jumping on rotating rising wheel can land back on it and restore support");
            // Reset to repeat the same moving-arc experiment with/without input.
            wheel.ResetWheel();wheel.locks[0].Activate();wheel.locks[1].Activate();platform=wheel.platforms[5];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);fromY=motor.Body.position.y;
            int idle=0;
            for(;idle<1000;idle++){WheelStep();if(actor.RidingWheel!=platform)break;}
            Check(idle<1000,"Finite traction eventually lets idle player slip off: ticks="+idle);
            wheel.ResetWheel();wheel.locks[0].Activate();wheel.locks[1].Activate();platform=wheel.platforms[5];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);int controlled=0;float maxRise=0;
            fromY=motor.Body.position.y;
            for(;controlled<1800;controlled++)
            {
                // Track the actual platform by ordinary horizontal input. This
                // is test input, not runtime auto-carry or positional parenting.
                float required=platform.Velocity.x-Mathf.Clamp(platform.Velocity.x,-actor.settings.wheelCarryMaxSpeed,actor.settings.wheelCarryMaxSpeed)
                    +(platform.body.position.x-motor.Body.position.x)*3;
                WheelStep(new ForestInput{movement=new SeasideInput{horizontal=Mathf.Clamp(required/motor.settings.moveSpeed,-1,1)}});
                maxRise=Mathf.Max(maxRise,motor.Body.position.y-fromY);
                if(actor.RidingWheel!=platform)break;
            }
            Check(controlled>idle+60&&maxRise>12,"Active forward input keeps pace longer than idle and climbs real wheel: controlled="+controlled+" idle="+idle+" rise="+maxRise+" end="+motor.Body.position);
            Check(wheel.Direction==-1,"Boarding and re-landing never reverse the wheel");
            wheel.ResetWheel();wheel.locks[0].Activate();wheel.locks[1].Activate();platform=wheel.platforms[5];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);
            for(int i=0;i<900&&platform.angle>60;i++)
            {
                float h=(platform.Velocity.x-Mathf.Clamp(platform.Velocity.x,-actor.settings.wheelCarryMaxSpeed,actor.settings.wheelCarryMaxSpeed)
                    +(platform.body.position.x-motor.Body.position.x)*3)/motor.settings.moveSpeed;
                WheelStep(new ForestInput{movement=new SeasideInput{horizontal=Mathf.Clamp(h,-1,1)}});
            }
            bool launchOnWheel=actor.RidingWheel==platform;
            WheelStep(Input(right:true,jump:true,held:true));
            bool reachedGoal=false;
            for(int i=0;i<240&&!reachedGoal;i++)
            {
                float h=Mathf.Clamp((179-motor.Body.position.x)*2,-1,1);
                WheelStep(new ForestInput{movement=new SeasideInput{horizontal=h,jumpHeld=true}});
                reachedGoal=goal.GetComponent<Collider2D>().OverlapPoint(motor.Body.position);
            }
            Check(launchOnWheel&&reachedGoal&&!session.Won,"Ordinary jump from clockwise wheel reaches real final platform without forced transfer pos="+motor.Body.position);
            level.Restart();wheel.forceRotation=false;
            // Right arc is intentionally descending now; it must not reverse.
            wheel.ResetWheel();wheel.locks[0].Activate();wheel.locks[1].Activate();platform=wheel.platforms[1];
            Place(platform.body.position+Vector2.up*1.20f);Step(count:8);float rightY=platform.body.position.y;
            for(int i=0;i<12;i++)WheelStep();
            Check(platform.body.position.y<rightY&&wheel.Direction==-1,"Right-side platform descends clockwise without boarding reversal");
            level.Restart();wheel.forceRotation=false;
            Check(!session.Won&&!wheel.Active&&wheel.UnlockCount==0,"Full restart resets real puzzle progress");
            wheel.forceRotation=true;rotation=wheel.Rotation;WheelStep();
            Check(wheel.Rotation<rotation&&wheel.UnlockCount==0,"Inspector preview spins clockwise without faking puzzle switches");
            Place(goal.transform.position);Step();
            Check(!session.Won&&!motor.frozen&&wheel.UnlockCount==0,"Preview goal remains freely walkable");
            level.Restart();wheel.forceRotation=false;
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Time.timeScale=0;
            Place(goal.transform.position);Physics2D.Simulate(Dt);
            Check(!session.Won,"Paused session cannot complete goal");
            session.Resume();Step();
            Check(!session.Won&&!motor.frozen&&wheel.UnlockCount==0,"Resuming inside old goal does not complete region");
            level.Restart();
            Place(goal.transform.position);wheel.locks[0].Activate();wheel.locks[1].Activate();Step();
            Check(!session.Won,"Old upper-deck goal never changes region");
            level.Restart();wheel.forceRotation=preview;
        }
        void TestCheckpoints()
        {
            var checkpoints=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
            Check(checkpoints.Length>=6,"Authored and manually added intermediate checkpoints available");
            foreach(var checkpoint in checkpoints)
            {
                Place(checkpoint.transform.position);Step();
                Check(session.CheckpointId==checkpoint.checkpointId,"Checkpoint trigger advances ID "+checkpoint.checkpointId);
                session.Die();session.Respawn();
                Check(Vector2.Distance(motor.Body.position,checkpoint.respawnAnchor.position)<.02f,"Death returns to current anchor "+checkpoint.checkpointId);
                Check(actor.IsFree(motor.Body.position)&&!Physics2D.OverlapBoxAll(motor.Body.position,motor.BodySize*.98f,0,1<<7).Any(),"Checkpoint spawn clears terrain and spikes "+checkpoint.checkpointId);
                Step(count:15);Check(!session.Respawning&&motor.Grounded,"Checkpoint settles safely on floor "+checkpoint.checkpointId);
            }
            Vector2 saved=(Vector2)checkpoints[0].respawnAnchor.position;
            Place(checkpoints[0].transform.position);Step();
            Check(session.CheckpointId==checkpoints[0].checkpointId&&session.RespawnPoint==saved,"Revisiting a lower ID becomes the latest checkpoint");
            session.Die();session.Respawn();
            Check(Vector2.Distance(motor.Body.position,saved)<.02f,"Death after backtracking returns to last touched lower ID");
            session.Respawn();Check(Vector2.Distance(motor.Body.position,saved)<.02f,"Manual respawn uses latest checkpoint");
            level.Restart();Check(session.CheckpointId==0&&Vector2.Distance(motor.Body.position,session.startPoint)<.02f,"Full restart clears checkpoint and returns to original spawn");
        }
    }
}
