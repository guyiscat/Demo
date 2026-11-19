using UnityEngine;

namespace Seaside
{
    // Shared physical support for wheel platforms, lifts and future moving terrain.
    public class MovingSurface : MonoBehaviour
    {
        public Rigidbody2D body;
        public Vector2 NextPosition{get;set;}
        public Vector2 Velocity{get;set;}
        public Collider2D Collider=>GetComponent<Collider2D>();
    }
}
