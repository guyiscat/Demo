using UnityEngine;

namespace Seaside
{
    [CreateAssetMenu(menuName="Game/Shared system settings")]
    public sealed class GameSystemSettings : ScriptableObject
    {
        static GameSystemSettings current;
        public static GameSystemSettings Current
        {
            get
            {
                if(current==null)current=Resources.Load<GameSystemSettings>("GameSystems");
                return current;
            }
        }
        [Header("两关共用动作参数")]
        public SeasideSettings movement;
        [Min(0),InspectorName("松墙下滑速度")] public float releasedSlideSpeed=3.5f;
        [Min(.01f),InspectorName("松墙下滑平滑时间")] public float releasedSlideBlend=.28f;
        [Header("两关共用镜头（以第二关为基准）")]
        public CameraTuning cameraTuning=new CameraTuning{followSize=18};
        [Header("两关共用会话")]
        [Min(1),InspectorName("目标帧率")] public int targetFrameRate=120;
        [Min(0),InspectorName("死亡复活等待秒数")] public float respawnDelay=.2f;
        [InspectorName("使用最后接触的复活点")] public bool latestCheckpointWins=true;
        [InspectorName("默认显示路线")] public bool showRoute=true;
        [Header("两关共用方向箭头")]
        [Min(0),InspectorName("箭头距身体边缘距离")] public float aimArrowPadding=.35f;
        [Min(.1f),InspectorName("方向箭头长度")] public float aimArrowLength=.8f;
        public CameraTuning CopyCameraTuning()=>JsonUtility.FromJson<CameraTuning>(JsonUtility.ToJson(cameraTuning));
    }
}
