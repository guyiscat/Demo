using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    public sealed class WorldVisualChecks : MonoBehaviour
    {
        string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(!Environment.GetCommandLineArgs().Contains("-worldVisual"))return;
            CampaignFlow.ResetVisualProgress();var go=new GameObject("WorldVisualChecks");DontDestroyOnLoad(go);go.AddComponent<WorldVisualChecks>();
        }
        string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:null;}
        IEnumerator Load(int index)
        {
            yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[index]);yield return null;yield return null;
            var session=FindAnyObjectByType<SeasideSession>();var actor=session.player.GetComponent<Forest.ForestPlayer>();if(actor!=null)actor.manualInput=true;
            session.player.externalInput=true;session.player.frozen=true;FindAnyObjectByType<Forest.ForestIntroCamera>()?.Cancel(true);
            CampaignFlow.Progress.explored=CampaignFlow.Scenes.ToArray();
        }
        void Place(Vector2 p){var motor=FindAnyObjectByType<SeasideMotor>();motor.WarpForTraversal(p);motor.frozen=true;Physics2D.SyncTransforms();FindAnyObjectByType<SeasideCamera>().Snap();}
        IEnumerator Start()
        {
            directory=Arg("-visualDir")??Path.Combine(Application.temporaryCachePath,"WorldVisuals");Directory.CreateDirectory(directory);
            yield return Load(5);yield return Capture("village-map");
            var session=FindAnyObjectByType<SeasideSession>();var point=FindAnyObjectByType<TravelPoint>();session.player.frozen=false;Place(point.transform.position);session.player.frozen=false;session.GetComponent<TravelPointMenu>().Open(point);yield return Capture("travel-menu");
            if(Arg("-visualHold")=="travel-menu")yield break;
            session.GetComponent<TravelPointMenu>().Close();
            yield return Load(0);yield return Capture("seaside-entry-portal");
            yield return Load(2);yield return Capture("mine-darkness-entry");
            var level=FindAnyObjectByType<Mine.MineLevel>();level.lift.manualSimulation=level.deepLift.manualSimulation=true;
            Place(new Vector2(11,217.5f));yield return Capture("mine-purple-switch");level.gate.Open();Place(new Vector2(90,165));yield return Capture("mine-open-gate");
            Place(new Vector2(120,35));yield return Capture("mine-corruption");
            var relic=level.interactions.Single(i=>i.completesLevel);Place(relic.transform.position);yield return Capture("mine-gold-item");level.player.motor.frozen=false;level.BeginInteraction(relic);level.AdvanceDialogue();level.AdvanceDialogue();level.player.motor.frozen=true;
            yield return Capture("mine-corruption-cleared");
            if(Arg("-visualHold")=="mine-darkness")yield break;
            yield return Load(3);yield return Capture("snow-updated-entry");
            Debug.Log("WORLD_VISUAL_FRAMES_COMPLETE "+directory);Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return new WaitForSecondsRealtime(.2f);
        }
    }
}
