using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    [RequireComponent(typeof(Camera))]
    public sealed class DisplayBrightness : MonoBehaviour
    {
        public static float Value=>Mathf.Clamp01(PlayerPrefs.GetFloat("Seaside.Brightness",.5f));
        Material material;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnScene;SceneManager.sceneLoaded+=OnScene;}
        static void OnScene(Scene scene,LoadSceneMode mode){foreach(var camera in FindObjectsByType<Camera>())if(camera.GetComponent<DisplayBrightness>()==null)camera.gameObject.AddComponent<DisplayBrightness>();}
        public static void Set(float value){PlayerPrefs.SetFloat("Seaside.Brightness",Mathf.Clamp01(value));PlayerPrefs.Save();}
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            var template=Resources.Load<Material>("UI/DisplayBrightness");if(template==null){Graphics.Blit(source,destination);return;}
            if(material==null)material=new Material(template);material.SetFloat("_Gain",Mathf.Lerp(.4f,1.6f,Value));Graphics.Blit(source,destination,material);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
