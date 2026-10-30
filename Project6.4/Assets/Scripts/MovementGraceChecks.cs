using System;
using UnityEngine;
namespace Seaside
{
    // The same deterministic cases run in the Editor and the built Windows player.
    public static class MovementGraceChecks
    {
        static SeasideMotor motor;
        static void Reset(Vector2 p){motor.ResetAt(p);Physics2D.SyncTransforms();}
        static void Step(float dt,SeasideInput input){motor.Tick(dt,input);Physics2D.Simulate(dt);}
        static void Land(float dt)
        {
            Reset(new Vector2(-200,1.2f));
            for(int i=0;i<Mathf.CeilToInt(.4f/dt);i++)Step(dt,new SeasideInput());
        }
        public static void Run(SeasideMotor player,Action<bool,string> check)
        {
            motor=player;
            var floor=new GameObject("Temporary coyote-time test platform");
            floor.layer=6;floor.transform.position=new Vector3(-200,-.5f,0);
            floor.AddComponent<BoxCollider2D>().size=new Vector2(8,1);
            floor.AddComponent<SeasideCell>().kind=CellKind.Ground;
            try
            {
                foreach(float dt in new[]{.005f,.01f,.02f})
                {
                    foreach(float delay in new[]{.02f,.10f,.12f,.16f,.20f})
                    {
                        Land(dt);bool left=false;
                        for(int i=0;i<300;i++)
                        {
                            Step(dt,new SeasideInput{horizontal=1});
                            if(!motor.Grounded){left=true;break;}
                        }
                        int ticks=Mathf.RoundToInt(delay/dt);
                        for(int i=1;i<ticks;i++)Step(dt,new SeasideInput{horizontal=1});
                        Step(dt,new SeasideInput{horizontal=1,jumpDown=true,jumpHeld=true});
                        bool jumped=motor.Body.linearVelocity.y>20;
                        check(left&&jumped==(delay<.14f),"Edge grace delay="+delay+"s dt="+dt+" expectedJump="+(delay<.14f));
                        if(jumped)
                        {
                            Step(dt,new SeasideInput{horizontal=1});
                            float prior=motor.Body.linearVelocity.y;
                            Step(dt,new SeasideInput{horizontal=1,jumpDown=true,jumpHeld=true});
                            check(motor.Body.linearVelocity.y<prior,
                                "Successful edge jump consumes grace once; delay="+delay+" dt="+dt);
                        }
                    }
                    Land(dt);Step(dt,new SeasideInput{jumpDown=true,jumpHeld=true});
                    float first=motor.Body.linearVelocity.y;
                    Step(dt,new SeasideInput{jumpHeld=false});
                    float before=motor.Body.linearVelocity.y;
                    Step(dt,new SeasideInput{jumpDown=true,jumpHeld=true});
                    check(!motor.Grounded&&motor.Body.linearVelocity.y<before&&before<first,
                        "Active takeoff cannot rearm grace / second air jump; dt="+dt);
                    bool extraRise=false;
                    for(int i=0;i<Mathf.CeilToInt(.35f/dt);i++)
                    {
                        before=motor.Body.linearVelocity.y;
                        Step(dt,new SeasideInput{jumpDown=i%2==0,jumpHeld=i%2==0});
                        extraRise|=motor.Body.linearVelocity.y>before+.01f;
                    }
                    check(!extraRise,"Repeated airborne taps cannot grant extra jumps; dt="+dt);
                    Reset(new Vector2(-200,1.35f));motor.Body.linearVelocity=Vector2.down*4;
                    bool buffered=false;int rises=0;
                    for(int i=0;i<Mathf.CeilToInt(.25f/dt);i++)
                    {
                        before=motor.Body.linearVelocity.y;
                        Step(dt,new SeasideInput{jumpDown=i==0,jumpHeld=true});
                        if(before<=0&&motor.Body.linearVelocity.y>20){buffered=true;rises++;}
                    }
                    check(buffered&&rises==1,"Pre-landing jump buffer fires once on contact; dt="+dt);
                    Reset(new Vector2(-200,5));bool lateJump=false;
                    for(int i=0;i<Mathf.CeilToInt(.7f/dt);i++)
                    {
                        Step(dt,new SeasideInput{jumpDown=i==0,jumpHeld=true});
                        lateJump|=motor.Body.linearVelocity.y>20;
                    }
                    check(!lateJump&&motor.Grounded,"Expired landing buffer does not auto-jump; dt="+dt);
                    Land(dt);Step(dt,new SeasideInput{dashDown=true,aim=Vector2.right});
                    for(int i=0;i<Mathf.CeilToInt(.5f/dt)&&motor.Dashing;i++)Step(dt,new SeasideInput());
                    Step(dt,new SeasideInput{jumpDown=true,jumpHeld=true});
                    check(motor.Body.linearVelocity.y<1&&!motor.Grounded,
                        "Dash leaving platform cannot inherit ground grace; dt="+dt);
                }
            }
            finally
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(floor);
                else UnityEngine.Object.DestroyImmediate(floor);
            }
        }
    }
}
