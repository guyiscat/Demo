using UnityEngine;
namespace Expedition
{
    public enum ProtectionMode { Cold, Heat }
    [CreateAssetMenu(menuName="Game/Thermal level settings")]
    public sealed class ThermalSettings : ScriptableObject
    {
        [Tooltip("获得核心后初始使用的防护模式。")]
        public ProtectionMode initialProtection=ProtectionMode.Cold;
        [Min(.5f),Tooltip("角色中心至攀岩点中心的有效半径（单位）。")]
        [HideInInspector] public float climbRadius=4;
        [Min(.05f),Tooltip("矩形补跳范围在角色半宽/半高之外额外延伸的距离。")]
        public float climbPadding=.5f;
        [Min(0),InspectorName("攀岩点提前按跳缓冲（秒）")] public float climbJumpBuffer=.15f;
        [Min(0),InspectorName("离开攀岩点跳跃宽限（秒）")] public float climbCoyoteTime=.15f;
        [Min(.1f),Tooltip("E交互距离（单位）。")]
        public float interactionDistance=3.5f;
        [Range(.01f,.15f),Tooltip("属性地形接触检查的身体边缘容差。")]
        public float contactPadding=.06f;
    }
}
