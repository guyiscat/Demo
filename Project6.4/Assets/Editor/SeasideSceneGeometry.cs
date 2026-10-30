using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Seaside;

// Ignore cosmetic names and parent paths; compare actual gameplay data and world geometry.
public static class SeasideSceneGeometry
{
    static string F(float value) => Math.Round(value, 4).ToString("0.0000", CultureInfo.InvariantCulture);
    static string V(Vector3 value) => F(value.x)+","+F(value.y)+","+F(value.z);
    public static string Signature(Scene scene)
    {
        var cells = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SeasideCell>(true));
        return string.Join("\n", cells.Select(c => {
            var b = c.GetComponent<BoxCollider2D>();
            string shape = "none";
            if (b != null)
            {
                var size = b.size * .5f;
                var corners = new[] { new Vector2(-size.x,-size.y), new Vector2(size.x,-size.y),
                    new Vector2(size.x,size.y), new Vector2(-size.x,size.y) };
                shape = string.Join(";", corners.Select(p => V(b.transform.TransformPoint(p+b.offset))))+
                    "|"+b.enabled+"|"+b.isTrigger+"|"+F(b.edgeRadius)+"|"+
                    (b.sharedMaterial == null ? "none" : F(b.sharedMaterial.friction)+","+F(b.sharedMaterial.bounciness));
            }
            var q = c.transform.rotation;
            return c.excelAddress+"|"+c.kind+"|"+V(c.transform.position)+"|"+V(c.transform.lossyScale)+
                "|"+F(q.x)+","+F(q.y)+","+F(q.z)+","+F(q.w)+"|"+c.gameObject.layer+"|"+c.gameObject.activeInHierarchy+
                "|"+c.attachable+"|"+c.checkpointId+"|"+V(c.respawn)+"|"+
                (c.respawnAnchor == null ? "none" : V(c.respawnAnchor.position))+"|"+shape;
        }).OrderBy(s => s, StringComparer.Ordinal));
    }
}
