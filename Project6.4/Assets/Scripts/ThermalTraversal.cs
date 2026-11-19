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
        readonly HashSet<ClimbPoint> used=new HashSet<ClimbPoint>();
        public string ModeName=>Mode==ProtectionMode.Cold?"防寒":"隔热";
        void Awake(){if(player==null)player=GetComponent<ForestPlayer>();ResetState();}
        void OnEnable(){if(player==null)player=GetComponent<ForestPlayer>();player.motor.ResetPerformed+=ResetState;}
        void OnDisable(){if(player!=null&&player.motor!=null)player.motor.ResetPerformed-=ResetState;}
        void Update()
        {
            if(!player.manualInput&&Input.GetKeyDown(KeyCode.S))TrySwitchMode();
            if(points!=null)foreach(var point in points)point.ShowState(Mode==point.requiredProtection&&!used.Contains(point),settings.climbRadius);
        }
        public void ResetState(){Mode=settings==null?ProtectionMode.Cold:settings.initialProtection;AirJumps=0;used.Clear();LastHazard=null;}
        public bool TrySwitchMode()
        {
            if(player.session.InputBlocked||player.motor.frozen)return false;
            Mode=Mode==ProtectionMode.Cold?ProtectionMode.Heat:ProtectionMode.Cold;
            SeasideLog.Event("PROTECTION_SWITCH",ModeName);ValidateContact();return true;
        }
        public bool Touch(ThermalSurface surface)
        {
            if(surface==null||player.session.InputBlocked||surface.requiredProtection==Mode)return true;
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
            if(thermal==null||thermal.requiredProtection==Mode)return true;
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
            ClimbPoint result=null;float distance=settings.climbRadius;
            foreach(var point in points)
            {
                if(point==null||!point.isActiveAndEnabled||point.requiredProtection!=Mode||used.Contains(point))continue;
                var delta=(Vector2)point.transform.position-player.motor.Body.position;float d=delta.magnitude;
                if(d>distance)continue;
                if(Physics2D.Linecast(player.motor.Body.position,point.transform.position,1<<6).collider!=null)continue;
                result=point;distance=d;
            }
            return result;
        }
        public bool TryAirJump(SeasideInput input)
        {
            if(HasNormalJumpContact())
            {
                // A physical landing or wall attachment begins a new aerial sequence.
                // Merely leaving/re-entering the same ring does not rearm it.
                if(player.motor.Grounded||player.motor.Attached)used.Clear();
                return false;
            }
            if(!input.jumpDown||player.session.InputBlocked||player.motor.frozen)return false;
            var point=NearestReadyPoint();if(point==null)return false;
            used.Add(point);AirJumps++;player.motor.JumpFromTraversal(input.horizontal);
            player.Refill();player.motor.RefillTraversalAbilities();
            SeasideLog.Event("CLIMB_AIR_JUMP",point.sourceAddress+" mode="+ModeName);return true;
        }
    }
}
