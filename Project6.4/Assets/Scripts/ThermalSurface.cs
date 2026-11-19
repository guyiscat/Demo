using UnityEngine;
namespace Expedition
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ThermalSurface : MonoBehaviour
    {
        public ProtectionMode requiredProtection;
        public string sourceAddress;
        void OnCollisionEnter2D(Collision2D collision){Validate(collision.collider);}
        void OnCollisionStay2D(Collision2D collision){Validate(collision.collider);}
        void Validate(Collider2D other)
        {
            var rules=other.GetComponent<ThermalTraversal>();
            if(rules!=null)rules.Touch(this);
        }
    }
}
