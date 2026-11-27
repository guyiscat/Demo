using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seaside
{
    public sealed class WorldTransition : MonoBehaviour
    {
        public static bool Active{get;private set;}
        public static float Alpha{get;private set;}
        public static float LastBlackDuration{get;private set;}
        string path,anchor;
        int direction=1;
        bool directArrival;
        public static void Begin(SeasideSession session,string destination,string arrival,int enterDirection=1,bool arriveDirectly=false)
        {
            if(Active)return;
            Active=true;LastBlackDuration=0;CampaignFlow.IsLoading=true;session.Travelling=true;
            session.player.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();session.player.BeginTraversal();session.player.frozen=true;
            var go=new GameObject("WorldTransition");DontDestroyOnLoad(go);
            var transition=go.AddComponent<WorldTransition>();transition.path=destination;transition.anchor=arrival;transition.direction=enterDirection;
            transition.directArrival=arriveDirectly;GameBindings.BlockFrame();
            transition.StartCoroutine(transition.Run());
        }
        IEnumerator Fade(float from,float to)
        {
            float seconds=Mathf.Max(.01f,GameSystemSettings.Current.regionFadeDuration),t=0;
            while(t<seconds){t+=Time.unscaledDeltaTime;Alpha=Mathf.Lerp(from,to,t/seconds);yield return null;}Alpha=to;
        }
        IEnumerator Run()
        {
            Time.timeScale=1;yield return Fade(0,1);
            float blackStarted=Time.realtimeSinceStartup;
            var operation=SceneManager.LoadSceneAsync(path);yield return operation;yield return null;yield return null;
            if(path==CampaignFlow.MenuScene)
            {
                while(Time.realtimeSinceStartup-blackStarted<Mathf.Max(0,GameSystemSettings.Current.regionBlackDuration))yield return null;
                LastBlackDuration=Time.realtimeSinceStartup-blackStarted;
                yield return Fade(1,0);CampaignFlow.MenuReady();Active=false;Alpha=0;Destroy(gameObject);yield break;
            }
            var session=FindAnyObjectByType<SeasideSession>();
            if(session==null){Fail();yield break;}
            session.Travelling=true;session.player.frozen=true;
            var persistence=session.GetComponent<CampaignCheckpointPersistence>();
            float deadline=Time.realtimeSinceStartup+15;
            while(persistence!=null&&!persistence.Ready&&Time.realtimeSinceStartup<deadline)yield return null;
            var region=session.GetComponent<WorldRegion>();var point=region?.Arrival(anchor);
            if(point==null){Fail();yield break;}
            Vector2 target=directArrival&&anchor!="Portal"?session.startPoint:(Vector2)point.position;bool shaft=anchor=="Shaft"&&path==CampaignFlow.Scenes[2];
            if(anchor=="Portal")target=TravelPoint.For(session).ArrivalPosition();
            if(shaft)target=session.GetComponent<Mine.MinePassage>().Landing;
            session.SetRegionArrival(shaft?"Entry":anchor,shaft?session.startPoint:target);session.player.WarpForTraversal(target);FindAnyObjectByType<SeasideCamera>()?.Snap();
            var intro=FindAnyObjectByType<Forest.ForestIntroCamera>();intro?.Cancel(true);
            while(Time.realtimeSinceStartup-blackStarted<Mathf.Max(0,GameSystemSettings.Current.regionBlackDuration))yield return null;
            LastBlackDuration=Time.realtimeSinceStartup-blackStarted;
            var arrival=session.player.GetComponent<RegionArrival>()??session.player.gameObject.AddComponent<RegionArrival>();
            if(directArrival)
            {
                // Reveal the player already at the village start; no viewport-edge movement.
                session.player.WarpForTraversal(target);FindAnyObjectByType<SeasideCamera>()?.Snap();
                yield return Fade(1,0);session.player.frozen=false;
            }
            else
            {
                var walk=StartCoroutine(shaft?arrival.Drop(session,target,session.GetComponent<Mine.MinePassage>().dropHeight):arrival.Walk(session,target,direction));
                yield return Fade(1,0);yield return walk;
            }
            session.Travelling=false;CampaignFlow.IsLoading=false;CampaignFlow.SaveCheckpoint(session);
            GameBindings.BlockFrame();
            Active=false;Alpha=0;Destroy(gameObject);
        }
        void Fail()
        {
            var session=FindAnyObjectByType<SeasideSession>();if(session!=null){session.Travelling=false;session.player.frozen=false;}
            Debug.LogError("Region transition failed: missing arrival/session for "+path);Active=false;Alpha=0;CampaignFlow.IsLoading=false;Destroy(gameObject);
        }
        void OnGUI()
        {
            if(Alpha<=0)return;int depth=GUI.depth;GUI.depth=-10000;var c=GUI.color;GUI.color=new Color(0,0,0,Alpha);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=c;GUI.depth=depth;
        }
        void OnDestroy(){Active=false;Alpha=0;}
    }
}
