using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public sealed class StartGameMenu : MonoBehaviour
    {
        const string VolumeKey = "Seaside.MasterVolume";
        const string FullscreenKey = "Seaside.Fullscreen";
        [Header("Scene loaded by Start Game (include it in the Build Scene List)")]
        public string gameScenePath = "Assets/Scenes/TheFirstScene_Seaside.unity";
        [Header("Ordered: Start, Settings, Exit")]
        public StartGameButton[] buttons;
        public Camera menuCamera;
        public Vector2 designSize = new Vector2(960, 540);
        public float pixelsPerUnit = 100;
        public int SelectedIndex { get; private set; }
        public bool SettingsOpen { get; private set; }
        public bool IsLoading { get; private set; }
        string loadError;
        Vector2 previousPointer;
        GUIStyle heading, label;
        Font uiFont;

        void Awake()
        {
            Time.timeScale = 1;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1));
            if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) != 0;
            SelectedIndex = 0;
            previousPointer = Input.mousePosition;
            RefreshHighlights();
            FitCamera();
            SeasideLog.Begin();
            SeasideLog.Event("START_MENU", "target=" + gameScenePath);
        }

        void Start()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-startupSmoke") >= 0)
                new GameObject("StartupMenuSmoke").AddComponent<StartGameMenuSmoke>();
        }

        void Update()
        {
            FitCamera();
            if (IsLoading) return;
            if (SettingsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
                return;
            }
            Vector2 pointer = Input.mousePosition;
            if ((pointer - previousPointer).sqrMagnitude > .01f) SelectAtScreenPoint(pointer);
            previousPointer = pointer;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) SelectNext(1);
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) SelectNext(-1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                ActivateSelection();
            if (!IsLoading && !SettingsOpen && Input.GetMouseButtonDown(0) && SelectAtScreenPoint(pointer))
                ActivateSelection();
        }

        // Fit the complete artwork at any aspect ratio. The camera clears unused margins to black.
        public void FitCamera()
        {
            if (menuCamera == null || pixelsPerUnit <= 0 || designSize.x <= 0 || designSize.y <= 0) return;
            menuCamera.orthographicSize = Mathf.Max(designSize.y, designSize.x / menuCamera.aspect) / (2 * pixelsPerUnit);
        }

        public void SelectNext(int direction)
        {
            if (SettingsOpen || IsLoading || buttons == null || buttons.Length == 0) return;
            SelectedIndex = (SelectedIndex + direction % buttons.Length + buttons.Length) % buttons.Length;
            RefreshHighlights();
        }

        public bool SelectAtScreenPoint(Vector2 pointer)
        {
            if (SettingsOpen || IsLoading || menuCamera == null || buttons == null ||
                !menuCamera.pixelRect.Contains(pointer)) return false;
            Vector3 world = menuCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -menuCamera.transform.position.z));
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null && buttons[i].Contains(world))
                {
                    SelectedIndex = i;
                    RefreshHighlights();
                    return true;
                }
            return false;
        }

        void RefreshHighlights()
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++) if (buttons[i] != null) buttons[i].SetHighlighted(i == SelectedIndex);
        }

        public void ActivateSelection()
        {
            if (IsLoading || SettingsOpen) return;
            switch (SelectedIndex)
            {
                case 0: StartGame(); break;
                case 1: OpenSettings(); break;
                case 2: QuitGame(); break;
            }
        }

        public void StartGame()
        {
            if (IsLoading) return;
            if (SceneUtility.GetBuildIndexByScenePath(gameScenePath) < 0)
            {
                loadError = "Target scene is missing from Build Profiles > Scene List.";
                Debug.LogError(loadError + " " + gameScenePath, this);
                return;
            }
            IsLoading = true;
            Time.timeScale = 1;
            SeasideLog.Event("START_GAME", "target=" + gameScenePath);
            SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
        }

        public void OpenSettings() { SettingsOpen = true; SeasideLog.Event("MENU_SETTINGS", "open"); }
        public void CloseSettings() { SettingsOpen = false; }

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            PlayerPrefs.Save();
        }

        public void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void QuitGame()
        {
            SeasideLog.Event("MENU_EXIT", "requested");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnGUI()
        {
            if (!SettingsOpen && !IsLoading && string.IsNullOrEmpty(loadError)) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2, (Screen.height - 540 * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            if (heading == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 24);
                heading = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 26, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 18, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            }
            GUI.color = new Color(.015f, .04f, .09f, .93f);
            GUI.DrawTexture(new Rect(255, 145, 450, 270), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (SettingsOpen)
            {
                GUI.Label(new Rect(280, 160, 400, 45), "设置 / SETTINGS", heading);
                GUI.Label(new Rect(290, 222, 165, 30), "主音量 / Volume", label);
                float volume = GUI.HorizontalSlider(new Rect(465, 235, 170, 25), AudioListener.volume, 0, 1);
                if (!Mathf.Approximately(volume, AudioListener.volume)) SetMasterVolume(volume);
                GUI.Label(new Rect(640, 219, 55, 30), Mathf.RoundToInt(volume * 100) + "%", label);
                GUI.Label(new Rect(290, 276, 230, 30), "全屏 / Fullscreen", label);
                bool fullscreen = GUI.Toggle(new Rect(545, 280, 80, 30), Screen.fullScreen, "");
                if (fullscreen != Screen.fullScreen) SetFullscreen(fullscreen);
                if (GUI.Button(new Rect(405, 347, 150, 40), "返回 / BACK")) CloseSettings();
            }
            else
            {
                GUI.Label(new Rect(285, 190, 390, 140), IsLoading ? "正在加载 / LOADING..." : loadError, label);
                if (!IsLoading && GUI.Button(new Rect(405, 347, 150, 40), "BACK")) loadError = null;
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void OnDestroy()
        {
            if (uiFont != null) Destroy(uiFont);
            SeasideLog.End();
        }
    }
}
