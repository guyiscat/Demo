using UnityEngine;
namespace Seaside
{
    public sealed class SeasideCamera:MonoBehaviour
    {
        public SeasideMotor player;
        public bool overview;
        public Vector2 mapSize;
        public CameraTuning tuning=new CameraTuning();
        public bool PreviewDash{get;set;}
        public bool PreviewWall{get;set;}
        readonly CameraMotion motion=new CameraMotion();
        Camera cameraComponent;
        bool ready;
        void Awake(){cameraComponent=GetComponent<Camera>();}
        void Start(){Snap();}
        void LateUpdate()
        {
            if(player==null)return;
            if(!ready)Snap();
            if(Time.timeScale<=0)return;
            Vector2 v=player.Body==null?Vector2.zero:player.Body.linearVelocity;
            // Rigidbody interpolation is read through Transform, and the camera updates after rendering poses.
            motion.Tick(player.transform.position,v,player.Grounded,player.Attached||player.WallSliding||PreviewWall,player.Dashing||PreviewDash,overview,mapSize,cameraComponent.aspect,Time.deltaTime,tuning);
            transform.position=motion.Position;cameraComponent.orthographicSize=motion.Size;
        }
        public void Snap()
        {
            if(player==null)return;
            if(cameraComponent==null)cameraComponent=GetComponent<Camera>();
            if(tuning==null)tuning=new CameraTuning();
            motion.Reset(player.transform.position,mapSize,cameraComponent.aspect,overview,tuning);
            transform.position=motion.Position;cameraComponent.orthographicSize=motion.Size;ready=true;
        }
    }
}

