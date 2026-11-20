using System;
using Forest;
using Seaside;
using UnityEngine;

namespace Mine
{
    public sealed class MineLevel : MonoBehaviour
    {
        public ForestPlayer player;
        public SeasideSession session;
        public SeasideCamera cameraRig;
        public MineSettings settings;
        public MineLift lift;
        public MineInteraction[] interactions;
        public string sourceSha256;
        public bool HasBitcoin{get;private set;}
        public bool WorkbenchRead{get;private set;}
        public string Dialogue=>pages==null?null:pages[page];
        public string DialogueTitle{get;private set;}
        public MineInteraction Nearby{get;private set;}
        string[] pages;
        int page;
        MineInteraction active;
        void OnEnable(){if(player!=null)player.motor.ResetPerformed+=CloseDialogue;}
        void OnDisable(){if(player!=null)player.motor.ResetPerformed-=CloseDialogue;}
        void Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mineChecks")>=0)gameObject.AddComponent<MineRuntimeChecks>();
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.R)&&(Input.GetKey(KeyCode.LeftControl)||HasBitcoin))ResetProgress();
            if(session.InputBlocked)return;
            if(pages!=null){if(Input.GetKeyDown(KeyCode.E))AdvanceDialogue();return;}
            RefreshNearby();
            if(Input.GetKeyDown(KeyCode.E)&&Nearby!=null)BeginInteraction(Nearby);
        }
        public void RefreshNearby()
        {
            Nearby=null;float distance=settings.interactionDistance;
            foreach(var item in interactions)
            {
                if(item==null||item.Used)continue;
                float d=item.Distance(player.motor.Body.position);
                if(d<distance){Nearby=item;distance=d;}
            }
        }
        public bool BeginInteraction(MineInteraction item)
        {
            if(item==null||item.Used||pages!=null||session.InputBlocked||item.Distance(player.motor.Body.position)>settings.interactionDistance)return false;
            active=item;page=0;player.CancelSpecial();player.motor.BeginTraversal();player.motor.frozen=true;
            if(item.kind==MineInteractionKind.Workbench)
            {
                DialogueTitle="矿洞工作台";
                pages=new[]{"开发进行还不到一半，团队的资金已经轮转不过来了，再这样下去项目会被叫停。",
                    "团队人员陆陆续续离职，剩下的人也不看好这个项目，我也得另外找份工作维持生计了。","团队解散，项目停工。"};
            }
            else if(item.kind==MineInteractionKind.LiftSwitch){DialogueTitle="电梯控制台";pages=new[]{"矿洞的电梯重新启动。"};}
            else if(item.completesLevel){DialogueTitle="矿洞遗物";pages=new[]{"获得：比特币。","当初的人们没有发现它的价值，直到它求而不得。"};}
            else {DialogueTitle="矿井竖井";pages=new[]{"竖井通往矿洞更深处，下面似乎还有遗物。"};}
            return true;
        }
        public void AdvanceDialogue()
        {
            if(pages==null||session.Paused)return;
            if(++page<pages.Length)return;
            var item=active;CloseDialogue();item.Used=true;
            if(item.kind==MineInteractionKind.Workbench)WorkbenchRead=true;
            else if(item.kind==MineInteractionKind.LiftSwitch)lift.Activate();
            else if(item.completesLevel){HasBitcoin=true;session.Complete();}
        }
        public void CloseDialogue(){pages=null;page=0;active=null;if(player!=null&&player.motor!=null&&!session.InputBlocked)player.motor.frozen=false;}
        void ResetProgress(){CloseDialogue();WorkbenchRead=HasBitcoin=false;foreach(var item in interactions)item.Used=false;lift.ResetLift();}
        public void Restart(){ResetProgress();session.Restart();}
        public void RestoreProgress(bool workbenchRead,bool liftActivated)
        {
            WorkbenchRead=workbenchRead;
            foreach(var item in interactions)
            {
                if(item.kind==MineInteractionKind.Workbench)item.Used=workbenchRead;
                if(item.kind==MineInteractionKind.LiftSwitch)item.Used=liftActivated;
            }
            if(liftActivated)lift.Activate();
        }
    }
}
