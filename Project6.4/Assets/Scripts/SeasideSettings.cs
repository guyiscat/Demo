using UnityEngine;
namespace Seaside
{
    [CreateAssetMenu(menuName="Seaside/Movement settings")]
    public sealed class SeasideSettings:ScriptableObject
    {
        public float moveSpeed=10f,groundAcceleration=100f,airAcceleration=80f;
        [Header("One body = 2 x 2 map cells")]
        public float bodySide=2f;
        [Header("Map pixels: tap 4 / hold 8 / dash 8 (1 pixel = 1 world cell)")]
        public float shortJumpHeight=4f,longJumpHeight=8f;
        public float ascentGravity=60f,fallGravityStart=35f,fallGravityEnd=85f,fallRampTime=.25f,terminalFallSpeed=32f;
        public float dashDistance=8f,dashSpeed=25f,dashHoverDuration=.03f;
        public float coyoteTime=.14f,jumpBuffer=.12f,wallKickSpeed=8f,wallKickLock=.12f;
        public float wallCoyoteTime=.10f;
    }
}
