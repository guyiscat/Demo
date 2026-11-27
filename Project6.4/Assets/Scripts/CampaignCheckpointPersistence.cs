using System.Collections;
using UnityEngine;
namespace Seaside
{
    public sealed class CampaignCheckpointPersistence : MonoBehaviour
    {
        SeasideSession session;string lastState;float timer;
        public bool Ready{get;private set;}
        IEnumerator Start()
        {
            session=GetComponent<SeasideSession>();yield return null;
            CampaignFlow.InitializeSession(session);Ready=true;lastState=State();
        }
        string State()
        {
            var d=CampaignFlow.Capture(session);var r=CampaignFlow.Record(d,session.gameObject.scene.path);
            return r.checkpointId+"|"+r.mineLift+"|"+r.workbench+"|"+r.forestLeft+"|"+r.forestRight+"|"+r.gateOpen+"|"+r.seasideRead+"|"+d.bitcoin+"|"+d.snowContinue;
        }
        void LateUpdate()
        {
            if(!Ready||!CampaignFlow.Enabled||session.InputBlocked)return;
            timer+=Time.unscaledDeltaTime;if(timer<.25f)return;timer=0;
            string state=State();if(state!=lastState){lastState=state;CampaignFlow.SaveCheckpoint(session);}
        }
        void OnApplicationPause(bool paused){if(paused&&Ready)CampaignFlow.SaveCheckpoint(session);}
        void OnApplicationQuit(){if(Ready)CampaignFlow.SaveCheckpoint(session);}
    }
}
