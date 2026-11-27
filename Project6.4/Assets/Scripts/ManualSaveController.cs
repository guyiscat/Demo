using UnityEngine;

namespace Seaside
{
    public sealed class ManualSaveController : MonoBehaviour
    {
        SeasideSession session;
        float messageUntil;
        public string Message{get;private set;}
        public bool MessageVisible=>Time.unscaledTime<messageUntil;
        void Awake(){session=GetComponent<SeasideSession>();}
        void Update(){if(Input.GetKeyDown(KeyCode.F5))Save();}
        public bool Save()
        {
            return SaveSlotScreen.Open(SaveSlotScreen.Purpose.Save,session);
        }
        public void Notify(string message){Message=message;messageUntil=Time.unscaledTime+3;}
        public void Draw(GameUIStyles styles)
        {
            if(!MessageVisible||session.Travelling)return;
            GameUIStyles.Panel(new Rect(370,172,700,50));GUI.Label(new Rect(390,185,660,32),GameBindings.Format(Message),styles.text);
        }
    }

    public static class ManualSaveSafety
    {
        public static bool CanSave(SeasideSession session,out string reason)
        {
            reason="请先站稳在远离危险的固定地面上再存档。";
            if(session==null||session.player==null||!CampaignFlow.ScenesContains(session.gameObject.scene.path))return false;
            var motor=session.player;
            if(session.Won||session.Respawning||session.Travelling||session.TravelMenuOpen||session.SaveMenuOpen||GameSettingsScreen.Opened||session.ShowHelp||motor.frozen||WorldTransition.Active||CampaignFlow.IsLoading)
            {reason="当前正在交互、转场或受限状态，无法存档。";return false;}
            var actor=motor.GetComponent<Forest.ForestPlayer>();
            if(motor.Dashing||motor.Attached||motor.WallSliding||!motor.Grounded||Mathf.Abs(motor.Body.linearVelocity.y)>.2f||
                actor!=null&&(actor.BeaconActive||actor.PostTeleportHoverRemaining>0||actor.RidingRail!=null||actor.RidingSurface!=null))return false;
            Physics2D.SyncTransforms();return SafeGround(motor,motor.Body.position);
        }
        public static bool SafeGround(SeasideMotor motor,Vector2 position)
        {
            if(motor==null||float.IsNaN(position.x)||float.IsNaN(position.y)||float.IsInfinity(position.x)||float.IsInfinity(position.y))return false;
            var size=motor.BodySize;
            foreach(var collider in Physics2D.OverlapBoxAll(position,size-Vector2.one*.04f,0,1<<6))
                if(!collider.isTrigger)return false;
            foreach(var collider in Physics2D.OverlapBoxAll(position,size+Vector2.one*.6f,0))
            {
                if(collider.gameObject.layer==7||collider.GetComponent<RegionExit>()!=null||collider.GetComponent<Expedition.ThermalSurface>()!=null||collider.GetComponent<Mine.MineGate>()!=null)return false;
            }
            // Permit the small step between adjoining authored floors, but require support
            // under both feet so an overhanging cliff cannot authorize a save.
            foreach(float offset in new[]{-.5f,0,.5f})
            {
                Vector2 foot=position+new Vector2(size.x*offset*.85f,-size.y*.5f+.05f);
                var hit=Physics2D.Raycast(foot,Vector2.down,.28f,1<<6);
                if(hit.collider==null||hit.collider.isTrigger||hit.normal.y<.95f||hit.collider.attachedRigidbody!=null||
                    hit.collider.GetComponent<MovingSurface>()!=null||hit.collider.GetComponent<Expedition.ThermalSurface>()!=null||hit.collider.GetComponent<Mine.MineGate>()!=null)return false;
            }
            return true;
        }
    }
}
