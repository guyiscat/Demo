using System;
using System.Linq;
using Seaside;
using UnityEditor;
public static class CampaignBuild
{
    public static string[] ScenesFrom(string entry)=>new[]{entry}.Concat(new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes).Where(p=>p!=entry)).ToArray();
    public static void ValidateSceneList()
    {
        var expected=new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes);
        if(!EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).SequenceEqual(expected))throw new Exception("Build scene order must be menu, seaside, forest, mine, snow, lighthouse.");
    }
}
