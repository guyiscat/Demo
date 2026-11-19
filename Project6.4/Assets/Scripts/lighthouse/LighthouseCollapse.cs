using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Lighthouse
{
    public sealed class LighthouseCollapse : MonoBehaviour
    {
        public LighthouseLevel level;
        public Transform mapRoot;
        public LineRenderer front;
        [Min(0),Tooltip("每个安全复活段开始后，坍塌追赶前的准备秒数。")]
        public float graceSeconds=8;
        [Min(.1f),Tooltip("坍塌边界每秒上升的世界单位。")]
        public float speed=1.15f;
        [Min(3),Tooltip("坍塌边界初始低于安全复活点的距离。")]
        public float initialOffset=12;
        [Min(10),Tooltip("每段安全复活点至下一段的限时秒数。")]
        public float segmentTimeLimit=150;
        public bool manualSimulation;
        public float Elapsed{get;private set;}
        public float Height{get;private set;}
        public float Remaining=>Mathf.Max(0,segmentTimeLimit-Elapsed);
        public int CollapsedCount=>collapsed.Count;
        public bool Started{get;private set;}
        readonly List<GameObject> collapsed=new List<GameObject>();
        readonly List<Renderer> candidates=new List<Renderer>();
        int checkpoint=-1;
        Vector2 anchor;
        void Start()
        {
            candidates.AddRange(mapRoot.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.gameObject.layer==6||r.gameObject.layer==7||r.GetComponent<Expedition.ClimbPoint>()!=null||r.GetComponent<Seaside.SeasideCell>()?.kind==Seaside.CellKind.Checkpoint));
            candidates.AddRange(mapRoot.GetComponentsInChildren<Forest.ForestZipline>().Select(r=>r.GetComponentInChildren<LineRenderer>()));
            ResetSegment();
        }
        void OnEnable(){if(level!=null&&level.player!=null)level.player.motor.ResetPerformed+=ResetSegment;}
        void OnDisable(){if(level!=null&&level.player!=null)level.player.motor.ResetPerformed-=ResetSegment;}
        void FixedUpdate(){if(!manualSimulation)Advance(Time.fixedDeltaTime);}
        public void ResetSegment()
        {
            foreach(var item in collapsed)if(item!=null)item.SetActive(true);collapsed.Clear();
            anchor=level.session.RespawnPoint==Vector2.zero?level.session.startPoint:level.session.RespawnPoint;
            checkpoint=level.session.CheckpointId;Elapsed=0;Height=anchor.y-initialOffset;Started=false;DrawFront();
        }
        public void Advance(float dt)
        {
            if(level.session.InputBlocked||level.session.ShowHelp||level.player.motor.frozen)return;
            if(checkpoint!=level.session.CheckpointId){ResetSegment();return;}
            // The player can read the instructions at the entrance/rest point.
            if(!Started)
            {
                if(Vector2.Distance(level.player.motor.Body.position,anchor)<2)return;
                Started=true;
            }
            Elapsed+=Mathf.Max(0,dt);Height=anchor.y-initialOffset+Mathf.Max(0,Elapsed-graceSeconds)*speed;DrawFront();
            foreach(var item in candidates)
            {
                if(item==null||!item.gameObject.activeInHierarchy||item.bounds.max.y>=Height)continue;
                var rail=item.GetComponentInParent<Forest.ForestZipline>();
                var target=rail!=null?rail.gameObject:item.gameObject;
                if(!collapsed.Contains(target)){collapsed.Add(target);target.SetActive(false);}
            }
            if(level.player.motor.Body.position.y-level.player.motor.BodySize.y*.5f<=Height||Elapsed>=segmentTimeLimit)level.session.Die();
        }
        void DrawFront()
        {
            if(front==null)return;front.positionCount=2;front.SetPosition(0,new Vector3(0,Height));front.SetPosition(1,new Vector3(level.cameraRig.mapSize.x,Height));
        }
    }
}
