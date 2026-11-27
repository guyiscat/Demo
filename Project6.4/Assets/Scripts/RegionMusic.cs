using UnityEngine;
using UnityEngine.SceneManagement;
namespace Seaside
{
    // One persistent 2D player, including when a region is entered directly in the editor.
    public sealed class RegionMusic : MonoBehaviour
    {
        public static RegionMusic Current{get;private set;}
        AudioSource first,second,target;
        public AudioClip Track=>target==null?null:target.clip;
        public int PlayRequests{get;private set;}
        public float EffectiveVolume=>target==null?0:target.volume*AudioListener.volume;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime(){Current=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Current!=null||GameMediaSettings.Current==null)return;
            var go=new GameObject("RegionMusic");DontDestroyOnLoad(go);go.AddComponent<RegionMusic>();
        }
        void Awake()
        {
            if(Current!=null&&Current!=this){Destroy(gameObject);return;}Current=this;
            first=Source();second=Source();AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat("Seaside.MasterVolume",1));
            SceneManager.sceneLoaded+=OnScene;Select(SceneManager.GetActiveScene().path);
        }
        AudioSource Source()
        {var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=true;s.spatialBlend=0;s.volume=0;s.ignoreListenerPause=true;return s;}
        void OnScene(Scene scene,LoadSceneMode mode){Select(scene.path);}
        public void Select(string path)
        {
            var clip=GameMediaSettings.Current.Track(path);
            if(clip==Track)return;
            if(clip==null){target=null;return;}
            target=target==first?second:first;target.Stop();target.clip=clip;target.volume=0;target.Play();PlayRequests++;
        }
        void Update()
        {
            var settings=GameMediaSettings.Current;
            float step=Time.unscaledDeltaTime*Mathf.Max(.01f,settings.musicVolume)/Mathf.Max(.01f,settings.musicFadeSeconds);
            foreach(var s in new[]{first,second})
            {
                s.volume=Mathf.MoveTowards(s.volume,s==target?settings.musicVolume:0,step);
                if(s!=target&&s.volume<=0&&s.isPlaying)s.Stop();
            }
        }
        void OnDestroy(){SceneManager.sceneLoaded-=OnScene;if(Current==this)Current=null;}
    }
}
