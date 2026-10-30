using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Seaside;

public static class SeasideArtSetup
{
    public const string PreviewScene = "Assets/Scenes/SeasideArtPreview.unity";
    public const string ArtPath = "Assets/Art/Backgrounds/Seaside.png";

    [MenuItem("Seaside/Art/Open alignment preview")]
    public static void OpenPreview()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(PreviewScene);
    }

    // Deliberately no menu command that overwrites existing preview edits.
    public static void CreatePreview()
    {
        if (File.Exists(PreviewScene)) throw new InvalidOperationException("Preview already exists; preserve manual edits.");
        // Use the canonical asset instead of restoring a duplicate over the user's artwork.
        if (!File.Exists(ArtPath)) throw new FileNotFoundException("Import the Seaside reference PNG into " + ArtPath, ArtPath);
        AssetDatabase.ImportAsset(ArtPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 50;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.maxTextureSize = 8192;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var serialized = new TextureImporterSettings();
        importer.ReadTextureSettings(serialized);
        serialized.spriteMeshType = SpriteMeshType.FullRect;
        serialized.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(serialized);
        importer.SaveAndReimport();

        if (!AssetDatabase.CopyAsset("Assets/Scenes/Seaside01.unity", PreviewScene)) throw new IOException("Scene copy failed.");
        EditorSceneManager.OpenScene(PreviewScene);
        var art = new GameObject("SEASIDE ART - select to align, visual only");
        var renderer = art.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        // Match the existing scene's built-in sprite shader.
        renderer.sharedMaterial = UnityEngine.Object.FindFirstObjectByType<SeasideMotor>().GetComponent<SpriteRenderer>().sharedMaterial;
        var alignment = art.AddComponent<SeasideArtAlignment>();
        alignment.Apply();
        UnityEngine.Object.FindFirstObjectByType<SeasideCamera>().overview = true;
        EditorSceneManager.MarkSceneDirty(art.scene);
        EditorSceneManager.SaveOpenScenes();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/Seaside01.unity", true),
            new EditorBuildSettingsScene(PreviewScene, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("SEASIDE_ART_PREVIEW_CREATED origin=" + alignment.worldOrigin + " width=" + alignment.worldWidth + " height=" + alignment.WorldHeight);
    }

    public static void CheckPreview()
    {
        // Validate the copied scene without altering either scene or reimporting the map.
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        var before = GeometrySignature();
        EditorSceneManager.OpenScene(PreviewScene);
        var after = GeometrySignature();
        if (before != after) throw new Exception("Preview geometry differs from original scene.");
        var art = UnityEngine.Object.FindFirstObjectByType<SeasideArtAlignment>();
        var layered = UnityEngine.Object.FindFirstObjectByType<SeasideArtRoot>();
        if (art == null && layered == null) throw new Exception("Preview artwork root missing.");
        var visualRoot = art != null ? art.gameObject : layered.gameObject;
        if (!visualRoot.GetComponentsInChildren<SpriteRenderer>(true).Any(r => r.sprite != null)) throw new Exception("Preview sprites missing.");
        if (visualRoot.GetComponentsInChildren<Collider2D>(true).Length != 0) throw new Exception("Art must have no collider.");
        var ground = UnityEngine.Object.FindObjectsByType<SeasideCell>(FindObjectsSortMode.None)
            .Where(c => c.kind == CellKind.Ground).Select(c => c.GetComponent<SpriteRenderer>()).Where(r => r != null).ToArray();
        var visibility = ground.Select(r => r.enabled).ToArray();
        foreach(var renderer in ground)renderer.enabled=false;
        if (GeometrySignature() != before) throw new Exception("Hiding whitebox changed geometry.");
        for(int i=0;i<ground.Length;i++)ground[i].enabled=visibility[i];
        if (ground.Where((r,i)=>r.enabled!=visibility[i]).Any())throw new Exception("Whitebox restore failed.");
        string report = "PASS Original and preview geometry signatures match\nPASS Art sprite exists and has no collider\nPASS Whitebox visibility toggle preserves all geometry\nPASS Whitebox can be restored\n";
        File.WriteAllText(Path.Combine(SeasidePaths.ReleaseRoot, "Docs/art-preview-checks.txt"), report);
        Debug.Log("SEASIDE_ART_CHECKS_PASS");
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
    }

    static string GeometrySignature()
    {
        return SeasideSceneGeometry.Signature(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
