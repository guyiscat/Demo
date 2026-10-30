using System;
using UnityEngine;
namespace Seaside
{
    [Serializable]
    public sealed class CameraTuning
    {
        public float followSize=10f;
        public float horizontalSmooth=.22f,verticalSmooth=.30f,fastVerticalSmooth=.12f,dashSmooth=.22f;
        public float horizontalDeadZone=2f,verticalDeadZone=4.2f,wallVerticalDeadZone=4.2f;
        public float lookAheadDistance=.6f,lookAheadSmooth=.45f,velocityPrediction=0f;
        public float verticalBias=1.1f,fallLookAhead=.35f,zoomSmooth=.40f,overviewMoveSmooth=.40f;
        public float directionHold=.20f,zoneSmooth=.30f;
    }
    // Time-based camera logic shared by the actual camera and frame-rate regression tests.
    public sealed class CameraMotion
    {
        public Vector3 Position{get;private set;}
        public float Size{get;private set;}
        Vector2 anchor,velocity;
        float look,lookVelocity,fallLook,fallLookVelocity,sizeVelocity,smoothY;
        float directionAge,zoneY,zoneVelocity,smoothX;
        int pendingDirection,stableDirection;
        bool initialized,previousOverview;
        public void Reset(Vector2 player,Vector2 map,float aspect,bool overview,CameraTuning t)
        {
            look=lookVelocity=fallLook=fallLookVelocity=sizeVelocity=0;
            velocity=Vector2.zero;smoothY=t.verticalSmooth;anchor=player;
            smoothX=t.horizontalSmooth;zoneY=t.verticalDeadZone;zoneVelocity=0;
            pendingDirection=stableDirection=0;directionAge=0;
            Size=DesiredSize(map,aspect,overview,t);
            Vector2 centre=overview?map*.5f:Bounds(player+Vector2.up*t.verticalBias,map,aspect,Size);
            Position=new Vector3(centre.x,centre.y,-10);initialized=true;previousOverview=overview;
        }
        static float DesiredSize(Vector2 map,float aspect,bool overview,CameraTuning t)
        {
            return overview?Mathf.Max(map.y*.62f,map.x/Mathf.Max(.5f,aspect)*.55f):t.followSize;
        }
        static Vector2 Bounds(Vector2 p,Vector2 map,float aspect,float size)
        {
            float halfWidth=size*aspect;
            p.x=map.x>halfWidth*2?Mathf.Clamp(p.x,halfWidth,map.x-halfWidth):map.x*.5f;
            p.y=Mathf.Clamp(p.y,size*.65f,Mathf.Max(size*.65f,map.y-size*.45f));
            return p;
        }
        public void Tick(Vector2 player,Vector2 playerVelocity,bool grounded,bool wall,bool dashing,bool overview,Vector2 map,float aspect,float dt,CameraTuning t)
        {
            if(!initialized)Reset(player,map,aspect,overview,t);
            dt=Mathf.Clamp(dt,.0001f,.1f);
            if(overview!=previousOverview)
            {
                // Discard stale tracking velocity when changing mode, but keep the current pose.
                velocity=Vector2.zero;sizeVelocity=0;anchor=player;previousOverview=overview;
                // The key event occurs at this rendered instant; start its transition next interval.
                return;
            }
            Size=Mathf.SmoothDamp(Size,DesiredSize(map,aspect,overview,t),ref sizeVelocity,t.zoomSmooth,Mathf.Infinity,dt);
            int direction=wall||dashing||Mathf.Abs(playerVelocity.x)<1f?0:(playerVelocity.x>0?1:-1);
            if(direction!=pendingDirection){pendingDirection=direction;directionAge=0;}
            else directionAge+=dt;
            if(directionAge>=t.directionHold)stableDirection=pendingDirection;
            float lookTarget=stableDirection*t.lookAheadDistance;
            look=Mathf.SmoothDamp(look,lookTarget,ref lookVelocity,t.lookAheadSmooth,Mathf.Infinity,dt);
            float falling=wall||grounded?0:Mathf.Clamp01(-playerVelocity.y/22f)*t.fallLookAhead;
            fallLook=Mathf.SmoothDamp(fallLook,falling,ref fallLookVelocity,.45f,Mathf.Infinity,dt);
            float fastFall=wall||grounded?0:Mathf.Clamp01((-playerVelocity.y-8f)/20f);
            float zoneTarget=Mathf.Lerp(wall?t.wallVerticalDeadZone:t.verticalDeadZone,1f,fastFall);
            zoneY=Mathf.SmoothDamp(zoneY,zoneTarget,ref zoneVelocity,Mathf.Lerp(t.zoneSmooth,.10f,fastFall),Mathf.Infinity,dt);
            anchor.x=Mathf.Clamp(anchor.x,player.x-t.horizontalDeadZone,player.x+t.horizontalDeadZone);
            anchor.y=Mathf.Clamp(anchor.y,player.y-zoneY,player.y+zoneY);
            Vector2 target=anchor+new Vector2(look,t.verticalBias-fallLook);
            target.x+=Mathf.Clamp(playerVelocity.x*t.velocityPrediction,-1.5f,1.5f);
            // Keep landings and low-speed wall slides calm; catch up faster for a dash or long fall.
            float desiredY=dashing?t.dashSmooth:playerVelocity.y< -12?t.fastVerticalSmooth:t.verticalSmooth;
            smoothY=Mathf.Lerp(smoothY,desiredY,1-Mathf.Exp(-dt/.1f));
            smoothX=Mathf.Lerp(smoothX,dashing?t.dashSmooth:t.horizontalSmooth,1-Mathf.Exp(-dt/.15f));
            Vector2 current=Position;
            if(overview)
            {
                target=map*.5f;
                current=Vector2.SmoothDamp(current,target,ref velocity,t.overviewMoveSmooth,Mathf.Infinity,dt);
            }
            else
            {
                target=Bounds(target,map,aspect,Size);
                current.x=Mathf.SmoothDamp(current.x,target.x,ref velocity.x,smoothX,Mathf.Infinity,dt);
                current.y=Mathf.SmoothDamp(current.y,target.y,ref velocity.y,smoothY,Mathf.Infinity,dt);
                // Safety framing only engages near screen edges, not for ordinary micro-movements.
                current.x=Mathf.Clamp(current.x,player.x-Size*aspect*.72f,player.x+Size*aspect*.72f);
                current.y=Mathf.Clamp(current.y,player.y-Size*.67f,player.y+Size*.67f);
                current=Bounds(current,map,aspect,Size);
            }
            Position=new Vector3(current.x,current.y,-10);
        }
    }
}

