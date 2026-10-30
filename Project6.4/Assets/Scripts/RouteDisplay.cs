using UnityEngine;
namespace Seaside
{
    public sealed class RouteDisplay:MonoBehaviour
    {
        SpriteRenderer[] pictures;SeasideSession game;
        void Start(){pictures=GetComponentsInChildren<SpriteRenderer>();game=FindFirstObjectByType<SeasideSession>();}
        void Update(){if(game==null)return;foreach(var sr in pictures)sr.enabled=game.ShowRoute;}
    }
}
