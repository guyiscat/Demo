using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using Seaside;
public static class WallJumpEditorChecks
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Seaside01.unity");
        var player=UnityEngine.Object.FindAnyObjectByType<SeasideMotor>();player.externalInput=true;
        var old=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
        var lines=new List<string>();
        try{WallJumpRegression.Run(player,(ok,label)=>lines.Add((ok?"PASS ":"FAIL ")+label));}
        finally
        {
            Physics2D.simulationMode=old;
            string tag=Environment.GetCommandLineArgs().Contains("-baseline")?"before":"after";
            string folder=PlaytestFixBuild.Root+"/Docs/Logs/Work/20261020-wall-jump";
            Directory.CreateDirectory(folder);File.WriteAllLines(folder+"/wall-jump-"+tag+".txt",lines);
        }
        Debug.Log("WALL_SWITCH_CHECKS pass="+lines.Count(x=>x.StartsWith("PASS"))+" fail="+lines.Count(x=>x.StartsWith("FAIL")));
        if(lines.Any(x=>x.StartsWith("FAIL")))throw new Exception("Wall-switch regression failed");
    }
}
