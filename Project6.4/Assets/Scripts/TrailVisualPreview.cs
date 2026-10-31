using System.Collections;
using System.IO;
using UnityEngine;
namespace Seaside
{
    // Opt-in rendered smoke test of a real motor dash; never saves or rebuilds the map.
    public sealed class TrailVisualPreview:MonoBehaviour
    {
        SeasideMotor player;
        float age;
        bool started,stopped;
        void Start()
        {
            player=GetComponent<SeasideSession>().player;player.externalInput=true;
            var map=SeasideMapRoot.FindInScene(gameObject.scene);if(map!=null){map.SetActive(false);Debug.Log("SEASIDE_PREVIEW_MAP_HIDDEN mode=trail scene="+gameObject.scene.name);}
            else throw new System.InvalidOperationException("Preview map root could not be resolved.");
            player.ResetAt(new Vector2(55,30));
            FindAnyObjectByType<SeasideCamera>().Snap();
            var ground=new GameObject("Temporary visual calibration floor");ground.transform.position=new Vector3(70,26,0);
            var sr=ground.AddComponent<SpriteRenderer>();sr.sprite=player.GetComponent<SpriteRenderer>().sprite;
            Vector2 native=sr.sprite.bounds.size;
            ground.transform.localScale=new Vector3(60/native.x,1/native.y,1);sr.color=new Color(.12f,.25f,.30f);
            StartCoroutine(Capture());
        }
        void FixedUpdate()
        {
            if(player==null||stopped)return;
            age+=Time.fixedDeltaTime;
            if(age<.4f){player.Body.linearVelocity=Vector2.zero;return;}
            player.Tick(Time.fixedDeltaTime,new SeasideInput{dashDown=!started,aim=Vector2.right});
            started=true;
        }
        IEnumerator Capture()
        {
            yield return new WaitForSeconds(.60f);
            yield return new WaitForEndOfFrame();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../..","Docs","Logs","VisualPreview",System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"dash-afterimages.png"));
            stopped=true;player.Body.linearVelocity=Vector2.zero;player.frozen=true;
            yield return new WaitForSeconds(.4f);Debug.Log("SEASIDE_TRAIL_PREVIEW_FINISHED");Application.Quit();
        }
        void OnGUI()
        {
            GUI.Label(new Rect(25,110,700,30),"VISUAL CHECK: REAL DASH + POOLED AFTERIMAGES (TEMPORARY TEST VIEW)");
        }
    }
}
