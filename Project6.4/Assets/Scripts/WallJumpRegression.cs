using System;
using UnityEngine;
namespace Seaside
{
    public static class WallJumpRegression
    {
        static SeasideMotor motor;
        static void Step(float dt,SeasideInput c){motor.Tick(dt,c);Physics2D.Simulate(dt);}
        static void Reset(Vector2 p){motor.ResetAt(p);Physics2D.SyncTransforms();}
        public static void Run(SeasideMotor player,Action<bool,string> check)
        {
            motor=player;var fixtures=new GameObject("Wall-switch input fixtures");
            foreach(float x in new[]{-260f,-300f})
            {
                var go=new GameObject("Test wall");go.transform.SetParent(fixtures.transform);
                go.layer=6;go.transform.position=new Vector3(x,25,0);
                go.AddComponent<BoxCollider2D>().size=new Vector2(1,50);
                go.AddComponent<SeasideCell>().kind=CellKind.Ground;
            }
            try
            {
                InputChecks(check);
                foreach(float dt in new[]{.005f,.01f,.02f})
                foreach(int side in new[]{-1,1})
                {
                    Vector2 start=new Vector2(side==1?-261.51f:-298.49f,25);
                    foreach(float delay in new[]{0f,.02f,.04f,.06f,.08f})
                    {
                        Reset(start);Step(dt,new SeasideInput{horizontal=side});
                        for(int i=0;i<Mathf.RoundToInt(delay/dt);i++)Step(dt,new SeasideInput{horizontal=-side});
                        Step(dt,new SeasideInput{horizontal=-side,jumpDown=true,jumpHeld=true});
                        bool kicked=motor.WallKicks==1&&motor.Body.linearVelocity.y>30&&motor.Body.linearVelocity.x*side< -7;
                        check(kicked,"Switch + SPACE: side="+side+" delay="+delay+" dt="+dt+" v="+motor.Body.linearVelocity);
                        if(kicked)
                        {
                            float y=motor.Body.position.y;
                            Step(dt,new SeasideInput{horizontal=-side,jumpHeld=true});
                            Step(dt,new SeasideInput{horizontal=-side,jumpDown=true,jumpHeld=true});
                            check(motor.WallKicks==1&&motor.Body.position.y>y,"Wall grace consumed once; upward next frames; side="+side+" dt="+dt);
                        }
                    }
                    Reset(start);Step(dt,new SeasideInput{horizontal=side});
                    for(int i=0;i<Mathf.CeilToInt(.24f/dt);i++)Step(dt,new SeasideInput{horizontal=-side});
                    Step(dt,new SeasideInput{horizontal=-side,jumpDown=true,jumpHeld=true});
                    check(motor.WallKicks==0&&motor.Body.linearVelocity.y<=0,"Expired wall grace rejects air jump; side="+side+" dt="+dt);
                }
            }
            finally
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(fixtures);
                else UnityEngine.Object.DestroyImmediate(fixtures);
            }
        }
        static void InputChecks(Action<bool,string> check)
        {
            foreach(int side in new[]{-1,1})
            {
                var keys=new SeasideInputLatch();
                keys.Sample(side==-1,side==1,false,false,false,Vector2.right,true,side);
                keys.Consume();
                keys.Sample(true,true,true,true,false,Vector2.right,true,side);
                var captured=keys.Consume();
                check(captured.horizontal==-side&&captured.jumpDown,"A+D overlap uses newest direction and keeps SPACE; side="+side);
                check(!keys.Consume().jumpDown,"SPACE edge consumed once; side="+side);
                keys.Sample(false,false,false,false,false,Vector2.right,false,0);
                keys.Sample(side==1,side==-1,true,true,false,Vector2.right,false,0);
                keys.Sample(side==1,side==-1,false,false,false,Vector2.right,false,0);
                captured=keys.Consume();
                check(captured.horizontal==-side&&captured.jumpDown&&!captured.jumpHeld,
                    "Short SPACE tap between physics steps is buffered; side="+side);
            }
        }
    }
}
