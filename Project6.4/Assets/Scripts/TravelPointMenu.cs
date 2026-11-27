using UnityEngine;

namespace Seaside
{
    public sealed class TravelPointMenu : MonoBehaviour
    {
        public SeasideSession session;
        public bool Opened=>session!=null&&session.TravelMenuOpen;
        int openedFrame;
        public static bool AllowsRegion(string path)=>path!=CampaignFlow.Scenes[4]&&CampaignFlow.ScenesContains(path);
        public void Open(TravelPoint point)
        {
            if(point==null||session==null||!AllowsRegion(session.gameObject.scene.path)||!point.Nearby||session.InputBlocked||session.player.frozen)return;
            TransferArtwork.ResetSelection();
            openedFrame=Time.frameCount;session.player.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();session.player.BeginTraversal();session.TravelMenuOpen=true;session.player.frozen=true;
        }
        public void Close(){if(session==null)return;if(Opened)GameBindings.BlockFrame();session.TravelMenuOpen=false;if(!session.InputBlocked)session.player.frozen=false;}
        void Update(){if(Opened&&Time.frameCount>openedFrame&&Input.GetKeyDown(KeyCode.Escape))Close();}
        public bool Select(string path)
        {
            if(!Opened||!AllowsRegion(path)||!AllowsRegion(session.gameObject.scene.path)||!CampaignFlow.Explored(path))return false;
            if(path==session.gameObject.scene.path){Close();return false;}
            return CampaignFlow.Travel(session,path,"Portal",true);
        }
        public void Draw(GameUIStyles styles)
        {
            if(!Opened)return;
            TransferArtwork.Draw(this,session);
        }
        void OnDisable(){Close();}
    }
}
