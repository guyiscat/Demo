using UnityEngine;

namespace Seaside
{
    // All levels share the Forest palette and the bundled project font.
    public sealed class GameUIStyles
    {
        public static readonly Color PanelColor=new Color(.025f,.06f,.05f,.94f);
        public static readonly Color ModalColor=new Color(.025f,.06f,.05f,.98f);
        public readonly GUIStyle text,title,button;
        public static Font SystemFont=>GameTypography.SystemFont;
        public static bool Button(Rect rect,string label,GUIStyle style)
        {bool clicked=GUI.Button(rect,label,style);if(clicked)GameBindings.BlockFrame();return clicked;}
        public static bool Button(Rect rect,GUIContent label,GUIStyle style)
        {bool clicked=GUI.Button(rect,label,style);if(clicked)GameBindings.BlockFrame();return clicked;}
        public GameUIStyles()
        {
            Font font=SystemFont;
            GUI.skin.font=font;
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true};text.normal.textColor=Color.white;
            title=new GUIStyle(text){fontSize=23,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
        }
        public static void Panel(Rect rect,bool modal=false)
        {
            var previous=GUI.color;GUI.color=modal?ModalColor:PanelColor;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        }
        // This is an imported shared asset; individual panels do not own it.
        public void Dispose(){ }
    }
}
