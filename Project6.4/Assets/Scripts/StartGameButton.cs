using UnityEngine;

namespace Seaside
{
    // Visual children keep their individual exported positions; this root is the hit target.
    public sealed class StartGameButton : MonoBehaviour
    {
        public GameObject normalVisual;
        public GameObject highlightedVisual;
        public Vector2 hitSize = new Vector2(1.27f, .42f);

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return Mathf.Abs(local.x) <= hitSize.x * .5f && Mathf.Abs(local.y) <= hitSize.y * .5f;
        }

        public void SetHighlighted(bool value)
        {
            if (normalVisual != null) normalVisual.SetActive(!value);
            if (highlightedVisual != null) highlightedVisual.SetActive(value);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(hitSize.x, hitSize.y, 0));
        }
    }
}
