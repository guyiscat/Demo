using UnityEngine;
namespace Seaside
{
    // Opt-in visual harness. This animates a target pose, not a gameplay completion replay.
    public sealed class CameraPreview:MonoBehaviour
    {
        SeasideMotor player;SeasideCamera follow;SeasideSession game;
        float elapsed;bool capture;
        string phase;
        void Start()
        {
            game=GetComponent<SeasideSession>();player=game.player;follow=FindFirstObjectByType<SeasideCamera>();
            var map=SeasideMapRoot.FindInScene(gameObject.scene);if(map!=null){map.SetActive(false);Debug.Log("SEASIDE_PREVIEW_MAP_HIDDEN mode=camera scene="+gameObject.scene.name);}
            else throw new System.InvalidOperationException("Preview map root could not be resolved.");
            var sprite=player.GetComponent<SpriteRenderer>().sprite;
            DemoBlock("Preview floor",new Vector2(75,3.5f),new Vector2(150,1),sprite);
            DemoBlock("Preview runway",new Vector2(40,14.5f),new Vector2(40,1),sprite);
            DemoBlock("Preview slide wall",new Vector2(62.5f,18),new Vector2(1,20),sprite);
            player.externalInput=true;player.Body.simulated=false;
            player.transform.position=new Vector3(38,16,0);follow.overview=false;follow.Snap();
            var args=System.Environment.GetCommandLineArgs();capture=System.Array.IndexOf(args,"-previewCapture")>=0;
        }
        void DemoBlock(string name,Vector2 position,Vector2 size,Sprite sprite)
        {
            var block=new GameObject(name);block.transform.position=position;block.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=block.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=new Color(.15f,.25f,.30f);
        }
        Vector2 Point(float t)
        {
            if(t<2){phase="RUN / TURN LOOKAHEAD";return new Vector2(38+8*t,16);}
            if(t<3.2f){phase="JUMP / APEX / LAND";float u=(t-2)/1.2f;return new Vector2(54+5*u,16+10*Mathf.Sin(u*Mathf.PI));}
            if(t<4.2f){phase="REVERSE DIRECTION";return new Vector2(59-8*(t-3.2f),16);}
            if(t<4.6f){phase="DASH / FAST FOLLOW";return new Vector2(51+25*(t-4.2f),16);}
            if(t<6.6f){phase="SLOW WALL DESCENT";return new Vector2(61,16-2.7f*(t-4.6f));}
            if(t<7.6f){phase="FAST FALL";return new Vector2(61,10.6f-6*(t-6.6f));}
            phase="OVERVIEW TRANSITION";return new Vector2(61,4.6f);
        }
        void Update()
        {
            elapsed+=Time.deltaTime;float t=elapsed%10;
            Vector2 p=Point(t);Vector2 previous=Point(Mathf.Max(0,t-Time.deltaTime));Point(t);
            player.transform.position=new Vector3(p.x,p.y,0);
            player.Body.linearVelocity=(p-previous)/Mathf.Max(.001f,Time.deltaTime);
            follow.PreviewDash=t>=4.2f&&t<4.6f;follow.PreviewWall=t>=4.6f&&t<6.6f;
            follow.overview=t>=7.6f;
            if(capture&&elapsed>6f)StartCoroutine(Capture());
        }
        System.Collections.IEnumerator Capture()
        {
            capture=false;yield return new WaitForEndOfFrame();
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../..","Docs"));
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"camera-preview.png"));
            yield return new WaitForSeconds(.3f);Application.Quit();
        }
        void OnGUI()
        {
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            var style=new GUIStyle(GUI.skin.box){fontSize=17};
            GUI.Box(new Rect(300,105,680,62),"CAMERA PREVIEW - SCRIPTED TARGET, NOT A ROUTE REPLAY\n"+phase,style);
        }
    }
}

