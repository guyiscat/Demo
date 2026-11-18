using System;
using UnityEngine;

namespace Seaside
{
    public static class SystemRuntimeChecks
    {
        public static void Run(SeasideSession session,Action<bool,string> check)
        {
            var common=GameSystemSettings.Current;var motor=session.player;
            var camera=UnityEngine.Object.FindAnyObjectByType<SeasideCamera>();
            check(common!=null&&motor.settings==common.movement,"Shared movement resource is active in "+session.gameObject.scene.name);
            check(motor.releasedSlideSpeed==common.releasedSlideSpeed&&motor.releasedSlideBlend==common.releasedSlideBlend,"Wall slide parameters come from shared settings");
            check(JsonUtility.ToJson(camera.tuning)==JsonUtility.ToJson(common.cameraTuning),"All follow camera parameters use the Forest baseline");
            check(session.latestCheckpointWins&&session.latestCheckpointWins==common.latestCheckpointWins,"Both levels use latest-contact checkpoint policy");
            check(Application.targetFrameRate==common.targetFrameRate,"Target frame rate uses shared settings");
            var hud=UnityEngine.Object.FindAnyObjectByType<GameHUD>();
            check(hud!=null&&hud.Content.controls.StartsWith(GameHUD.CommonControls),"HUD uses shared Chinese action hints");
            check(hud.Content.help.StartsWith(GameHUD.CommonHelp),"Both help panels include the same basic controls");
            check(hud.Content.status.StartsWith(GameHUD.DashStatus(motor)),"Both HUDs use identical dash readiness wording");
            var arrows=motor.GetComponents<PlayerAimIndicator>();
            check(arrows.Length==1&&arrows[0].Arrow!=null,"Exactly one shared arrow renderer is initialized");
            check(common.aimArrowLength>0&&common.aimArrowPadding>=0,"Both arrows use valid shared dimensions");
        }
    }
}
