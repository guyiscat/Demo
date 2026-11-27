using Seaside;
using UnityEngine;
namespace Mine
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class MineTouchSwitch : MonoBehaviour
    {
        public MineLevel level;
        public MineLift lift;
        public MineGate gate;
        public void Touch(SeasideMotor player)
        {
            if(player!=level.player.motor||level.session.InputBlocked)return;
            if(lift!=null)lift.Activate();if(gate!=null)gate.Open();
            var sr=GetComponent<SpriteRenderer>();if(sr!=null)sr.color=new Color(.4f,1,.6f);
            CampaignFlow.SaveCheckpoint(level.session);
        }
        void OnTriggerEnter2D(Collider2D other){Touch(other.GetComponent<SeasideMotor>());}
        void OnTriggerStay2D(Collider2D other){Touch(other.GetComponent<SeasideMotor>());}
    }
}
