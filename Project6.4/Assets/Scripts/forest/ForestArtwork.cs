using System.Collections.Generic;
using System.Linq;
using Seaside;
using UnityEngine;
namespace Forest
{
    // Presentation only: all authored collider transforms and mechanisms stay in place.
    public sealed class ForestArtwork : MonoBehaviour
    {
        public int ThornGroups{get;private set;}
        public int CoveredHazards{get;private set;}
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        Sprite Sprite(string name){if(!sprites.TryGetValue(name,out var value))sprites[name]=value=Resources.Load<Sprite>("ForestArt/"+name);return value;}
        SpriteRenderer Picture(string name,Sprite sprite,Vector2 position,Vector2 size,Transform parent,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;
            if(sprite!=null)go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return r;
        }
        void Start()
        {
            var level=GetComponent<ForestLevel>();if(level==null||Sprite("part2_bg_2")==null)return;
            var root=new GameObject("ForestArtRoot").transform;root.SetParent(level.transform.parent,false);
            var size=level.cameraRig.mapSize;Picture("ForestBackground",Sprite("part2_bg_2"),new Vector2(size.x*.5f,50),new Vector2(size.x+30,150),root,-120);
            foreach(var cell in FindObjectsByType<SeasideCell>().Where(c=>c.gameObject.scene==gameObject.scene&&c.kind==CellKind.Ground))
            {
                var renderer=cell.GetComponent<SpriteRenderer>();if(renderer==null)continue;
                renderer.sprite=Sprite("TerrainTile");renderer.drawMode=SpriteDrawMode.Tiled;renderer.size=cell.GetComponent<BoxCollider2D>().size;
                renderer.color=cell.attachable?Color.white:new Color(.55f,.63f,.57f);renderer.sortingOrder=1;
            }
            var wheel=level.wheel;
            foreach(var line in wheel.rim.GetComponentsInChildren<LineRenderer>())line.enabled=false;
            Picture("WheelPedestal",Sprite("part2_wheel_1"),wheel.center+new Vector2(0,-wheel.radius.y*.53f),new Vector2(wheel.radius.x*1.3f,wheel.radius.y*1.35f),root,-30);
            Picture("WheelArtwork",Sprite("part2_wheel_2"),wheel.center,wheel.radius*2,wheel.rim,-15);
            foreach(var r in wheel.GetComponentsInChildren<SpriteRenderer>())if(r.name=="Hub")r.enabled=false;
            foreach(var platform in wheel.platforms)
            {
                var r=platform.GetComponent<SpriteRenderer>();if(r!=null)r.enabled=false;
                var b=platform.Collider.bounds;Picture("WheelStepArtwork",Sprite("part2_wheel_3"),b.center,b.size,platform.transform,4);
            }
            // The bridge and waterfall are resized to their actual saved geometry.
            var water=level.player.waterfall;var bounds=water.curtainBounds.bounds;
            foreach(var r in water.GetComponentsInChildren<SpriteRenderer>())if(r!=water.occlusion)r.enabled=false;
            Picture("WaterfallArtwork",Sprite("part2_water_2"),bounds.center,bounds.size,root,8);
            Picture("LowerWaterArtwork",Sprite("part2_water_1"),new Vector2(wheel.center.x,0),new Vector2(wheel.radius.x*2.4f,10),root,-25);
            foreach(var rail in level.player.ziplines)
            {
                foreach(var r in rail.GetComponentsInChildren<SpriteRenderer>())r.color=new Color(.1f,.3f,.16f);
                foreach(var line in rail.GetComponentsInChildren<LineRenderer>()){line.startColor=line.endColor=new Color(.08f,.25f,.16f);line.startWidth=line.endWidth=.12f;}
                var cable=Picture("CableArtwork",Sprite("Cable"),(rail.StartPoint+rail.EndPoint)*.5f+Vector2.down*.35f,new Vector2(rail.Length,1.1f),rail.transform,5);
                cable.transform.rotation=Quaternion.FromToRotation(Vector3.right,rail.Tangent);
            }
            var spikes=FindObjectsByType<SeasideCell>().Where(c=>c.gameObject.scene==gameObject.scene&&c.kind==CellKind.Spike).ToList();
            while(spikes.Count>0)
            {
                var first=spikes[0];spikes.RemoveAt(0);var group=new List<SeasideCell>{first};var queue=new Queue<SeasideCell>();queue.Enqueue(first);
                while(queue.Count>0)
                {
                    var from=queue.Dequeue();foreach(var next in spikes.ToArray())
                    {var d=next.transform.position-from.transform.position;if((Mathf.Abs(d.x)<.15f&&Mathf.Abs(d.y)<=1.05f||Mathf.Abs(d.y)<.15f&&Mathf.Abs(d.x)<=1.05f)&&Quaternion.Angle(next.transform.rotation,first.transform.rotation)<1){group.Add(next);spikes.Remove(next);queue.Enqueue(next);}}
                }
                var b=first.GetComponent<Collider2D>().bounds;foreach(var c in group){b.Encapsulate(c.GetComponent<Collider2D>().bounds);c.GetComponent<SpriteRenderer>().enabled=false;}
                bool vertical=b.size.y>b.size.x;string name=vertical&&group.Count>2?"part2_thorn_l":group.Count>2?"part2_thorn_m":"part2_thorn_s";
                var r=Picture("Thorns_"+first.excelAddress,Sprite(name),b.center,new Vector2(Mathf.Max(.9f,b.size.x),Mathf.Max(.9f,b.size.y)),root,7);
                if(vertical&&name!="part2_thorn_l")
                {r.transform.rotation=Quaternion.Euler(0,0,90);r.transform.localScale=new Vector3(Mathf.Max(.9f,b.size.y)/r.sprite.bounds.size.x,Mathf.Max(.9f,b.size.x)/r.sprite.bounds.size.y,1);}
                // Match the actual dangerous volume; these visuals add no collision.
                ThornGroups++;CoveredHazards+=group.Count;
            }
        }
    }
}
