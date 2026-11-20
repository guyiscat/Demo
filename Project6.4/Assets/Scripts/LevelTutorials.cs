using UnityEngine;

namespace Seaside
{
    public sealed class LevelTutorials : MonoBehaviour
    {
        public SeasideSession session;
        public TutorialZone[] zones;
        public string Current=>session==null||session.player==null?null:At(session.player.Body.position);
        public string At(Vector2 position)
        {
            TutorialZone best=null;
            if(zones!=null)foreach(var zone in zones)
                if(zone!=null&&zone.Contains(position)&&(best==null||zone.priority>best.priority))best=zone;
            return best==null?null:best.text;
        }
    }
}
