using System.Collections.Generic;
using Forest;
using Seaside;
using UnityEngine;
namespace Expedition
{
    [RequireComponent(typeof(ForestPlayer))]
    public sealed class ThermalTraversal : MonoBehaviour
    {
        public ForestPlayer player;
        public ThermalSettings settings;
        public ClimbPoint[] points;
        public ProtectionMode Mode{get;private set;}
        public int AirJumps{get;private set;}
        public string LastHazard{get;private set;}
        public bool HasCore=>CampaignFlow.CoreObtained;
        public string ModeName=>!HasCore?"未获得核心":Mode==ProtectionMode.Cold?"防寒":"隔热";
        public Vector2 PointHalfExtents=>player.motor.BodySize*.5f+Vector2.one*settings.climbPadding;
        float jumpBuffer,climbGrace;ClimbPoint gracePoint;int inputEpoch;
        void Awake(){if(player==null)player=GetComponent<ForestPlayer>();Mode=CampaignFlow.Progress.protectionSelected?(ProtectionMode)CampaignFlow.Progress.protectionMode:settings.initialProtection;ResetState();}
        void OnEnable(){if(player==null)player=GetComponent<ForestPlayer>();player.motor.ResetPerformed+=ResetState;}
        void OnDisable(){if(player!=null&&player.motor!=null)player.motor.ResetPerformed-=ResetState;}
        void Update()
        {
            if(!player.manualInput&&GameBindings.Pressed(GameAction.Protection))TrySwitchMode();
            if(points!=null)foreach(var point in points)if(point!=null)point.ShowState(HasCore&&Mode==point.requiredProtection,PointHalfExtents);
        }
        public void ResetState(){AirJumps=0;LastHazard=null;ClearJumpGrace();}
        public void ClearJumpGrace(){jumpBuffer=climbGrace=0;gracePoint=null;}
        public bool TrySwitchMode()
        {
            if(player.session.InputBlocked||player.motor.frozen)return false;
            if(!HasCore){player.session.GetComponent<ManualSaveController>()?.Notify("先带回矿洞比特币，与村庄机器人交互获得核心。");return false;}
            Mode=Mode==ProtectionMode.Cold?ProtectionMode.Heat:ProtectionMode.Cold;
            ClearJumpGrace();
            CampaignFlow.Progress.protectionSelected=true;CampaignFlow.Progress.protectionMode=(int)Mode;
            SeasideLog.Event("PROTECTION_SWITCH",ModeName);ValidateContact();return true;
        }
        public bool Touch(ThermalSurface surface)
        {
            if(surface==null||player.session.InputBlocked||HasCore&&surface.requiredProtection==Mode)return true;
            LastHazard=surface.requiredProtection==ProtectionMode.Cold?"冻伤":"灼伤";
            player.CancelSpecial();player.motor.Body.linearVelocity=Vector2.zero;player.session.Die();return false;
        }
        public bool ValidateContact()
        {
            if(player.session.InputBlocked)return false;
            if(player.RidingRail!=null&&!ValidateRail(player.RidingRail))return false;
            foreach(var hit in Physics2D.OverlapBoxAll(player.motor.Body.position,player.motor.BodySize+Vector2.one*settings.contactPadding,0,1<<6))
            {
                var surface=hit.GetComponent<ThermalSurface>();if(surface!=null&&!Touch(surface))return false;
            }
            return true;
        }
        public bool ValidateRail(ForestZipline rail)
        {
            var thermal=rail.GetComponent<ThermalZipline>();
            if(thermal==null||HasCore&&thermal.requiredProtection==Mode)return true;
            LastHazard=thermal.requiredProtection==ProtectionMode.Cold?"冻伤":"灼伤";
            player.CancelSpecial();player.session.Die();return false;
        }
        bool HasNormalJumpContact()
        {
            var p=player.motor.Body.position;var size=player.motor.BodySize;
            var floor=Physics2D.BoxCast(p+Vector2.down*(size.y*.5f-.02f),new Vector2(size.x-.12f,.02f),0,Vector2.down,.06f,1<<6);
            if(floor.collider!=null&&floor.normal.y>.65f&&player.motor.Body.linearVelocity.y<=.01f)return true;
            foreach(var direction in new[]{Vector2.left,Vector2.right})
                if(Physics2D.BoxCast(p,new Vector2(size.x-.04f,size.y*.7f),0,direction,.10f,1<<6).collider!=null)return true;
            return false;
        }
        public ClimbPoint NearestReadyPoint()
        {
            if(!HasCore)return null;
            ClimbPoint result=null;float distance=float.PositiveInfinity;var extent=PointHalfExtents;
            foreach(var point in points)
            {
                if(point==null||!point.isActiveAndEnabled||point.requiredProtection!=Mode)continue;
                var delta=(Vector2)point.transform.position-player.motor.Body.position;float d=delta.magnitude;
                if(Mathf.Abs(delta.x)>extent.x||Mathf.Abs(delta.y)>extent.y||d>distance)continue;
                if(Physics2D.Linecast(player.motor.Body.position,point.transform.position,1<<6).collider!=null)continue;
                result=point;distance=d;
            }
            return result;
        }
        public bool TryAirJump(SeasideInput input,float dt=.02f)
        {
            if(inputEpoch!=GameBindings.InputEpoch){ClearJumpGrace();inputEpoch=GameBindings.InputEpoch;}
            if(player.session.InputBlocked||player.motor.frozen||!HasCore){ClearJumpGrace();return false;}
            jumpBuffer=input.jumpDown?Mathf.Max(.001f,settings.climbJumpBuffer):Mathf.Max(0,jumpBuffer-dt);
            var point=NearestReadyPoint();
            if(point!=null){gracePoint=point;climbGrace=Mathf.Max(.001f,settings.climbCoyoteTime);}
            else climbGrace=Mathf.Max(0,climbGrace-dt);
            if(input.dashDown){ClearJumpGrace();return false;}
            if(HasNormalJumpContact()){jumpBuffer=0;return false;}
            if(jumpBuffer<=0)return false;
            if(point==null&&climbGrace>0&&gracePoint!=null&&gracePoint.isActiveAndEnabled&&gracePoint.requiredProtection==Mode&&Physics2D.Linecast(player.motor.Body.position,gracePoint.transform.position,1<<6).collider==null)point=gracePoint;
            if(point==null)return false;
            jumpBuffer=climbGrace=0;gracePoint=null;
            AirJumps++;player.motor.JumpFromTraversal(input.horizontal);
            player.Refill();player.motor.RefillTraversalAbilities();
            SeasideLog.Event("CLIMB_AIR_JUMP",point.sourceAddress+" mode="+ModeName);return true;
        }
    }
}
