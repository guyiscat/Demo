using UnityEngine;
namespace Seaside
{
    [CreateAssetMenu(menuName="Game/Shared media settings")]
    public sealed class GameMediaSettings : ScriptableObject
    {
        static GameMediaSettings current;
        public static GameMediaSettings Current=>current!=null?current:current=Resources.Load<GameMediaSettings>("GameMedia");
        [Header("角色动作：全关卡共用")]
        public Sprite idle,jumpRise,jumpApex,jumpFall,dash,aimArrow;
        public Sprite jumpTransition;
        public Sprite[] run,teleport;
        [Min(1),InspectorName("行走帧率")] public float runFrameRate=10;
        [Min(1),InspectorName("传送效果帧率")] public float teleportFrameRate=16;
        [Header("区域背景音乐")]
        public AudioClip menu,seaside,forest,village,mine,snow,lighthouse;
        [Range(0,1),InspectorName("背景音乐音量")] public float musicVolume=.5f;
        [Min(.01f),InspectorName("音乐切换淡入淡出秒数")] public float musicFadeSeconds=1.25f;
        public AudioClip Track(string path)
        {
            if(path==CampaignFlow.MenuScene)return menu;
            int index=System.Array.IndexOf(CampaignFlow.Scenes,path);
            return index<0?null:new[]{seaside,forest,mine,snow,lighthouse,village}[index];
        }
    }
}
