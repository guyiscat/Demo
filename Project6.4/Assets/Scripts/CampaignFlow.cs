using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public static class CampaignFlow
    {
        // Stable first five indices preserve old saves; Village adds a region, not a sixth stage.
        public static readonly string[] Scenes={"Assets/Scenes/TheFirstScene_Seaside.unity","Assets/Scenes/ForestWhitebox.unity","Assets/Scenes/MineWhitebox.unity","Assets/Scenes/SnowWhitebox.unity","Assets/Scenes/LighthouseWhitebox.unity","Assets/Scenes/Village.unity"};
        public const string MenuScene="Assets/Scenes/StartGame.unity";
        static bool resumeRequested;
        static CampaignSaveData progress;
        public static string LoadError{get;private set;}
        public static bool IsLoading{get;internal set;}
        public static bool LighthouseUnlocked=>Progress.snowContinue;
        public static bool BitcoinObtained=>Progress.bitcoin;
        public static bool CoreObtained=>Progress.coreObtained;
        public static CampaignSaveData Progress
        {
            get{if(progress==null)progress=new CampaignSaveData();return progress;}
        }
        public static string CurrentTitle(string path)
        {int i=Array.IndexOf(Scenes,path);return i<0?"区域":new[]{"海边","森林","矿洞","雪山","灯塔","村庄"}[i];}
        public static string RegionLabel(string path)
        {int i=Array.IndexOf(Scenes,path);return i<0?"Unknown":new[]{"Seaside","Forest","Mine","Snow","Lighthouse","Village"}[i];}
        public static bool ScenesContains(string path)=>Scenes.Contains(path);
        public static bool IsLegacyCheck=>new[]{"-respawnChecks","-settingsChecks","-checkpointChecks","-mediaChecks","-forestChecks","-mineChecks","-thermalChecks","-startupSmoke","-thermalVisual","-copyVisual","-worldVisual","-regression","-calibrate","-smokeTest","-trailPreview","-cameraPreview","-forestCapture","-capture"}.Any(Environment.GetCommandLineArgs().Contains);
        public static bool Enabled=>Application.isPlaying&&!IsLegacyCheck;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime(){progress=null;resumeRequested=false;IsLoading=false;LoadError=null;}
        public static void ResetVisualProgress(){progress=new CampaignSaveData();}
        // Retained API for old tooling: goals no longer select a next map.
        public static string NextScene(string current,bool ended=false)=>null;
        public static bool IsStoryEnding(SeasideSession session)
        {
            var snow=UnityEngine.Object.FindAnyObjectByType<Snow.SnowLevel>();
            return session.gameObject.scene.path==Scenes[4]||snow!=null&&snow.Outcome==Snow.SnowOutcome.Stayed;
        }
        public static void Attach(SeasideSession session)
        {
            if(session.GetComponent<ManualSaveController>()==null)session.gameObject.AddComponent<ManualSaveController>();
            if(!Enabled||!Scenes.Contains(session.gameObject.scene.path))return;
            LoadError=null;
            if(session.GetComponent<CampaignCheckpointPersistence>()==null)session.gameObject.AddComponent<CampaignCheckpointPersistence>();
        }
        internal static void InitializeSession(SeasideSession session)
        {
            string path=session.gameObject.scene.path;
            var data=Progress;
            bool restore=resumeRequested;resumeRequested=false;
            RestoreMechanisms(session,Record(data,path));
            if(restore&&data.scene==path&&!data.ended)
            {
                RestoreManual(session,data);
            }
            data.explored=(data.explored??new string[0]).Concat(new[]{path}).Distinct().ToArray();
            if(!WorldTransition.Active){IsLoading=false;SaveCheckpoint(session);}
        }
        public static bool StartNew()
        {
            if(!CanLoad(Scenes[0]))return false;
            progress=new CampaignSaveData();resumeRequested=false;return Load(Scenes[0]);
        }
        public static bool Continue()
        {return Continue(CampaignSave.LatestSlot);}
        public static bool Continue(int slot)
        {
            if(!CampaignSave.TryReadSlot(slot,out var data)||!CampaignSave.IsManual(data)){LoadError="该槽位暂无有效手动存档，请先在安全地面按 F5 保存。";return false;}
            if(!CanLoad(data.scene)||IsLoading)return false;
            progress=data;resumeRequested=true;CampaignSave.SelectSlot(slot);return Load(data.scene);
        }
        public static void MenuReady(){IsLoading=false;LoadError=null;}
        public static void ReturnToMenu(SeasideSession session)
        {SaveCheckpoint(session);if(CanLoad(MenuScene))Load(MenuScene);}
        public static bool HasSave=>CampaignSave.LatestSlot>=0;
        public static string SaveDescription=>HasSave?"继续游戏 · 选择要读取的存档":"暂无存档";
        public static bool CanLoad(string path)
        {
            if(SceneUtility.GetBuildIndexByScenePath(path)>=0){LoadError=null;return true;}
            LoadError="区域未包含在当前版本中。";return false;
        }
        static bool Load(string path)
        {
            if(IsLoading)return false;IsLoading=true;Time.timeScale=1;
            try{SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);return true;}
            catch(Exception e){IsLoading=false;LoadError="区域加载失败，请重试。";Debug.LogException(e);return false;}
        }
        public static void Complete(SeasideSession session)
        {
            if(!Enabled)return;
            // Endings and automatic checkpoints never overwrite the player's manual slot.
            Capture(session);
        }
        public static void UnlockLighthouse(SeasideSession session)
        {Progress.snowContinue=true;SaveCheckpoint(session);}
        public static void GrantBitcoin(SeasideSession session)
        {Progress.bitcoin=true;SaveCheckpoint(session);}
        public static void GrantCore(SeasideSession session)
        {Progress.coreObtained=true;SaveCheckpoint(session);}
        public static void SnowEnding(SeasideSession session)
        {Capture(session);WorldTransition.Begin(session,MenuScene,"Entry");}
        public static bool Explored(string path)=>Progress.explored!=null&&Progress.explored.Contains(path);
        public static bool Travel(SeasideSession session,string destination,string arrival="Entry",bool requireExplored=false,int enterDirection=1)
        {
            if(!Enabled||session==null||session.Travelling||session.Respawning||session.Won||IsLoading||!Scenes.Contains(destination))return false;
            if(destination==Scenes[4]&&!LighthouseUnlocked||requireExplored&&!Explored(destination)||!CanLoad(destination))return false;
            if(session.player.frozen&&!session.TravelMenuOpen)return false;
            session.GetComponent<TravelPointMenu>()?.Close();SaveCheckpoint(session);
            bool portal=arrival=="Portal";
            bool mineReturn=!portal&&session.gameObject.scene.path==Scenes[2]&&destination==Scenes[5];
            WorldTransition.Begin(session,destination,mineReturn?"Entry":arrival,enterDirection,mineReturn||portal);return true;
        }
        public static RegionSave Record(CampaignSaveData data,string path)
        {
            var record=data.regions?.FirstOrDefault(r=>r.scene==path);
            if(record!=null)return record;
            // Migrate the single-region v1 slot without discarding its mechanisms.
            return new RegionSave{scene=path,checkpointId=data.scene==path?data.checkpointId:0,mineLift=data.scene==path&&data.mineLift,workbench=data.scene==path&&data.workbench,forestLeft=data.scene==path&&data.forestLeft,forestRight=data.scene==path&&data.forestRight};
        }
        public static void SaveCheckpoint(SeasideSession session)
        {
            if(!Enabled||session==null||session.Won||session.Respawning||session.Travelling||!Scenes.Contains(session.gameObject.scene.path))return;
            Capture(session);
        }
        public static bool SaveManual(SeasideSession session,out string message)
        {return SaveManual(session,CampaignSave.SelectedSlot,out message);}
        public static bool SaveManual(SeasideSession session,int slot,out string message)
        {
            if(!Enabled){message="当前检查模式不可保存。";return false;}
            if(!ManualSaveSafety.CanSave(session,out message))return false;
            // Clone the complete world snapshot. Subsequent gameplay mutates only in-memory progress.
            var snapshot=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(Capture(session)));
            snapshot.manualSnapshot=true;snapshot.ended=false;snapshot.manualPosition=session.player.Body.position;
            snapshot.manualProtection=(int)(session.player.GetComponent<Expedition.ThermalTraversal>()?.Mode??Expedition.ProtectionMode.Cold);
            if(!CampaignSave.WriteSlot(slot,snapshot)){message=CampaignSave.LastError;return false;}
            message="已保存到存档 "+(slot+1)+" · "+CurrentTitle(snapshot.scene);return true;
        }
        public static CampaignSaveData Capture(SeasideSession session)
        {
            var data=Progress;string path=session.gameObject.scene.path;
            var record=Record(data,path);record.checkpointId=session.CheckpointId;record.timer=session.Timer;record.deaths=session.Deaths;record.arrival=session.RegionArrivalKey;
            var mine=UnityEngine.Object.FindAnyObjectByType<Mine.MineLevel>();
            if(mine!=null){record.mineLift=mine.lift.Activated;record.workbench=mine.WorkbenchRead;record.gateOpen=mine.gate!=null&&mine.gate.Opened;data.bitcoin|=mine.HasBitcoin;}
            var wheel=UnityEngine.Object.FindAnyObjectByType<Forest.ForestWaterwheel>();
            if(wheel!=null){record.forestLeft=wheel.LeftUnlocked;record.forestRight=wheel.RightUnlocked;}
            var story=session.GetComponent<SeasideStory>();if(story!=null)record.seasideRead=story.Read;
            data.regions=(data.regions??new RegionSave[0]).Where(r=>r.scene!=path).Concat(new[]{record}).ToArray();
            data.explored=(data.explored??new string[0]).Concat(new[]{path}).Distinct().ToArray();
            data.scene=path;data.checkpointId=record.checkpointId;data.timer=record.timer;data.deaths=record.deaths;
            data.mineLift=record.mineLift;data.workbench=record.workbench;data.forestLeft=record.forestLeft;data.forestRight=record.forestRight;
            return data;
        }
        public static void Restore(SeasideSession session,CampaignSaveData data)
        {progress=data;RestoreMechanisms(session,Record(data,session.gameObject.scene.path));RestoreManual(session,data);}
        static void RestoreManual(SeasideSession session,CampaignSaveData data)
        {
            Physics2D.SyncTransforms();
            if(CampaignSave.IsManual(data)&&ManualSaveSafety.SafeGround(session.player,data.manualPosition))
            {
                session.RestoreManualPosition(data.manualPosition,data.timer,data.deaths);
                var thermal=session.player.GetComponent<Expedition.ThermalTraversal>();
                if(thermal!=null&&(int)thermal.Mode!=data.manualProtection)thermal.TrySwitchMode();
            }
            else
            {
                session.RestoreManualPosition(session.startPoint,data.timer,data.deaths);
                session.GetComponent<ManualSaveController>()?.Notify("原存档位置已不安全，已恢复到区域入口。");
            }
        }
        static void RestoreMechanisms(SeasideSession session,RegionSave record)
        {
            var mine=UnityEngine.Object.FindAnyObjectByType<Mine.MineLevel>();if(mine!=null){mine.RestoreProgress(record.workbench,record.mineLift);mine.RestoreWorld(record.gateOpen,Progress.bitcoin);}
            var wheel=UnityEngine.Object.FindAnyObjectByType<Forest.ForestWaterwheel>();
            if(wheel!=null)foreach(var item in wheel.locks)if(item.index==0&&record.forestLeft||item.index==1&&record.forestRight)item.Activate();
            session.GetComponent<SeasideStory>()?.RestoreRead(record.seasideRead);
            UnityEngine.Object.FindAnyObjectByType<Snow.SnowLevel>()?.RestoreOutcome(Progress.snowContinue);
        }
        public static string TransitionMessage(SeasideSession session)=>null;
    }
    [Serializable] public sealed class RegionSave
    {
        public string scene,arrival;public int checkpointId,deaths;public float timer;
        public bool mineLift,workbench,forestLeft,forestRight,gateOpen,seasideRead;
    }
    [Serializable] public sealed class CampaignSaveData
    {
        public int version=1;public string scene,savedAt;public int checkpointId,deaths;public float timer;
        public bool mineLift,workbench,forestLeft,forestRight,ended,bitcoin,snowContinue;
        public bool coreObtained,protectionSelected;public int protectionMode;
        public bool manualSnapshot;
        public Vector2 manualPosition;
        public int manualProtection;
        public string[] explored;
        public RegionSave[] regions;
    }
    public static class CampaignSave
    {
        public const int SlotCount=5;
        public static int SelectedSlot{get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSlot(){SelectedSlot=0;}
        public static void SelectSlot(int slot){if(slot<0||slot>=SlotCount)throw new ArgumentOutOfRangeException(nameof(slot));SelectedSlot=slot;}
        public static int LatestSlot
        {
            get
            {
                int latest=-1;DateTime timestamp=DateTime.MinValue;
                for(int i=0;i<SlotCount;i++)if(TryReadSlot(i,out var data)&&IsManual(data))
                {DateTime.TryParse(data.savedAt,null,System.Globalization.DateTimeStyles.RoundtripKind,out var date);if(latest<0||date>timestamp){latest=i;timestamp=date;}}
                return latest;
            }
        }
        public static bool IsManual(CampaignSaveData data)=>data!=null&&data.manualSnapshot&&!data.ended&&Finite(data.manualPosition.x)&&Finite(data.manualPosition.y)&&data.manualProtection>=0&&data.manualProtection<=1;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&Mathf.Abs(value)<100000;
        public static string LastError{get;private set;}
        public static string FilePath=>SlotPath(SelectedSlot);
        public static string SlotPath(int slot)
        {
            if(slot<0||slot>=SlotCount)throw new ArgumentOutOfRangeException(nameof(slot));
            var path=BaseFilePath;return slot==0?path:Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"-slot-"+(slot+1)+Path.GetExtension(path));
        }
        static string BaseFilePath
        {
            get
            {
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-campaignSavePath");
                if((args.Contains("-campaignChecks")||args.Contains("-saveProbe")||args.Contains("-slotChecks")||args.Contains("-slotProbe")||args.Contains("-menuChecks")||args.Contains("-newArtChecks"))&&i>=0&&i+1<args.Length)return Path.GetFullPath(args[i+1]);
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
                if(data.manualSnapshot&&(!Finite(data.manualPosition.x)||!Finite(data.manualPosition.y)||data.manualProtection<0||data.manualProtection>1))return false;
                save=data;return true;
            }
            catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}catch(ArgumentException){return false;}
        }
        public static bool TryRead(out CampaignSaveData save)=>Read(FilePath,out save)||Read(FilePath+".bak",out save);
        public static bool TryReadSlot(int slot,out CampaignSaveData save)
        {save=null;if(slot<0||slot>=SlotCount)return false;var path=SlotPath(slot);return Read(path,out save)||Read(path+".bak",out save);}
        public static bool Write(CampaignSaveData save)
        {return WriteSlot(SelectedSlot,save);}
        public static bool WriteSlot(int slot,CampaignSaveData save)
        {
            if(slot<0||slot>=SlotCount||save==null){LastError="无效的存档槽位。";return false;}
            try
            {
                save.savedAt=DateTime.UtcNow.ToString("o");var path=SlotPath(slot);Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path+".tmp",JsonUtility.ToJson(save,true));
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
                SelectedSlot=slot;LastError=null;return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException)
            {LastError="存档写入失败，请检查存储空间和权限。";Debug.LogWarning(LastError+" "+e.Message);return false;}
        }
        public static bool Clear()
        {return DeleteSlot(SelectedSlot);}
        public static bool HasFiles(int slot)
        {var path=SlotPath(slot);return File.Exists(path)||File.Exists(path+".bak");}
        public static bool DeleteSlot(int slot)
        {
            try
            {
                string path=SlotPath(slot);if(File.Exists(path))File.Copy(path,path+".previous",true);
                foreach(var suffix in new[]{"",".bak",".tmp",".png"})if(File.Exists(path+suffix))File.Delete(path+suffix);
                LastError=null;return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException)
            {LastError="无法删除存档，请检查存储空间和权限。";Debug.LogWarning(LastError+" "+e.Message);return false;}
        }
    }
}
