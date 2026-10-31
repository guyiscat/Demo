using UnityEngine;

namespace Seaside
{
    // Presentation only. Existing colliders, triggers and map transforms are never changed.
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SeasideArtAlignment : MonoBehaviour
    {
        [Tooltip("Bottom-left corner of the full illustration, in map units.")]
        public Vector2 worldOrigin = new Vector2(0, -5);
        [Min(1)] public float worldWidth = 155;
        [Range(0, 1)] public float opacity = 1;
        public bool showArtwork = true;
        public bool showWhitebox = true;
        public bool showControls = true;

        public float WorldHeight
        {
            get
            {
                var sprite = GetComponent<SpriteRenderer>().sprite;
                return sprite == null ? 0 : worldWidth * sprite.rect.height / sprite.rect.width;
            }
        }

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        void Start()
        {
            if (!Application.isPlaying) return;
            var args = System.Environment.GetCommandLineArgs();
            int capture = System.Array.IndexOf(args, "-artRenderCapture");
            if (capture >= 0 && capture + 1 < args.Length)
                StartCoroutine(CaptureArtwork(args[capture + 1], System.Array.IndexOf(args, "-artOnly") >= 0));
        }

        // Explicit visual verification path; Camera.Render also works with a hidden player window.
        System.Collections.IEnumerator CaptureArtwork(string path, bool artOnly)
        {
            yield return null;
            var session = FindAnyObjectByType<SeasideSession>();
            session.player.frozen = true;
            var follow = FindAnyObjectByType<SeasideCamera>();
            follow.overview = true;
            follow.GetComponent<Camera>().aspect = 16f / 9;
            follow.Snap();
            showWhitebox = !artOnly;
            Apply();
            var camera = follow.GetComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                pixels.Apply();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
                Debug.Log("SEASIDE_ART_CAPTURE_PASS scene=" + gameObject.scene.name + " whitebox=" + showWhitebox + " path=" + path);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Destroy(target); Destroy(pixels);
            }
            Application.Quit(0);
        }

        [ContextMenu("Apply art alignment (visuals only)")]
        public void Apply()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;
            worldWidth = Mathf.Max(1, worldWidth);
            opacity = Mathf.Clamp01(opacity);
            Vector2 size = new Vector2(worldWidth, WorldHeight);
            transform.position = new Vector3(worldOrigin.x + size.x * .5f, worldOrigin.y + size.y * .5f, 0);
            transform.localScale = new Vector3(size.x / renderer.sprite.bounds.size.x, size.y / renderer.sprite.bounds.size.y, 1);
            renderer.sortingOrder = -100;
            renderer.color = new Color(1, 1, 1, opacity);
            renderer.enabled = showArtwork;
            if (!gameObject.scene.IsValid()) return;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var cell in root.GetComponentsInChildren<SeasideCell>(true))
                    if (cell.kind == CellKind.Ground && cell.TryGetComponent<SpriteRenderer>(out var visual))
                        visual.enabled = showWhitebox;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            bool changed = false;
            if (Input.GetKeyDown(KeyCode.F6)) { showWhitebox = !showWhitebox; changed = true; }
            if (Input.GetKeyDown(KeyCode.F7)) { showArtwork = !showArtwork; changed = true; }
            if (changed)
            {
                Apply();
                SeasideLog.Event("ART_PREVIEW", "whitebox=" + showWhitebox + " artwork=" + showArtwork);
            }
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !showControls) return;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, wordWrap = true };
            GUI.Box(new Rect(20, 92, 1240, 50),
                "ART ALIGNMENT PREVIEW - illustration differs from playable geometry\nF6: whitebox " +
                (showWhitebox ? "ON" : "OFF (colliders remain)") + "   F7: art   TAB: overview", style);
            GUI.matrix = oldMatrix;
        }
    }
}
