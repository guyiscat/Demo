using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forest;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class NewArtworkRuntimeChecks : MonoBehaviour
    {
        readonly List<string> lines=new List<string>();string folder;bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-newArtChecks")){var go=new GameObject("NewArtworkRuntimeChecks");DontDestroyOnLoad(go);go.AddComponent<NewArtworkRuntimeChecks>();}}
        string Arg(string flag){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,flag);return i>=0&&i+1<a.Length?a[i+1]:null;}
        void Check(bool ok,string label){lines.Add((ok?"PASS ":"FAIL ")+label);Debug.Log(lines.Last());if(!ok)throw new InvalidOperationException(label);}
        void Finish(bool ok){if(finished)return;finished=true;File.WriteAllLines(Path.Combine(folder,"new-art-runtime-checks.txt"),lines);Application.Quit(ok?0:1);}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){lines.Add("FAIL "+message+" "+stack);Finish(false);}}
        SeasideSession Session=>FindAnyObjectByType<SeasideSession>();
        IEnumerator Wait(string path)
        {
            float until=Time.realtimeSinceStartup+30;
            while((SceneManager.GetActiveScene().path!=path||Session==null||WorldTransition.Active||Session.GetComponent<CampaignCheckpointPersistence>()?.Ready!=true)&&Time.realtimeSinceStartup<until)yield return null;
            yield return null;yield return null;Check(Session!=null&&Session.gameObject.scene.path==path&&!WorldTransition.Active,"Region ready "+CampaignFlow.RegionLabel(path));
        }
        IEnumerator Shot(string name)
        {if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;}
        IEnumerator Start()
        {
            folder=Arg("-newArtReport")??Application.temporaryCachePath;Directory.CreateDirectory(folder);Application.logMessageReceived+=OnLog;
            Check(!string.IsNullOrEmpty(Arg("-campaignSavePath")),"Requires isolated test save path");
            foreach(var name in new[]{"suspend_bg","suspend_continue_on","suspend_continue_off","suspend_set_on","suspend_save_on"})Check(Resources.Load<Texture2D>("Suspend/"+name)!=null,"Suspend image included in actual player "+name);
            Check(GameMediaSettings.Current.jumpRise.name=="JumpNew1"&&GameMediaSettings.Current.jumpFall.name=="JumpNew4"&&GameMediaSettings.Current.jumpTransition!=null,"Updated jump JPEG poses assigned to shared actor art");
            Check(GameMediaSettings.Current.jumpRise.pixelsPerUnit==GameMediaSettings.Current.idle.pixelsPerUnit,"New jump sprites retain the existing actor world scale");
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[1]);yield return Wait(CampaignFlow.Scenes[1]);
            var forest=FindAnyObjectByType<ForestLevel>();forest.player.manualInput=true;forest.player.motor.ResetAt(forest.session.startPoint);forest.Intro.Cancel();yield return null;
            var art=forest.GetComponent<ForestArtwork>();
            int hazards=FindObjectsByType<SeasideCell>().Count(c=>c.kind==CellKind.Spike);
            Check(art!=null&&art.CoveredHazards==hazards&&art.ThornGroups>0,"All authored Forest active hazards covered by reused thorn art: "+hazards);
            Check(GameObject.Find("ForestArtRoot").GetComponentsInChildren<Collider2D>().Length==0,"Forest artwork adds no gameplay collision");
            Check(GameObject.Find("WheelArtwork")!=null&&GameObject.Find("WaterfallArtwork")!=null&&GameObject.Find("ForestBackground")!=null,"Layered forest background, rotating wheel and waterfall loaded");
            forest.cameraRig.overview=true;forest.cameraRig.Snap();forest.player.waterfall.FullMapReveal=true;yield return Shot("forest-overview");
            forest.cameraRig.overview=false;forest.cameraRig.Snap();yield return Shot("forest-entry");
            forest.player.motor.WarpForTraversal(forest.wheel.center);forest.cameraRig.Snap();yield return Shot("forest-wheel");
            Session.Pause();yield return Shot("suspend-new");
            bool original=GameSystemSettings.Current.useSuspendArtwork;GameSystemSettings.Current.useSuspendArtwork=false;yield return Shot("suspend-previous");GameSystemSettings.Current.useSuspendArtwork=original;Session.Resume();
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[5]);yield return Wait(CampaignFlow.Scenes[5]);
            var villageArt=Session.GetComponent<Village.VillageArtwork>();
            Check(villageArt!=null&&villageArt.CoveredGround==FindObjectsByType<SeasideCell>().Count(c=>c.kind==CellKind.Ground),"Village sand art follows every saved ground collider");
            Check(villageArt.ArtRoot.GetComponentsInChildren<Collider2D>().Length==0,"Village artwork adds no collision or trigger");
            var villageActor=Session.player.GetComponent<ForestPlayer>();villageActor.manualInput=true;Session.player.ResetAt(Session.startPoint);
            var villageCamera=FindAnyObjectByType<SeasideCamera>();villageCamera.overview=true;villageCamera.Snap();yield return Shot("village-overview");villageCamera.overview=false;
            foreach(var view in new[]{(new Vector2(17,5.05f),"village-mine-entrance"),(new Vector2(79,5.05f),"village-robot")})
            {Session.player.WarpForTraversal(view.Item1);Session.player.frozen=true;villageCamera.Snap();yield return Shot(view.Item2);Session.player.frozen=false;}
            Check(TransferArtwork.RegionCount==5&&!TravelPointMenu.AllowsRegion(CampaignFlow.Scenes[4]),"Five travel regions exclude Lighthouse");
            for(int index=0;index<TransferArtwork.RegionCount;index++)
            {
                string path=TransferArtwork.RegionAt(index);
                yield return SceneManager.LoadSceneAsync(path);yield return Wait(path);var session=Session;session.player.ResetAt(session.startPoint);
                var point=TravelPoint.For(session);Check(point!=null,"Authored portal exists in "+CampaignFlow.RegionLabel(path));
                CampaignFlow.Progress.explored=CampaignFlow.Scenes;CampaignFlow.Progress.snowContinue=true;
                string target=path==CampaignFlow.Scenes[5]?CampaignFlow.Scenes[0]:CampaignFlow.Scenes[5];
                session.player.WarpForTraversal(point.transform.position);Physics2D.SyncTransforms();session.GetComponent<TravelPointMenu>().Open(point);
                if(index==0)
                {
                    CampaignFlow.Progress.explored=new[]{path};
                    Check(!session.GetComponent<TravelPointMenu>().Select(target)&&session.GetComponent<TravelPointMenu>().Opened,"Unexplored travel destination cannot be selected and menu stays open");
                    yield return Shot("transfer-locked");CampaignFlow.Progress.explored=CampaignFlow.Scenes;
                }
                Check(!session.GetComponent<TravelPointMenu>().Select(CampaignFlow.Scenes[4]),"Menu API rejects Lighthouse even when explored and unlocked");
                if(index==0)yield return Shot("transfer-five-regions");
                Check(session.GetComponent<TravelPointMenu>().Select(target),"Portal requests explored destination "+CampaignFlow.RegionLabel(path));
                bool noWalk=true;while(WorldTransition.Active){var motor=FindAnyObjectByType<SeasideMotor>();if(motor!=null&&motor.GetComponent<RegionArrival>()?.Walking==true)noWalk=false;yield return null;}
                yield return Wait(target);point=TravelPoint.For(Session);
                Check(Session.RegionArrivalKey=="Portal"&&Vector2.Distance(Session.player.Body.position,point.ArrivalPosition())<.15f&&point.Nearby&&noWalk,"Portal lands directly at target marker, no entry walk: "+CampaignFlow.RegionLabel(path));
                Check(ManualSaveSafety.SafeGround(Session.player,Session.player.Body.position),"Destination portal stands on safe fixed ground: "+CampaignFlow.RegionLabel(target));
                var returnMenu=Session.GetComponent<TravelPointMenu>();returnMenu.Open(point);Check(returnMenu.Select(path),"Portal allows return trip to "+CampaignFlow.RegionLabel(path));
                yield return Wait(path);point=TravelPoint.For(Session);
                Check(Session.RegionArrivalKey=="Portal"&&Vector2.Distance(Session.player.Body.position,point.ArrivalPosition())<.15f&&point.Nearby,"Return trip arrives at portal in "+CampaignFlow.RegionLabel(path)+" player="+Session.player.Body.position+" point="+point.transform.position);
                Check(ManualSaveSafety.SafeGround(Session.player,Session.player.Body.position),"Portal safe fixed support in "+CampaignFlow.RegionLabel(path));
            }
            // Real shared actor aura: locked until the village core, tracks S mode,
            // survives death and a saved/revisited region without changing physics.
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[3]);yield return Wait(CampaignFlow.Scenes[3]);
            var thermal=Session.player.GetComponent<Expedition.ThermalTraversal>();var aura=Session.player.GetComponent<ProtectionAura>();
            Session.player.GetComponent<ForestPlayer>().manualInput=true;Session.player.ResetAt(Session.startPoint);
            CampaignFlow.Progress.coreObtained=false;aura.Refresh();Check(!aura.Glow.enabled,"No protection glow before acquiring village core");
            CampaignFlow.GrantCore(Session);if(thermal.Mode!=Expedition.ProtectionMode.Cold)thermal.TrySwitchMode();aura.Refresh();
            Check(aura.Glow.enabled&&aura.Glow.color.b>aura.Glow.color.r&&aura.Glow.sortingOrder<Session.player.GetComponent<SpriteRenderer>().sortingOrder,"Cold core glow is blue and behind actor");
            var size=Session.player.BodySize;FindAnyObjectByType<SeasideCamera>()?.Snap();yield return Shot("protection-blue");
            Check(thermal.TrySwitchMode(),"Protection input switches mode with core");aura.Refresh();
            Check(aura.Glow.enabled&&aura.Glow.color.r>aura.Glow.color.b&&Session.player.BodySize==size,"Heat glow is red without changing collision size");yield return Shot("protection-red");
            Session.Pause();Check(!thermal.TrySwitchMode(),"Paused S cannot change protection mode");aura.Refresh();Check(aura.DisplayedMode==Expedition.ProtectionMode.Heat,"Paused glow keeps active heat mode");Session.Resume();
            Session.Die();aura.Refresh();Check(!aura.Glow.enabled,"Death hides protection glow");Session.Respawn();aura.Refresh();
            Check(thermal.Mode==Expedition.ProtectionMode.Heat&&aura.Glow.enabled&&aura.Glow.color.r>aura.Glow.color.b,"Respawn keeps selected red heat glow");
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[4]);yield return Wait(CampaignFlow.Scenes[4]);
            var lighthousePoint=TravelPoint.For(Session);Session.player.ResetAt(lighthousePoint.transform.position);Physics2D.SyncTransforms();Session.GetComponent<TravelPointMenu>().Open(lighthousePoint);
            Check(!Session.GetComponent<TravelPointMenu>().Opened&&!lighthousePoint.Usable,"Lighthouse cannot open regional teleport during chase");
            aura=Session.player.GetComponent<ProtectionAura>();aura.Refresh();Check(aura.Glow.enabled&&aura.DisplayedMode==Expedition.ProtectionMode.Heat,"Protection glow carries heat mode into Lighthouse");
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[5]);yield return Wait(CampaignFlow.Scenes[5]);aura=Session.player.GetComponent<ProtectionAura>();aura.Refresh();
            Check(aura.Glow.enabled&&aura.DisplayedMode==Expedition.ProtectionMode.Heat,"Acquired protection glow stays consistent when revisiting a non-thermal region");
            // Save restore calls the same story restore used by Continue/revisit.
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[0]);yield return Wait(CampaignFlow.Scenes[0]);var story=Session.GetComponent<SeasideStory>();story.RestoreRead(true);yield return null;
            var actor=Session.player.GetComponent<ForestPlayer>();actor.manualInput=true;Session.player.ResetAt(new Vector2(-90,50));actor.Tick(.01f,new ForestInput{teleportDown=true,movement=new SeasideInput{aim=Vector2.right}});
            Check(actor.BeaconActive&&!actor.beaconTrail.enabled,"Restored seaside skill works and has no red line");actor.Tick(.02f,default);actor.Tick(.01f,new ForestInput{teleportDown=true});
            Check(actor.Teleports==1,"Restored seaside skill actually teleports");Session.Respawn();Check(actor.TeleportCharged&&!actor.BeaconActive,"Death/respawn preserves gained skill and restores its charge");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                actor.manualInput=true;actor.CancelSpecial();Session.player.ResetAt(new Vector2(83.3f,69));Session.player.Body.simulated=false;
                foreach(var pose in new[]{(18f,"jump-rise"),(3f,"jump-apex"),(0f,"jump-transition"),(-15f,"jump-fall")})
                {Session.player.Body.linearVelocity=Vector2.up*pose.Item1;FindAnyObjectByType<SeasideCamera>()?.Snap();yield return Shot(pose.Item2);}
                Session.player.Body.simulated=true;
            }
            Finish(true);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
