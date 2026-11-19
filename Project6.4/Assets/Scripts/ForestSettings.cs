using UnityEngine;

namespace Forest
{
    [CreateAssetMenu(menuName="Forest/Whitebox settings")]
    public sealed class ForestSettings : ScriptableObject
    {
        [Tooltip("Workbook L6:M7 to V6:W7: 10 cells center to center; 1 cell = 1 unit.")]
        public float teleportDistance=10;
        public float beaconSpeed=45,beaconRadius=.12f;
        [InspectorName("允许传送打断冲刺")] public bool teleportInterruptsDash=true;
        [Min(0),InspectorName("滑索滑动速度")] public float ziplineSpeed=16;
        [Min(0),InspectorName("滑索抓取距离")] public float ziplineCatchDistance=2.1f;
        [Min(0),InspectorName("滑索身体接触容差")] public float ziplineContactPadding=.55f;
        [Min(0),InspectorName("离索土狼时间（秒）")] public float ziplineCoyoteTime=.18f;
        [Min(0),InspectorName("传送后滞空时间（秒）")] public float teleportHoverDuration=1;
        [Header("开场全图展示")]
        [Min(0),InspectorName("向前行走触发距离")] public float introWalkDistance=3;
        [Min(.01f),InspectorName("全图镜头缩放速度（每秒进度）")] public float introZoomSpeed=.5f;
        [Min(0),InspectorName("全图镜头停留时间（秒）")] public float introHoldDuration=3;
        public float wheelDegreesPerSecond=12;
        [Header("Wheel platform traction (world units)")]
        [Min(0),InspectorName("平台水平带动速度上限")] public float wheelCarryMaxSpeed=3;
        [Min(0),InspectorName("平台水平摩擦加速度")] public float wheelCarryAcceleration=20;
    }
}
