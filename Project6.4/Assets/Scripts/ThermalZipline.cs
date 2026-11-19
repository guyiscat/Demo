using UnityEngine;
namespace Expedition
{
    [RequireComponent(typeof(Forest.ForestZipline))]
    public sealed class ThermalZipline : MonoBehaviour
    {
        public ProtectionMode requiredProtection;
    }
}
