using UnityEngine;
namespace Mine
{
    public sealed class MineGate : MonoBehaviour
    {
        public Collider2D barrier;
        public SpriteRenderer visual;
        public bool Opened{get;private set;}
        public void Open(){Opened=true;if(barrier!=null)barrier.enabled=false;if(visual!=null)visual.enabled=false;}
        public void ResetGate(){Opened=false;if(barrier!=null)barrier.enabled=true;if(visual!=null)visual.enabled=true;}
    }
}
