using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    // Opt-in screenshots of the actual HUD and artwork; never loads/writes a player save.
    public sealed class LevelCopyVisualChecks : MonoBehaviour
    {
        string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(!Environment.GetCommandLineArgs().Contains("-copyVisual"))return;
            var go=new GameObject("LevelCopyVisualChecks");DontDestroyOnLoad(go);go.AddComponent<LevelCopyVisualChecks>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int a=Array.IndexOf(args,"-copyDir");
            folder=a>=0&&a+1<args.Length?args[a+1]:Path.Combine(Application.temporaryCachePath,"LevelCopyVisuals");Directory.CreateDirectory(folder);
            for(int stage=0;stage<4;stage++)
            {
                yield return SceneManager.LoadSceneAsync(CampaignFlow.Scenes[stage]);yield return null;yield return null;
                var session=FindAnyObjectByType<SeasideSession>();var motor=session.player;
                var traversal=motor.GetComponent<Forest.ForestPlayer>();if(traversal!=null)traversal.manualInput=true;
                motor.externalInput=true;motor.frozen=true;
                var camera=FindAnyObjectByType<SeasideCamera>();
                var intro=FindAnyObjectByType<Forest.ForestIntroCamera>();intro?.Cancel(true);
                var tutorials=session.GetComponent<LevelTutorials>();
                if(tutorials!=null)foreach(var zone in tutorials.zones)
                {
                    var cp=FindObjectsByType<SeasideCell>().FirstOrDefault(c=>c.kind==CellKind.Checkpoint&&zone.Contains(c.respawnAnchor.position));
                    motor.WarpForTraversal(cp!=null?(Vector2)cp.respawnAnchor.position:(Vector2)zone.GetComponent<Collider2D>().bounds.center);
                    camera.Snap();yield return Capture(stage+"-"+zone.name);
                }
                if(stage==0)
                {
                    var story=session.GetComponent<SeasideStory>();
                    motor.WarpForTraversal(new Vector2(story.lighthouse.bounds.min.x-1.1f,story.lighthouse.bounds.min.y+1.3f));camera.Snap();
                    yield return Capture("seaside-lighthouse-prompt");
                    if(args.Contains("-copyHold"))yield break;
                    story.BeginInteraction();yield return Capture("seaside-story-1");story.AdvanceDialogue();yield return Capture("seaside-story-2");
                }
                if(stage==2)
                {
                    var level=FindAnyObjectByType<Mine.MineLevel>();var bench=level.interactions.Single(x=>x.kind==Mine.MineInteractionKind.Workbench);
                    motor.WarpForTraversal(bench.transform.position);camera.Snap();level.BeginInteraction(bench);
                    yield return Capture("mine-workbench-1");level.AdvanceDialogue();yield return Capture("mine-workbench-2");
                }
                if(stage==3)
                {
                    var level=FindAnyObjectByType<Snow.SnowLevel>();motor.WarpForTraversal(level.goal.transform.position);camera.Snap();level.BeginInteraction();
                    yield return Capture("snow-fragment");level.AdvanceDialogue();level.AdvanceDialogue();yield return Capture("snow-choices");
                }
            }
            Debug.Log("LEVEL_COPY_VISUAL_PASS "+folder);Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.2f);
        }
    }
}
