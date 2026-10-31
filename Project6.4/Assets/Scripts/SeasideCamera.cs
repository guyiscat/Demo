using UnityEngine;
namespace Seaside
{
    public sealed class SeasideCamera:MonoBehaviour
    {
        public SeasideMotor player;
        public bool overview;
        public Vector2 mapSize;
        public CameraTuning tuning=new CameraTuning();
        [Tooltip("Keep the full visible rectangle inside the background, including overview and zoom transitions.")]
        public bool constrainToBackground=true;
        [Tooltip("Optional explicit boundary. If empty, find bg3 under this scene's SeasideArtRoot at runtime.")]
        public SpriteRenderer backgroundBoundary;
        public bool PreviewDash{get;set;}
        public bool PreviewWall{get;set;}
        readonly CameraMotion motion=new CameraMotion();
        Camera cameraComponent;
        Camera letterboxCamera;
        Rect originalViewport;
        bool viewportCaptured,lookedForBackground;
        bool ready;
        void Awake(){EnsureCamera();}
        void Start(){Snap();}
        void LateUpdate(){UpdateView(Time.deltaTime);}
        public void UpdateView(float deltaTime)
        {
            if(player==null)return;
            if(!ready)Snap();
            bool bounded=TryBackgroundBounds(out var bounds);
            Vector2 origin=bounded?(Vector2)bounds.min:Vector2.zero;
            Vector2 area=bounded?(Vector2)bounds.size:mapSize;
            if(Time.timeScale>0)
            {
                Vector2 v=player.Body==null?Vector2.zero:player.Body.linearVelocity;
                // Use the original display aspect, not the changing letterbox aspect, to avoid feedback.
                motion.Tick((Vector2)player.transform.position-origin,v,player.Grounded,player.Attached||player.WallSliding||PreviewWall,player.Dashing||PreviewDash,overview,area,DisplayAspect(),deltaTime,tuning);
            }
            // Even when paused, an aspect/size change must not expose outside artwork.
            ApplyPose(origin,bounded,bounds);
        }
        public void Snap()
        {
            if(player==null)return;
            EnsureCamera();
            if(tuning==null)tuning=new CameraTuning();
            bool bounded=TryBackgroundBounds(out var bounds);
            Vector2 origin=bounded?(Vector2)bounds.min:Vector2.zero;
            motion.Reset((Vector2)player.transform.position-origin,bounded?(Vector2)bounds.size:mapSize,DisplayAspect(),overview,tuning);
            ApplyPose(origin,bounded,bounds);ready=true;
        }

        void EnsureCamera()
        {
            if(cameraComponent==null)cameraComponent=GetComponent<Camera>();
            if(!viewportCaptured){originalViewport=cameraComponent.rect;viewportCaptured=true;}
        }

        float DisplayAspect()
        {
            EnsureCamera();
            var target=cameraComponent.targetTexture;
            float width=target!=null?target.width:Screen.width;
            float height=target!=null?target.height:Screen.height;
            return Mathf.Max(.001f,width*originalViewport.width/Mathf.Max(1,height*originalViewport.height));
        }

        bool TryBackgroundBounds(out Bounds bounds)
        {
            bounds=default;
            if(!constrainToBackground)return false;
            if(backgroundBoundary==null&&!lookedForBackground)
            {
                lookedForBackground=true;
                foreach(var root in FindObjectsByType<SeasideArtRoot>(FindObjectsInactive.Include))
                {
                    if(root.gameObject.scene!=gameObject.scene)continue;
                    foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                        if(renderer.sprite!=null&&renderer.sprite.name=="bg3")
                        {backgroundBoundary=renderer;break;}
                    if(backgroundBoundary!=null)break;
                }
            }
            if(backgroundBoundary==null||backgroundBoundary.sprite==null)return false;
            bounds=backgroundBoundary.bounds;
            return bounds.size.x>.001f&&bounds.size.y>.001f;
        }

