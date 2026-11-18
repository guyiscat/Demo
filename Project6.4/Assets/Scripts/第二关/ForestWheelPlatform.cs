using UnityEngine;

namespace Forest
{
    public sealed class ForestWheelPlatform : MonoBehaviour
    {
        public ForestWaterwheel wheel;
        public Rigidbody2D body;
        public float initialAngle,angle,previousAngle;
        public Vector2 NextPosition{get;set;}
        public Vector2 Velocity{get;set;}
        public Collider2D Collider=>GetComponent<Collider2D>();
    }
}
