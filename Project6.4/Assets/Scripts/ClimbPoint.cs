using UnityEngine;
namespace Expedition
{
    public sealed class ClimbPoint : MonoBehaviour
    {
        public ProtectionMode requiredProtection;
        public string sourceAddress;
        public LineRenderer rangeRing;
        public void ShowState(bool available,float radius)
        {
            if(rangeRing==null)return;
            var color=requiredProtection==ProtectionMode.Cold?new Color(0,.7f,.95f):new Color(.94f,.42f,.51f);
            color.a=available?.65f:.14f;rangeRing.startColor=rangeRing.endColor=color;
            const int segments=48;rangeRing.positionCount=segments;rangeRing.loop=true;
            for(int i=0;i<segments;i++)
            {float a=i*Mathf.PI*2/segments;rangeRing.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
        }
    }
}
