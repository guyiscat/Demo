using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Expedition
{
    // Deterministic real-player screenshots; not enabled in normal play.
    public sealed class ThermalVisualChecks : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForSeconds(.5f);var level=GetComponent<ThermalLevel>();
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-visualDir");
            string folder=i>=0&&i+1<args.Length?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
            level.player.manualInput=true;level.player.motor.frozen=true;
            int hold=Array.IndexOf(args,"-visualHold");
            if(hold>=0&&hold+1<args.Length)
            {
                string stage=args[hold+1];
                if(stage=="help")level.session.ShowHelp=true;
                else if(stage=="overview")level.cameraRig.overview=true;
                else if(stage=="choices"||stage=="completion")
                {
                    level.player.motor.WarpForTraversal(level.goal.transform.position);level.cameraRig.Snap();level.player.motor.frozen=false;level.BeginInteraction();
                    level.AdvanceDialogue();level.AdvanceDialogue();
                    if(stage=="completion"&&level is Snow.SnowLevel heldSnow){heldSnow.Choose(1);heldSnow.AdvanceDialogue();}
                }
                // Remain in this deterministic state until the review window is closed.
                yield break;
            }
            // Leave time to foreground the real player window before capture.
            yield return new WaitForSeconds(12);
            yield return Capture(folder,"entry");
            level.session.ShowHelp=true;yield return Capture(folder,"help");level.session.ShowHelp=false;
            level.cameraRig.overview=true;yield return new WaitForSeconds(1);yield return Capture(folder,"overview");level.cameraRig.overview=false;
            level.player.motor.WarpForTraversal(level.goal.transform.position);level.cameraRig.Snap();
            level.player.motor.frozen=false;level.BeginInteraction();
            if(level is Snow.SnowLevel snow){snow.AdvanceDialogue();snow.AdvanceDialogue();yield return Capture(folder,"choices");snow.Choose(1);snow.AdvanceDialogue();}
            else{level.AdvanceDialogue();level.AdvanceDialogue();}
            yield return Capture(folder,"completion");Application.Quit(0);
        }
        IEnumerator Capture(string folder,string name)
        {
            yield return new WaitForSeconds(4);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSeconds(1);
        }
    }
}
