using UnityEngine;

namespace Seaside
{
    public struct GameHUDContent
    {
        public string title,controls,statistics,status,help,completion;
    }
    public abstract class GameHUD : MonoBehaviour
    {
        public const string CommonControls="A/D 移动   空格 跳跃   左键 冲刺";
        public const string CommonFooter="Tab 近景/全景   B 路线   R 最近复活点复活   Ctrl+R 重开   Esc 暂停   H 帮助";
        public const string CommonHelp="短按/长按空格改变跳跃高度；朝墙按住方向可悬停。\n松开方向沿墙缓慢下滑；重新朝墙按方向可再次悬停。\n空格可蹬墙跳；接触地面或可攀附墙面恢复冲刺。\n金色箭头指示行动方向；鼠标左键沿箭头方向冲刺。\n复活点以最后接触为准；R复活，Ctrl+R重新开始本关。";
        GameUIStyles styles;
        protected abstract SeasideSession Session{get;}
        public abstract GameHUDContent Content{get;}
        protected virtual void Replay(){Session.Restart();}
        public static string Statistics(SeasideSession session)=>"复活点 "+session.CheckpointId+"   时间 "+session.Timer.ToString("F1")+"   死亡 "+session.Deaths;
        public static string DashStatus(SeasideMotor motor)=>"冲刺 "+(motor.DashReady?"就绪":"已用");
        public static string MotorState(SeasideMotor motor)=>motor.frozen?"停止":motor.Dashing?"冲刺中":motor.Attached?"贴墙悬停":motor.WallSliding?"沿墙下滑":motor.Grounded?"地面":motor.Body.linearVelocity.y>0?"上升":"下落";
        protected virtual void OnGUI()
        {
            if(Session==null||Session.player==null)return;
            if(styles==null)styles=new GameUIStyles();
            var oldMatrix=GUI.matrix;var oldColor=GUI.color;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1440*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;
            var content=Content;
            GameUIStyles.Panel(new Rect(0,0,1440,80));
            GUI.Label(new Rect(22,10,600,32),content.title,styles.title);
            GUI.Label(new Rect(22,45,1396,28),content.controls,styles.text);
            GUI.Label(new Rect(635,15,790,32),content.statistics,styles.text);
            GameUIStyles.Panel(new Rect(16,825,1408,60));
            GUI.Label(new Rect(30,831,1370,28),content.status,styles.text);
            GUI.Label(new Rect(30,857,1370,25),CommonFooter,styles.text);
            if(Session.Paused||Session.ShowHelp||Session.Won)
            {
                GameUIStyles.Panel(new Rect(350,190,740,500),true);
                GUI.Label(new Rect(380,210,680,40),Session.Won?"本关通关 / COMPLETE":Session.Paused?"暂停 / PAUSED":"玩法说明",styles.title);
                string message=Session.Won?content.completion+"\n时间 "+Session.Timer.ToString("F2")+" 秒；死亡 "+Session.Deaths+" 次。\n按 R 重新挑战本关。":content.help;
                GUI.Label(new Rect(380,260,680,345),message,styles.text);
                if(GUI.Button(new Rect(630,625,180,38),Session.Won?"再玩一次":"继续",styles.button))
                {Session.ShowHelp=false;if(Session.Won)Replay();else Session.Resume();}
            }
            GUI.matrix=oldMatrix;GUI.color=oldColor;
        }
        protected virtual void OnDestroy(){styles?.Dispose();}
    }
}
