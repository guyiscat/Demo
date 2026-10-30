using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    /// <summary>Identifies playable map geometry independently of hierarchy names.</summary>
    [DisallowMultipleComponent]
    public sealed class SeasideMapRoot : MonoBehaviour
    {
        public static GameObject FindInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var marker = root.GetComponentInChildren<SeasideMapRoot>(true);
                if (marker != null) return marker.gameObject;
            }
            // Compatibility for the original Seaside01 scene: derive the common map parent
            // from its cells, so that it does not need to be renamed or rewritten.
            Transform ancestor = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cell in root.GetComponentsInChildren<SeasideCell>(true))
                {
                    if (ancestor == null) ancestor = cell.transform;
                    else
                    {
                        while (ancestor != null && cell.transform != ancestor && !cell.transform.IsChildOf(ancestor))
                            ancestor = ancestor.parent;
                        if (ancestor == null) return null;
                    }
                }
            return ancestor == null ? null : ancestor.gameObject;
        }
    }
}
