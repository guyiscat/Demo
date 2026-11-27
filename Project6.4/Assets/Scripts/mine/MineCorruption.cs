using UnityEngine;
namespace Mine
{
    [DefaultExecutionOrder(600)]
    public sealed class MineCorruption : MonoBehaviour
    {
        public MineLevel level;
        [InspectorName("乱码触发高度")] public float thresholdY=43;
        [Min(0),InspectorName("镜头抖动幅度")] public float shake=.16f;
        public bool Active
        {
            get
            {
                if(level==null||level.HasBitcoin)return false;
                if(level.player.motor.Body.position.y<thresholdY&&level.player.motor.Body.position.x>110)Started=true;
                return Started;
            }
        }
        public bool Started{get;private set;}
        public void ResetCorruption(){Started=false;}
        void LateUpdate()
        {
            if(!Active||level.session.InputBlocked||level.player.motor.frozen)return;
            Started=true;float t=Time.unscaledTime*43;
            transform.position+=new Vector3(Mathf.Sin(t)*shake,Mathf.Cos(t*1.37f)*shake,0);
        }
        void OnGUI()
        {
            if(!Active||level.session.Paused||level.session.Travelling)return;
            var old=GUI.color;GUI.depth=100;
            GUI.color=new Color(.05f,.9f,1,.24f);
            float t=Time.unscaledTime;
            for(int i=0;i<6;i++){float y=Mathf.Repeat(t*70+i*143,Screen.height);GUI.DrawTexture(new Rect(0,y,Screen.width,2),Texture2D.whiteTexture);}
            GUI.color=Color.white;
            GUI.Label(new Rect(Screen.width*.27f,Screen.height*.23f,Screen.width*.6f,40),"0101  ▒▒  ERROR / 数据失真  ▒▒  1010",new GUIStyle(GUI.skin.label){font=Seaside.GameTypography.SystemFont,fontSize=20});GUI.color=old;
        }
    }
}
