using UnityEngine;
namespace Seaside
{
    [System.Serializable] public struct SeasideInput
    {
        public float horizontal;
        public bool jumpDown,jumpHeld,dashDown,wallReleased;
        public Vector2 aim;
    }
    [RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]
    public sealed class SeasideMotor:MonoBehaviour
    {
        public SeasideSettings settings;
        public bool externalInput,frozen;
        public Rigidbody2D Body{get;private set;}
        public bool Grounded{get;private set;}
        public bool Attached{get;private set;}
        public bool WallSliding{get;private set;}
        public bool DashStoppedByTerrain{get;private set;}
        public int ReleasedWallSide=>releasedWallSide;
        public float releasedSlideSpeed=3.5f,releasedSlideBlend=.28f;
        public bool DashReady{get;private set;}=true;
        public bool TeleportReady{get;private set;}=true;
        public bool Dashing=>dashRemaining>0;
        public Vector2 BodySize=>GetComponent<BoxCollider2D>().size;
        public int WallSide{get;private set;}
        public int DashUses{get;private set;}
        public int WallKicks{get;private set;}
        public float FallSpeed=>Mathf.Max(0,-Body.linearVelocity.y);
        public string State=>frozen?"STOP":Dashing?"DASH":Attached?"ATTACHED":WallSliding?"WALL SLIDE":Grounded?"GROUND":Body.linearVelocity.y>0?"RISE":"FALL";
        readonly SeasideInputLatch controls=new SeasideInputLatch();
        float coyote,buffer,wallLock,dashRemaining,hover,fallAge,jumpOrigin;
        bool variableJump,brakePending,groundJumpAvailable,requireGroundExit;
        int releasedWallSide;
        float separationTime,slideVelocity;
        float wallGrace;
        int graceWallSide;
        Vector2 dashDirection;
        SpriteRenderer picture;
        SeasideSession session;
        void Awake(){Body=GetComponent<Rigidbody2D>();picture=GetComponent<SpriteRenderer>();session=FindAnyObjectByType<SeasideSession>();}
        void Update()
        {
            if(!externalInput)
            {
                Vector3 mouse=Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 aim=((Vector2)mouse-(Vector2)transform.position).normalized;
                controls.Sample(Input.GetKey(KeyCode.A),Input.GetKey(KeyCode.D),Input.GetKeyDown(KeyCode.Space),
                    Input.GetKey(KeyCode.Space),Input.GetMouseButtonDown(0),aim,Attached,WallSide);
            }
            if(picture!=null)picture.color=Attached?new Color(1,.77f,.23f):WallSliding?new Color(.35f,.65f,1f):DashReady?new Color(.15f,.82f,.8f):new Color(.43f,.52f,.6f);
        }
        void FixedUpdate()
        {
            if(externalInput)return;
            Tick(Time.fixedDeltaTime,controls.Consume());
        }
        public void Tick(float dt,SeasideInput c)
        {
            if(Body==null)Body=GetComponent<Rigidbody2D>();
            if(frozen||(session!=null&&session.InputBlocked)){Body.linearVelocity=Vector2.zero;return;}
            Vector2 p=Body.position;int mask=1<<6;
            Vector2 size=BodySize;
            var floor=Physics2D.BoxCast(p+Vector2.down*(size.y*.5f-.02f),new Vector2(size.x-.12f,.02f),0,Vector2.down,.055f,mask);
            bool floorContact=floor.collider!=null&&floor.normal.y>.65f;
            if(!floorContact)requireGroundExit=false;
            // Only a downward/standing floor contact can rearm the single ground jump.
            Grounded=floorContact&&Body.linearVelocity.y<=.01f&&!requireGroundExit&&!Dashing;
            var leftHit=Physics2D.BoxCast(p,new Vector2(size.x-.04f,size.y*.7f),0,Vector2.left,.10f,mask).collider;
            var rightHit=Physics2D.BoxCast(p,new Vector2(size.x-.04f,size.y*.7f),0,Vector2.right,.10f,mask).collider;
            bool left=leftHit!=null,right=rightHit!=null;
            WallSide=right?1:left?-1:0;
            wallLock=Mathf.Max(0,wallLock-dt);
            coyote=Grounded?settings.coyoteTime:Mathf.Max(0,coyote-dt);
            if(Grounded)groundJumpAvailable=true;
            else if(coyote<=0)groundJumpAvailable=false;
            buffer=c.jumpDown?settings.jumpBuffer:Mathf.Max(0,buffer-dt);
            if(c.jumpDown)SeasideLog.Event("JUMP_REQUEST","pos="+p+" dir="+c.horizontal+" wall="+WallSide+" groundGrace="+coyote+" wallGrace="+wallGrace+" lock="+wallLock);
            bool wasAttached=Attached;
            if(Grounded){DashReady=TeleportReady=true;fallAge=0;releasedWallSide=0;}
            if(releasedWallSide!=0)
            {
                bool separated=releasedWallSide==1?!right:!left;
                separationTime=separated?separationTime+dt:0;
                if(separationTime>=.12f){releasedWallSide=0;separationTime=0;}
            }
            if(wasAttached&&(c.wallReleased||c.horizontal*WallSide<=.1f))
            {
                releasedWallSide=WallSide;separationTime=0;slideVelocity=0;
            }
            var wall=right?rightHit:leftHit;var cell=wall==null?null:wall.GetComponent<SeasideCell>();
            wallGrace=Mathf.Max(0,wallGrace-dt);
            if(Grounded){wallGrace=0;graceWallSide=0;}
            else if(WallSide!=0&&wallLock<=0&&!Dashing)
            {wallGrace=settings.wallCoyoteTime;graceWallSide=WallSide;}
            // Current held direction wins, including release/repress between two physics ticks.
            Attached=!Grounded&&WallSide!=0&&(cell==null||cell.attachable)&&c.horizontal*WallSide>.1f&&wallLock<=0&&!Dashing;
            WallSliding=!Grounded&&!Attached&&WallSide!=0&&releasedWallSide==WallSide&&c.horizontal*WallSide>=-.1f&&wallLock<=0&&!Dashing;
            if(Attached)
            {
                DashReady=TeleportReady=true;Body.linearVelocity=Vector2.zero;fallAge=0;hover=0;variableJump=false;
                coyote=0;groundJumpAvailable=false;slideVelocity=0;
            }
            if(c.dashDown&&DashReady)
            {
                DashReady=false;DashUses++;dashDirection=c.aim.sqrMagnitude>.001f?c.aim.normalized:Vector2.right;DashStoppedByTerrain=false;
                dashRemaining=settings.dashDistance;hover=0;Attached=false;variableJump=false;
                coyote=buffer=0;groundJumpAvailable=false;
                wallGrace=0;graceWallSide=0;
            }
            if(Dashing)
            {
                float distance=Mathf.Min(settings.dashSpeed*dt,dashRemaining);
                float allowed=distance;bool hitTerrain=false;
                foreach(var hit in Physics2D.BoxCastAll(p,size-Vector2.one*.01f,0,dashDirection,distance+.025f,mask))
                {
                    if(hit.collider==null||hit.normal.sqrMagnitude<.5f||Vector2.Dot(hit.normal,dashDirection)>=-.02f)continue;
                    allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-.025f));hitTerrain=true;
                }
                if(hitTerrain)
                {
                    Body.linearVelocity=dashDirection*(allowed/dt);
                    dashRemaining=0;hover=0;brakePending=true;DashStoppedByTerrain=true;
                    WallSliding=false;return;
                }
                // Shorten the last fixed step so the commanded burst matches the configured map-cell distance.
                Body.linearVelocity=dashDirection*(distance/dt);dashRemaining=Mathf.Max(0,dashRemaining-distance);
                if(dashRemaining<.00001f){dashRemaining=0;hover=settings.dashHoverDuration;fallAge=0;brakePending=true;}
                return;
            }
            Vector2 v=Body.linearVelocity;
            if(brakePending){v=Vector2.zero;brakePending=false;}
            if(Attached)v=Vector2.zero;
            else if(WallSliding)
            {
                v.x=0;
                v.y=Mathf.SmoothDamp(v.y,-releasedSlideSpeed,ref slideVelocity,releasedSlideBlend,Mathf.Infinity,dt);
                v.y=Mathf.Min(v.y,-.08f);fallAge=0;hover=0;variableJump=false;
            }
            else
            {
                if(wallLock<=0)v.x=Mathf.MoveTowards(v.x,c.horizontal*settings.moveSpeed,(Grounded?settings.groundAcceleration:settings.airAcceleration)*dt);
                if(hover>0){hover-=dt;v.y=0;}
                else if(v.y>0)
                {
                    // Fixed-step ballistic update: with .5*g*dt correction the measured apex is stable.
                    float g=settings.ascentGravity;
                    v.y-=g*dt;
                    if(variableJump&&!c.jumpHeld)
                    {
                        float remaining=Mathf.Max(0,settings.shortJumpHeight-(p.y-jumpOrigin));
                        v.y=Mathf.Min(v.y,Mathf.Max(0,Mathf.Sqrt(2*g*remaining)-.5f*g*dt));
                        variableJump=false;
                    }
                    if(v.y<=0)fallAge=0;
                }
                else
                {
                    fallAge+=dt;float g=Mathf.Lerp(settings.fallGravityStart,settings.fallGravityEnd,Mathf.Clamp01(fallAge/settings.fallRampTime));
                    v.y=Mathf.Max(-settings.terminalFallSpeed,v.y-g*dt);
                }
            }
            bool groundJump=groundJumpAvailable&&(Grounded||coyote>0);
            int kickSide=WallSide!=0?WallSide:graceWallSide;
            bool wallJump=!Grounded&&wallLock<=0&&(WallSide!=0||wallGrace>0);
            if(buffer>0&&(groundJump||wallJump))
            {
                bool kick=!groundJump&&wallJump;
                SeasideLog.Event(kick?"WALL_JUMP":"GROUND_JUMP","pos="+p+" side="+(kick?kickSide:0)+" grace="+(kick?wallGrace:coyote));
                jumpOrigin=p.y;variableJump=true;hover=0;fallAge=0;
                v.y=Mathf.Sqrt(2*settings.ascentGravity*settings.longJumpHeight)-.5f*settings.ascentGravity*dt;
                if(kick){v.x=-kickSide*settings.wallKickSpeed;wallLock=settings.wallKickLock;WallKicks++;releasedWallSide=kickSide;}
                wallGrace=0;graceWallSide=0;
                coyote=buffer=0;groundJumpAvailable=false;requireGroundExit=true;
                Attached=false;WallSliding=false;Grounded=false;
            }
            Body.linearVelocity=v;
            if(p.y< -6)session?.Die();
        }
        public void ResetAt(Vector2 p)
        {
            GetComponent<DashAfterimage>()?.Clear();
            if(Body==null)Body=GetComponent<Rigidbody2D>();Body.position=p;transform.position=new Vector3(p.x,p.y,0);Body.linearVelocity=Vector2.zero;
            dashRemaining=hover=fallAge=wallLock=coyote=buffer=0;variableJump=brakePending=false;Attached=false;Grounded=false;WallSide=0;
            DashReady=TeleportReady=true;frozen=false;controls.Clear();DashUses=WallKicks=0;
            releasedWallSide=0;separationTime=slideVelocity=0;WallSliding=DashStoppedByTerrain=false;
            groundJumpAvailable=requireGroundExit=false;
            wallGrace=0;graceWallSide=0;
        }
    }
}
