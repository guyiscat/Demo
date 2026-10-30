using UnityEngine;
namespace Seaside
{
    public sealed class SeasideHUD:MonoBehaviour
    {
        SeasideSession game;GUIStyle title,small,body,center;Texture2D dot;
        void Awake(){game=GetComponent<SeasideSession>();}
        void Init()
        {
            title=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;
            small=new GUIStyle(GUI.skin.label){fontSize=13};small.normal.textColor=new Color(.65f,.8f,.84f);
            body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};body.normal.textColor=Color.white;
            center=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};dot=Texture2D.whiteTexture;
        }
        void Panel(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,dot);GUI.color=old;}
        void OnGUI()
        {
            if(game==null||game.player==null)return;if(title==null)Init();
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            Panel(new Rect(0,0,1280,87),new Color(.025f,.10f,.14f,.96f));
            GUI.Label(new Rect(25,14,600,38),"SEASIDE / STABLE CAMERA + TRAIL",title);
            GUI.Label(new Rect(26,51,720,25),"BUILD 4-8-8  |  BODY 2 x 2  |  TAP 4 / HOLD 8 / DASH 8 MAP PIXELS",small);
            GUI.Label(new Rect(760,22,180,32),"TIME  "+game.Timer.ToString("F1"),body);
            GUI.Label(new Rect(940,22,140,32),"DEATHS  "+game.Deaths,body);
            GUI.Label(new Rect(1100,22,170,32),"CHECKPOINT "+game.CheckpointId,body);
            Panel(new Rect(20,640,1240,61),new Color(.025f,.10f,.14f,.96f));
            GUI.Label(new Rect(35,651,940,30),"A / D  MOVE     SPACE  VARIABLE JUMP     LEFT CLICK  DASH TO CURSOR",body);
            var hint=new GUIStyle(small);hint.normal.textColor=new Color(.06f,.25f,.32f);
            GUI.Label(new Rect(35,613,1130,23),"HOLD TOWARD WALL  ATTACH + REFILL     TAB  MAP     B  BLUE ROUTE     R  RESPAWN     CTRL+R  RESTART     H  HELP",hint);
            GUI.Label(new Rect(1000,652,230,30),game.player.Attached?"ATTACHED / REFILLED":game.player.WallSliding?"SLOW WALL SLIDE":game.player.DashReady?"DASH READY":"DASH USED",center);
            if(!FindFirstObjectByType<SeasideCamera>().overview)
            {
                Vector2 mouse=Input.mousePosition;mouse.y=Screen.height-mouse.y;mouse=new Vector2(mouse.x*1280/Screen.width,mouse.y*720/Screen.height);
                GUI.Label(new Rect(mouse.x-8,mouse.y-13,24,26),"+",body);
            }
            if(game.Paused||game.Won||game.ShowHelp)
            {
                Panel(new Rect(300,180,680,345),new Color(.025f,.09f,.13f,.97f));
                GUI.Label(new Rect(330,205,600,40),game.Won?"SEASIDE COMPLETE":game.Paused?"PAUSED":"CONTROLS",title);
                GUI.Label(new Rect(330,265,620,200),game.Won?"R: PLAY AGAIN\nTime "+game.Timer.ToString("F2")+" s / deaths "+game.Deaths:
                    "A / D: move; SPACE: tap 4 / hold 8 map pixels\nLeft mouse: 8-pixel dash with fading afterimages\nHold toward wall: hang; release: slow slide\nPress toward wall again: rehang; SPACE: wall kick\nEdge grace: 0.14s / landing buffer: 0.12s\nOlder checkpoints ignored; impact stops dash\n1 map pixel = 1 cell; body = 2 x 2 cells",body);
                if(GUI.Button(new Rect(565,470,150,35),"CLOSE")){game.ShowHelp=false;game.Resume();}
            }
        }
    }
}
