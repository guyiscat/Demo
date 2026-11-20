using UnityEngine;

namespace Seaside
{
    public enum StartMenuAction { NewGame, Continue, Settings, Exit }
    // Visual children keep their individual exported positions; this root is the hit target.
    public sealed class StartGameButton : MonoBehaviour
    {
        public StartMenuAction action;
        public bool Available{get;set;}=true;
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
            value&=Available;
            if (normalVisual != null) normalVisual.SetActive(!value);
            if (highlightedVisual != null) highlightedVisual.SetActive(value);
            foreach(var label in GetComponentsInChildren<TextMesh>(true))label.color=Available?Color.white:new Color(.58f,.62f,.61f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(hitSize.x, hitSize.y, 0));
        }
    }
}
