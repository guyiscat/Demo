using System;
using System.Collections.Generic;
using UnityEngine;
namespace Seaside
{
    public static class CameraComfortChecks
    {
        static Vector2 Point(int test,float t)
        {
            if(test==0)return new Vector2(50+1.5f*Mathf.Sin(t*4*Mathf.PI),25);
            if(test==1)return new Vector2(50,25+4*Mathf.Sin((t%1)*Mathf.PI));
            if(test==2)return new Vector2(50,25+1.1f*Mathf.Sin(t*5));
            if(test==3)return new Vector2(50,60-Mathf.Min(t,1.4f)*32);
            return new Vector2(50+Mathf.Min(t,.32f)*25,25);
        }
        public static List<string> Run(Action<bool,string> check)
        {
            var rows=new List<string>{"scenario,fps,old_travel,new_travel,old_peak_accel,new_peak_accel,max_player_gap"};
            string[] names={"rapid_reverse","repeated_short_jump","wall_mode_toggle","fast_fall","dash_stop"};
            for(int test=0;test<5;test++)foreach(int fps in new[]{30,60,120})
            {
                float dt=1f/fps,oldTravel=0,newTravel=0,oldAccel=0,newAccel=0,gap=0;
                Vector2 map=new Vector2(155,73),p=Point(test,0),ov=Vector2.zero,nv=Vector2.zero;
                var old=new PreviousCameraMotion();var now=new CameraMotion();
                var ot=new PreviousCameraTuning();var nt=new CameraTuning();
                old.Reset(p,map,16f/9,false,ot);now.Reset(p,map,16f/9,false,nt);
                for(int i=1;i<=fps*4;i++)
                {
                    float t=i*dt;p=Point(test,t);Vector2 v=(p-Point(test,t-dt))/dt;
                    Vector2 a=old.Position,b=now.Position;
                    bool wall=test==2&&(i/(fps/10))%2==0;
                    bool ground=test==1&&(t%1)<dt;
                    bool dash=test==4&&t<.32f;
                    old.Tick(p,v,ground,wall,dash,false,map,16f/9,dt,ot);
                    now.Tick(p,v,ground,wall,dash,false,map,16f/9,dt,nt);
                    Vector2 va=((Vector2)old.Position-a)/dt,vb=((Vector2)now.Position-b)/dt;
                    oldTravel+=Vector2.Distance(a,old.Position);newTravel+=Vector2.Distance(b,now.Position);
                    oldAccel=Mathf.Max(oldAccel,(va-ov).magnitude/dt);newAccel=Mathf.Max(newAccel,(vb-nv).magnitude/dt);
                    ov=va;nv=vb;
                    gap=Mathf.Max(gap,Mathf.Abs(now.Position.y-p.y));
                }
                rows.Add(names[test]+","+fps+","+oldTravel+","+newTravel+","+oldAccel+","+newAccel+","+gap);
                if(test<=2)
                    check(newTravel<oldTravel*.65f,names[test]+" camera travel reduced: old="+oldTravel.ToString("F3")+" new="+newTravel.ToString("F3")+" fps="+fps);
                if(test==3)check(newAccel<350,"Fast fall avoids hard safety-edge catch-up; peak accel="+newAccel.ToString("F2")+" fps="+fps);
                if(test==4)check(newAccel<oldAccel*.6f,"Dash stop acceleration reduced; old="+oldAccel.ToString("F2")+" new="+newAccel.ToString("F2")+" fps="+fps);
                check(gap<=nt.followSize*.67f+.01f,names[test]+" vertical safety framing fps="+fps+" gap="+gap.ToString("F3"));
            }
            foreach(float aspect in new[]{4f/3,16f/9,21f/9})
            {
                var rig=new CameraMotion();var tune=new CameraTuning();Vector2 map=new Vector2(155,73);
                rig.Reset(new Vector2(75,35),map,aspect,false,tune);
                bool safe=true;
                for(int i=0;i<240;i++)
                {
                    Vector2 p=new Vector2(75+Mathf.Min(i,32)*.25f,35);
                    rig.Tick(p,i<32?Vector2.right*25:Vector2.zero,false,false,i<32,false,map,aspect,.01f,tune);
                    safe&=Mathf.Abs(rig.Position.x-p.x)<tune.followSize*aspect*.73f;
                }
                check(safe,"Horizontal dash framing across aspect="+aspect.ToString("F3"));
            }
            return rows;
        }
    }
}
