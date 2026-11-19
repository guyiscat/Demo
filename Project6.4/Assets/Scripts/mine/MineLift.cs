using Seaside;
using UnityEngine;

namespace Mine
{
    [DefaultExecutionOrder(-200)]
    public sealed class MineLift : MovingSurface
    {
        public MineSettings settings;
        public Vector2 lowerStop,upperStop;
        public bool Activated{get;private set;}
        public bool manualSimulation;
        bool movingUp;
        float wait;
        void Awake(){if(body==null)body=GetComponent<Rigidbody2D>();NextPosition=body.position;}
        void FixedUpdate(){if(!manualSimulation)Advance(Time.fixedDeltaTime);}
        public void Activate(){Activated=true;}
        public void ResetLift()
        {
            Activated=false;movingUp=false;wait=0;
            body.position=upperStop;transform.position=upperStop;
            NextPosition=upperStop;Velocity=Vector2.zero;
        }
        public void Advance(float dt)
        {
            NextPosition=body.position;Velocity=Vector2.zero;
            var session=FindAnyObjectByType<SeasideSession>();
            if(!Activated||dt<=0||(session!=null&&session.InputBlocked))return;
            if(wait>0){wait=Mathf.Max(0,wait-dt);return;}
            Vector2 target=movingUp?upperStop:lowerStop;
            NextPosition=Vector2.MoveTowards(body.position,target,settings.liftSpeed*dt);
            Velocity=(NextPosition-body.position)/dt;body.MovePosition(NextPosition);
            if(Vector2.Distance(NextPosition,target)<.001f){movingUp=!movingUp;wait=settings.liftWait;}
        }
    }
}
