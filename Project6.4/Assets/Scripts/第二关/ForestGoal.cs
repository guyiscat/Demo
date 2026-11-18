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
            if(!session.InputBlocked&&other.GetComponent<SeasideMotor>()==session.player)session.Complete();
        }
    }
}
