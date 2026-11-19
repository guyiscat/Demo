using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public static class GameTypography
    {
        static Font bundled;
        public static Font SystemFont
        {
            get
            {
                var settings=GameSystemSettings.Current;
                if(settings!=null&&settings.systemFont!=null)return settings.systemFont;
                if(bundled==null)bundled=Resources.Load<Font>("Fonts/AlibabaPuHuiTi-Regular");
                return bundled;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded-=OnSceneLoaded;
            SceneManager.sceneLoaded+=OnSceneLoaded;
            ApplyWorldLabels();
        }
        static void OnSceneLoaded(Scene scene,LoadSceneMode mode){ApplyWorldLabels();}
        public static void ApplyWorldLabels()
        {
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))Apply(text);
        }
        public static void Apply(TextMesh text)
        {
            var font=SystemFont;
            if(text==null||font==null)return;
            text.font=font;
            text.GetComponent<MeshRenderer>().sharedMaterial=font.material;
        }
    }
}
