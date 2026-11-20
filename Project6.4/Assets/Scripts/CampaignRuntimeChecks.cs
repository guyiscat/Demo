using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    // Explicit process-level verification. Tests always receive a separate save path.
    public sealed class CampaignRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();bool finished;
        string reportPath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();
            if(!args.Contains("-campaignChecks")&&!args.Contains("-saveProbe"))return;
            var go=new GameObject("CampaignRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<CampaignRuntimeChecks>();
        }
        string Argument(string flag,string fallback=null)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        void Check(bool ok,string label)
        {
            lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());
            if(!ok)Finish(false);
        }
        void OnLog(string message,string stack,LogType kind)
        {
            if(finished||kind!=LogType.Exception&&kind!=LogType.Error&&kind!=LogType.Assert)return;
            lines.Add("FAIL "+message+" "+stack);Finish(false);
        }
        void Finish(bool success)
        {
            if(finished)return;finished=true;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllLines(reportPath,lines);Debug.Log(success?"CAMPAIGN_RUNTIME_PASS assertions="+lines.Count:"CAMPAIGN_RUNTIME_FAIL");Application.Quit(success?0:1);
        }
        IEnumerator WaitScene(string path)
        {
            float until=Time.realtimeSinceStartup+25;
            while(SceneManager.GetActiveScene().path!=path&&Time.realtimeSinceStartup<until)yield return null;
            Check(SceneManager.GetActiveScene().path==path,"Actual scene load: "+path);
            yield return null;yield return null;
            var session=FindAnyObjectByType<SeasideSession>();
            if(session!=null)
            {
                while((session.GetComponent<CampaignCheckpointPersistence>()==null||!session.GetComponent<CampaignCheckpointPersistence>().Ready)&&Time.realtimeSinceStartup<until)yield return null;
                Check(session.GetComponent<CampaignCheckpointPersistence>()?.Ready==true,"Scene initialized and save restore completed");
            }
        }
        IEnumerator Load(string path){SceneManager.LoadSceneAsync(path);yield return WaitScene(path);}
        IEnumerator Start()
        {
            reportPath=Argument("-campaignReport",Path.Combine(Application.persistentDataPath,"campaign-runtime-checks.txt"));
            Application.logMessageReceived+=OnLog;
            if(string.IsNullOrEmpty(Argument("-campaignSavePath"))){Check(false,"Test requires isolated save path");yield break;}
            yield return null;yield return null;
            if(Argument("-saveProbe")!=null){yield return Probe(Argument("-saveProbe"));yield break;}
            CampaignSave.Clear();var menu=FindAnyObjectByType<StartGameMenu>();menu.RefreshAvailability();
            Check(menu.buttons.Length==4&&menu.buttons.Select(b=>b.action).SequenceEqual(new[]{StartMenuAction.NewGame,StartMenuAction.Continue,StartMenuAction.Settings,StartMenuAction.Exit}),"Four menu actions wired by action, not three hardcoded indexes");
            Check(!menu.CanContinue&&!menu.buttons[1].Available,"No save disables Continue");
            menu.SelectNext(1);menu.ActivateSelection();Check(!menu.IsLoading&&!menu.NewGamePrompt,"Disabled Continue does not load or erase data");
            menu.SelectNext(1);menu.ActivateSelection();Check(menu.SettingsOpen,"Settings remains correct fourth-row action mapping");menu.CloseSettings();
            var good=new CampaignSaveData{scene=CampaignFlow.Scenes[2],checkpointId=1,mineLift=true,workbench=true};
            Check(CampaignSave.Write(good)&&CampaignSave.TryRead(out var roundtrip)&&roundtrip.mineLift&&roundtrip.checkpointId==1,"Disk save roundtrip retains checkpoint and mechanism state");
            CampaignSave.Write(good);File.WriteAllText(CampaignSave.FilePath,"{broken");
            Check(CampaignSave.TryRead(out var backup)&&backup.mineLift,"Corrupt primary save recovers from previous complete file");
            CampaignSave.Clear();File.WriteAllText(CampaignSave.FilePath,"{\"version\":1,\"scene\":\"arbitrary.unity\"}");
            Check(!CampaignSave.TryRead(out _),"Unrecognized scene cannot become a load target");CampaignSave.Clear();
            CampaignSave.Write(good);menu.RefreshAvailability();menu.StartGame();
            Check(menu.NewGamePrompt&&!menu.IsLoading&&CampaignSave.TryRead(out _),"New Game asks before replacing an existing progress slot");
            menu.ConfirmNewGame();yield return WaitScene(CampaignFlow.Scenes[0]);
            for(int i=0;i<CampaignFlow.Scenes.Length;i++)
            {
                var session=FindAnyObjectByType<SeasideSession>();
                Check(session!=null&&session.gameObject.scene.path==CampaignFlow.Scenes[i],"Campaign order stage "+i);
                CheckTutorials(session,i);
                Check(CampaignSave.TryRead(out var entry)&&entry.scene==CampaignFlow.Scenes[i]&&!entry.ended,"Entry save targets the actual current stage");
                var cps=FindObjectsByType<SeasideCell>(FindObjectsInactive.Include).Where(c=>c.kind==CellKind.Checkpoint).OrderByDescending(c=>c.checkpointId).ToArray();
                Check(cps.Select(c=>c.checkpointId).Distinct().Count()==cps.Length,"Checkpoint IDs unique in "+CampaignFlow.CurrentTitle(CampaignFlow.Scenes[i]));
                if(i>=2)Check(cps.Length>=(i==2?7:i==3?6:5),"New map has route rest points beyond original markings");
                foreach(var cp in cps)
                {
                    session.Checkpoint(cp);session.Respawn();Physics2D.SyncTransforms();
                    Vector2 spawn=cp.respawnAnchor.position;
                    Check(Vector2.Distance(session.player.Body.position,spawn)<.01f,"Actual checkpoint restore: "+cp.name);
                    Check(!Physics2D.OverlapBoxAll(spawn,new Vector2(1.98f,1.98f),0,1<<7).Any(),"Respawn clears spikes: "+cp.name);
                    Check(CampaignSave.TryRead(out var saved)&&saved.checkpointId==cp.checkpointId&&saved.scene==CampaignFlow.Scenes[i],"Checkpoint contact commits disk progress: "+cp.name);
                    if(cp.excelAddress!=null&&cp.excelAddress.StartsWith("CampaignRest_"))
                    {
                        Check(!Physics2D.OverlapBoxAll(spawn,new Vector2(2.02f,2.02f),0,(1<<6)|(1<<7)).Any(),"Added rest has clear body: "+cp.name);
                        var support=Physics2D.Raycast(spawn+Vector2.down,Vector2.down,.2f,1<<6).collider;
                        Check(support!=null&&support.GetComponent<MovingSurface>()==null&&support.GetComponent<Expedition.ThermalSurface>()==null,"Added save rests on neutral static support: "+cp.name);
                        yield return new WaitForSeconds(.15f);Check(session.player.Grounded&&!session.Respawning,"Added checkpoint settles physically: "+cp.name);
                    }
                }
                // Legacy mechanics tests cover their physical goal triggers; here complete the real per-level narrative conditions too.
                if(i==0)
                {
                    var story=session.GetComponent<SeasideStory>();
                    Check(story!=null&&!story.BeginInteraction(),"Seaside lighthouse cannot be read remotely");
                    var finish=FindObjectsByType<SeasideCell>().Single(c=>c.kind==CellKind.Finish);
                    session.player.WarpForTraversal(finish.GetComponent<Collider2D>().bounds.center);
                    yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                    Check(!session.Won,"Touching seaside finish cannot skip its narrative");
                    Vector2 approach=new Vector2(story.lighthouse.bounds.min.x-1.1f,story.lighthouse.bounds.min.y+1.3f);
                    session.player.WarpForTraversal(approach);Physics2D.SyncTransforms();
                    Check(story.CanInteract&&story.BeginInteraction(),"E interaction available beside current seaside lighthouse artwork");
                    Check(story.Dialogue=="这里写着“这里曾是我最初的梦想”，似乎缺少什么"&&session.player.frozen&&!session.Won,"First seaside page freezes movement and defers progression");
                    session.ShowHelp=true;story.AdvanceDialogue();Check(story.Dialogue==story.pages[0],"Help overlay cannot advance seaside dialogue");session.ShowHelp=false;
                    session.Respawn();Check(story.Dialogue==null&&!session.player.frozen&&!session.Won,"Respawning cancels dialogue safely without completing or skipping it");
                    session.player.WarpForTraversal(approach);Check(story.BeginInteraction(),"Seaside story can be reopened after respawn");
                    yield return ContinueSameScene(session);session=FindAnyObjectByType<SeasideSession>();story=session.GetComponent<SeasideStory>();
                    Check(story.Dialogue==null&&!session.Won,"Continue restores safe checkpoint, not a frozen mid-dialogue state");
                    session.player.WarpForTraversal(approach);story.BeginInteraction();story.AdvanceDialogue();
                    Check(story.Dialogue=="获得传送：鼠标右键瞬移一段距离"&&!session.Won,"Second seaside page grants next-stage teleport before completion");
                    story.AdvanceDialogue();Check(session.Won,"Finishing seaside story triggers normal campaign transition");
                }
                else if(i==1)
                {
                    var wheel=FindAnyObjectByType<Forest.ForestWaterwheel>();foreach(var item in wheel.locks)item.Activate();
                    CampaignFlow.SaveCheckpoint(session);yield return ContinueSameScene(session);session=FindAnyObjectByType<SeasideSession>();
                    Check(FindAnyObjectByType<Forest.ForestWaterwheel>().UnlockCount==2,"Continue restores both Forest wheel locks");session.Complete();
                }
                else if(i==2)
                {
                    var mine=FindAnyObjectByType<Mine.MineLevel>();
                    var bench=mine.interactions.Single(p=>p.kind==Mine.MineInteractionKind.Workbench);
                    session.player.WarpForTraversal(bench.transform.position);Check(mine.BeginInteraction(bench),"Workbench interaction physically reachable at its map marker");
                    foreach(var expected in new[]{"开发进行还不到一半，团队的资金已经轮转不过来了，再这样下去项目会被叫停。","团队人员陆陆续续离职，剩下的人也不看好这个项目，我也得另外找份工作维持生计了。","团队解散，项目停工。"})
                    {Check(mine.Dialogue==expected,"Workbench PDF page: "+expected);mine.AdvanceDialogue();}
                    var liftSwitch=mine.interactions.Single(p=>p.kind==Mine.MineInteractionKind.LiftSwitch);
                    session.player.WarpForTraversal(liftSwitch.transform.position);Check(mine.BeginInteraction(liftSwitch)&&mine.Dialogue=="矿洞的电梯重新启动。","Switch displays its PDF line before activation");mine.AdvanceDialogue();
                    CampaignFlow.SaveCheckpoint(session);
                    Check(CampaignSave.TryRead(out var state)&&state.mineLift&&state.workbench,"Mine checkpoint captures activated lift and workbench");
                    yield return ContinueSameScene(session);
                    session=FindAnyObjectByType<SeasideSession>();mine=FindAnyObjectByType<Mine.MineLevel>();
                    Check(mine.lift.Activated&&mine.WorkbenchRead,"Menu Continue restores Mine mechanism progress");
                    var relic=mine.interactions.Single(p=>p.completesLevel);session.player.WarpForTraversal(relic.transform.position);
                    Check(mine.BeginInteraction(relic)&&mine.Dialogue=="获得：比特币。","Mine relic displays first PDF page");mine.AdvanceDialogue();
                    Check(mine.Dialogue=="当初的人们没有发现它的价值，直到它求而不得。"&&!session.Won,"Mine relic displays second page before leaving");mine.AdvanceDialogue();
                }
                else if(i==3)
                {
                    var snow=FindAnyObjectByType<Snow.SnowLevel>();session.player.WarpForTraversal(snow.goal.transform.position);
                    Check(snow.BeginInteraction()&&snow.Dialogue=="一份不全的游戏 demo 策划案。","Snow fragment first PDF page");snow.AdvanceDialogue();
                    Check(snow.Dialogue=="未焚烧干净的残章中还有着对你的设定描述。","Snow fragment second PDF page");snow.AdvanceDialogue();
                    Check(snow.Choosing&&!session.Won,"Snow choices wait for explicit selection");snow.Choose(1);
                    Check(snow.Dialogue=="这个东西也许还有用。"&&!session.Won,"Carry choice displays follow-up before continuing");snow.AdvanceDialogue();
                }
                else if(i==4)
                {
                    var tower=FindAnyObjectByType<Lighthouse.LighthouseLevel>();session.player.WarpForTraversal(tower.goal.transform.position);
                    Check(tower.BeginInteraction(),"Tower actual final lamp interaction");tower.AdvanceDialogue();tower.AdvanceDialogue();
                }
                else session.Complete();
                if(i<4)
                {
                    Check(CampaignSave.TryRead(out var next)&&next.scene==CampaignFlow.Scenes[i+1]&&next.checkpointId==0,"Completion saves next entrance before automatic transition");
                    yield return WaitScene(CampaignFlow.Scenes[i+1]);
                }
                else
                {
                    Check(session.Won&&CampaignSave.TryRead(out var final)&&final.ended,"Final stage ends campaign without trying a sixth stage");
                    yield return new WaitForSecondsRealtime(1);Check(SceneManager.GetActiveScene().path==CampaignFlow.Scenes[4],"Final ending remains on tower");
                }
            }
            yield return Load(CampaignFlow.Scenes[3]);
            var ending=FindAnyObjectByType<Snow.SnowLevel>();ending.session.player.WarpForTraversal(ending.goal.transform.position);
            ending.BeginInteraction();ending.AdvanceDialogue();ending.AdvanceDialogue();ending.Choose(0);
            yield return new WaitForSecondsRealtime(1.2f);
            Check(ending.session.Won&&SceneManager.GetActiveScene().path==CampaignFlow.Scenes[3]&&!CampaignFlow.HasSave,"Stay choice remains an ending and disables continuing past it");
            Finish(true);
        }
        void CheckTutorials(SeasideSession session,int stage)
        {
            var tutorials=session.GetComponent<LevelTutorials>();
            if(tutorials==null){Check(stage==2||stage==4,"No unrelated tutorial areas added to stage "+stage);return;}
            Vector2 original=session.player.Body.position;
            foreach(var zone in tutorials.zones)
            {
                session.player.WarpForTraversal(zone.GetComponent<Collider2D>().bounds.center);Physics2D.SyncTransforms();
                Check(tutorials.Current==zone.text,"Real player position enters tutorial: "+zone.name);
            }
            session.player.WarpForTraversal(new Vector2(-50,-50));Check(tutorials.Current==null,"Leaving all areas clears top tutorial");
            session.player.WarpForTraversal(original);
        }
        IEnumerator ContinueSameScene(SeasideSession session)
        {
            int expected=session.CheckpointId;string path=session.gameObject.scene.path;
            CampaignFlow.ReturnToMenu(session);yield return WaitScene(CampaignFlow.MenuScene);
            var menu=FindAnyObjectByType<StartGameMenu>();Check(menu.CanContinue&&menu.buttons[1].Available,"Returning to menu enables Continue");menu.ContinueGame();
            yield return WaitScene(path);Check(FindAnyObjectByType<SeasideSession>().CheckpointId==expected,"Continue resumes last checkpoint, not entrance");
        }
        IEnumerator Probe(string phase)
        {
            if(phase=="write")
            {
                CampaignSave.Clear();yield return Load(CampaignFlow.Scenes[2]);
                var mine=FindAnyObjectByType<Mine.MineLevel>();var cp=FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).Last();
                mine.RestoreProgress(true,true);mine.session.Checkpoint(cp);mine.session.Respawn();CampaignFlow.SaveCheckpoint(mine.session);
                Check(CampaignSave.TryRead(out var data)&&data.checkpointId==cp.checkpointId&&data.mineLift,"First process writes a durable Mine checkpoint");Finish(true);
            }
            else
            {
                var menu=FindAnyObjectByType<StartGameMenu>();Check(menu.CanContinue,"Fresh process discovers previous process save");
                CampaignSave.TryRead(out var data);menu.ContinueGame();yield return WaitScene(data.scene);
                var mine=FindAnyObjectByType<Mine.MineLevel>();Check(mine.session.CheckpointId==data.checkpointId&&mine.lift.Activated&&mine.WorkbenchRead,"Fresh process restores checkpoint and lift/workbench");Finish(true);
            }
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
