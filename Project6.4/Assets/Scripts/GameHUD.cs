using UnityEngine;

namespace Seaside
{
    public struct GameHUDContent
    {
        public string title,controls,statistics,status,help,completion,prompt,dialogueTitle,dialogue,choice1,choice2,completionTitle,tutorial;
    }
    public abstract class GameHUD : MonoBehaviour
    {
        public const string CommonControls="A/D 移动   空格 跳跃   左键 冲刺";
        public const string CommonFooter="F5 手动存档   Tab 近景/全景   B 路线   R 复活   Ctrl+R 重开   Esc 暂停   H 帮助";
        public const string CommonHelp="短按空格小跳，长按跳得更高。\n\n朝墙按方向可悬停，空格蹬墙跳。\n灰色墙面无法攀爬。\n\n冲刺跟随身旁箭头，落地后恢复。\n\n安全地面按 F5 选档保存。";
        GameUIStyles styles;GUIStyle tutorialStyle,helpText,helpHeading,keyStyle;
        protected abstract SeasideSession Session{get;}
        public abstract GameHUDContent Content{get;}
        protected virtual void Replay(){Session.Restart();}
        protected virtual void AdvanceDialogue(){ }
        protected virtual void ChooseDialogue(int choice){ }
        public static string Statistics(SeasideSession session)=>"复活点 "+session.CheckpointId+"   时间 "+session.Timer.ToString("F1")+"   死亡 "+session.Deaths;
        public static string DashStatus(SeasideMotor motor)=>"冲刺 "+(motor.DashReady?"就绪":"已用");
        public static string MotorState(SeasideMotor motor)=>motor.frozen?"停止":motor.Dashing?"冲刺中":motor.Attached?"贴墙悬停":motor.WallSliding?"沿墙下滑":motor.Grounded?"地面":motor.Body.linearVelocity.y>0?"上升":"下落";
        protected virtual void OnGUI()
        {
            if(WorldTransition.Active||GameSettingsScreen.Opened||SaveSlotScreen.Opened)return;
            GUI.depth=0;
            if(Session==null||Session.player==null)return;
            if(styles==null)styles=new GameUIStyles();
            var oldMatrix=GUI.matrix;var oldColor=GUI.color;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1440*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;
            var content=Content;
            content.controls=GameBindings.Format(content.controls);content.help=GameBindings.Format(content.help);content.dialogue=GameBindings.Format(content.dialogue);content.prompt=GameBindings.Format(content.prompt);
            if(Session.ShowHelp||Session.Paused||Session.Won)
            {
                if(Session.ShowHelp)DrawHelp();else if(Session.Paused)DrawPause();else DrawCompletion(content);
                Session.GetComponent<ManualSaveController>()?.Draw(styles);
                GUI.matrix=oldMatrix;GUI.color=oldColor;return;
            }
            string nearby=FindNearbyPrompt();if(!string.IsNullOrEmpty(nearby)&&string.IsNullOrEmpty(content.dialogue))content.prompt=GameBindings.Format(nearby);
            GameUIStyles.Panel(new Rect(0,0,1440,80));
            GUI.Label(new Rect(22,10,600,32),content.title,styles.title);
            GUI.Label(new Rect(22,45,1396,28),content.controls,styles.text);
            GUI.Label(new Rect(635,15,790,32),content.statistics,styles.text);
            GameUIStyles.Panel(new Rect(16,825,1408,60));
            GUI.Label(new Rect(30,831,1370,28),content.status,styles.text);
            GUI.Label(new Rect(30,857,1370,25),GameBindings.Format(CommonFooter),styles.text);
            if(!Session.InputBlocked&&!Session.ShowHelp)
            {
                string tutorial=Session.GetComponent<LevelTutorials>()?.Current??content.tutorial;
                if(!string.IsNullOrEmpty(tutorial))tutorial=tutorial.Replace("在攀岩点周围可以二次跳跃","靠近匹配属性攀岩点，可反复按空格补跳");
                tutorial=GameBindings.Format(tutorial);
                if(string.IsNullOrEmpty(content.dialogue)&&!string.IsNullOrEmpty(tutorial))
                {
                    GameUIStyles.Panel(new Rect(300,100,840,66));
                    if(tutorialStyle==null)tutorialStyle=new GUIStyle(styles.text){fontSize=styles.text.fontSize+1};
                    GUI.Label(new Rect(325,119,790,35),tutorial,tutorialStyle);
                }
                if(!string.IsNullOrEmpty(content.dialogue))
                {
                    GameUIStyles.Panel(new Rect(350,270,740,330),true);
                    GUI.Label(new Rect(380,290,680,40),content.dialogueTitle,styles.title);
                    GUI.Label(new Rect(380,345,680,160),content.dialogue,styles.text);
                    if(!string.IsNullOrEmpty(content.choice1))
                    {
                        if(GameUIStyles.Button(new Rect(380,480,680,42),content.choice1,styles.button))ChooseDialogue(0);
                        if(GameUIStyles.Button(new Rect(380,535,680,42),content.choice2,styles.button))ChooseDialogue(1);
                    }
                    else if(GameUIStyles.Button(new Rect(620,535,200,38),GameBindings.Format("继续 / E"),styles.button))AdvanceDialogue();
                }
                else if(!string.IsNullOrEmpty(content.prompt))
                {
                    GameUIStyles.Panel(new Rect(470,745,500,50));
                    GUI.Label(new Rect(495,755,450,32),content.prompt,styles.text);
                }
            }
            Session.GetComponent<TravelPointMenu>()?.Draw(styles);
            FindAnyObjectByType<Village.VillageRobot>()?.Draw(styles);
            Session.GetComponent<ManualSaveController>()?.Draw(styles);
            GUI.matrix=oldMatrix;GUI.color=oldColor;
        }
        void Dim()
        {var color=GUI.color;GUI.color=new Color(0,0,0,.55f);GUI.DrawTexture(new Rect(-1440,-900,4320,2700),Texture2D.whiteTexture);GUI.color=color;}
        void HelpStyles()
        {
            if(helpText!=null)return;
            helpText=new GUIStyle(styles.text){fontSize=20,wordWrap=true};
            helpHeading=new GUIStyle(styles.title){fontSize=21};
            keyStyle=new GUIStyle(helpText){alignment=TextAnchor.MiddleLeft};keyStyle.normal.textColor=new Color(.6f,.9f,.8f);
        }
        void DrawPause()
        {
            if(GameSystemSettings.Current.useSuspendArtwork){SuspendArtwork.Draw(Session);return;}
            Dim();GameUIStyles.Panel(new Rect(420,195,600,490),true);
            GUI.Label(new Rect(460,230,520,40),"已暂停",styles.title);
            GUI.Label(new Rect(460,285,520,32),CampaignFlow.CurrentTitle(Session.gameObject.scene.path)+"  ·  "+System.TimeSpan.FromSeconds(Session.Timer).ToString(@"hh\:mm\:ss")+"  ·  死亡 "+Session.Deaths,styles.text);
            if(GameUIStyles.Button(new Rect(460,345,520,52),"继续游戏",styles.button))Session.Resume();
            if(GameUIStyles.Button(new Rect(460,417,250,46),"保存游戏",styles.button))Session.GetComponent<ManualSaveController>()?.Save();
            if(GameUIStyles.Button(new Rect(730,417,250,46),"游戏设置",styles.button))GameSettingsScreen.Open(()=>{});
            if(GameUIStyles.Button(new Rect(460,483,250,46),"操作帮助",styles.button))Session.SetHelp(true);
            if(CampaignFlow.Enabled&&GameUIStyles.Button(new Rect(730,483,250,46),"返回开始页",styles.button))CampaignFlow.ReturnToMenu(Session);
            GUI.Label(new Rect(460,562,520,60),"离开前记得保存。复活点不会自动存档。",styles.text);
            GUI.Label(new Rect(460,633,520,30),GameBindings.Label(GameAction.Pause)+" 返回游戏",styles.text);
        }
        void HelpRow(float y,string key,string action)
        {GUI.Label(new Rect(285,y,210,36),key,keyStyle);GUI.Label(new Rect(505,y,250,36),action,helpText);}
        void DrawHelp()
        {
            HelpStyles();Dim();GameUIStyles.Panel(new Rect(245,155,950,600),true);
            GUI.Label(new Rect(285,185,400,40),"操作帮助",styles.title);
            GUI.Label(new Rect(285,246,400,42),"基本操作",helpHeading);
                float y=316;
                HelpRow(y,GameBindings.Label(GameAction.Left)+" / "+GameBindings.Label(GameAction.Right),"移动");y+=43;
                HelpRow(y,GameBindings.Label(GameAction.Jump),"跳跃 / 蹬墙跳");y+=43;
                HelpRow(y,GameBindings.Label(GameAction.Dash),"沿箭头方向冲刺");y+=43;
                if(Session.player.GetComponent<Forest.ForestPlayer>()!=null)
                {HelpRow(y,GameBindings.Label(GameAction.Teleport),"发射信标 / 再按传送");y+=43;HelpRow(y,GameBindings.Label(GameAction.Ride),"抓住滑索");y+=43;}
                HelpRow(y,GameBindings.Label(GameAction.Interact),"交互");y+=43;
                if(Session.player.GetComponent<Expedition.ThermalTraversal>()!=null)HelpRow(y,GameBindings.Label(GameAction.Protection),"切换防寒 / 隔热");
                GUI.Label(new Rect(800,316,345,34),"移动与存档",helpHeading);
                GUI.Label(new Rect(800,364,345,250),GameBindings.Format(CommonHelp),helpText);
                GUI.Label(new Rect(285,647,855,30),"R 回复活点    Ctrl+R 重开    Tab 切换镜头    B 显示路线",styles.text);
            if(GameUIStyles.Button(new Rect(905,692,240,40),Session.Paused?"返回暂停菜单":"返回游戏",styles.button))Session.SetHelp(false);
            GUI.Label(new Rect(285,697,560,30),"H / "+GameBindings.Label(GameAction.Pause)+" 关闭帮助",styles.text);
        }
        void DrawCompletion(GameHUDContent content)
        {
            string transition=CampaignFlow.TransitionMessage(Session);Dim();GameUIStyles.Panel(new Rect(350,230,740,410),true);
            GUI.Label(new Rect(390,260,660,42),string.IsNullOrEmpty(content.completionTitle)?"旅程结束":content.completionTitle,styles.title);
            GUI.Label(new Rect(390,330,660,190),content.completion+"\n\n用时 "+Session.Timer.ToString("F1")+" 秒  ·  死亡 "+Session.Deaths+" 次。"+(transition==null?"":"\n"+transition),styles.text);
            if(transition==null&&GameUIStyles.Button(new Rect(390,558,300,46),"再玩一次",styles.button))Replay();
            if(CampaignFlow.Enabled&&transition==null&&GameUIStyles.Button(new Rect(730,558,320,46),"返回开始页",styles.button))CampaignFlow.ReturnToMenu(Session);
        }
        string FindNearbyPrompt()
        {
            if(Session.InputBlocked||Session.player.frozen)return null;
            foreach(var point in FindObjectsByType<TravelPoint>())if(point.Usable&&point.Nearby)return "按 E 打开区域传送";
            foreach(var exit in FindObjectsByType<RegionExit>())if(exit.Inside)return exit.Hint;
            var mine=FindAnyObjectByType<Mine.MineLevel>();
            if(mine!=null)foreach(var lift in new[]{mine.lift,mine.deepLift})
                if(lift!=null&&Mathf.Min(Vector2.Distance(Session.player.Body.position,lift.upperStop),Vector2.Distance(Session.player.Body.position,lift.lowerStop))<5)
                    return !lift.Activated?"先触摸黄色开关解锁电梯":lift.Moving?"电梯运行中":"按 E 乘坐 / 呼叫电梯";
            return null;
        }
        protected virtual void OnDestroy(){styles?.Dispose();}
    }
}
