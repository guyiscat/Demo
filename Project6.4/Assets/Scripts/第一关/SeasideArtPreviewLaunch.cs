using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public static class SeasideArtPreviewLaunch
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SelectPreview()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-artPreview") >= 0)
                SceneManager.LoadScene("SeasideArtPreview");
        }
    }
}
