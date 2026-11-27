using UnityEngine;
namespace Seaside
{
    public sealed class TravelPoint : MonoBehaviour
    {
        public SeasideSession session;
        public static TravelPoint For(SeasideSession owner)
        {foreach(var point in FindObjectsByType<TravelPoint>())if(point.session==owner)return point;return null;}
        [Tooltip("可选：指定传送后的站立位置。留空时在标志旁选择安全地面。")]
        public Transform arrivalAnchor;
        public Vector2 ArrivalPosition()
        {
            var motor=session.player;Physics2D.SyncTransforms();
            if(arrivalAnchor!=null&&ManualSaveSafety.SafeGround(motor,arrivalAnchor.position))return arrivalAnchor.position;
            Vector2 center=transform.position;if(ManualSaveSafety.SafeGround(motor,center))return center;
            Vector2 result=center;float best=float.PositiveInfinity;
            float radius=Mathf.Max(1,interactionDistance-.1f);
            for(float x=-radius;x<=radius+.01f;x+=.25f)
            {
                Vector2 from=center+new Vector2(x,radius);
                foreach(var hit in Physics2D.RaycastAll(from,Vector2.down,radius*2+motor.BodySize.y,1<<6))
                {
                    if(hit.collider==null||hit.collider.isTrigger||hit.normal.y<.95f)continue;
                    var candidate=hit.point+Vector2.up*(motor.BodySize.y*.5f+.05f);
                    float distance=Vector2.Distance(candidate,center);
                    if(distance>radius||distance>=best||!ManualSaveSafety.SafeGround(motor,candidate))continue;
                    best=distance;result=candidate;
                }
            }
            return result;
        }
        [Min(0)] public float interactionDistance=3;
        public bool Nearby=>session!=null&&Vector2.Distance(session.player.Body.position,transform.position)<=interactionDistance;
        public bool Usable=>session!=null&&TravelPointMenu.AllowsRegion(session.gameObject.scene.path);
        void Update()
        {
            if(!Usable||session.InputBlocked||session.player.frozen||session.ShowHelp||!Nearby||!GameBindings.Pressed(GameAction.Interact))return;
            session.GetComponent<TravelPointMenu>()?.Open(this);
        }
    }
}
