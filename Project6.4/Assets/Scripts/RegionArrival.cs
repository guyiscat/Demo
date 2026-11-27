using System.Collections;
using UnityEngine;

namespace Seaside
{
    public sealed class RegionArrival : MonoBehaviour
    {
        public bool Walking{get;private set;}
        public Vector2 Target{get;private set;}
        public int Direction{get;private set;}=1;
        public bool Dropping{get;private set;}
        public IEnumerator Drop(SeasideSession session,Vector2 target,float height)
        {
            var motor=session.player;var body=motor.Body;Target=target;Dropping=Walking=true;Direction=0;
            session.Travelling=true;motor.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();motor.BeginTraversal();motor.frozen=true;
            var rig=Object.FindAnyObjectByType<SeasideCamera>();motor.WarpForTraversal(target);rig.overview=false;rig.Snap();
            Vector2 position=target+Vector2.up*height;bool simulated=body.simulated;body.simulated=false;motor.WarpForTraversal(position);
            try
            {
                float speed=2;
                while(position.y>target.y+.001f)
                {speed+=25*Time.unscaledDeltaTime;position=Vector2.MoveTowards(position,target,speed*Time.unscaledDeltaTime);body.position=position;motor.transform.position=position;yield return null;}
            }
            finally
            {Dropping=Walking=false;body.simulated=simulated;motor.WarpForTraversal(target);motor.Body.linearVelocity=Vector2.down*3;motor.frozen=false;session.Travelling=false;Physics2D.SyncTransforms();rig.Snap();}
        }
        public IEnumerator Walk(SeasideSession session,Vector2 target,int direction=1)
        {
            var motor=session.player;var body=motor.Body;
            session.Travelling=true;motor.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();motor.BeginTraversal();motor.frozen=true;
            Target=target;motor.WarpForTraversal(target);Physics2D.SyncTransforms();
            var rig=Object.FindAnyObjectByType<SeasideCamera>();rig.overview=false;rig.Snap();
            var camera=rig.GetComponent<Camera>();
            Direction=direction<0?-1:1;
            Vector2 edge=camera.ViewportToWorldPoint(new Vector3(Direction<0?1.04f:-.04f,0,-camera.transform.position.z));
            Vector2 position=new Vector2(Direction<0?Mathf.Max(edge.x,target.x+2):Mathf.Min(edge.x,target.x-2),target.y);
            Walking=true;bool simulated=body.simulated;body.simulated=false;
            motor.WarpForTraversal(position);
            try
            {
                while(Vector2.Distance(position,target)>.001f)
                {
                    position=Vector2.MoveTowards(position,target,Mathf.Max(.1f,GameSystemSettings.Current.regionEntrySpeed)*Time.unscaledDeltaTime);
                    body.position=position;motor.transform.position=position;yield return null;
                }
            }
            finally
            {
                Walking=false;body.simulated=simulated;motor.WarpForTraversal(target);motor.frozen=false;session.Travelling=false;
                Physics2D.SyncTransforms();rig.Snap();
            }
        }
    }
}
