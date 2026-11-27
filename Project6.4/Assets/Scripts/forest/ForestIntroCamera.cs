using UnityEngine;

namespace Forest
{
    // Runs after the ordinary follow camera and waterfall boundary pass.
    [DefaultExecutionOrder(250)]
    public sealed class ForestIntroCamera : MonoBehaviour
    {
        public ForestLevel level;
        public bool IsPlaying=>phase>=1&&phase<=3;
        public bool Completed=>phase==4;
        public int Phase=>phase;
        int phase;
        float walkDistance,lastX,blend,hold;
        Vector3 startPosition;
        float startSize;
        bool initialized;
        void Start()
        {
            lastX=level.player.transform.position.x;initialized=true;
            level.player.motor.ResetPerformed+=OnRespawn;
        }
        void LateUpdate()
        {
            if(level.player.manualInput||level.session.Travelling)return;
            Tick(Time.deltaTime);
        }
        void OnRespawn()
        {
            bool restart=level.session.Timer<=.01f&&level.session.Deaths==0&&level.session.CheckpointId==0;
            Cancel(restart||phase==0);
        }
        public void Cancel(bool rearm=false)
        {
            bool wasPlaying=IsPlaying;
            phase=rearm?0:4;blend=hold=walkDistance=0;
            lastX=level.player.transform.position.x;
            level.player.waterfall.FullMapReveal=false;
            if(wasPlaying){level.cameraRig.Snap();level.player.waterfall.ResetSide(level.player.transform.position);}
        }
        public void Tick(float dt)
        {
            var actor=level.player;var session=level.session;
            if(!initialized){lastX=actor.transform.position.x;initialized=true;}
            // The follow rig still writes a pose while paused. Reapply the
            // reveal pose without advancing any of its timers.
            if(session.Paused)dt=0;
            else if(session.InputBlocked||actor.motor.frozen){if(IsPlaying)Cancel();lastX=actor.transform.position.x;return;}
            float x=actor.transform.position.x;
            if(phase==0)
            {
                if(dt<=0)return;
                // Count actual forward walking; teleports, dashes and rides cannot trigger the shot.
                float delta=x-lastX;lastX=x;
                if(actor.motor.Grounded&&!actor.motor.Dashing&&!actor.BeaconActive&&actor.RidingRail==null&&delta>0&&delta<actor.motor.settings.moveSpeed*dt*2+.1f)
                    walkDistance+=delta;
                if(walkDistance<actor.settings.introWalkDistance)return;
                var camera=level.cameraRig.GetComponent<Camera>();
                startPosition=camera.transform.position;startSize=camera.orthographicSize;
                phase=1;blend=0;level.player.waterfall.FullMapReveal=true;
            }
            if(!IsPlaying)return;
            var view=level.cameraRig.GetComponent<Camera>();
            GetFullMapPose(view,out var fullPosition,out var fullSize);
            float speed=Mathf.Max(.01f,actor.settings.introZoomSpeed);
            if(phase==1)
            {
                blend=Mathf.MoveTowards(blend,1,speed*dt);
                float t=Mathf.SmoothStep(0,1,blend);
                view.transform.position=Vector3.Lerp(startPosition,fullPosition,t);
                view.orthographicSize=Mathf.Lerp(startSize,fullSize,t);
                if(blend>=1){phase=2;hold=0;}
            }
            else if(phase==2)
            {
                view.transform.position=fullPosition;view.orthographicSize=fullSize;
                hold+=dt;
                if(hold>=actor.settings.introHoldDuration){phase=3;blend=1;}
            }
            else
            {
                blend=Mathf.MoveTowards(blend,0,speed*dt);
                float t=Mathf.SmoothStep(0,1,blend);
                view.transform.position=Vector3.Lerp(view.transform.position,fullPosition,t);
                view.orthographicSize=Mathf.Lerp(view.orthographicSize,fullSize,t);
                if(blend<=0){phase=4;actor.waterfall.FullMapReveal=false;}
            }
            // The reveal must also work when the player has selected Tab overview.
            if(actor.waterfall.occlusion!=null&&IsPlaying)actor.waterfall.occlusion.enabled=false;
        }
        public void GetFullMapPose(Camera view,out Vector3 position,out float size)
        {
            Vector2 min=Vector2.Min(Vector2.zero,level.wheel.center-level.wheel.radius);
            Vector2 max=Vector2.Max(level.cameraRig.mapSize,level.wheel.center+level.wheel.radius);
            Vector2 center=(min+max)*.5f,extent=(max-min)*.5f+Vector2.one*2;
            // Leave breathing room under the top and bottom HUD at every aspect ratio.
            size=Mathf.Max(extent.y/.78f,extent.x/(Mathf.Max(.1f,view.aspect)*.94f));
            position=new Vector3(center.x,center.y,-10);
        }
        void OnDisable(){if(level!=null&&level.player!=null)Cancel();}
        void OnDestroy(){if(level!=null&&level.player!=null)level.player.motor.ResetPerformed-=OnRespawn;}
    }
}
