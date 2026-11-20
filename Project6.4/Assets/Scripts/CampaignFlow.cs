using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public static class CampaignFlow
    {
        public static readonly string[] Scenes={"Assets/Scenes/TheFirstScene_Seaside.unity","Assets/Scenes/ForestWhitebox.unity","Assets/Scenes/MineWhitebox.unity","Assets/Scenes/SnowWhitebox.unity","Assets/Scenes/LighthouseWhitebox.unity"};
        public const string MenuScene="Assets/Scenes/StartGame.unity";
        static bool resumeRequested;
        public static string LoadError{get;private set;}
        public static bool IsLoading{get;private set;}
        public static string CurrentTitle(string path)
        {int i=Array.IndexOf(Scenes,path);return i<0?"关卡":new[]{"海边","森林","矿洞","雪山","灯塔"}[i];}
        public static bool IsLegacyCheck=>new[]{"-forestChecks","-mineChecks","-thermalChecks","-startupSmoke","-thermalVisual","-copyVisual","-regression","-calibrate","-smokeTest","-trailPreview","-cameraPreview","-forestCapture","-capture"}.Any(Environment.GetCommandLineArgs().Contains);
        public static bool Enabled=>Application.isPlaying&&!IsLegacyCheck;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime(){resumeRequested=false;IsLoading=false;LoadError=null;}
        public static string NextScene(string current,bool ended=false)
        {int i=Array.IndexOf(Scenes,current);return !ended&&i>=0&&i<Scenes.Length-1?Scenes[i+1]:null;}
        public static bool IsStoryEnding(SeasideSession session)
        {
            var snow=UnityEngine.Object.FindAnyObjectByType<Snow.SnowLevel>();
            return session.gameObject.scene.path==Scenes[4]||snow!=null&&snow.Outcome==Snow.SnowOutcome.Stayed;
        }
        public static void Attach(SeasideSession session)
        {
            if(!Enabled||!Scenes.Contains(session.gameObject.scene.path))return;
            IsLoading=false;LoadError=null;
            if(session.GetComponent<CampaignCheckpointPersistence>()==null)session.gameObject.AddComponent<CampaignCheckpointPersistence>();
        }
        internal static void InitializeSession(SeasideSession session)
        {
            bool restore=resumeRequested;resumeRequested=false;
            if(restore&&CampaignSave.TryRead(out var save)&&save.scene==session.gameObject.scene.path&&!save.ended)Restore(session,save);
            SaveCheckpoint(session);
        }
        public static bool StartNew()
        {
            if(!CanLoad(Scenes[0]))return false;
            if(!CampaignSave.Clear()){LoadError=CampaignSave.LastError;return false;}resumeRequested=false;return Load(Scenes[0]);
        }
        public static bool Continue()
        {
            if(!CampaignSave.TryRead(out var save)||save.ended){LoadError="暂无可继续的存档。";return false;}
            if(!CanLoad(save.scene))return false;resumeRequested=true;return Load(save.scene);
        }
        public static void MenuReady(){IsLoading=false;LoadError=null;}
        public static void ReturnToMenu(SeasideSession session)
        {SaveCheckpoint(session);if(CanLoad(MenuScene))Load(MenuScene);}
        public static bool HasSave=>CampaignSave.TryRead(out var save)&&!save.ended;
        public static string SaveDescription=>CampaignSave.TryRead(out var save)&&!save.ended?CurrentTitle(save.scene)+" · "+(save.checkpointId==0?"关卡入口":"存档点 "+save.checkpointId):"暂无存档";
        static bool CanLoad(string path)
        {
            if(SceneUtility.GetBuildIndexByScenePath(path)>=0){LoadError=null;return true;}
            LoadError="关卡未包含在当前版本中，请使用最新游戏版本。";Debug.LogError(LoadError+" "+path);return false;
        }
        static bool Load(string path)
        {
            if(IsLoading)return false;IsLoading=true;Time.timeScale=1;
            try{SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);return true;}
            catch(Exception e){IsLoading=false;LoadError="关卡加载失败，请返回后重试。";Debug.LogException(e);return false;}
        }
        public static void Complete(SeasideSession session)
        {
            if(!Enabled||!Scenes.Contains(session.gameObject.scene.path))return;
            string next=NextScene(session.gameObject.scene.path,IsStoryEnding(session));
            if(next==null){var terminal=Capture(session);terminal.ended=true;CampaignSave.Write(terminal);return;}
            CampaignSave.Write(new CampaignSaveData{scene=next});session.StartCoroutine(Advance(session,next));
        }
        static IEnumerator Advance(SeasideSession session,string next)
        {
            yield return new WaitForSecondsRealtime(GameSystemSettings.Current==null?.8f:Mathf.Max(0,GameSystemSettings.Current.nextLevelDelay));
            if(session==null||!session.Won||IsLoading)yield break;
            if(CanLoad(next)){resumeRequested=false;Load(next);}
        }
        public static void SaveCheckpoint(SeasideSession session)
        {
            if(!Enabled||session.Won||session.Respawning||!Scenes.Contains(session.gameObject.scene.path))return;
            CampaignSave.Write(Capture(session));
        }
        public static CampaignSaveData Capture(SeasideSession session)
        {
            var save=new CampaignSaveData{scene=session.gameObject.scene.path,checkpointId=session.CheckpointId,timer=session.Timer,deaths=session.Deaths};
            var mine=UnityEngine.Object.FindAnyObjectByType<Mine.MineLevel>();
            if(mine!=null){save.mineLift=mine.lift.Activated;save.workbench=mine.WorkbenchRead;}
            var wheel=UnityEngine.Object.FindAnyObjectByType<Forest.ForestWaterwheel>();
            if(wheel!=null){save.forestLeft=wheel.LeftUnlocked;save.forestRight=wheel.RightUnlocked;}
            return save;
        }
        public static void Restore(SeasideSession session,CampaignSaveData save)
        {
            session.RestoreCheckpoint(save.checkpointId,save.timer,save.deaths);
            var mine=UnityEngine.Object.FindAnyObjectByType<Mine.MineLevel>();if(mine!=null)mine.RestoreProgress(save.workbench,save.mineLift);
            var wheel=UnityEngine.Object.FindAnyObjectByType<Forest.ForestWaterwheel>();
            if(wheel!=null)foreach(var item in wheel.locks)if(item.index==0&&save.forestLeft||item.index==1&&save.forestRight)item.Activate();
        }
        public static string TransitionMessage(SeasideSession session)
        {
            if(!Enabled||IsStoryEnding(session))return null;string next=NextScene(session.gameObject.scene.path);
            return next==null?null:string.IsNullOrEmpty(LoadError)?"正在前往"+CurrentTitle(next)+"……":LoadError;
        }
    }
    [Serializable] public sealed class CampaignSaveData
    {
        public int version=1;public string scene,savedAt;public int checkpointId,deaths;public float timer;
        public bool mineLift,workbench,forestLeft,forestRight,ended;
    }
    public static class CampaignSave
    {
        public static string LastError{get;private set;}
        public static string FilePath
        {
            get
            {
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-campaignSavePath");
                if((args.Contains("-campaignChecks")||args.Contains("-saveProbe"))&&i>=0&&i+1<args.Length)return Path.GetFullPath(args[i+1]);
                return Path.Combine(Application.persistentDataPath,"campaign-save.json");
            }
        }
        static bool Read(string path,out CampaignSaveData save)
        {
            save=null;
            try
            {
                if(!File.Exists(path)||new FileInfo(path).Length>65536)return false;
                var data=JsonUtility.FromJson<CampaignSaveData>(File.ReadAllText(path));
                if(data==null||data.version!=1||!CampaignFlow.Scenes.Contains(data.scene)||data.checkpointId<0||data.checkpointId>1000||data.deaths<0||float.IsNaN(data.timer)||float.IsInfinity(data.timer)||data.timer<0)return false;
                save=data;return true;
            }
            catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}catch(ArgumentException){return false;}
        }
        public static bool TryRead(out CampaignSaveData save)=>Read(FilePath,out save)||Read(FilePath+".bak",out save);
        public static bool Write(CampaignSaveData save)
        {
            try
            {
                save.savedAt=DateTime.UtcNow.ToString("o");var path=FilePath;Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path+".tmp",JsonUtility.ToJson(save,true));
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
                LastError=null;return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException)
            {LastError="存档写入失败，请检查存储空间和权限。";Debug.LogWarning(LastError+" "+e.Message);return false;}
        }
        public static bool Clear()
        {
            try
            {
                string path=FilePath;if(File.Exists(path))File.Copy(path,path+".previous",true);
                foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);
                LastError=null;return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException)
            {LastError="无法创建新存档，请检查存储空间和权限。";Debug.LogWarning(LastError+" "+e.Message);return false;}
        }
    }
}
