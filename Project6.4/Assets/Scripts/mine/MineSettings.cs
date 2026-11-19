using UnityEngine;

namespace Mine
{
    [CreateAssetMenu(menuName="Mine/Level settings")]
    public sealed class MineSettings : ScriptableObject
    {
        [Min(.1f),InspectorName("电梯移动速度")] public float liftSpeed=8;
        [Min(0),InspectorName("电梯到站停留秒数")] public float liftWait=2;
        [Min(0),InspectorName("交互距离")] public float interactionDistance=3.5f;
    }
}