        void ApplyPose(Vector2 origin,bool bounded,Bounds bounds)
        {
            Vector3 position=motion.Position+(Vector3)origin;
            float size=motion.Size;
            Rect viewport=originalViewport;
            if(bounded)
                BackgroundCameraFrame.Fit(bounds,position,size,DisplayAspect(),originalViewport,out position,out size,out viewport);
            float projectionAspect=DisplayAspect()*(viewport.width/originalViewport.width)/(viewport.height/originalViewport.height);
            if(bounded)viewport=PixelAlignedViewport(viewport);
            transform.position=position;
            cameraComponent.orthographicSize=size;
            cameraComponent.rect=viewport;
            // Use the exact projection aspect, avoiding pixel-rounding leaks at the background edge.
            if(bounded)cameraComponent.aspect=projectionAspect;
            else cameraComponent.ResetAspect();
            cameraComponent.ResetProjectionMatrix();
            if(bounded)ConstrainProjection(bounds);
            bool bars=bounded&&(Mathf.Abs(viewport.width-originalViewport.width)>.00001f||Mathf.Abs(viewport.height-originalViewport.height)>.00001f);
            UpdateLetterbox(bars);
        }

        Rect PixelAlignedViewport(Rect viewport)
        {
            var target=cameraComponent.targetTexture;
            float width=target!=null?target.width:Screen.width;
            float height=target!=null?target.height:Screen.height;
            float left=Mathf.Ceil(viewport.x*width),bottom=Mathf.Ceil(viewport.y*height);
            float right=Mathf.Floor(viewport.xMax*width),top=Mathf.Floor(viewport.yMax*height);
            return new Rect(left/width,bottom/height,Mathf.Max(1,right-left)/width,Mathf.Max(1,top-bottom)/height);
        }

        void ConstrainProjection(Bounds bounds)
        {
            // Fractional viewport pixels can slightly expand Unity's actual projection during zoom.
            // Correct the real frustum too, rather than relying only on ideal rectangle arithmetic.
            for(int i=0;i<3;i++)
            {
                float depth=-transform.position.z;
                var low=cameraComponent.ViewportToWorldPoint(new Vector3(0,0,depth));
                var high=cameraComponent.ViewportToWorldPoint(new Vector3(1,1,depth));
                float ratio=Mathf.Min(1,bounds.size.x/Mathf.Max(.0001f,high.x-low.x),bounds.size.y/Mathf.Max(.0001f,high.y-low.y));
                if(ratio<1){cameraComponent.orthographicSize*=ratio*.999999f;continue;}
                float dx=low.x<bounds.min.x?bounds.min.x-low.x:high.x>bounds.max.x?bounds.max.x-high.x:0;
                float dy=low.y<bounds.min.y?bounds.min.y-low.y:high.y>bounds.max.y?bounds.max.y-high.y:0;
                if(dx==0&&dy==0)break;
                transform.position+=new Vector3(dx,dy,0);
            }
        }

        void UpdateLetterbox(bool visible)
        {
            if(visible&&letterboxCamera==null)
            {
                var go=new GameObject("Background boundary letterbox");
                go.hideFlags=HideFlags.HideAndDontSave;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
                letterboxCamera=go.AddComponent<Camera>();
                letterboxCamera.cullingMask=0;
                letterboxCamera.clearFlags=CameraClearFlags.SolidColor;
                letterboxCamera.backgroundColor=Color.black;
                letterboxCamera.orthographic=true;
                letterboxCamera.allowHDR=letterboxCamera.allowMSAA=false;
                letterboxCamera.useOcclusionCulling=false;
            }
            if(letterboxCamera==null)return;
            letterboxCamera.enabled=visible;
            letterboxCamera.depth=cameraComponent.depth-1;
            letterboxCamera.rect=originalViewport;
            letterboxCamera.targetTexture=cameraComponent.targetTexture;
            letterboxCamera.targetDisplay=cameraComponent.targetDisplay;
        }

        void OnDisable()
        {
            if(cameraComponent!=null&&viewportCaptured){cameraComponent.rect=originalViewport;cameraComponent.ResetAspect();}
            if(letterboxCamera!=null)letterboxCamera.enabled=false;
            ready=false;lookedForBackground=false;
        }

        void OnDestroy()
        {
            if(letterboxCamera==null)return;
            if(Application.isPlaying)Destroy(letterboxCamera.gameObject);
            else DestroyImmediate(letterboxCamera.gameObject);
        }
    }
}

