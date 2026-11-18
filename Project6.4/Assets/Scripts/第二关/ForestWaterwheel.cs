using UnityEngine;

namespace Forest
{
    [DefaultExecutionOrder(-200)]
    public sealed class ForestWaterwheel : MonoBehaviour
    {
        public ForestSettings settings;
        public Vector2 center,radius;
        public Transform rim;
        public ForestWheelPlatform[] platforms;
        public ForestUnlock[] locks;
        public Transform deliveryAnchor;
        public Transform deliveryClearance;
        public SpriteRenderer transferBasket;
        [Tooltip("Upper-right edge of the workbook wheel, adjacent to the top platform.")]
        public float deliveryAngle=52;
        public bool LeftUnlocked{get;private set;}
        public bool RightUnlocked{get;private set;}
        [Header("Preview")]
        [Tooltip("Preview rotation without changing the two puzzle unlock states. Turn off for normal gameplay.")]
        [InspectorName("调试：强制转动")]
        public bool forceRotation;
        public bool Active=>forceRotation||(LeftUnlocked&&RightUnlocked);
        public int UnlockCount=>(LeftUnlocked?1:0)+(RightUnlocked?1:0);
        public float Rotation{get;private set;}
        public int Direction=>-1;
        public Vector2 Orbit(float angle)
        {
            float a=angle*Mathf.Deg2Rad;
            return center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y);
        }
        public void Unlock(int index)
        {
            if(index==0)LeftUnlocked=true;else if(index==1)RightUnlocked=true;
            Seaside.SeasideLog.Event("FOREST_UNLOCK","side="+index+" count="+UnlockCount);
        }
        void FixedUpdate(){Advance(Time.fixedDeltaTime);}
        public void Advance(float dt)
        {
            float delta=Active?-Mathf.Abs(settings.wheelDegreesPerSecond)*dt:0;
            Rotation+=delta;
            if(rim!=null)rim.localRotation=Quaternion.Euler(0,0,Rotation);
            foreach(var platform in platforms)
            {
                platform.previousAngle=platform.angle;
                platform.angle=Mathf.Repeat(platform.angle+delta,360);
                platform.NextPosition=Orbit(platform.angle);
                platform.Velocity=dt>0?(platform.NextPosition-platform.body.position)/dt:Vector2.zero;
                platform.body.MovePosition(platform.NextPosition);
            }
        }
        public bool AtDelivery(ForestWheelPlatform platform)
        {
            return Mathf.Abs(Mathf.DeltaAngle(platform.angle,deliveryAngle))<=settings.wheelDegreesPerSecond*Time.fixedDeltaTime+1;
        }
        public void ResetWheel()
        {
            LeftUnlocked=RightUnlocked=false;Rotation=0;
            if(rim!=null)rim.localRotation=Quaternion.identity;
            foreach(var item in locks)if(item!=null)item.ResetUnlock();
            foreach(var platform in platforms)
            {
                platform.angle=platform.previousAngle=platform.initialAngle;
                platform.NextPosition=Orbit(platform.angle);
                platform.body.position=platform.NextPosition;
                platform.Velocity=Vector2.zero;
            }
        }
    }
}
