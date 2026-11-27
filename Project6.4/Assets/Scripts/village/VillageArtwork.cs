using System.Linq;
using Seaside;
using UnityEngine;
namespace Village
{
    // All geometry and interactions remain authored by Village.unity. The distant
    // painted bridge is scenery; only the foreground sand marks walkable ground.
    public sealed class VillageArtwork : MonoBehaviour
    {
        public int CoveredGround{get;private set;}
        public Transform ArtRoot{get;private set;}
        Sprite Load(string name)=>Resources.Load<Sprite>("VillageArt/"+name);
        SpriteRenderer Picture(string name,Sprite sprite,Vector2 position,Vector2 size,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(ArtRoot,false);go.transform.position=position;
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;
            if(sprite!=null)go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            return r;
        }
        void Start()
        {
            var sky=Load("part3_bg_3");if(sky==null)return;
            var session=GetComponent<SeasideSession>();var camera=FindAnyObjectByType<SeasideCamera>();
            ArtRoot=new GameObject("VillageArtRoot").transform;ArtRoot.SetParent(transform.parent,false);
            float width=Mathf.Max(117,camera.mapSize.x)+30,height=width*sky.bounds.size.y/sky.bounds.size.x;
            Picture("VillageBackdrop",sky,new Vector2(camera.mapSize.x*.5f,-6+height*.5f),new Vector2(width,height),-120);
            camera.GetComponent<Camera>().backgroundColor=new Color(1,.95f,.73f);
            Picture("VillageFarDunes",Load("part3_bg_2"),new Vector2(camera.mapSize.x*.5f,-4.5f),new Vector2(width,width*732/6432),-100);
            Picture("VillageNearDunes",Load("part3_bg_1"),new Vector2(camera.mapSize.x*.5f,-7.5f),new Vector2(width,width*447/6432),-90);
            var tile=Load("SandTile");var rim=Load("GroundRim");
            foreach(var cell in FindObjectsByType<SeasideCell>().Where(c=>c.gameObject.scene==gameObject.scene&&c.kind==CellKind.Ground))
            {
                var picture=cell.GetComponent<SpriteRenderer>();var collider=cell.GetComponent<BoxCollider2D>();
                if(picture==null||collider==null)continue;
                picture.sprite=tile;picture.drawMode=SpriteDrawMode.Tiled;picture.size=collider.size;picture.color=Color.white;picture.sortingOrder=1;
                var bounds=collider.bounds;
                if(Physics2D.OverlapPoint(new Vector2(bounds.center.x,bounds.max.y+.05f),1<<6)==null)
                    Picture("GroundEdge_"+cell.excelAddress,rim,new Vector2(bounds.center.x,bounds.max.y-.08f),new Vector2(bounds.size.x,.16f),3);
                CoveredGround++;
            }
            // The whitebox pit stays fully open, with its actual trigger and route.
            var pit=session.GetComponent<WorldRegion>().exits.FirstOrDefault(e=>e.destination==CampaignFlow.Scenes[2]);
            if(pit!=null)
            {
                var bounds=pit.GetComponent<Collider2D>().bounds;
                Picture("MineShaftBackdrop",Load("ShaftShade"),new Vector2(bounds.center.x,-3),new Vector2(Mathf.Max(1,bounds.size.x-.15f),14),-20);
            }
            // Keep the existing robot and portal unmistakably above the scenery.
            var robot=FindAnyObjectByType<VillageRobot>();
            if(robot!=null){var picture=robot.GetComponent<SpriteRenderer>();if(picture!=null)picture.sortingOrder=20;}
        }
    }
}
