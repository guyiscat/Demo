using System;
using UnityEngine;
namespace Seaside
{
    public enum GameAction { Left,Right,Jump,Interact,Dash,Teleport,Ride,Protection,Pause }
    public static class GameBindings
    {
        static readonly KeyCode[] defaults={KeyCode.A,KeyCode.D,KeyCode.Space,KeyCode.E,KeyCode.Mouse0,KeyCode.Mouse1,KeyCode.W,KeyCode.S,KeyCode.Escape};
        static KeyCode[] keys;static int blockedFrame=-1;
        static readonly System.Collections.Generic.HashSet<KeyCode> releaseRequired=new System.Collections.Generic.HashSet<KeyCode>();
        public static int InputEpoch{get;private set;}
        public static int Revision{get;private set;}
        static void Ensure(){if(keys!=null)return;keys=(KeyCode[])defaults.Clone();for(int i=0;i<keys.Length;i++){var value=(KeyCode)PlayerPrefs.GetInt("Seaside.Binding."+i,(int)defaults[i]);if(Enum.IsDefined(typeof(KeyCode),value))keys[i]=value;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetRuntime(){keys=null;blockedFrame=-1;Revision=0;InputEpoch=0;releaseRequired.Clear();}
        public static KeyCode Key(GameAction action){Ensure();return keys[(int)action];}
        static bool CanRead(GameAction action)
        {
            var key=Key(action);
            if(releaseRequired.Contains(key)){if(Input.GetKey(key))return false;releaseRequired.Remove(key);}
            return Time.frameCount>blockedFrame&&!GameSettingsScreen.Opened&&!SaveSlotScreen.Opened;
        }
        public static bool Held(GameAction action)=>CanRead(action)&&Input.GetKey(Key(action));
        public static bool Pressed(GameAction action)=>CanRead(action)&&Input.GetKeyDown(Key(action));
        public static void BlockFrame()
        {
            // UI can run after Update but before the next physics tick. Invalidate queued
            // edges as well as the current click, and require held activation keys to lift.
            blockedFrame=Time.frameCount+1;InputEpoch++;Ensure();
            foreach(var key in keys)if(Input.GetKey(key))releaseRequired.Add(key);
        }
        public static bool Set(GameAction action,KeyCode key,out string message)
        {
            Ensure();message=null;
            if(key==KeyCode.None||key==KeyCode.F5||key==KeyCode.R||key==KeyCode.H||key==KeyCode.B||key==KeyCode.Tab||key==KeyCode.LeftControl||key==KeyCode.RightControl||key>=KeyCode.Alpha1&&key<=KeyCode.Alpha5)
            {message="这个按键用于固定操作或剧情选择，请选择其他按键。";return false;}
            int index=Array.IndexOf(keys,key);var previous=keys[(int)action];if(index>=0)keys[index]=previous;keys[(int)action]=key;
            for(int i=0;i<keys.Length;i++)PlayerPrefs.SetInt("Seaside.Binding."+i,(int)keys[i]);PlayerPrefs.Save();Revision++;return true;
        }
        public static void ResetDefaults(){keys=(KeyCode[])defaults.Clone();for(int i=0;i<keys.Length;i++)PlayerPrefs.DeleteKey("Seaside.Binding."+i);PlayerPrefs.Save();Revision++;}
        public static string Label(GameAction action)=>Label(Key(action));
        public static string Label(KeyCode key)=>key==KeyCode.Space?"空格":key==KeyCode.Escape?"Esc":key==KeyCode.Mouse0?"鼠标左键":key==KeyCode.Mouse1?"鼠标右键":key==KeyCode.Mouse2?"鼠标中键":key.ToString();
        public static string Format(string text)
        {
            if(string.IsNullOrEmpty(text))return text;
            return text.Replace("A/D","{LEFT}/{RIGHT}").Replace("鼠标左键","{DASH}").Replace("左键","{DASH}").Replace("鼠标右键","{TELEPORT}").Replace("右键","{TELEPORT}")
                .Replace("空格","{JUMP}").Replace("按 E","按 {INTERACT}").Replace("按E","按{INTERACT}").Replace("/ E","/ {INTERACT}").Replace("E交互","{INTERACT}交互").Replace("E 交互","{INTERACT} 交互")
                .Replace("S 防寒","{PROTECTION} 防寒").Replace("S键","{PROTECTION}键").Replace("按 S","按 {PROTECTION}").Replace("W 滑索","{RIDE} 滑索").Replace("W 抓","{RIDE} 抓").Replace("按W","按{RIDE}").Replace("Esc 暂停","{PAUSE} 暂停")
                .Replace("{LEFT}",Label(GameAction.Left)).Replace("{RIGHT}",Label(GameAction.Right)).Replace("{DASH}",Label(GameAction.Dash)).Replace("{TELEPORT}",Label(GameAction.Teleport)).Replace("{JUMP}",Label(GameAction.Jump)).Replace("{INTERACT}",Label(GameAction.Interact)).Replace("{PROTECTION}",Label(GameAction.Protection)).Replace("{RIDE}",Label(GameAction.Ride)).Replace("{PAUSE}",Label(GameAction.Pause));
        }
    }
}
