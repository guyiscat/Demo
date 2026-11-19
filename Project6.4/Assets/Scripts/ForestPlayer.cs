using System.Collections.Generic;
using UnityEngine;
using Seaside;

namespace Forest
{
    public struct ForestInput
    {
        public SeasideInput movement;
        public bool teleportDown,rideHeld;
    }

    // Shared traversal driver for Forest and Mine; keep the original type name
    // and namespace so existing scene references remain valid. Normal movement
    // executes SeasideMotor.Tick with the unchanged shared Movement asset.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(SeasideMotor))]
    public sealed class ForestPlayer : MonoBehaviour
    {
        public ForestSettings settings;
        public SeasideMotor motor;
        public SeasideSession session;
        public ForestWaterwheel wheel;
        public ForestWaterfall waterfall;
        public Expedition.ThermalTraversal environment;
        public ForestZipline[] ziplines;
        public SpriteRenderer beacon;
        public LineRenderer beaconTrail;
        public bool manualInput;
        public bool BeaconActive{get;private set;}
        public Vector2 BeaconPosition{get;private set;}
        public float BeaconDistance{get;private set;}
        public bool TeleportCharged{get;private set;}=true;
        public ForestZipline RidingRail{get;private set;}
        public MovingSurface RidingSurface{get;private set;}
        public ForestWheelPlatform RidingWheel=>RidingSurface as ForestWheelPlatform;
        public int Facing{get;private set;}=1;
        public int Teleports{get;private set;}
        public string LastTeleportResult{get;private set;}="READY";
        public float PostTeleportHoverRemaining{get;private set;}
        public float RailCoyoteRemaining{get;private set;}
        public ForestAimIndicator AimIndicator{get;private set;}
        public Vector2 AimDirection{get;private set;}=Vector2.right;
        readonly SeasideInputLatch inputLatch=new SeasideInputLatch();
        readonly List<Vector2> beaconPath=new List<Vector2>();
        bool teleportPressed,rideHeld,touchedTerrain;
        float railDistance,railDirection,catchCooldown;
        Vector2 beaconDirection,launchPosition;
        float wheelCarrySpeed;

