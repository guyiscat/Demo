using UnityEngine;

namespace Seaside
{
    // All non-artwork game UI uses the Forest palette and Chinese font stack.
    public sealed class GameUIStyles
    {
        public static readonly Color PanelColor=new Color(.025f,.06f,.05f,.94f);
        public static readonly Color ModalColor=new Color(.025f,.06f,.05f,.98f);
        public readonly GUIStyle text,title,button;
        readonly Font font;
        public GameUIStyles()
        {
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Noto Sans CJK SC","Arial"},20);
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true};text.normal.textColor=Color.white;
            title=new GUIStyle(text){fontSize=23,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
        }
        public static void Panel(Rect rect,bool modal=false)
        {
            var previous=GUI.color;GUI.color=modal?ModalColor:PanelColor;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        }
        public void Dispose(){if(font!=null)Object.Destroy(font);}
    }
}
