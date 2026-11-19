using UnityEngine;
using Seaside;

namespace Forest
{
    [DefaultExecutionOrder(200)]
    public sealed class ForestWaterfall : MonoBehaviour
    {
        public SeasideMotor player;
        public BoxCollider2D curtainBounds;
        public SpriteRenderer occlusion;
        public SeasideCamera cameraRig;
        [Min(.1f),InspectorName("瀑布换侧镜头移动速度")]
        [Tooltip("World units per second. Higher values cross the waterfall faster.")]
        public float cameraTransitionSpeed=40;
        public int VisibleSide{get;private set;}=-1;
        public bool IsTransitioning{get;private set;}
        public bool FullMapReveal{get;set;}
        Vector3 displayedPosition;
        bool hasDisplayedPosition;
        float transitionRemaining;
        void Awake(){ResolveCamera();HideMask();}
        void LateUpdate(){Refresh(Time.deltaTime);}
        void OnDisable(){HideMask();hasDisplayedPosition=false;IsTransitioning=false;}
        void ResolveCamera(){if(cameraRig==null)cameraRig=FindAnyObjectByType<SeasideCamera>();}
        void HideMask(){if(occlusion!=null)occlusion.enabled=false;}
        void RefreshOverviewMask(Bounds bounds)
        {
            if(occlusion==null)return;
            occlusion.enabled=cameraRig!=null&&cameraRig.overview&&!IsTransitioning&&!FullMapReveal;
            if(!occlusion.enabled)return;
            const float span=4096;
            float edge=VisibleSide<0?bounds.max.x:bounds.min.x;
            occlusion.transform.position=new Vector3(edge-VisibleSide*span*.5f,bounds.center.y,-1);
            occlusion.size=new Vector2(span,span);occlusion.color=Color.black;
        }
        public void ResetSide(Vector2 position)
        {
            // Respawn/restart is an explicit snap, never a cross-map camera pan.
            VisibleSide=position.x>curtainBounds.bounds.center.x?1:-1;
            IsTransitioning=false;hasDisplayedPosition=false;transitionRemaining=0;Refresh(0);
        }
        public void Refresh(){Refresh(0);}
        public void Refresh(float dt)
        {
            ResolveCamera();var bounds=curtainBounds.bounds;
            float half=player.BodySize.x*.5f;
            float x=player.Body!=null?player.Body.position.x:player.transform.position.x;
            int next=VisibleSide;
            if(x-half>=bounds.max.x+.001f)next=1;
            else if(x+half<=bounds.min.x-.001f)next=-1;
            if(next!=VisibleSide)
            {
                VisibleSide=next;IsTransitioning=hasDisplayedPosition;
                // Overview is already centered on the map. Keep both sides
                // revealed for the same crossing interval before changing mask.
                transitionRemaining=(bounds.size.x+player.BodySize.x)/Mathf.Max(.1f,cameraTransitionSpeed);
            }
            if(cameraRig==null){HideMask();return;}
            var camera=cameraRig.GetComponent<Camera>();
            Vector3 target=camera.transform.position;
            if(!cameraRig.overview)
            {
                float depth=-target.z;
                float left=camera.ViewportToWorldPoint(new Vector3(0,0,depth)).x;
                float right=camera.ViewportToWorldPoint(new Vector3(1,0,depth)).x;
                float shift=VisibleSide<0?Mathf.Min(0,bounds.max.x-right):Mathf.Max(0,bounds.min.x-left);
                target.x+=shift;
            }
            if(IsTransitioning)
            {
                float step=Mathf.Max(.1f,cameraTransitionSpeed)*Mathf.Max(0,dt);
                displayedPosition=Vector3.MoveTowards(displayedPosition,target,step);
                transitionRemaining=Mathf.Max(0,transitionRemaining-Mathf.Max(0,dt));
                if(Vector3.Distance(displayedPosition,target)<.01f&&transitionRemaining<=0)
                {displayedPosition=target;IsTransitioning=false;}
                camera.transform.position=displayedPosition;
            }
            else{camera.transform.position=target;displayedPosition=target;}
            hasDisplayedPosition=true;RefreshOverviewMask(bounds);
        }
    }
}