        void Awake()
        {
            if(motor==null)motor=GetComponent<SeasideMotor>();
            if(session==null)session=FindAnyObjectByType<SeasideSession>();
            motor.externalInput=true;
            AimIndicator=gameObject.AddComponent<ForestAimIndicator>();
            AimIndicator.player=this;
        }
        void OnEnable()
        {
            if(motor==null)motor=GetComponent<SeasideMotor>();
            motor.ResetPerformed+=OnRespawn;
        }
        void OnDisable(){if(motor!=null)motor.ResetPerformed-=OnRespawn;CancelSpecial();}
        void Update()
        {
            if(manualInput)return;
            if(session.Paused||session.InputBlocked||motor.frozen)
            {ClearInput();return;}
            Vector2 aim=ReadAimDirection();
            inputLatch.Sample(Input.GetKey(KeyCode.A),Input.GetKey(KeyCode.D),Input.GetKeyDown(KeyCode.Space),
                Input.GetKey(KeyCode.Space),Input.GetMouseButtonDown(0),aim,motor.Attached,motor.WallSide);
            teleportPressed|=Input.GetMouseButtonDown(1);rideHeld=Input.GetKey(KeyCode.W);
        }
        void FixedUpdate()
        {
            if(manualInput)return;
            Tick(Time.fixedDeltaTime,new ForestInput{movement=inputLatch.Consume(),teleportDown=teleportPressed,rideHeld=rideHeld});
            teleportPressed=false;
        }
        void ClearInput(){inputLatch.Clear();teleportPressed=rideHeld=false;}
        public Vector2 ReadAimDirection()
        {
            var camera=Camera.main;
            if(camera!=null)
            {
                Vector2 direction=(Vector2)camera.ScreenToWorldPoint(Input.mousePosition)-(Vector2)transform.position;
                if(direction.sqrMagnitude>.001f)AimDirection=direction.normalized;
            }
            return AimDirection;
        }
        void OnRespawn()
        {
            CancelSpecial();TeleportCharged=true;motor.SetTeleportCharge(true);
            touchedTerrain=false;catchCooldown=0;Facing=1;ClearInput();
            waterfall?.ResetSide(motor.Body.position);
        }
        public void CancelSpecial()
        {
            if(BeaconActive&&motor!=null&&motor.Body!=null)ResetHoverMotion();
            BeaconActive=false;RidingRail=null;RidingSurface=null;beaconPath.Clear();
            wheelCarrySpeed=0;
            PostTeleportHoverRemaining=RailCoyoteRemaining=0;
            if(beacon!=null)beacon.enabled=false;
            if(beaconTrail!=null)beaconTrail.enabled=false;
        }
        public void Tick(float dt,ForestInput input)
        {
            if(session.Paused){ClearInput();return;}
            if(session.InputBlocked||motor.frozen){CancelSpecial();motor.Body.linearVelocity=Vector2.zero;return;}
            if(environment!=null&&!environment.ValidateContact())return;
            if(Mathf.Abs(input.movement.horizontal)>.1f)Facing=input.movement.horizontal>0?1:-1;
            if(input.movement.aim.sqrMagnitude>.001f)AimDirection=input.movement.aim.normalized;
            catchCooldown=Mathf.Max(0,catchCooldown-dt);
            bool contact=TouchesTerrain();
            if(contact&&!touchedTerrain)Refill();
            touchedTerrain=contact;
            if(input.teleportDown)
            {
                if(BeaconActive){CommitTeleport("MANUAL");return;}
                if(TeleportCharged&&(!motor.Dashing||settings.teleportInterruptsDash))LaunchBeacon(input.movement.aim);
            }
            if(BeaconActive&&AdvanceBeacon(dt))return;
            if(BeaconActive)
            {
                TickBeaconHover(dt,input.movement);
                motor.SetTeleportCharge(TeleportCharged);
                return;
            }
            if(environment!=null&&RidingRail==null&&!motor.Dashing&&environment.TryAirJump(input.movement))
            {PostTeleportHoverRemaining=RailCoyoteRemaining=0;return;}
            if(PostTeleportHoverRemaining>0)
            {
                if(input.movement.dashDown&&motor.DashReady)PostTeleportHoverRemaining=0;
                else
                {
                    motor.Body.linearVelocity=new Vector2(motor.Body.linearVelocity.x,0);
                    motor.Tick(dt,input.movement);
                    if(input.movement.jumpDown&&motor.Body.linearVelocity.y>0)PostTeleportHoverRemaining=0;
                    else
                    {
                        motor.Body.linearVelocity=new Vector2(motor.Body.linearVelocity.x,0);
                        PostTeleportHoverRemaining=Mathf.Max(0,PostTeleportHoverRemaining-dt);
                        if(input.rideHeld&&catchCooldown<=0&&TryCatchRail())PostTeleportHoverRemaining=0;
                        motor.SetTeleportCharge(TeleportCharged);
                        return;
                    }
                    motor.SetTeleportCharge(TeleportCharged);return;
                }
            }
            if(RidingRail!=null)
            {
                if(input.movement.jumpDown){LeaveRide(true,input.movement.horizontal);return;}
                if(input.movement.dashDown)LeaveRide(false,0);
                else {RideRail(dt,input.movement.horizontal);return;}
            }
            if(RailCoyoteRemaining>0)
            {
                if(input.movement.dashDown)RailCoyoteRemaining=0;
                else if(input.movement.jumpDown)
                {RailCoyoteRemaining=0;motor.JumpFromTraversal(input.movement.horizontal);return;}
                else RailCoyoteRemaining=Mathf.Max(0,RailCoyoteRemaining-dt);
            }
            TickLocomotion(dt,input.movement);
            if(motor.Grounded||motor.Attached)RailCoyoteRemaining=0;
            // The legacy reserved property is mirrored to the Forest charge. A
            // launch while standing cannot refill on every tick of the same contact.
            motor.SetTeleportCharge(TeleportCharged);
            if(input.rideHeld&&catchCooldown<=0&&!motor.Dashing&&TryCatchRail())return;
        }
        public void Refill(){TeleportCharged=true;motor.SetTeleportCharge(true);}
        void RefillOnRail(){Refill();motor.RefillTraversalAbilities();}
        bool TouchesTerrain()
        {
            foreach(var hit in Physics2D.OverlapBoxAll(motor.Body.position,motor.BodySize+Vector2.one*.12f,0,1<<6))
                if(!hit.isTrigger)return true;
            return false;
        }
        public bool IsFree(Vector2 point,Collider2D ignore=null)
        {
            foreach(var hit in Physics2D.OverlapBoxAll(point,motor.BodySize-Vector2.one*.006f,0,1<<6))
                if(!hit.isTrigger&&hit!=ignore)return false;
            return true;
        }
        void LaunchBeacon(Vector2 aim)
        {
            // A beacon suspends the character, including an ongoing jump, dash
            // or ride. Preserve only self-propelled horizontal momentum.
            bool interruptedDash=motor.Dashing;
            ResetHoverMotion();
            // Cancel the burst immediately instead of coasting horizontally at
            // dash speed during beacon hover. Normal A/D movement resumes below.
            if(interruptedDash)motor.Body.linearVelocity=Vector2.zero;
            RidingRail=null;RidingSurface=null;wheelCarrySpeed=0;
            PostTeleportHoverRemaining=RailCoyoteRemaining=0;
            TeleportCharged=false;motor.SetTeleportCharge(false);BeaconActive=true;BeaconDistance=0;
            beaconDirection=aim.sqrMagnitude>.001f?aim.normalized:new Vector2(Facing,0);
            launchPosition=BeaconPosition=motor.Body.position;beaconPath.Clear();beaconPath.Add(BeaconPosition);
            beacon.enabled=true;beacon.transform.position=BeaconPosition;
            beaconTrail.enabled=true;beaconTrail.SetPosition(0,launchPosition);beaconTrail.SetPosition(1,BeaconPosition);
            SeasideLog.Event("FOREST_BEACON","launch="+launchPosition+" aim="+beaconDirection);
        }
        void ResetHoverMotion()
        {
            float horizontal=motor.Body.linearVelocity.x-(RidingSurface!=null?wheelCarrySpeed:0);
            motor.BeginTraversal();
            motor.Body.linearVelocity=new Vector2(horizontal,0);
        }
        void TickBeaconHover(float dt,SeasideInput movement)
        {
            // Reuse normal horizontal acceleration and collision probes. Jump,
            // dash and W do not interrupt this short beacon-flight window or
            // queue a vertical launch for its end. Gravity is manual in Tick,
            // so cancel its vertical result rather than changing gravityScale.
            motor.Body.linearVelocity=new Vector2(motor.Body.linearVelocity.x,0);
            motor.Tick(dt,new SeasideInput{horizontal=movement.horizontal,wallReleased=movement.wallReleased});
            motor.Body.linearVelocity=new Vector2(motor.Body.linearVelocity.x,0);
        }
        bool AdvanceBeacon(float dt)
        {
            float travel=Mathf.Min(settings.beaconSpeed*dt,settings.teleportDistance-BeaconDistance);
            bool blocked=false;
            foreach(var hit in Physics2D.CircleCastAll(BeaconPosition,settings.beaconRadius,beaconDirection,travel,1<<6))
            {
                if(hit.collider==null||hit.collider.isTrigger)continue;
                travel=Mathf.Min(travel,Mathf.Max(0,hit.distance-.005f));blocked=true;
            }
            int steps=Mathf.Max(1,Mathf.CeilToInt(travel/.08f));
            Vector2 previous=BeaconPosition;
            for(int i=1;i<=steps;i++)beaconPath.Add(previous+beaconDirection*(travel*i/steps));
            BeaconPosition+=beaconDirection*travel;BeaconDistance+=travel;
            beacon.transform.position=BeaconPosition;beaconTrail.SetPosition(1,BeaconPosition);
            if(blocked||BeaconDistance>=settings.teleportDistance-.0001f)
            {CommitTeleport(blocked?"TERRAIN":"RANGE");return true;}
            return false;
        }
        void CommitTeleport(string reason)
        {
            bool found=false;Vector2 destination=motor.Body.position;
            // A small beacon can enter a hole which the 2x2 character cannot.
            // Search actual travelled samples backwards, never teleport into a solid.
            for(int i=beaconPath.Count-1;i>=0;i--)if(IsFree(beaconPath[i]))
            {destination=beaconPath[i];found=true;break;}
            LastTeleportResult=found?reason:"NO SAFE LANDING";
            wheelCarrySpeed=0;
            BeaconActive=false;beacon.enabled=false;beaconTrail.enabled=false;
            RidingRail=null;RidingSurface=null;catchCooldown=.2f;
            if(found)
            {
                motor.WarpForTraversal(destination);Teleports++;touchedTerrain=false;
                PostTeleportHoverRemaining=Mathf.Max(0,settings.teleportHoverDuration);
                Physics2D.SyncTransforms();waterfall?.Refresh();
                environment?.ValidateContact();
                foreach(var hit in Physics2D.OverlapBoxAll(destination,motor.BodySize-Vector2.one*.01f,0,1<<7))
                    if(hit.GetComponent<SeasideCell>()?.kind==CellKind.Spike){session.Die();break;}
            }
            else ResetHoverMotion();
            SeasideLog.Event("FOREST_TELEPORT",LastTeleportResult+" at="+destination+" distance="+BeaconDistance);
            beaconPath.Clear();
        }
        bool TryCatchRail()
        {
            ForestZipline nearest=null;float best=float.MaxValue;
            foreach(var rail in ziplines)
            {
                if(rail==null||!rail.isActiveAndEnabled)continue;
                if(!rail.CanCatch(motor.Body.position,motor.BodySize,settings.ziplineContactPadding,settings.ziplineCatchDistance))continue;
                float distance=Vector2.Distance(motor.Body.position,rail.Point(rail.Project(motor.Body.position)));
                if(distance<best){best=distance;nearest=rail;}
            }
            if(nearest==null)return false;
            if(environment!=null&&!environment.ValidateRail(nearest))return false;
            float progress=nearest.Project(motor.Body.position);
            Vector2 target=nearest.Point(progress)-Vector2.up*(motor.BodySize.y*.5f+.12f);
            if(!IsFree(target))return false;
            RidingRail=nearest;railDistance=progress;
            RailCoyoteRemaining=PostTeleportHoverRemaining=0;
            railDirection=Vector2.Dot(new Vector2(Facing,0),nearest.Tangent)>=0?1:-1;
            motor.WarpForTraversal(target);RefillOnRail();return true;
        }
        void RideRail(float dt,float horizontal)
        {
            var rail=RidingRail;RefillOnRail();
            if(Mathf.Abs(horizontal)>.1f)
                railDirection=Vector2.Dot(new Vector2(horizontal,0),rail.Tangent)>=0?1:-1;
            float next=Mathf.Clamp(railDistance+railDirection*settings.ziplineSpeed*dt,0,rail.Length);
            Vector2 target=rail.Point(next)-Vector2.up*(motor.BodySize.y*.5f+.12f);
            Vector2 delta=target-motor.Body.position;
            if(BodyBlocked(delta,null)||!IsFree(target)){LeaveRide(false,0);return;}
            motor.Body.MovePosition(target);motor.Body.linearVelocity=Vector2.zero;railDistance=next;
            if(next<=0||next>=rail.Length)
            {
                RidingRail=null;catchCooldown=.25f;
                RailCoyoteRemaining=Mathf.Max(0,settings.ziplineCoyoteTime);
                motor.Body.linearVelocity=rail.Tangent*railDirection*settings.ziplineSpeed;
            }
        }
        // Wheel support is a physical contact, not a traversal lock. Each step
        // re-evaluates the feet so walking, jumping and landing can all reattach.
        void TickLocomotion(float dt,SeasideInput movement)
        {
            var support=FindMovingSupport();
            Vector2 velocity=motor.Body.linearVelocity;
            if(support!=null)
            {
                // Let the existing motor calculate the player's own velocity.
                if(RidingSurface==support)velocity.x-=wheelCarrySpeed;
                else wheelCarrySpeed=0;
                velocity.y=0;
                motor.Body.linearVelocity=velocity;
            }
            else wheelCarrySpeed=0;
            RidingSurface=support;
            motor.Tick(dt,movement);
            if(support==null)return;
            if(!motor.Grounded||motor.Dashing||movement.jumpDown&&motor.Body.linearVelocity.y>1)
            {RidingSurface=null;wheelCarrySpeed=0;return;}
            float target=Mathf.Clamp(support.Velocity.x,-settings.wheelCarryMaxSpeed,settings.wheelCarryMaxSpeed);
            wheelCarrySpeed=Mathf.MoveTowards(wheelCarrySpeed,target,settings.wheelCarryAcceleration*dt);
            velocity=motor.Body.linearVelocity;
            velocity.x+=wheelCarrySpeed;
            // Vertical support carries normally; horizontal traction is capped.
            // Idle players slip off, while A/D can help keep pace or leave.
            float top=support.NextPosition.y+support.Collider.bounds.extents.y;
            velocity.y=(top+motor.BodySize.y*.5f+.012f-motor.Body.position.y)/dt;
            motor.Body.linearVelocity=velocity;
        }
        MovingSurface FindMovingSupport()
        {
            if(motor.Dashing)return null;
            Vector2 foot=motor.Body.position-Vector2.up*(motor.BodySize.y*.5f-.02f);
            foreach(var hit in Physics2D.BoxCastAll(foot,new Vector2(motor.BodySize.x-.12f,.02f),0,Vector2.down,.10f,1<<6))
            {
                if(hit.collider==null||hit.collider.isTrigger||hit.normal.y<.65f)continue;
                var platform=hit.collider.GetComponent<MovingSurface>();
                if(platform==null)continue;
                if(platform is ForestWheelPlatform wheelPlatform&&wheelPlatform.wheel!=wheel)continue;
                float relativeY=motor.Body.linearVelocity.y-platform.Velocity.y;
                if(relativeY>1&&RidingSurface!=platform)return null;
                return platform;
            }
            return null;
        }
        bool BodyBlocked(Vector2 delta,Collider2D ignore)
        {
            if(delta.sqrMagnitude<.000001f)return false;
            foreach(var hit in Physics2D.BoxCastAll(motor.Body.position,motor.BodySize-Vector2.one*.01f,0,delta.normalized,delta.magnitude,1<<6))
                if(hit.collider!=null&&!hit.collider.isTrigger&&hit.collider!=ignore&&Vector2.Dot(hit.normal,delta)<-.0001f)return true;
            return false;
        }
        void LeaveRide(bool jump,float direction)
        {
            RidingRail=null;RidingSurface=null;catchCooldown=.25f;
            wheelCarrySpeed=0;
            RailCoyoteRemaining=PostTeleportHoverRemaining=0;
            if(jump)motor.JumpFromTraversal(direction);else motor.BeginTraversal();
        }
    }
}
