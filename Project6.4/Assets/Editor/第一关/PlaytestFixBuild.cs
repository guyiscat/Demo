using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Seaside;
public static class PlaytestFixBuild
{
    public static string Root => SeasidePaths.ReleaseRoot;
    public static void Build()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        // Build the saved scene as authored. Do not rewrite gameplay settings or resave
        // the unrelated original scene merely to normalize Unity's serialized format.
        AssetDatabase.SaveAssets();
        SeasideTests.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        PlaytestRegression.Run();
        if(File.Exists(SeasideArtSetup.PreviewScene))SeasideHierarchyRefactor.ValidateScenes();
        if(File.Exists(SeasideArtSetup.PreviewScene))SeasideArtSetup.CheckPreview();
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        Directory.CreateDirectory(Root+"/Play64");
        var scenes=File.Exists(SeasideArtSetup.PreviewScene)?new[]{"Assets/Scenes/Seaside01.unity",SeasideArtSetup.PreviewScene}:new[]{"Assets/Scenes/Seaside01.unity"};
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Root+"/Play64/SeasideWhitebox.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        File.Copy(Path.Combine(Application.dataPath,"../Tools/StartWithLogs.ps1"),Root+"/Play64/StartWithLogs.ps1",true);
        Debug.Log("PLAYTEST_FIX_BUILD_SUCCESS bytes="+report.summary.totalSize);
    }
}

