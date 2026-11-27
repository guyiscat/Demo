using UnityEngine;
namespace Seaside
{
    public static class TransferArtwork
    {
        // Stable campaign indices; Lighthouse is a story/chase entrance only.
        static readonly int[] regions={0,1,5,2,3};
        static readonly string[] names={"sea","forest","villages","mines","snow"};
        static readonly Rect[] off={new Rect(294,220,14,54),new Rect(383,224,15,53),new Rect(472,224,14,54),new Rect(561,263,15,51),new Rect(651,227,13,53)};
        static readonly Rect[] on={new Rect(280,201,42,72),new Rect(370,224,40,71),new Rect(461,208,38,69),new Rect(547,248,42,66),new Rect(638,227,40,70)};
        static readonly Rect[] hits={new Rect(275,207,50,78),new Rect(365,212,50,80),new Rect(454,213,50,78),new Rect(543,249,50,74),new Rect(632,216,50,78)};
        static int selected=5;
        public static int RegionCount=>regions.Length;
        public static string RegionAt(int index)=>CampaignFlow.Scenes[regions[index]];
        public static void ResetSelection(){selected=5;}
        static void Image(string name,Rect rect)
        {var t=Resources.Load<Texture2D>("Transfer/transfer_"+name);if(t!=null)GUI.DrawTexture(rect,t);}
        static bool Available(int index,SeasideSession session)=>index==5||CampaignFlow.Explored(RegionAt(index))&&RegionAt(index)!=session.gameObject.scene.path;
        static void Activate(int index,TravelPointMenu menu,SeasideSession session)
        {if(!Available(index,session))return;GameBindings.BlockFrame();if(index==5)menu.Close();else menu.Select(RegionAt(index));}
        public static void Draw(TravelPointMenu menu,SeasideSession session)
        {
            var matrix=GUI.matrix;var color=GUI.color;bool enabled=GUI.enabled;
            try
            {
                GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,.65f);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
                float scale=Mathf.Min(Screen.width/960f,Screen.height/540f);
                GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)*.5f,(Screen.height-540*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
                Image("bg",new Rect(188,138,587,263));Image("line",new Rect(300,181,360,130));
                var e=Event.current;
                if(e.type==EventType.KeyDown)
                {
                    int step=e.keyCode==KeyCode.RightArrow||e.keyCode==KeyCode.DownArrow||e.keyCode==KeyCode.D||e.keyCode==KeyCode.S?1:e.keyCode==KeyCode.LeftArrow||e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.A||e.keyCode==KeyCode.W?-1:0;
                    if(step!=0){for(int n=0;n<6;n++){selected=(selected+step+6)%6;if(Available(selected,session))break;}e.Use();}
                    else if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter){Activate(selected,menu,session);e.Use();}
                }
                int hover=-1;
                for(int i=0;i<6;i++)if((i==5?new Rect(410,328,143,53):hits[i]).Contains(e.mousePosition))hover=i;
                if(e.type==EventType.MouseMove&&hover>=0&&Available(hover,session))selected=hover;
                for(int i=0;i<5;i++)
                {
                    bool explored=CampaignFlow.Explored(RegionAt(i)),current=RegionAt(i)==session.gameObject.scene.path;
                    bool glow=explored&&(current||selected==i||hover==i);
                    GUI.color=explored?Color.white:new Color(1,1,1,.32f);Image(names[i]+(glow?"_on":"_off"),glow?on[i]:off[i]);GUI.color=Color.white;
                    GUI.enabled=enabled&&Available(i,session);
                    if(GameUIStyles.Button(hits[i],GUIContent.none,GUIStyle.none)){selected=i;Activate(i,menu,session);}
                    GUI.enabled=enabled;
                }
                bool resume=selected==5||hover==5;
                Image("continue_"+(resume?"on":"off"),resume?new Rect(418,328,127,53):new Rect(443,345,77,19));
                if(GameUIStyles.Button(new Rect(410,328,143,53),GUIContent.none,GUIStyle.none))Activate(5,menu,session);
                var style=new GUIStyle(GUI.skin.label){font=GameTypography.SystemFont,fontSize=12,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(.7f,.73f,.76f);
                int info=hover>=0&&hover<5?hover:selected<5?selected:-1;
                string text=info<0?"选择已探索的区域  ·  Esc 返回":CampaignFlow.CurrentTitle(RegionAt(info))+(RegionAt(info)==session.gameObject.scene.path?" · 当前位置":CampaignFlow.Explored(RegionAt(info))?" · 点击传送":" · 尚未探索");
                GUI.Label(new Rect(245,407,470,26),text,style);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabled;}
        }
    }
}
