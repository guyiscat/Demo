using UnityEngine;

namespace Forest
{
    public sealed class ForestZipline : MonoBehaviour
    {
        public string excelAddress;
        public Transform start,end;
        LineRenderer cable;
        void Awake(){cable=GetComponentInChildren<LineRenderer>();}
        void LateUpdate()
        {
            if(cable==null||start==null||end==null)return;
            cable.SetPosition(0,start.position);cable.SetPosition(1,end.position);
        }
        public Vector2 StartPoint=>(Vector2)start.position;
        public Vector2 EndPoint=>(Vector2)end.position;
        public float Length=>Vector2.Distance(StartPoint,EndPoint);
        public Vector2 Tangent=>(EndPoint-StartPoint).normalized;
        public float Project(Vector2 point)=>Mathf.Clamp(Vector2.Dot(point-StartPoint,Tangent),0,Length);
        public Vector2 Point(float distance)=>StartPoint+Tangent*distance;
        public bool CanCatch(Vector2 bodyCenter,Vector2 bodySize,float padding,float centerReach=0)
        {
            // Preserve the existing head-side reach while also accepting every
            // physical body edge, including approaches from above or the sides.
            var nearest=Point(Project(bodyCenter));
            if(Vector2.Distance(bodyCenter,nearest)<=centerReach&&bodyCenter.y<=nearest.y+bodySize.y*.25f)return true;
            // Clip the cable against the full body box, expanded a little for
            // forgiving contact at the head, feet, sides and segment endpoints.
            Vector2 half=bodySize*.5f+Vector2.one*Mathf.Max(0,padding);
            Vector2 a=StartPoint-bodyCenter,d=EndPoint-StartPoint;
            float low=0,high=1;
            for(int axis=0;axis<2;axis++)
            {
                if(Mathf.Abs(d[axis])<.00001f){if(Mathf.Abs(a[axis])>half[axis])return false;continue;}
                float enter=(-half[axis]-a[axis])/d[axis],exit=(half[axis]-a[axis])/d[axis];
                low=Mathf.Max(low,Mathf.Min(enter,exit));high=Mathf.Min(high,Mathf.Max(enter,exit));
                if(low>high)return false;
            }
            return true;
        }
        void OnDrawGizmosSelected()
        {
            if(start==null||end==null)return;
            Gizmos.color=Color.green;Gizmos.DrawLine(start.position,end.position);
        }
    }
}
