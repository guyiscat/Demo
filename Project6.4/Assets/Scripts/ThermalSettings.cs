using UnityEngine;
namespace Expedition
{
    public enum ProtectionMode { Cold, Heat }
    [CreateAssetMenu(menuName="Game/Thermal level settings")]
    public sealed class ThermalSettings : ScriptableObject
    {
        [Tooltip("独立关卡已持有核心，初始使用的防护模式。")]
        public ProtectionMode initialProtection=ProtectionMode.Cold;
        [Min(.5f),Tooltip("角色中心至攀岩点中心的有效半径（单位）。")]
        public float climbRadius=4;
        [Min(.1f),Tooltip("E交互距离（单位）。")]
        public float interactionDistance=3.5f;
        [Range(.01f,.15f),Tooltip("属性地形接触检查的身体边缘容差。")]
        public float contactPadding=.06f;
    }
}
