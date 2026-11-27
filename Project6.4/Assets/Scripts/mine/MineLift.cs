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
        public bool Moving{get;private set;}
        public bool manualSimulation;
        public bool startsUnlocked;
        bool movingUp;
        float wait;
        void Awake(){if(body==null)body=GetComponent<Rigidbody2D>();NextPosition=body.position;Activated=startsUnlocked;}
        void FixedUpdate(){if(!manualSimulation)Advance(Time.fixedDeltaTime);}
        public void Activate(){Activated=true;}
        public bool Use(SeasideMotor player)
        {
            if(!Activated||Moving||player==null)return false;
            Vector2 p=player.Body.position;
            float upper=Vector2.Distance(p,upperStop),lower=Vector2.Distance(p,lowerStop);
            bool aboard=Mathf.Abs(p.x-body.position.x)<=Collider.bounds.extents.x+1&&Mathf.Abs(p.y-(body.position.y+Collider.bounds.extents.y+1))<2;
            if(!aboard&&Mathf.Min(upper,lower)>5)return false;
            // From a vacant stop, E calls the platform. Aboard, E departs to the other stop.
            movingUp=!aboard?upper<lower:Vector2.Distance(body.position,lowerStop)<Vector2.Distance(body.position,upperStop);
            Moving=true;return true;
        }
        public void ResetLift()
        {
            Activated=false;Moving=false;movingUp=false;wait=0;
            body.position=upperStop;transform.position=upperStop;
            NextPosition=upperStop;Velocity=Vector2.zero;
        }
        public void Advance(float dt)
        {
            NextPosition=body.position;Velocity=Vector2.zero;
            var session=FindAnyObjectByType<SeasideSession>();
            if(!Activated||!Moving||dt<=0||(session!=null&&(session.InputBlocked||session.player.frozen)))return;
            if(wait>0){wait=Mathf.Max(0,wait-dt);return;}
            Vector2 target=movingUp?upperStop:lowerStop;
            NextPosition=Vector2.MoveTowards(body.position,target,settings.liftSpeed*dt);
            Velocity=(NextPosition-body.position)/dt;body.MovePosition(NextPosition);
            if(Vector2.Distance(NextPosition,target)<.001f){Moving=false;Velocity=Vector2.zero;}
        }
    }
}
