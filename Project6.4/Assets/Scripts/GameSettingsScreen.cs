using System;
using UnityEngine;
namespace Seaside
{
    public sealed class GameSettingsScreen : MonoBehaviour
    {
        static GameSettingsScreen current;Action closed;GameAction? capture;int captureFrame;int tab;string message;
        GUIStyle text,tabText,keyText,link;Texture2D art;
        public static bool Opened=>current!=null;
        public static GameSettingsScreen Current=>current;
        public int Tab=>tab;
        public GameAction? Capturing=>capture;
        public static void Open(Action onClose)
        {if(Opened)return;var go=new GameObject("GameSettingsScreen");current=go.AddComponent<GameSettingsScreen>();current.closed=onClose;GameBindings.BlockFrame();}
        public static void Close(){if(current==null)return;var previous=current;current=null;GameBindings.BlockFrame();previous.closed?.Invoke();Destroy(previous.gameObject);}
        public void SelectTab(int index){tab=Mathf.Clamp(index,0,1);capture=null;message=null;}
        public void BeginBinding(GameAction action){capture=action;captureFrame=Time.frameCount;message="请按新按键或鼠标按钮，Esc 取消；重复按键会交换。";}
        void Update(){if(Input.GetKeyDown(KeyCode.Escape)){if(capture!=null){capture=null;message=null;}else Close();}}
        public void ResetPage()
        {
            capture=null;message="已恢复默认设置。";
            if(tab==0)GameBindings.ResetDefaults();else{DisplayBrightness.Set(.5f);AudioListener.volume=1;PlayerPrefs.SetFloat("Seaside.MasterVolume",1);PlayerPrefs.Save();}
        }
        void Styles()
        {
            if(text!=null)return;var font=GameTypography.SystemFont;text=new GUIStyle(GUI.skin.label){font=font,fontSize=22,alignment=TextAnchor.MiddleLeft};text.normal.textColor=Color.white;
            tabText=new GUIStyle(text){fontSize=23,alignment=TextAnchor.MiddleCenter};keyText=new GUIStyle(tabText){fontSize=19};link=new GUIStyle(tabText){fontSize=22};
        }
        bool Link(Rect rect,string label,bool selected=false)
        {
            bool hit=GameUIStyles.Button(rect,GUIContent.none,GUIStyle.none);var color=GUI.color;GUI.color=selected?Color.white:new Color(.7f,.73f,.85f);GUI.Label(rect,(selected?"▪  ":"")+label+(selected?"  ▪":""),link);GUI.color=color;return hit;
        }
        void Box(Rect rect)
        {GUI.color=new Color(.65f,.72f,.95f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=new Color(.05f,.06f,.26f);GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Texture2D.whiteTexture);GUI.color=Color.white;}
        void Binding(GameAction action,string label,float x,float y)
        {
            GUI.Label(new Rect(x,y,176,36),label,text);var rect=new Rect(x+197,y,112,36);Box(rect);
            GUI.Label(rect,capture==action?"请按键…":GameBindings.Label(action),keyText);
            if(GameUIStyles.Button(rect,GUIContent.none,GUIStyle.none))BeginBinding(action);
        }
        void Slider(string label,float y,float value,Action<float> setter)
        {
            GUI.Label(new Rect(260,y-18,110,38),label,text);var rect=new Rect(380,y-8,286,30);GUI.color=new Color(.9f,.93f,1);
            GUI.DrawTexture(new Rect(rect.x,y,rect.width,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.x+value*(rect.width-18),y-9,18,10),Texture2D.whiteTexture);GUI.color=Color.white;
            var e=Event.current;if((e.type==EventType.MouseDown||e.type==EventType.MouseDrag)&&rect.Contains(e.mousePosition)){setter(Mathf.Clamp01((e.mousePosition.x-rect.x)/rect.width));e.Use();}
            GUI.Label(new Rect(690,y-18,100,38),Mathf.RoundToInt(value*100)+"%",text);
        }
        void OnGUI()
        {
            GUI.depth=-5000;Styles();var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1186f,Screen.height/669f);
            GUI.matrix=Matrix4x4.identity;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1186*scale)*.5f,(Screen.height-669*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);GUI.color=Color.white;
            var e=Event.current;
            if(capture!=null&&Time.frameCount>captureFrame&&(e.type==EventType.KeyDown&&e.keyCode!=KeyCode.Escape||e.type==EventType.MouseDown))
            {
                var key=e.type==EventType.MouseDown?(KeyCode)((int)KeyCode.Mouse0+e.button):e.keyCode;
                if(GameBindings.Set(capture.Value,key,out var error)){capture=null;message="按键已保存。";}else message=error;e.Use();
            }
            var assets=MenuArtwork.Current;art=assets==null?null:tab==0?assets.controls:assets.display;
            if(art!=null)GUI.DrawTexture(new Rect(0,0,1186,669),art,ScaleMode.StretchToFill);else{GUI.color=new Color(.03f,.04f,.24f);GUI.DrawTexture(new Rect(0,0,1186,669),Texture2D.whiteTexture);GUI.color=Color.white;}
            if(Link(new Rect(220,88,177,44),"游戏操作",tab==0))SelectTab(0);
            if(Link(new Rect(421,88,210,44),"显示与声音",tab==1))SelectTab(1);
            if(tab==0)
            {
                Binding(GameAction.Left,"左",260,225);Binding(GameAction.Right,"右",612,225);
                Binding(GameAction.Jump,"跳跃",260,275);Binding(GameAction.Interact,"交互",612,275);
                Binding(GameAction.Dash,"冲刺",260,325);Binding(GameAction.Teleport,"传送",612,325);
                Binding(GameAction.Ride,"滑索",260,375);Binding(GameAction.Protection,"防寒 / 隔热",612,375);Binding(GameAction.Pause,"暂停游戏",260,425);
            }
            else
            {
                Slider("亮度",296,DisplayBrightness.Value,DisplayBrightness.Set);
                Slider("声音",347,AudioListener.volume,v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("Seaside.MasterVolume",v);PlayerPrefs.Save();});
                GUI.Label(new Rect(260,398,110,38),"全屏",text);bool full=GUI.Toggle(new Rect(380,405,32,32),Screen.fullScreen,GUIContent.none);
                if(full!=Screen.fullScreen){Screen.fullScreen=full;PlayerPrefs.SetInt("Seaside.Fullscreen",full?1:0);PlayerPrefs.Save();}
            }
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(250,487,820,45),message,new GUIStyle(text){fontSize=18});
            if(Link(new Rect(220,562,185,50),"返回游戏",true))Close();if(Link(new Rect(438,562,150,50),"重置设置"))ResetPage();
            GUI.matrix=matrix;GUI.color=color;
        }
        void OnDestroy(){if(current==this)current=null;}
    }
}
