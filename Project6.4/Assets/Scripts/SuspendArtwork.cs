using System;
using UnityEngine;
namespace Seaside
{
    public static class SuspendArtwork
    {
        static int selected;static int opened=-1;
        static Texture2D Texture(string name)=>Resources.Load<Texture2D>("Suspend/"+name);
        static void Image(string name,Rect rect){var t=Texture(name);if(t!=null)GUI.DrawTexture(rect,t);}
        public static void Draw(SeasideSession session)
        {
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(0,0,0,.55f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
            float scale=Mathf.Min(Screen.width/960f,Screen.height/540f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)*.5f,(Screen.height-540*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            Image("suspend_bg",new Rect(281,138,401,263));
            if(opened<Time.frameCount-2)selected=0;opened=Time.frameCount;
            var e=Event.current;
            if(e.type==EventType.KeyDown)
            {
                if(e.keyCode==KeyCode.DownArrow||e.keyCode==KeyCode.S){selected=(selected+1)%3;e.Use();}
                else if(e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.W){selected=(selected+2)%3;e.Use();}
                else if(e.keyCode==KeyCode.Return){Activate(selected,session);e.Use();}
            }
            string[] names={"continue","set","save"};
            for(int i=0;i<3;i++)
            {
                var hit=new Rect(398,207+i*42,167,42);if(hit.Contains(e.mousePosition)&&e.type==EventType.MouseMove)selected=i;
                bool hover=selected==i||hit.Contains(e.mousePosition);
                Image("suspend_"+names[i]+(hover?"_on":"_off"),hover?new Rect(418,202+i*42,127,53):new Rect(443,219+i*42,77,19));
                if(GameUIStyles.Button(hit,GUIContent.none,GUIStyle.none)){selected=i;Activate(i,session);}
            }
            var style=new GUIStyle(GUI.skin.label){font=GameTypography.SystemFont,fontSize=12,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(.72f,.75f,.78f);
            if(GameUIStyles.Button(new Rect(355,364,120,24),"H 操作帮助",new GUIStyle(style)))session.SetHelp(true);
            if(GameUIStyles.Button(new Rect(487,364,120,24),"返回开始页",new GUIStyle(style)))CampaignFlow.ReturnToMenu(session);
            GUI.matrix=matrix;GUI.color=color;
        }
        static void Activate(int index,SeasideSession session)
        {GameBindings.BlockFrame();if(index==0)session.Resume();else if(index==1)GameSettingsScreen.Open(()=>{});else session.GetComponent<ManualSaveController>()?.Save();}
    }
}
