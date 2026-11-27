using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public sealed class SaveSlotRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();string folder;bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-slotChecks")&&!args.Contains("-slotProbe"))return;
            var go=new GameObject("SaveSlotRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<SaveSlotRuntimeChecks>();
        }
        string Arg(string flag){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,flag);return i>=0&&i+1<a.Length?a[i+1]:null;}
        void Check(bool value,string label){lines.Add((value?"PASS ":"FAIL ")+label);if(!value)throw new InvalidOperationException(label);}
        void Finish(bool success)
        {if(finished)return;finished=true;File.WriteAllLines(Path.Combine(folder,"slot-runtime-checks.txt"),lines);Application.Quit(success?0:1);}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){lines.Add("FAIL "+message+" "+stack);Finish(false);}}
        SeasideSession Session=>FindAnyObjectByType<SeasideSession>();
        IEnumerator WaitScene(string path)
        {
            float until=Time.realtimeSinceStartup+25;
            while((SceneManager.GetActiveScene().path!=path||CampaignFlow.IsLoading)&&Time.realtimeSinceStartup<until)yield return null;
            yield return null;yield return null;
            Check(SceneManager.GetActiveScene().path==path,"Loads chosen scene "+CampaignFlow.RegionLabel(path));
            if(Session!=null){Session.player.ResetAt(Session.startPoint);yield return new WaitForSeconds(.2f);}
        }
        IEnumerator Shot(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
        }
        IEnumerator Start()
        {
            folder=Arg("-slotReport")??Application.temporaryCachePath;Directory.CreateDirectory(folder);Application.logMessageReceived+=OnLog;
            if(string.IsNullOrEmpty(Arg("-campaignSavePath"))){Check(false,"Requires isolated save path");yield break;}
            yield return null;yield return null;
            if(Arg("-slotProbe")=="read")
            {
                Check(CampaignSave.TryReadSlot(0,out var old)&&CampaignSave.IsManual(old)&&old.scene==CampaignFlow.Scenes[0],"Fresh process retains legacy slot 1");
                Check(CampaignSave.TryReadSlot(4,out var newer)&&CampaignSave.IsManual(newer)&&newer.scene==CampaignFlow.Scenes[1]&&newer.snowContinue,"Fresh process retains independent slot 5");
                Vector2 expected=newer.manualPosition;Check(CampaignFlow.Continue(4),"Fresh process explicitly loads slot 5");
                float until=Time.realtimeSinceStartup+25;while((SceneManager.GetActiveScene().path!=newer.scene||CampaignFlow.IsLoading)&&Time.realtimeSinceStartup<until)yield return null;
                yield return null;yield return null;
                Check(Session!=null&&Session.gameObject.scene.path==newer.scene&&Vector2.Distance(Session.player.Body.position,expected)<.15f&&CampaignFlow.LighthouseUnlocked,"Chosen slot restores precise position and its world state across processes");Finish(true);yield break;
            }
            for(int i=0;i<CampaignSave.SlotCount;i++)Check(CampaignSave.DeleteSlot(i),"Isolated slot initialized "+i);
            Check(CampaignSave.SlotPath(0)==Path.GetFullPath(Arg("-campaignSavePath")),"Slot 1 keeps legacy file path");
            Check(!CampaignFlow.HasSave,"Continue disabled when all slots empty");
            Check(CampaignFlow.StartNew(),"Starts game without a disk snapshot");yield return WaitScene(CampaignFlow.Scenes[0]);
            Check(CampaignFlow.SaveManual(Session,0,out var message),"Legacy slot safe-ground write: "+message);
            string legacy=File.ReadAllText(CampaignSave.SlotPath(0));
            Check(Session.GetComponent<ManualSaveController>().Save(),"F5 action opens chooser instead of silently overwriting");
            Check(SaveSlotScreen.Opened&&Session.SaveMenuOpen&&Session.InputBlocked&&Time.timeScale==0,"Save chooser pauses world and blocks player");
            Check(File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"Opening chooser leaves old file intact");
            SaveSlotScreen.Current.Select(1);yield return Shot("save-choose");
            Check(SaveSlotScreen.Current.Activate(),"Empty slot accepts chosen save");yield return null;
            Check(!SaveSlotScreen.Opened&&!Session.SaveMenuOpen&&Time.timeScale==1,"Successful save restores game time/input");
            Check(CampaignSave.TryReadSlot(1,out var slot2)&&CampaignSave.IsManual(slot2)&&File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"Slot 2 independent from legacy slot");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {Check(File.Exists(CampaignSave.SlotPath(1)+".png"),"Graphical save writes actual world thumbnail");File.Copy(CampaignSave.SlotPath(1)+".png",Path.Combine(folder,"captured-preview.png"),true);}
            string slot2Bytes=File.ReadAllText(CampaignSave.SlotPath(1));
            Session.GetComponent<ManualSaveController>().Save();SaveSlotScreen.Current.Select(1);
            Check(!SaveSlotScreen.Current.Activate()&&SaveSlotScreen.Current.AwaitingConfirmation,"Occupied slot requires overwrite confirmation");yield return Shot("overwrite-confirm");
            SaveSlotScreen.Current.CancelConfirmation();SaveSlotScreen.Close();yield return null;
            Check(File.ReadAllText(CampaignSave.SlotPath(1))==slot2Bytes,"Cancelling overwrite leaves bytes intact");
            CampaignFlow.UnlockLighthouse(Session);Session.GetComponent<ManualSaveController>().Save();SaveSlotScreen.Current.Select(1);SaveSlotScreen.Current.Activate();
            Check(SaveSlotScreen.Current.Confirm(),"Confirmed overwrite writes target slot");yield return null;
            Check(CampaignSave.TryReadSlot(1,out var overwritten)&&overwritten.snowContinue,"Overwrite stores updated world");
            File.WriteAllText(CampaignSave.SlotPath(1),"{broken");
            Check(CampaignSave.TryReadSlot(1,out var recovered)&&!recovered.snowContinue,"Corrupt primary recovers its own previous backup");
            Check(File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"Backup recovery never reads or modifies another slot");
            Session.GetComponent<ManualSaveController>().Save();SaveSlotScreen.Current.Select(1);yield return Shot("saved-preview");SaveSlotScreen.Current.RequestDelete();
            Check(SaveSlotScreen.Current.AwaitingConfirmation,"Deletion requires confirmation");SaveSlotScreen.Current.CancelConfirmation();
            Check(CampaignSave.HasFiles(1),"Cancelling deletion preserves target");SaveSlotScreen.Current.RequestDelete();Check(SaveSlotScreen.Current.Confirm(),"Confirmed deletion removes target");SaveSlotScreen.Close();yield return null;
            Check(!CampaignSave.TryReadSlot(1,out _)&&!File.Exists(CampaignSave.SlotPath(1)+".bak")&&File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"Delete clears primary and backup only for selected slot");
            Session.player.ResetAt(new Vector2(-100,100));yield return new WaitForFixedUpdate();
            Check(!Session.GetComponent<ManualSaveController>().Save()&&!SaveSlotScreen.Opened,"Unsafe airborne F5 cannot open save picker");
            Session.player.ResetAt(Session.startPoint);yield return new WaitForSeconds(.2f);
            typeof(SeasideSession).GetProperty("Paused").SetValue(Session,true);Time.timeScale=0;
            Check(Session.GetComponent<ManualSaveController>().Save(),"Pause menu safe-ground chooser accepted");SaveSlotScreen.Close();yield return null;
            Check(Session.Paused&&Time.timeScale==0&&!Session.SaveMenuOpen,"Returning from save chooser preserves paused menu");Session.Resume();
            CampaignFlow.ReturnToMenu(Session);yield return WaitScene(CampaignFlow.MenuScene);
            Check(CampaignFlow.StartNew(),"New game can start with existing slots");yield return WaitScene(CampaignFlow.Scenes[0]);
            Check(File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"New game retains all prior manual saves");
            SceneManager.LoadSceneAsync(CampaignFlow.Scenes[1]);yield return WaitScene(CampaignFlow.Scenes[1]);CampaignFlow.UnlockLighthouse(Session);
            Check(CampaignFlow.SaveManual(Session,4,out message),"Independent last slot saves Forest snapshot");
            string slot5Bytes=File.ReadAllText(CampaignSave.SlotPath(4));int selected=CampaignSave.SelectedSlot;
            Directory.CreateDirectory(CampaignSave.SlotPath(2)+".tmp");
            Check(!CampaignFlow.SaveManual(Session,2,out message)&&CampaignSave.SelectedSlot==selected,"Failed write does not change selected slot");Directory.Delete(CampaignSave.SlotPath(2)+".tmp");
            Check(File.ReadAllText(CampaignSave.SlotPath(4))==slot5Bytes&&File.ReadAllText(CampaignSave.SlotPath(0))==legacy,"Write failure leaves previous slots unchanged");
            CampaignFlow.ReturnToMenu(Session);yield return WaitScene(CampaignFlow.MenuScene);var menu=FindAnyObjectByType<StartGameMenu>();menu.RefreshAvailability();menu.ContinueGame();
            Check(SaveSlotScreen.Opened&&SaveSlotScreen.Current.Mode==SaveSlotScreen.Purpose.Load,"Continue opens slot selector");SaveSlotScreen.Current.Select(0);yield return Shot("load-choose");
            Check(SaveSlotScreen.Current.Activate(),"Continue loads selected older slot, not newest");yield return WaitScene(CampaignFlow.Scenes[0]);
            Check(!CampaignFlow.LighthouseUnlocked&&CampaignSave.SelectedSlot==0,"Chosen older save restores its own world state");
            Check(File.ReadAllText(CampaignSave.SlotPath(4))==slot5Bytes,"Loading older slot preserves newer slot");Finish(true);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
