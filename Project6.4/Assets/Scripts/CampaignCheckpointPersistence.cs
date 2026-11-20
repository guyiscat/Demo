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
        {var d=CampaignFlow.Capture(session);return d.checkpointId+"|"+d.mineLift+"|"+d.workbench+"|"+d.forestLeft+"|"+d.forestRight;}
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
