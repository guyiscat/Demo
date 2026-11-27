using UnityEngine;
namespace Seaside
{
    // Render-only layer: the authored collider, rigidbody and movement remain unchanged.
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerArtwork : MonoBehaviour
    {
        SeasideMotor motor;SeasideSession session;Forest.ForestPlayer actor;
        SpriteRenderer picture;GameMediaSettings settings;
        Vector2 previous;float runClock;int facing=1;
        Vector3 authoredScale;
        SpriteRenderer departure,arrival;float teleportClock=-1;int teleports;
        public string Pose{get;private set;}
        public SpriteRenderer Picture=>picture;
        public Vector2 ColliderSizeAtInstall{get;private set;}
        public Vector3 ScaleAtInstall=>authoredScale;
        public bool TeleportVisible=>teleportClock>=0;
        void Awake()
        {
            motor=GetComponent<SeasideMotor>();actor=GetComponent<Forest.ForestPlayer>();session=FindAnyObjectByType<SeasideSession>();
            picture=GetComponent<SpriteRenderer>();settings=GameMediaSettings.Current;previous=transform.position;
            if(settings==null||settings.idle==null){enabled=false;return;}
            authoredScale=transform.localScale;
            ColliderSizeAtInstall=motor.BodySize;
            picture.drawMode=SpriteDrawMode.Simple;picture.sprite=settings.idle;picture.color=Color.white;transform.localScale=authoredScale;
            if(actor!=null){departure=Effect("TeleportDeparture");arrival=Effect("TeleportArrival");}
            motor.ResetPerformed+=ResetVisual;
        }
        SpriteRenderer Effect(string name)
        {
            var go=new GameObject(name);go.transform.SetParent(transform.parent,false);var r=go.AddComponent<SpriteRenderer>();
            r.sortingLayerID=picture.sortingLayerID;r.sortingOrder=picture.sortingOrder+1;r.enabled=false;return r;
        }
        void ResetVisual(){previous=transform.position;runClock=0;teleportClock=-1;if(departure!=null)departure.enabled=arrival.enabled=false;}
        void LateUpdate()
        {
            if(actor==null)actor=GetComponent<Forest.ForestPlayer>();
            if(actor!=null&&departure==null){departure=Effect("TeleportDeparture");arrival=Effect("TeleportArrival");}
            Vector2 position=transform.position;float dx=position.x-previous.x;
            var entry=GetComponent<RegionArrival>();bool walking=entry!=null&&entry.Walking;
            if(walking&&!entry.Dropping)facing=entry.Direction;
            else if(!motor.frozen&&Mathf.Abs(dx)>.001f&&Mathf.Abs(dx)<2)facing=dx>0?1:-1;
            Sprite sprite=settings.idle;Pose="Idle";
            if(!session.Respawning)
            {
                if(entry!=null&&entry.Dropping){sprite=settings.jumpFall;Pose="JumpFall";}
                else if(motor.Dashing){sprite=settings.dash;Pose="Dash";}
                else if(actor!=null&&(actor.BeaconActive||actor.PostTeleportHoverRemaining>0)){sprite=settings.jumpApex;Pose="Hover";}
                else if(walking||motor.Grounded&&Mathf.Abs(motor.Body.linearVelocity.x)>.1f)
                {runClock+=walking?Time.unscaledDeltaTime:Time.deltaTime;sprite=settings.run[(int)(runClock*settings.runFrameRate)%settings.run.Length];Pose="Run";}
                else if(!motor.frozen&&!motor.Grounded)
                {float y=motor.Body.linearVelocity.y;sprite=y>5?settings.jumpRise:y>1?settings.jumpApex:y< -5?settings.jumpFall:settings.jumpTransition??settings.jumpApex;Pose=y>1?"JumpRise":y< -1?"JumpFall":"JumpApex";}
            }
            picture.sprite=sprite;picture.flipX=facing<0;picture.color=Color.white;transform.localScale=authoredScale;
            if(actor!=null)
            {
                if(actor.Teleports!=teleports)
                {
                    if(actor.Teleports>teleports&&!session.Respawning&&!session.Travelling&&Vector2.Distance(previous,position)>1)
                    {teleportClock=0;departure.transform.position=previous;arrival.transform.position=position;}
                    teleports=actor.Teleports;
                }
                if(session.Respawning||session.Travelling){teleportClock=-1;departure.enabled=arrival.enabled=false;}
                if(teleportClock>=0)
                {
                    int frame=(int)(teleportClock*settings.teleportFrameRate);int half=settings.teleport.Length/2;
                    departure.enabled=arrival.enabled=frame<half;
                    if(frame<half){departure.sprite=settings.teleport[frame];arrival.sprite=settings.teleport[half+frame];departure.flipX=arrival.flipX=facing<0;}
                    teleportClock+=Time.deltaTime;if(frame>=half)teleportClock=-1;
                }
            }
            previous=position;
        }
        void OnDestroy()
        {if(motor!=null)motor.ResetPerformed-=ResetVisual;if(departure!=null)Destroy(departure.gameObject);if(arrival!=null)Destroy(arrival.gameObject);}
    }
}
