using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    // Opt-in player check; absent from normal sessions unless -startupSmoke is provided.
    public sealed class StartGameMenuSmoke : MonoBehaviour
    {
        string folder;
        int assertions;
        bool complete;
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            folder = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../..")), "Docs/StartupValidation");
            Directory.CreateDirectory(folder);
            Application.logMessageReceived += OnLog;
            yield return null;
            var menu = FindAnyObjectByType<StartGameMenu>();
            try
            {
                Check(menu != null && menu.SelectedIndex == 0 && !menu.SettingsOpen, "Initial selection");
                Check(!menu.SelectAtScreenPoint(Vector2.zero), "Background does not activate buttons");
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pointer = menu.menuCamera.WorldToScreenPoint(menu.buttons[i].transform.position);
                    Check(menu.SelectAtScreenPoint(pointer) && menu.SelectedIndex == i, "Pointer selection " + i);
                    for (int j = 0; j < 3; j++) Check(menu.buttons[j].highlightedVisual.activeSelf == (i == j), "Exclusive highlight");
                }
                menu.SelectNext(1); Check(menu.SelectedIndex == 0, "Keyboard wrap forward");
                menu.SelectNext(-1); Check(menu.SelectedIndex == 2, "Keyboard wrap backward");
                menu.SelectNext(-1); menu.ActivateSelection(); Check(menu.SettingsOpen, "Settings action");
                Check(!menu.SelectAtScreenPoint(menu.menuCamera.WorldToScreenPoint(menu.buttons[0].transform.position)), "Modal blocks underlying menu");
                bool hadVolume = PlayerPrefs.HasKey("Seaside.MasterVolume");
                float previous = PlayerPrefs.GetFloat("Seaside.MasterVolume", 1);
                try { menu.SetMasterVolume(.35f); Check(Mathf.Abs(AudioListener.volume - .35f) < .001f, "Volume applied"); }
                finally
                {
                    if (hadVolume) PlayerPrefs.SetFloat("Seaside.MasterVolume", previous); else PlayerPrefs.DeleteKey("Seaside.MasterVolume");
                    PlayerPrefs.Save(); AudioListener.volume = previous;
                }
            }
            catch (Exception ex) { Fail(ex.ToString()); yield break; }
            menu.CloseSettings(); menu.SelectNext(-1);
            // Hidden Windows players have no reliable back buffer. Render the artwork explicitly.
            try { CaptureArtwork(menu); }
            catch (Exception ex) { Fail(ex.ToString()); yield break; }
            string targetScene = menu.gameScenePath;
            menu.ActivateSelection();
            float deadline = Time.realtimeSinceStartup + 25;
            while (SceneManager.GetActiveScene().path != targetScene && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            try
            {
                Check(SceneManager.GetActiveScene().path == "Assets/Scenes/TheFirstScene_Seaside.unity", "Start loads authored level");
                Check(FindAnyObjectByType<SeasideSession>() != null && FindAnyObjectByType<SeasideMotor>() != null, "Playable session and player");
                Check(FindAnyObjectByType<StartGameMenu>() == null, "Menu is unloaded");
                Check(Mathf.Approximately(Time.timeScale, 1), "Gameplay time restored");
                complete = true;
                File.WriteAllText(Path.Combine(folder, "runtime-result.txt"), "PASS assertions=" + assertions + " target=" + SceneManager.GetActiveScene().path);
                Debug.Log("START_MENU_RUNTIME_PASS assertions=" + assertions);
                Application.Quit(0);
            }
            catch (Exception ex) { Fail(ex.ToString()); }
        }

        void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
        void CaptureArtwork(StartGameMenu menu)
        {
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                menu.menuCamera.targetTexture = target; menu.FitCamera(); menu.menuCamera.Render();
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Check(pixels.GetPixel(640, 360).maxColorComponent > .1f, "Runtime artwork renders");
                File.WriteAllBytes(Path.Combine(folder, "runtime-artwork.png"), pixels.EncodeToPNG());
            }
            finally
            {
                menu.menuCamera.targetTexture = null; menu.FitCamera(); RenderTexture.active = previous;
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
        void OnLog(string message, string stack, LogType type)
        {
            if (!complete && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Fail(message + "\n" + stack);
        }
        void Fail(string message)
        {
            complete = true;
            File.WriteAllText(Path.Combine(folder, "runtime-result.txt"), "FAIL " + message);
            Application.Quit(1);
        }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
