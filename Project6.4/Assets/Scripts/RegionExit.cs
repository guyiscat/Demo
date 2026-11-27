using UnityEngine;

namespace Seaside
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RegionExit : MonoBehaviour
    {
        public SeasideSession session;
        public string destination,arrival="Entry";
        public bool requiresInteraction,requiresLighthouse,requiresSeasideStory;
        public bool requiresUpward;
        public string Hint=>requiresLighthouse&&!CampaignFlow.LighthouseUnlocked?"雪山选择继续后，向左前往灯塔":requiresSeasideStory&&session.GetComponent<SeasideStory>()?.Read!=true?"先在灯塔按 E 阅读":requiresInteraction?"按 E 返回村庄":"继续前行："+CampaignFlow.CurrentTitle(destination);
        public bool Inside=>session!=null&&GetComponent<Collider2D>().OverlapPoint(session.player.Body.position);
        public int TravelDirection=>requiresInteraction||destination==CampaignFlow.Scenes[2]?1:transform.position.x<session.startPoint.x?-1:1;
        public bool TryTravel()
        {
            if(session==null||session.InputBlocked||session.player.frozen||!Inside)return false;
            if(requiresUpward&&session.player.Body.linearVelocity.y<=.1f)return false;
            if(requiresLighthouse&&!CampaignFlow.LighthouseUnlocked)return false;
            if(requiresSeasideStory&&session.GetComponent<SeasideStory>()?.Read!=true)return false;
            return CampaignFlow.Travel(session,destination,destination==CampaignFlow.Scenes[2]&&!requiresInteraction?"Shaft":arrival,false,TravelDirection);
        }
        void Update()
        {
            if(!Inside)return;
            if(!requiresInteraction||GameBindings.Pressed(GameAction.Interact))TryTravel();
        }
        void OnDrawGizmosSelected(){var b=GetComponent<BoxCollider2D>();Gizmos.color=Color.cyan;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(b.offset,b.size);}
    }
}
