using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Seaside;

public static class SeasideFeelUpgrade
{
    public static void ApplyAndBuild()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        var player=UnityEngine.Object.FindFirstObjectByType<SeasideMotor>();
        var settings=player.settings;
        settings.ascentGravity=60;settings.fallGravityStart=35;settings.fallGravityEnd=85;
        settings.fallRampTime=.25f;settings.terminalFallSpeed=32;settings.dashHoverDuration=.03f;
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        Debug.Log("FEEL_UPGRADE preserves heights="+settings.shortJumpHeight+","+settings.longJumpHeight+" dash="+settings.dashDistance);
        // No scene regeneration: user-authored map and checkpoint edits are kept intact.
        SeasideTests.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        Directory.CreateDirectory(SeasideBuilder.Root+"/Play");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/Seaside01.unity"},
            locationPathName=SeasideBuilder.Root+"/Play/SeasideWhitebox.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("SEASIDE_FEEL_BUILD_SUCCESS bytes="+report.summary.totalSize);
    }
}
