using System.Linq;
using Seaside;
using UnityEngine;
namespace Mine
{
    public sealed class MinePassage : MonoBehaviour
    {
        public float shaftX=86;
        [Min(2)] public float dropHeight=12;
        SeasideSession session;
        public Vector2 Landing=>new Vector2(shaftX,session.startPoint.y);
        public RegionExit UpperExit{get;private set;}
        void Awake()
        {
            session=GetComponent<SeasideSession>();var go=new GameObject("UpperShaftToVillage");go.transform.SetParent(transform,false);
            go.transform.position=new Vector2(shaftX,session.startPoint.y+17f);
            var trigger=go.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(4,1.5f);
            UpperExit=go.AddComponent<RegionExit>();UpperExit.session=session;UpperExit.destination=CampaignFlow.Scenes[5];UpperExit.arrival="Mine";UpperExit.requiresUpward=true;
            var region=GetComponent<WorldRegion>();region.exits=region.exits.Concat(new[]{UpperExit}).ToArray();
        }
    }
}
