using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using Mine;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class CampaignRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();bool finished;string report;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-campaignChecks")&&!args.Contains("-saveProbe"))return;
            var go=new GameObject("WorldRegionRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<CampaignRuntimeChecks>();
        }
        string Arg(string flag){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,flag);return i>=0&&i+1<a.Length?a[i+1]:null;}
        void Check(bool ok,string label){lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());if(!ok)Finish(false);}
        void Finish(bool ok){if(finished)return;finished=true;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));File.WriteAllLines(report,lines);Application.Quit(ok?0:1);}
        void OnLog(string message,string stack,LogType type){if(!finished&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)){lines.Add("FAIL "+message+" "+stack);Finish(false);}}
        IEnumerator WaitRegion(string path)
        {
            float until=Time.realtimeSinceStartup+35;
            while((SceneManager.GetActiveScene().path!=path||WorldTransition.Active)&&Time.realtimeSinceStartup<until)yield return null;
            Check(SceneManager.GetActiveScene().path==path&&!WorldTransition.Active,"Actual region transition completes "+CampaignFlow.RegionLabel(path));
            yield return null;yield return null;
            var session=FindAnyObjectByType<SeasideSession>();
            while(session!=null&&session.GetComponent<CampaignCheckpointPersistence>()?.Ready!=true&&Time.realtimeSinceStartup<until)yield return null;
            if(session!=null)Check(!session.Travelling&&!session.player.frozen,"Arrival restores input after automatic walk "+path);
            if(session!=null)Check(FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).All(c=>c.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled)),"All automatic checkpoint visuals start hidden in "+path);
        }
        IEnumerator Load(string path){SceneManager.LoadSceneAsync(path);yield return WaitRegion(path);}
        SeasideSession Session=>FindAnyObjectByType<SeasideSession>();
        IEnumerator Cross(string destination)
        {
            var session=Session;var region=session.GetComponent<WorldRegion>();var exit=region.exits.First(e=>e.destination==destination&&!e.requiresUpward);
            session.player.WarpForTraversal(exit.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();
            bool expectDrop=destination==CampaignFlow.Scenes[2]&&!exit.requiresInteraction;
            bool expectDirect=session.gameObject.scene.path==CampaignFlow.Scenes[2]&&destination==CampaignFlow.Scenes[5];
            int expectedDirection=expectDrop?0:exit.TravelDirection;
            string priorDisk=File.Exists(CampaignSave.FilePath)?File.ReadAllText(CampaignSave.FilePath):null;
            float began=Time.realtimeSinceStartup;
            Check(exit.TryTravel(),"Walking into actual exit requests "+CampaignFlow.RegionLabel(destination));
            Check(session.Travelling&&session.InputBlocked&&session.player.frozen,"Departure blocks manual input");
            bool walkSeen=false,correctSideSeen=false;float previousX=0;bool hadPosition=false,monotonic=true,directionCorrect=true;bool capturedBlack=false,capturedWalk=false;bool atStart=true;
            float limit=Time.realtimeSinceStartup+35;
            while(WorldTransition.Active&&Time.realtimeSinceStartup<limit)
            {
                var motor=FindAnyObjectByType<SeasideMotor>();var arrival=motor==null?null:motor.GetComponent<RegionArrival>();
                if(expectDirect&&Session!=null&&Session.gameObject.scene.path==destination&&WorldTransition.Alpha<.999f)
                    atStart&=Vector2.Distance(motor.Body.position,Session.startPoint)<.05f&&Session.InputBlocked;
                if(arrival?.Walking==true)
                {
                    walkSeen=true;float x=expectDrop?motor.Body.position.y:motor.Body.position.x;directionCorrect&=arrival.Direction==expectedDirection;
                    correctSideSeen|=expectDrop?arrival.Dropping&&Mathf.Abs(motor.Body.position.x-arrival.Target.x)<.01f&&x>arrival.Target.y+2:(x-arrival.Target.x)*expectedDirection<-2;
                    if(hadPosition)monotonic&=expectDrop?x<=previousX+.01f:(x-previousX)*expectedDirection>=-.01f;
                    previousX=x;hadPosition=true;Check(motor.frozen&&Session.InputBlocked,"Automatic arrival owns controls");
                }
                if(Arg("-worldScreens") is string folder)
                {
                    Directory.CreateDirectory(folder);
                    if(WorldTransition.Alpha>=.999f&&!capturedBlack){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"black-"+CampaignFlow.RegionLabel(destination)+".png"));capturedBlack=true;}
                    if(arrival?.Walking==true&&WorldTransition.Alpha<=0&&!capturedWalk){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"walk-"+CampaignFlow.RegionLabel(destination)+".png"));capturedWalk=true;}
                }
                yield return new WaitForSecondsRealtime(.06f);
            }
            yield return WaitRegion(destination);
            Check(Time.realtimeSinceStartup-began>=GameSystemSettings.Current.regionBlackDuration,"Black transition holds configured duration");
            Check(WorldTransition.LastBlackDuration>=GameSystemSettings.Current.regionBlackDuration,"Opaque black phase meets configured duration; actual="+WorldTransition.LastBlackDuration.ToString("F3"));
            Check(expectDirect?!walkSeen&&atStart&&Vector2.Distance(Session.player.Body.position,Session.startPoint)<.1f:walkSeen&&correctSideSeen&&monotonic&&directionCorrect,expectDirect?"Mine return reveals player at Village start with no sideways walk":"Arrival follows exit direction from correct viewport edge: "+expectedDirection);
            Check(CampaignFlow.Explored(destination),"Exploration retained in current play session");
            Check(priorDisk==(File.Exists(CampaignSave.FilePath)?File.ReadAllText(CampaignSave.FilePath):null),"Crossing regions never overwrites the manual save slot");
            session=Session;region=session.GetComponent<WorldRegion>();
            if(session.CheckpointId==0)
                Check(Vector2.Distance(session.RespawnPoint,region.Arrival(session.RegionArrivalKey).position)<.02f,"Arrival side becomes safe respawn until next checkpoint");
        }
        IEnumerator Start()
        {
            report=Arg("-campaignReport")??Path.Combine(Application.temporaryCachePath,"world-region-checks.txt");Application.logMessageReceived+=OnLog;
            if(string.IsNullOrEmpty(Arg("-campaignSavePath"))){Check(false,"Requires isolated save path");yield break;}
            yield return null;yield return null;
            if(Arg("-saveProbe")!=null){yield return Probe(Arg("-saveProbe"));yield break;}
            CampaignSave.Clear();var menu=FindAnyObjectByType<StartGameMenu>();menu.RefreshAvailability();Check(menu.buttons.Length==4&&!menu.CanContinue,"Four-action menu and empty Continue slot");
            Check(CampaignFlow.StartNew(),"New game starts first region");yield return WaitRegion(CampaignFlow.Scenes[0]);
            var session=Session;Check(CampaignFlow.NextScene(CampaignFlow.Scenes[0])==null,"Old linear next-stage mapping removed");session.Complete();
            Check(!session.Won&&!WorldTransition.Active,"Old Complete cannot teleport or freeze a nonterminal region");
            var west=session.GetComponent<WorldRegion>().exits.Single(e=>e.destination==CampaignFlow.Scenes[4]);
            session.player.WarpForTraversal(west.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();
            Check(!west.TryTravel()&&!CampaignFlow.LighthouseUnlocked,"Seaside west blocks Lighthouse before Snow continue");
            session.Respawn();var story=session.GetComponent<SeasideStory>();session.player.WarpForTraversal(new Vector2(story.InteractionBounds.min.x-1.1f,story.InteractionBounds.min.y+1.3f));
            Check(story.BeginInteraction(),"Existing seaside story remains interactable");story.AdvanceDialogue();story.AdvanceDialogue();
            Check(story.Read&&!session.Won&&!WorldTransition.Active,"Reading seaside story does not change maps");
            var seasideActor=session.player.GetComponent<ForestPlayer>();Check(seasideActor!=null&&seasideActor.settings!=null,"Seaside story actually installs usable teleport driver");
            seasideActor.manualInput=true;session.player.ResetAt(new Vector2(-100,50));
            seasideActor.Tick(.01f,new ForestInput{teleportDown=true,movement=new SeasideInput{aim=Vector2.right}});
            Check(seasideActor.BeaconActive&&!seasideActor.beaconTrail.enabled,"Seaside gained teleport launches beacon without red trail");
            seasideActor.Tick(.03f,default);var seasideTarget=seasideActor.BeaconPosition;seasideActor.Tick(.01f,new ForestInput{teleportDown=true});
            Check(seasideActor.Teleports==1&&Vector2.Distance(session.player.Body.position,seasideTarget)<.03f,"Seaside second right click performs actual teleport");session.Respawn();seasideActor.manualInput=false;
            var point=FindAnyObjectByType<TravelPoint>();session.player.WarpForTraversal(point.transform.position);var travelMenu=session.GetComponent<TravelPointMenu>();travelMenu.Open(point);
            Check(travelMenu.Opened&&session.InputBlocked&&!travelMenu.Select(CampaignFlow.Scenes[3]),"Travel menu blocks controls and rejects unexplored Snow");travelMenu.Close();
            yield return Cross(CampaignFlow.Scenes[1]);
            session=Session;session.Complete();Check(!session.Won,"Forest old goal completion leaves player in control");
            yield return TestHover(session);yield return TestDeath(session);
            yield return TestManualSave();
            yield return Cross(CampaignFlow.Scenes[5]);
            Check(FindAnyObjectByType<Village.VillageRobot>()!=null,"Village loaded from authored workbook with robot");
            yield return Cross(CampaignFlow.Scenes[2]);
            var passage=Session.GetComponent<MinePassage>();Check(passage!=null,"Mine has an upper jumping passage");
            var upper=passage.UpperExit;Session.player.WarpForTraversal(upper.GetComponent<Collider2D>().bounds.center);Session.player.Body.linearVelocity=Vector2.down*5;
            Check(!upper.TryTravel(),"Falling into Mine cannot immediately return through upper exit");
            Session.player.Body.linearVelocity=Vector2.up*10;Check(upper.TryTravel(),"Jumping upward through Mine entry returns to Village");yield return WaitRegion(CampaignFlow.Scenes[5]);
            Check(Vector2.Distance(Session.player.Body.position,Session.startPoint)<.1f&&Session.RegionArrivalKey=="Entry"&&Session.player.GetComponent<RegionArrival>()?.Walking!=true,"Upper shaft return teleports directly to Village start");
            yield return Cross(CampaignFlow.Scenes[2]);
            yield return TestMine();
            yield return Cross(CampaignFlow.Scenes[5]);
            Check(CampaignFlow.BitcoinObtained&&!CampaignFlow.CoreObtained,"Bitcoin alone does not grant thermal core");
            var robot=FindAnyObjectByType<Village.VillageRobot>();Session.player.WarpForTraversal(robot.transform.position);robot.Begin();robot.Advance();robot.Advance();
            Check(CampaignFlow.CoreObtained,"Village robot grants core after Bitcoin dialogue");
            yield return Cross(CampaignFlow.Scenes[3]);
            var snow=FindAnyObjectByType<Snow.SnowLevel>();snow.session.player.WarpForTraversal(snow.goal.transform.position);
            Check(snow.BeginInteraction(),"Snow fragment E interaction");snow.AdvanceDialogue();snow.AdvanceDialogue();snow.Choose(1);snow.AdvanceDialogue();
            Check(CampaignFlow.LighthouseUnlocked&&!snow.session.Won&&!WorldTransition.Active,"Snow Continue unlocks Lighthouse without moving player");
            yield return Cross(CampaignFlow.Scenes[5]);yield return Cross(CampaignFlow.Scenes[1]);yield return Cross(CampaignFlow.Scenes[0]);
            Check(Session.GetComponent<SeasideStory>().Read,"Region revisit preserves seaside story state");
            yield return Cross(CampaignFlow.Scenes[4]);
            Check(SceneManager.GetActiveScene().path==CampaignFlow.Scenes[4],"Lighthouse is reached only after unlocked seaside west walk");
            yield return Load(CampaignFlow.Scenes[0]);var directStory=Session.GetComponent<SeasideStory>();Session.player.WarpForTraversal(directStory.InteractionBounds.center);Physics2D.SyncTransforms();
            Check(directStory.CanTravel&&directStory.BeginInteraction(),"Unlocked seaside lighthouse E interaction uses normal fade travel");yield return WaitRegion(CampaignFlow.Scenes[4]);
            yield return Load(CampaignFlow.Scenes[5]);
            session=Session;point=FindAnyObjectByType<TravelPoint>();session.player.WarpForTraversal(point.transform.position);travelMenu=session.GetComponent<TravelPointMenu>();travelMenu.Open(point);
            Check(travelMenu.Select(CampaignFlow.Scenes[2]),"Clicking explored Mine starts portal transition");yield return WaitRegion(CampaignFlow.Scenes[2]);
            Check(Session.RegionArrivalKey=="Portal"&&Vector2.Distance(Session.player.Body.position,TravelPoint.For(Session).ArrivalPosition())<.15f,"Region portal arrives at destination portal, not map start");
            var mine=FindAnyObjectByType<MineLevel>();Check(mine.lift.Activated&&mine.gate.Opened&&mine.HasBitcoin,"Revisit restores lift unlock, open gate and collected item");
            Session.player.ResetAt(Session.startPoint);yield return new WaitForSeconds(.2f);
            Check(CampaignFlow.SaveManual(Session,out var saveMessage),"Manual save on real Mine ground: "+saveMessage);
            Vector2 lastManual=Session.player.Body.position;
            CampaignFlow.ReturnToMenu(Session);yield return WaitRegion(CampaignFlow.MenuScene);Check(CampaignFlow.Continue(),"Continue accepts manual six-region snapshot");yield return WaitRegion(CampaignFlow.Scenes[2]);
            Check(Vector2.Distance(Session.player.Body.position,lastManual)<.1f,"Continue restores exact latest player-saved position");
            Check(FindAnyObjectByType<MineLevel>().HasBitcoin&&CampaignFlow.LighthouseUnlocked,"Continue restores global and per-region progress");
            yield return Load(CampaignFlow.Scenes[4]);
            var tower=FindAnyObjectByType<Lighthouse.LighthouseLevel>();tower.collapse.manualSimulation=true;tower.session.player.WarpForTraversal(tower.goal.transform.position);tower.BeginInteraction();tower.AdvanceDialogue();tower.AdvanceDialogue();
            Check(tower.session.Won&&!WorldTransition.Active,"Final Lighthouse remains a story ending");
            yield return Load(CampaignFlow.Scenes[3]);var endingSnow=FindAnyObjectByType<Snow.SnowLevel>();endingSnow.Restart();endingSnow.session.player.WarpForTraversal(endingSnow.goal.transform.position);endingSnow.BeginInteraction();endingSnow.AdvanceDialogue();endingSnow.AdvanceDialogue();endingSnow.Choose(0);
            Check(WorldTransition.Active,"Snow give-up starts opaque ending transition");yield return WaitRegion(CampaignFlow.MenuScene);Check(FindAnyObjectByType<StartGameMenu>()!=null,"Snow give-up returns to main menu");
            Finish(true);
        }
        IEnumerator TestManualSave()
        {
            var session=Session;var motor=session.player;var actor=motor.GetComponent<ForestPlayer>();
            session.Restart();motor.ResetAt(session.startPoint);yield return new WaitForSeconds(.2f);
            Check(!CampaignFlow.HasSave&&!File.Exists(CampaignSave.FilePath),"Automatic spawn/checkpoint/region events do not create a Continue slot");
            Vector2 position=motor.Body.position;
            Check(CampaignFlow.SaveManual(session,out string message),"F5 API accepts real safe static ground: "+message);
            Check(CampaignFlow.HasSave&&CampaignSave.TryRead(out var first)&&CampaignSave.IsManual(first)&&Vector2.Distance(first.manualPosition,position)<.01f,"Manual slot records exact safe position");
            string bytes=File.ReadAllText(CampaignSave.FilePath);
            var automatic=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).Last();
            session.Checkpoint(automatic);session.Respawn();yield return new WaitForSeconds(.15f);
            Check(Vector2.Distance(session.player.Body.position,automatic.respawnAnchor.position)<.1f,"Auto checkpoint still controls in-session respawn");
            Check(File.ReadAllText(CampaignSave.FilePath)==bytes,"Auto checkpoint and respawn leave manual bytes untouched");
            // Exact snapshot includes mechanisms as of the manual save, not subsequent uncommitted progress.
            CampaignFlow.UnlockLighthouse(session);Check(CampaignFlow.LighthouseUnlocked,"Unsaved story progress exists in memory");
            CampaignFlow.ReturnToMenu(session);yield return WaitRegion(CampaignFlow.MenuScene);
            Check(CampaignFlow.Continue(),"Continue loads player's earlier manual snapshot");yield return WaitRegion(CampaignFlow.Scenes[1]);session=Session;motor=session.player;actor=motor.GetComponent<ForestPlayer>();
            Check(Vector2.Distance(motor.Body.position,position)<.1f&&!CampaignFlow.LighthouseUnlocked,"Continue restores saved position/state, not newer automatic progress");
            Check(File.ReadAllText(CampaignSave.FilePath)==bytes,"Menu exit and Continue never rewrite manual file");
            typeof(SeasideSession).GetProperty("Paused").SetValue(session,true);Time.timeScale=0;Check(CampaignFlow.SaveManual(session,out message),"Paused safe-ground Save Game button is accepted: "+message);session.Resume();
            bytes=File.ReadAllText(CampaignSave.FilePath);
            motor.ResetAt(new Vector2(-100,50));yield return new WaitForFixedUpdate();
            Check(!CampaignFlow.SaveManual(session,out message),"Airborne save refused");
            actor.manualInput=true;motor.ResetAt(session.startPoint);actor.Tick(.01f,new ForestInput{movement=new SeasideInput{dashDown=true,aim=Vector2.right}});
            Check(!CampaignFlow.SaveManual(session,out message),"Dashing save refused");actor.CancelSpecial();motor.ResetAt(session.startPoint);actor.manualInput=false;yield return new WaitForSeconds(.2f);
            session.Travelling=true;Check(!CampaignFlow.SaveManual(session,out message),"Transition save refused");session.Travelling=false;
            motor.frozen=true;Check(!CampaignFlow.SaveManual(session,out message),"Dialogue-frozen save refused");motor.frozen=false;
            session.TravelMenuOpen=true;Check(!CampaignFlow.SaveManual(session,out message),"Travel-menu save refused");session.TravelMenuOpen=false;
            var floor=new GameObject("ManualSaveTestFloor");floor.layer=6;floor.transform.position=new Vector2(-100,40);floor.AddComponent<BoxCollider2D>().size=new Vector2(8,1);
            var hazard=new GameObject("ManualSaveTestSpike");hazard.layer=7;hazard.transform.position=new Vector2(-98.8f,41.5f);hazard.AddComponent<BoxCollider2D>().isTrigger=true;Physics2D.SyncTransforms();
            Check(!ManualSaveSafety.SafeGround(motor,new Vector2(-100,41.52f)),"Ground near spikes refused");hazard.SetActive(false);
            Check(ManualSaveSafety.SafeGround(motor,new Vector2(-100,41.52f)),"Same neutral ground is valid after hazard removed");
            Check(!ManualSaveSafety.SafeGround(motor,new Vector2(-96.1f,41.52f)),"Cliff edge without both feet supported refused");
            floor.AddComponent<MovingSurface>();Check(!ManualSaveSafety.SafeGround(motor,new Vector2(-100,41.52f)),"Moving-platform support refused even when stationary");Destroy(floor);Destroy(hazard);
            session.Die();Check(!CampaignFlow.SaveManual(session,out message),"Death lock save refused");session.Respawn();yield return null;
            Check(File.ReadAllText(CampaignSave.FilePath)==bytes,"All rejected saves leave last manual snapshot intact");
            session.Restart();yield return new WaitForSeconds(.2f);Check(File.ReadAllText(CampaignSave.FilePath)==bytes,"Ctrl+R restart does not overwrite player's manual snapshot");
        }
        IEnumerator TestHover(SeasideSession session)
        {
            var actor=session.player.GetComponent<ForestPlayer>();var motor=actor.motor;actor.manualInput=true;motor.ResetAt(new Vector2(-100,45));Physics2D.SyncTransforms();
            actor.Tick(.01f,new ForestInput{teleportDown=true,movement=new SeasideInput{aim=Vector2.right,horizontal=1}});
            Vector2 p=motor.Body.position;actor.Tick(.01f,new ForestInput{movement=new SeasideInput{horizontal=-1}});
            Check(actor.BeaconActive&&motor.Body.linearVelocity==Vector2.zero,"Beacon hover rejects horizontal movement");
            actor.Tick(.01f,new ForestInput{teleportDown=true});Check(Mathf.Approximately(actor.PostTeleportHoverRemaining,actor.settings.teleportHoverDuration),"Teleport starts exactly configured hover");p=motor.Body.position;
            int holdSteps=Mathf.Max(0,Mathf.FloorToInt(actor.settings.teleportHoverDuration*.5f/.01f));
            for(int i=0;i<holdSteps;i++)actor.Tick(.01f,new ForestInput{movement=new SeasideInput{horizontal=i%2==0?1:-1}});
            Check((actor.settings.teleportHoverDuration==0||actor.PostTeleportHoverRemaining>0)&&motor.Body.linearVelocity==Vector2.zero&&motor.Body.position==p,"A/D cannot move during landing hover");
            for(int i=0;i<Mathf.CeilToInt(actor.settings.teleportHoverDuration/.01f)+5;i++)actor.Tick(.01f,new ForestInput());Check(actor.PostTeleportHoverRemaining<=0&&motor.Body.linearVelocity.y<0,"Hover expires and gravity resumes");session.Respawn();actor.manualInput=false;yield return null;
        }
        IEnumerator TestDeath(SeasideSession session)
        {
            session.Die();Vector2 p=session.player.Body.position;int deaths=session.Deaths;
            session.player.GetComponent<ForestPlayer>().Tick(.02f,new ForestInput{movement=new SeasideInput{horizontal=1,dashDown=true,jumpDown=true},teleportDown=true});
            Check(session.Respawning&&session.InputBlocked&&session.player.frozen&&session.player.Body.linearVelocity==Vector2.zero,"Death locks all motor/traversal inputs immediately");
            yield return new WaitForSecondsRealtime(.25f);Check(session.Respawning&&session.Deaths==deaths,"Death lock persists during first 0.25 seconds");
            yield return new WaitForSecondsRealtime(.32f);Check(!session.Respawning&&!session.player.frozen,"Death lock releases after configured 0.5 seconds");
        }
        IEnumerator TestMine()
        {
            var mine=FindAnyObjectByType<MineLevel>();var actor=mine.player;var motor=actor.motor;actor.manualInput=true;
            Check(mine.interactions.Single(p=>p.completesLevel).GetComponent<BitcoinGlow>()!=null,"Bottom Bitcoin has a pulsing golden light point");
            var dark=mine.cameraRig.GetComponent<MineDarkness>();Check(dark.mouseLight&&dark.mouseRadius>0&&dark.mouseFeather>0,"Mine-only mouse illumination exposes radius and feather");
            var previous=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;mine.lift.manualSimulation=mine.deepLift.manualSimulation=true;
            var yellow=FindObjectsByType<MineTouchSwitch>().Single(s=>s.lift!=null);var purple=FindObjectsByType<MineTouchSwitch>().Single(s=>s.gate!=null);
            var lift=mine.lift;Vector2 start=lift.body.position;lift.Advance(2);Check(start==lift.body.position&&!lift.Activated,"Yellow lift cannot move before touching switch");
            motor.ResetAt(yellow.transform.position);yellow.Touch(motor);lift.Advance(2);Check(lift.Activated&&!lift.Moving&&lift.body.position==start,"Touch unlocks lift but never auto-starts it");
            motor.ResetAt(lift.body.position+Vector2.up*(lift.Collider.bounds.extents.y+1.012f));Physics2D.SyncTransforms();
            Check(lift.Use(motor),"E starts yellow lift while aboard");float before=motor.Body.position.y;
            for(int i=0;i<40;i++){lift.Advance(.01f);actor.Tick(.01f,new ForestInput());Physics2D.Simulate(.01f);}
            Check(actor.RidingSurface==lift&&motor.Body.position.y<before-1,"Physical descending lift carries character");
            for(int i=0;i<2500&&lift.Moving;i++){lift.Advance(.01f);actor.Tick(.01f,new ForestInput());Physics2D.Simulate(.01f);}
            Check(!lift.Moving&&Vector2.Distance(lift.body.position,lift.lowerStop)<.02f,"Yellow lift stops at lower endpoint");start=lift.body.position;lift.Advance(10);Physics2D.Simulate(.01f);Check(lift.body.position==start,"At stop, no automatic return trip");
            Check(lift.Use(motor),"E sends yellow lift back upward");
            motor.ResetAt(purple.transform.position);purple.Touch(motor);Check(mine.gate.Opened&&!mine.gate.barrier.enabled,"Touching purple switch opens actual gate collider");
            lift=mine.deepLift;motor.ResetAt(lift.body.position+Vector2.up*(lift.Collider.bounds.extents.y+1.012f));Physics2D.SyncTransforms();Check(lift.Use(motor),"E starts deep blue-marker lift");
            bool corruptionSeen=false;var corruption=mine.cameraRig.GetComponent<MineCorruption>();
            for(int i=0;i<2500&&lift.Moving;i++){lift.Advance(.01f);actor.Tick(.01f,new ForestInput());Physics2D.Simulate(.01f);corruptionSeen|=corruption.Active;}
            Check(corruptionSeen&&!lift.Moving&&motor.Body.position.y<5,"Deep lift crosses red threshold and carries player to bottom");
            motor.WarpForTraversal(new Vector2(120,70));Check(corruption.Active,"Crossing the red threshold latches corruption until item pickup");
            var relic=mine.interactions.Single(p=>p.completesLevel);motor.WarpForTraversal(relic.transform.position);Physics2D.SyncTransforms();
            Check(mine.BeginInteraction(relic),"Golden bottom item E interaction");mine.AdvanceDialogue();mine.AdvanceDialogue();
            Check(mine.HasBitcoin&&!corruption.Active&&!mine.session.Won&&!WorldTransition.Active,"Picking item clears corruption while staying in Mine");
            motor.WarpForTraversal(lift.body.position+Vector2.up*(lift.Collider.bounds.extents.y+1.012f));Check(lift.Use(motor),"E starts bottom-to-top return trip");
            for(int i=0;i<2500&&lift.Moving;i++){lift.Advance(.01f);actor.Tick(.01f,new ForestInput());Physics2D.Simulate(.01f);}
            Check(!lift.Moving&&Vector2.Distance(lift.body.position,lift.upperStop)<.02f&&motor.Body.position.y>85,"Deep lift safely returns to upper landing");
            CampaignFlow.SaveCheckpoint(mine.session);Physics2D.simulationMode=previous;actor.manualInput=false;mine.lift.manualSimulation=mine.deepLift.manualSimulation=false;yield return null;
        }
        IEnumerator Probe(string phase)
        {
            if(phase=="ui")
            {
                // Interactive smoke test uses an isolated slot, never the player's save.
                yield return Load(CampaignFlow.Scenes[1]);yield break;
            }
            if(phase=="write")
            {
                CampaignSave.Clear();yield return Load(CampaignFlow.Scenes[2]);var mine=FindAnyObjectByType<MineLevel>();mine.RestoreProgress(true,true);mine.gate.Open();CampaignFlow.GrantBitcoin(mine.session);CampaignFlow.GrantCore(mine.session);CampaignFlow.UnlockLighthouse(mine.session);
                mine.session.player.ResetAt(mine.session.startPoint+Vector2.right*1.5f);yield return new WaitForSeconds(.2f);
                Check(CampaignFlow.SaveManual(mine.session,out var message),"First process manually saves a non-checkpoint position: "+message);
                Check(CampaignSave.TryRead(out var data)&&CampaignSave.IsManual(data)&&data.bitcoin&&data.snowContinue&&Vector2.Distance(data.manualPosition,mine.session.startPoint)>1,"Manual save stores exact custom position and world state");
                string before=File.ReadAllText(CampaignSave.FilePath);var cp=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).Last();mine.session.Checkpoint(cp);mine.session.Respawn();CampaignFlow.SaveCheckpoint(mine.session);
                Check(File.ReadAllText(CampaignSave.FilePath)==before,"Different automatic checkpoint cannot replace cross-process manual slot");Finish(true);
            }
            else
            {
                Check(CampaignFlow.Continue(),"Fresh process can continue expanded save");yield return WaitRegion(CampaignFlow.Scenes[2]);var mine=FindAnyObjectByType<MineLevel>();Check(mine.HasBitcoin&&mine.gate.Opened&&mine.lift.Activated&&CampaignFlow.LighthouseUnlocked,"Fresh process restores gate, lift and global unlocks");
                CampaignSave.TryRead(out var saved);Check(CampaignFlow.CoreObtained,"Fresh process restores acquired village core");Check(Vector2.Distance(mine.player.motor.Body.position,saved.manualPosition)<.1f,"Fresh process Continue restores manual coordinates instead of latest auto checkpoint");Finish(true);
            }
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
