using UnityEngine;

namespace Seaside
{
    [DefaultExecutionOrder(300)]
    public class PlayerAimIndicator : MonoBehaviour
    {
        protected SeasideMotor motor;
        public Vector2 DisplayedDirection{get;private set;}=Vector2.right;
        public LineRenderer Arrow{get;private set;}
        readonly Vector3[] points=new Vector3[5];
        LineRenderer outline;
        Material material;
        protected virtual bool CanDisplay=>motor!=null&&motor.enabled&&!motor.frozen;
        protected virtual Vector2 ResolveDirection()=>motor.externalInput?motor.AimDirection:motor.ReadAimDirection();
        protected virtual void Start()
        {
            motor=GetComponent<SeasideMotor>();
            material=new Material(Shader.Find("Sprites/Default"));
            outline=CreateLine("AimArrowOutline",.20f,49,new Color(.025f,.06f,.05f));
            Arrow=CreateLine("AimArrow",.10f,50,new Color(1,.87f,.25f));
        }
        LineRenderer CreateLine(string name,float width,int order,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;
            line.useWorldSpace=true;line.positionCount=5;line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;line.sortingOrder=order;
            line.numCapVertices=line.numCornerVertices=4;return line;
        }
        protected virtual void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(Arrow==null||motor==null)return;
            Arrow.enabled=outline.enabled=CanDisplay;if(!CanDisplay)return;
            DisplayedDirection=ResolveDirection();
            Vector2 d=DisplayedDirection,n=new Vector2(-d.y,d.x),half=motor.BodySize*.5f;
            float radius=Mathf.Min(Mathf.Abs(d.x)>.001f?half.x/Mathf.Abs(d.x):float.PositiveInfinity,
                Mathf.Abs(d.y)>.001f?half.y/Mathf.Abs(d.y):float.PositiveInfinity);
            var settings=GameSystemSettings.Current;
            Vector2 root=(Vector2)transform.position+d*(radius+settings.aimArrowPadding);
            float length=settings.aimArrowLength;
            Vector2 tip=root+d*length,back=tip-d*(length*.38f);
            points[0]=root;points[1]=points[3]=tip;
            points[2]=back+n*length*.28f;points[4]=back-n*length*.28f;
            Arrow.SetPositions(points);outline.SetPositions(points);
        }
        protected virtual void OnDisable(){if(Arrow!=null)Arrow.enabled=false;if(outline!=null)outline.enabled=false;}
        protected virtual void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
