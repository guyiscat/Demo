using UnityEngine;
namespace Expedition
{
    public sealed class ClimbPoint : MonoBehaviour
    {
        public ProtectionMode requiredProtection;
        public string sourceAddress;
        public LineRenderer rangeRing;
        public void ShowState(bool available,Vector2 halfExtents)
        {
            if(rangeRing==null)return;
            var color=requiredProtection==ProtectionMode.Cold?new Color(0,.7f,.95f):new Color(.94f,.42f,.51f);
            color.a=available?.65f:.14f;rangeRing.startColor=rangeRing.endColor=color;
            rangeRing.positionCount=4;rangeRing.loop=true;rangeRing.useWorldSpace=false;
            rangeRing.SetPosition(0,new Vector3(-halfExtents.x,-halfExtents.y));rangeRing.SetPosition(1,new Vector3(-halfExtents.x,halfExtents.y));
            rangeRing.SetPosition(2,new Vector3(halfExtents.x,halfExtents.y));rangeRing.SetPosition(3,new Vector3(halfExtents.x,-halfExtents.y));
        }
    }
}
