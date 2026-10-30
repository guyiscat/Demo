using System;
using UnityEngine;
namespace Seaside
{
    public static class AfterimageChecks
    {
        public static void Run(SeasideMotor player,Action<bool,string> check)
        {
            var fx=player.GetComponent<DashAfterimage>();
            check(fx!=null,"Player has pooled dash afterimage component");if(fx==null)return;
            fx.Clear();int before=fx.SpawnCount;
            for(int i=0;i<32;i++)fx.VisualTick(.01f,true,new Vector3(50+i*.25f,25,0));
            check(fx.SpawnCount>before+8&&fx.ActiveCount>0,"Dash emits visible spaced afterimages");
            check(fx.ActiveCount<=12,"Afterimage pool bounded to twelve sprites");
            int count=fx.SpawnCount;
            for(int i=0;i<40;i++)fx.VisualTick(.01f,false,new Vector3(58,25,0));
            check(fx.ActiveCount==0&&fx.SpawnCount==count,"Afterimages fade completely without post-dash emission");
            fx.VisualTick(.01f,true,new Vector3(50,25,0));player.ResetAt(player.Body.position);
            check(fx.ActiveCount==0,"Respawn/reset clears afterimages immediately");
        }
    }
}
