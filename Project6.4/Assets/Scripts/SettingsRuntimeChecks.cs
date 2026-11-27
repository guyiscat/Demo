using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Seaside
{
    public sealed class SettingsRuntimeChecks : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-settingsChecks"))new GameObject("SettingsRuntimeChecks").AddComponent<SettingsRuntimeChecks>();}
        IEnumerator Start()
        {
            yield return null;var a=Environment.GetCommandLineArgs();string folder=a[Array.IndexOf(a,"-settingsReport")+1];Directory.CreateDirectory(folder);
            var keys=Enum.GetValues(typeof(GameAction)).Cast<GameAction>().Select(GameBindings.Key).ToArray();var hadKeys=Enumerable.Range(0,keys.Length).Select(i=>PlayerPrefs.HasKey("Seaside.Binding."+i)).ToArray();bool hadBrightness=PlayerPrefs.HasKey("Seaside.Brightness");float brightness=DisplayBrightness.Value;float volume=AudioListener.volume;bool hadVolume=PlayerPrefs.HasKey("Seaside.MasterVolume");float storedVolume=PlayerPrefs.GetFloat("Seaside.MasterVolume",1);
            var lines=new System.Collections.Generic.List<string>();bool failed=false;
            Action<bool,string> check=(ok,label)=>{failed|=!ok;lines.Add((ok?"PASS ":"FAIL ")+label);};
            var menu=FindAnyObjectByType<StartGameMenu>();menu.OpenSettings();yield return null;
            var art=MenuArtwork.Current;check(art!=null&&art.controls!=null&&art.display!=null,"Imported settings backgrounds load in actual player");Debug.Log("SETTINGS_ART_PROBE "+(art==null?"missing config":art.name+" controls="+art.controls+" display="+art.display));
            var brightnessMaterial=Resources.Load<Material>("UI/DisplayBrightness");check(brightnessMaterial!=null&&brightnessMaterial.shader!=null,"Brightness shader retained in player");
            check(menu.SettingsOpen&&GameSettingsScreen.Opened,"Settings opens dedicated reference-art UI");
            var settings=GameSettingsScreen.Current;check(settings.Tab==0,"Controls tab opens first");
            GameBindings.ResetDefaults();check(GameBindings.Set(GameAction.Jump,KeyCode.J,out var message)&&GameBindings.Key(GameAction.Jump)==KeyCode.J,"Jump binding saves a real alternate key");
            check(GameBindings.Set(GameAction.Left,KeyCode.D,out message)&&GameBindings.Key(GameAction.Right)==KeyCode.A,"Duplicate binding swaps rather than disables an action");
            check(!GameBindings.Set(GameAction.Jump,KeyCode.F5,out message),"Fixed manual-save shortcut cannot be reassigned");
            check(GameBindings.Format("A/D 空格 左键 按 E").Contains("J"),"Gameplay HUD reflects current bindings");settings.ResetPage();
            GameBindings.Set(GameAction.Left,KeyCode.Space,out message);check(GameBindings.Format("A/D 移动，空格跳跃")=="空格/D 移动，A跳跃","Swapping movement and jump keys keeps each action hint correct");settings.ResetPage();
            check(GameBindings.Key(GameAction.Jump)==KeyCode.Space&&GameBindings.Key(GameAction.Left)==KeyCode.A,"Controls reset returns all default keys");
            yield return Capture(folder,"controls");settings.SelectTab(1);DisplayBrightness.Set(.75f);menu.SetMasterVolume(.35f);
            check(settings.Tab==1&&Mathf.Abs(DisplayBrightness.Value-.75f)<.001f&&Mathf.Abs(AudioListener.volume-.35f)<.001f,"Display page applies brightness and master audio");
            yield return Capture(folder,"display");settings.ResetPage();check(DisplayBrightness.Value==.5f&&AudioListener.volume==1,"Display reset restores neutral brightness and full master audio");
            GameSettingsScreen.Close();yield return null;check(!menu.SettingsOpen&&!GameSettingsScreen.Opened,"Return closes modal and restores menu input");
            for(int i=0;i<keys.Length;i++){if(hadKeys[i])PlayerPrefs.SetInt("Seaside.Binding."+i,(int)keys[i]);else PlayerPrefs.DeleteKey("Seaside.Binding."+i);}if(hadBrightness)PlayerPrefs.SetFloat("Seaside.Brightness",brightness);else PlayerPrefs.DeleteKey("Seaside.Brightness");if(hadVolume)PlayerPrefs.SetFloat("Seaside.MasterVolume",storedVolume);else PlayerPrefs.DeleteKey("Seaside.MasterVolume");PlayerPrefs.Save();AudioListener.volume=volume;
            File.WriteAllLines(Path.Combine(folder,"settings-runtime-checks.txt"),lines);Application.Quit(failed?1:0);
        }
        IEnumerator Capture(string folder,string name){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;}
    }
}
