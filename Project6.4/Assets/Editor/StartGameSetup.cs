using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Seaside;

// All scene generation/build entry points are batch-only, to protect open unsaved scenes.
public static class StartGameSetup
{
    public const string MenuScene = "Assets/Scenes/StartGame.unity";
    public const string GameScene = "Assets/Scenes/TheFirstScene_Seaside.unity";
    const string Art = "Assets/Art/startgame/";
    [Serializable] class Layout { public float width, height; public Layer[] children; }
    [Serializable] class Layer { public string name; public float x, y, width, height; public TextureRef texture; }
    [Serializable] class TextureRef { public string uri; }
    static string Output => Path.Combine(SeasidePaths.ReleaseRoot, "Docs/StartupValidation");

    static void RequireBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in an isolated batch project; never replace an open scene.");
    }

    public static void GenerateAndBuild()
    {
        RequireBatch();
        if (File.Exists(MenuScene)) throw new InvalidOperationException("StartGame already exists. Generation will not overwrite authored edits.");
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Art + "startgame.json.ps"));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("StartGame");
        var cameraObject = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(root.transform);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 2.7f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.nearClipPlane = .1f; camera.farClipPlane = 30;
        var art = Child("Art", root.transform);
        var background = Child("Backgrounds", art);
        var emblem = Child("Emblem", art);
        var title = Child("Title", art);
        var controls = Child("MenuButtons", root.transform);
        var menu = root.AddComponent<StartGameMenu>();
        menu.menuCamera = camera; menu.gameScenePath = GameScene;
        menu.buttons = new StartGameButton[3];
        var names = new Dictionary<string, string> {
            { "bg", "Background" }, { "mb", "BackdropMask" }, { "ring", "EmblemRing" },
            { "demoshadow", "HeroShadow" }, { "demo", "HeroEmblem" }, { "light", "LightRays" },
            { "line", "Divider" }, { "logo", "TitleLogo" }
        };
        for (int i = 0; i < layout.children.Length; i++)
        {
            Layer layer = layout.children[i];
            if (layer.name.EndsWith("_on") || layer.name.EndsWith("_off")) continue;
            Transform parent = layer.name == "bg" || layer.name == "mb" ? background :
                layer.name == "logo" || layer.name == "line" ? title : emblem;
            Picture(layer, names[layer.name], parent, i, layout);
        }
        string[] keys = { "start", "set", "exit" };
        string[] buttonNames = { "StartButton", "SettingsButton", "ExitButton" };
        for (int i = 0; i < keys.Length; i++)
        {
            Layer on = layout.children.First(l => l.name == keys[i] + "_on");
            Layer off = layout.children.First(l => l.name == keys[i] + "_off");
            var button = Child(buttonNames[i], controls).gameObject.AddComponent<StartGameButton>();
            button.transform.position = Position(on, layout);
            button.normalVisual = Picture(off, "Normal", button.transform, 20, layout);
            button.highlightedVisual = Picture(on, "Highlighted", button.transform, 21, layout);
            button.SetHighlighted(i == 0);
            menu.buttons[i] = button;
        }
        EditorSceneManager.SaveScene(scene, MenuScene);
        var existing = EditorBuildSettings.scenes.Where(s => s.path != MenuScene && s.path != GameScene).ToList();
        existing.Insert(0, new EditorBuildSettingsScene(GameScene, true));
        existing.Insert(0, new EditorBuildSettingsScene(MenuScene, true));
        EditorBuildSettings.scenes = existing.ToArray();
        AssetDatabase.SaveAssets();
        ValidateAndBuild();
    }

    static Transform Child(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }

    static Vector3 Position(Layer layer, Layout layout)
    {
        return new Vector3((layer.x + layer.width / 2 - layout.width / 2) / 100,
            (layout.height / 2 - layer.y - layer.height / 2) / 100, 0);
    }

    static GameObject Picture(Layer layer, string name, Transform parent, int order, Layout layout)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + layer.texture.uri);
        if (sprite == null) throw new Exception("Missing sprite: " + layer.texture.uri);
        var go = Child(name, parent).gameObject;
        go.transform.position = Position(layer, layout);
        go.transform.localScale = new Vector3(layer.width / sprite.rect.width, layer.height / sprite.rect.height, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
        return go;
    }

    public static void ValidateAndBuild()
    {
        RequireBatch(); Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene(MenuScene);
        var menu = UnityEngine.Object.FindAnyObjectByType<StartGameMenu>();
        if (menu == null || menu.buttons.Length != 3 || menu.menuCamera == null) throw new Exception("Menu wiring incomplete");
        if (menu.gameScenePath != GameScene || !File.Exists(menu.gameScenePath)) throw new Exception("Invalid game target");
        var renderers = menu.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length != 14 || renderers.Any(r => r.sprite == null)) throw new Exception("Expected 14 valid artwork layers");
        if (menu.GetComponentsInChildren<Collider2D>(true).Length != 0) throw new Exception("Menu should not contain game collisions");
        if (menu.buttons.Any(b => b == null || b.normalVisual == null || b.highlightedVisual == null)) throw new Exception("Button references incomplete");
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            EditorSceneManager.OpenScene(scene.path);
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    throw new Exception("Missing script in " + scene.path + ": " + transform.name);
        }
        EditorSceneManager.OpenScene(MenuScene);
        menu = UnityEngine.Object.FindAnyObjectByType<StartGameMenu>();
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(1680, 720) })
            Capture(menu, size);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length < 2 || scenes[0] != MenuScene || !scenes.Contains(GameScene)) throw new Exception("Start menu must be first in scene list");
        string executable = Path.Combine(SeasidePaths.ReleaseRoot, "Play64/SeasideWhitebox.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = executable, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        File.WriteAllText(Path.Combine(Output, "build-result.json"), JsonUtility.ToJson(new Result {
            result = report.summary.result.ToString(), bytes = (long)report.summary.totalSize,
            errors = (int)report.summary.totalErrors, scenes = scenes
        }, true));
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Startup build failed: " + report.summary.result);
        Debug.Log("START_GAME_BUILD_PASS layers=14 buttons=3 target=" + GameScene);
    }

    [Serializable] class Result { public string result; public long bytes; public int errors; public string[] scenes; }

    static void Capture(StartGameMenu menu, Vector2Int size)
    {
        var target = new RenderTexture(size.x, size.y, 24);
        var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            menu.menuCamera.targetTexture = target; menu.FitCamera(); menu.menuCamera.Render();
            RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Output, "start-menu-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            menu.menuCamera.targetTexture = null; RenderTexture.active = previous;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
