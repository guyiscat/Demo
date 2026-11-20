using UnityEngine;

namespace Seaside
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TutorialZone : MonoBehaviour
    {
        [TextArea, InspectorName("顶部文案")] public string text;
        [InspectorName("重叠区域优先级")] public int priority;
        public bool Contains(Vector2 position)
        {
            var area=GetComponent<BoxCollider2D>();
            return isActiveAndEnabled&&area.enabled&&area.OverlapPoint(position);
        }
        void OnDrawGizmosSelected()
        {
            var area=GetComponent<BoxCollider2D>();
            Gizmos.color=new Color(.2f,1,.6f,.5f);Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.DrawWireCube(area.offset,area.size);
        }
    }
}
