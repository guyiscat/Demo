using Expedition;
using UnityEngine;
namespace Seaside
{
    // Visual only: follows the actor in world space without changing its scale,
    // colliders, lighting or traversal abilities.
    [DefaultExecutionOrder(100)]
    public sealed class ProtectionAura : MonoBehaviour
    {
        [Min(.1f),InspectorName("防护光效直径")] public float diameter=5.5f;
        [Range(0,1),InspectorName("防护光效亮度")] public float brightness=.85f;
        public Color coldColor=new Color(.15f,.55f,1f);
        public Color heatColor=new Color(1f,.16f,.08f);
        SeasideMotor motor;SpriteRenderer actor,glow;ThermalTraversal thermal;Sprite sprite;
        public SpriteRenderer Glow=>glow;
        public ProtectionMode DisplayedMode=>thermal!=null?thermal.Mode:(ProtectionMode)CampaignFlow.Progress.protectionMode;
        void Awake()
        {
            motor=GetComponent<SeasideMotor>();actor=GetComponent<SpriteRenderer>();thermal=GetComponent<ThermalTraversal>();
            sprite=Resources.Load<Sprite>("Effects/ProtectionAura");
            var go=new GameObject("ProtectionGlow");go.transform.SetParent(transform.parent,false);glow=go.AddComponent<SpriteRenderer>();glow.sprite=sprite;Refresh();
        }
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(glow==null)return;
            var session=thermal!=null?thermal.player.session:FindAnyObjectByType<SeasideSession>();
            glow.enabled=CampaignFlow.CoreObtained&&actor!=null&&actor.enabled&&sprite!=null&&session!=null&&!session.Respawning&&!session.Travelling;
            if(!glow.enabled)return;
            glow.sortingLayerID=actor.sortingLayerID;glow.sortingOrder=actor.sortingOrder-1;
            glow.transform.position=transform.position+Vector3.up*.15f;
            // Sibling world-space object avoids the actor's authored visual scale.
            var scale=glow.transform.parent==null?Vector3.one:glow.transform.parent.lossyScale;
            float size=diameter/Mathf.Max(.001f,sprite.bounds.size.x);
            glow.transform.localScale=new Vector3(size/Mathf.Max(.001f,Mathf.Abs(scale.x)),size/Mathf.Max(.001f,Mathf.Abs(scale.y)),1);
            var color=DisplayedMode==ProtectionMode.Cold?coldColor:heatColor;color.a=brightness;glow.color=color;
        }
        void OnDisable(){if(glow!=null)glow.enabled=false;}
        void OnDestroy(){if(glow!=null)Destroy(glow.gameObject);}
    }
}
