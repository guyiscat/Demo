using Seaside;
using UnityEngine;

namespace Forest
{
    // Keeps the Forest API while sharing the renderer with level one.
    [DefaultExecutionOrder(300)]
    public sealed class ForestAimIndicator : PlayerAimIndicator
    {
        public ForestPlayer player;
        protected override bool CanDisplay=>base.CanDisplay&&player!=null&&player.enabled&&!player.session.Won;
        protected override Vector2 ResolveDirection()=>player.manualInput?player.AimDirection:player.ReadAimDirection();
    }
}
