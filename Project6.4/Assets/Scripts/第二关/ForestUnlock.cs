using UnityEngine;
using Seaside;

namespace Forest
{
    public sealed class ForestUnlock : MonoBehaviour
    {
        public string excelAddress;
        public int index;
        public ForestWaterwheel wheel;
        public SpriteRenderer picture;
        public bool Activated{get;private set;}
        void OnTriggerEnter2D(Collider2D other)
        {
            if(other.GetComponent<SeasideMotor>()!=null)Activate();
        }
        public void Activate()
        {
            if(Activated)return;
            Activated=true;picture.color=new Color(.35f,1,.58f);wheel.Unlock(index);
        }
        public void ResetUnlock(){Activated=false;picture.color=new Color(.35f,.8f,1);}
    }
}
