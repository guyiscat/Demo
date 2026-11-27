using UnityEngine;
using Seaside;

namespace Forest
{
    public sealed class ForestGoal : MonoBehaviour
    {
        public ForestWaterwheel wheel;
        public SeasideSession session;
        void OnTriggerEnter2D(Collider2D other){TryComplete(other);}
        void OnTriggerStay2D(Collider2D other){TryComplete(other);}
        void TryComplete(Collider2D other)
        {
            // Region exits, rather than the old goal, now connect maps.
        }
    }
}
