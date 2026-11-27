using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    // Opt-in regression/visual harness. Does not write any player save files.
    public sealed class MenuRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();string folder;bool finished;
        public string InteractiveStep{get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-menuChecks"))return;
            var go=new GameObject("MenuRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<MenuRuntimeChecks>();
        }
        string Arg(string flag){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,flag);return i>=0&&i+1<a.Length?a[i+1]:null;}
        void Check(bool ok,string message){lines.Add((ok?"PASS ":"FAIL ")+message);Debug.Log(lines.Last());if(!ok)throw new InvalidOperationException(message);}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){lines.Add("FAIL "+message+" "+stack);Finish(false);}}
        void Finish(bool ok){if(finished)return;finished=true;File.WriteAllLines(Path.Combine(folder,"menu-runtime-checks.txt"),lines);Application.Quit(ok?0:1);}
        SeasideSession Session=>FindAnyObjectByType<SeasideSession>();
        IEnumerator Load(string path)
        {
            SceneManager.LoadSceneAsync(path);float until=Time.realtimeSinceStartup+25;
            while((SceneManager.GetActiveScene().path!=path||Session==null||Session.GetComponent<CampaignCheckpointPersistence>()?.Ready!=true)&&Time.realtimeSinceStartup<until)yield return null;
            Check(Session!=null&&Session.gameObject.scene.path==path,"Loaded authored region "+CampaignFlow.RegionLabel(path));
            Session.player.ResetAt(Session.startPoint);FindAnyObjectByType<SeasideCamera>()?.Snap();yield return new WaitForSeconds(.2f);
        }
        IEnumerator Shot(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
        }
        void QueuedInputMustClear(Action ui,string label)
        {
            var latch=new SeasideInputLatch();latch.Sample(true,false,true,true,true,Vector2.right,false,0);ui();
            var result=latch.Consume();Check(!result.jumpDown&&!result.dashDown&&result.horizontal==0&&!result.jumpHeld,label+" clears previously sampled gameplay edges");
            Check(!GameBindings.Pressed(GameAction.Dash)&&!GameBindings.Pressed(GameAction.Teleport)&&!GameBindings.Held(GameAction.Jump),label+" blocks same-frame gameplay input");
        }
        IEnumerator Start()
        {
            folder=Arg("-menuReport")??Application.temporaryCachePath;Directory.CreateDirectory(folder);Application.logMessageReceived+=OnLog;
            Check(!string.IsNullOrEmpty(Arg("-campaignSavePath")),"Requires isolated save path");
            yield return null;yield return null;
            foreach(string path in CampaignFlow.Scenes)
            {
                yield return Load(path);var session=Session;var hud=FindAnyObjectByType<GameHUD>();
                QueuedInputMustClear(session.Pause,"Opening pause");
                Check(session.InputBlocked&&session.Paused&&Time.timeScale==0,"Pause freezes player/world");yield return Shot(CampaignFlow.RegionLabel(path)+"-pause");
                var actor=session.player.GetComponent<Forest.ForestPlayer>();
                string latchName=actor==null?"controls":"inputLatch";
                object owner=actor==null?(object)session.player:actor;
                var latch=(SeasideInputLatch)owner.GetType().GetField(latchName,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(owner);
                latch.Sample(false,false,true,true,true,Vector2.right,false,0);
                if(actor!=null)typeof(Forest.ForestPlayer).GetField("teleportPressed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(actor,true);
                int dashes=session.player.DashUses,teleports=actor==null?0:actor.Teleports;
                QueuedInputMustClear(session.Resume,"Continue button");yield return new WaitForFixedUpdate();
                Check(!session.player.Dashing&&session.player.DashUses==dashes&&!session.InputBlocked,"Real motor rejects a dash queued while paused");
                Check(actor==null||!actor.BeaconActive&&actor.Teleports==teleports,"Traversal rejects a teleport queued while paused");
                float timer=session.Timer;QueuedInputMustClear(()=>session.SetHelp(true),"Opening help");yield return new WaitForSecondsRealtime(.1f);
                Check(session.InputBlocked&&Time.timeScale==0&&session.Timer==timer,"Reading help pauses player and timer");
                Check(typeof(GameHUD).GetField("helpTab",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)==null,"Help has only basic controls, no current-region tab");
                yield return Shot(CampaignFlow.RegionLabel(path)+"-help-basic");
                QueuedInputMustClear(()=>session.SetHelp(false),"Closing help");
                session.Pause();session.SetHelp(true);session.SetHelp(false);
                Check(session.Paused&&Time.timeScale==0,"Closing help opened from pause returns to pause");session.Resume();
                session.Pause();GameSettingsScreen.Open(()=>{});QueuedInputMustClear(GameSettingsScreen.Close,"Closing settings");
                Check(session.Paused&&Time.timeScale==0,"Settings return preserves pause");session.Resume();
                yield return new WaitForSeconds(.1f);
                if(ManualSaveSafety.CanSave(session,out _))
                {
                    Check(session.GetComponent<ManualSaveController>().Save(),"Safe-ground save opens chooser");
                    QueuedInputMustClear(SaveSlotScreen.Close,"Cancelling save chooser");
                    Check(!session.SaveMenuOpen&&Time.timeScale==1,"Save cancellation restores gameplay");
                }
                var point=FindAnyObjectByType<TravelPoint>();session.player.ResetAt(point.transform.position);Physics2D.SyncTransforms();
                var travel=session.GetComponent<TravelPointMenu>();travel.Open(point);
                if(path==CampaignFlow.Scenes[4]){Check(!travel.Opened&&!point.Usable,"Lighthouse chase has no regional travel chooser");continue;}
                Check(travel.Opened,"Travel chooser opened");
                QueuedInputMustClear(travel.Close,"Closing travel chooser");
                Check(!travel.Opened&&!session.InputBlocked&&!session.player.frozen,"Travel close restores input");
            }
            yield return Load(CampaignFlow.Scenes[5]);var robot=FindAnyObjectByType<Village.VillageRobot>();
            CampaignFlow.GrantBitcoin(Session);Session.player.WarpForTraversal(robot.transform.position);Physics2D.SyncTransforms();robot.Begin();robot.Advance();
            QueuedInputMustClear(robot.Advance,"Robot core dialogue completion");
            Check(CampaignFlow.CoreObtained&&!Session.player.frozen,"Robot core dialogue restores gameplay without leftover input");
            // Also cover the seaside dialogue callback, using its real close method.
            yield return Load(CampaignFlow.Scenes[0]);var story=Session.GetComponent<SeasideStory>();Session.player.WarpForTraversal(story.InteractionBounds.center);Physics2D.SyncTransforms();
            Check(story.BeginInteraction(),"Seaside dialogue opens");story.AdvanceDialogue();QueuedInputMustClear(story.AdvanceDialogue,"Finishing dialogue through UI or rebound interaction key");
            if(Arg("-interactive")!=null)
            {
                foreach(int index in new[]{0,1})
                {
                    yield return Load(CampaignFlow.Scenes[index]);var session=Session;var motor=session.player;int dashes=motor.DashUses;
                    session.Pause();InteractiveStep="click-continue-"+index;File.WriteAllText(Path.Combine(folder,"interactive-step.txt"),InteractiveStep);
                    float until=Time.realtimeSinceStartup+180;
                    while(session.Paused&&Time.realtimeSinceStartup<until)yield return null;
                    Check(!session.Paused,"Real left click activates Continue "+index);yield return new WaitForSeconds(.3f);
                    Check(motor.DashUses==dashes&&!motor.Dashing,"Real UI click does not trigger motor dash "+index);
                }
                InteractiveStep="done";File.WriteAllText(Path.Combine(folder,"interactive-step.txt"),InteractiveStep);
            }
            Finish(true);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
