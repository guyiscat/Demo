using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Seaside;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SystemContractChecks
{
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run scene checks in an isolated batch project.");
        var log=new List<string>();
        Action<bool,string> check=(ok,message)=>{log.Add((ok?"PASS ":"FAIL ")+message);};
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var settings=GameSystemSettings.Current;
        check(settings!=null&&settings.movement==AssetDatabase.LoadAssetAtPath<SeasideSettings>("Assets/Settings/Movement.asset"),"Shared system asset references canonical Movement.asset");
        check(settings.cameraTuning.followSize==18,"Common camera uses Forest followSize 18");
        check(!EditorBuildSettings.scenes.Any(s=>s.path==ForestBuilder.ScenePath),"Forest remains independent from first-level progression");
        foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);EditorSceneManager.OpenScene(path);
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script after moves: "+path+" / "+t.name);
            var session=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();
            if(session==null)continue;
            var motor=session.player;var camera=UnityEngine.Object.FindAnyObjectByType<SeasideCamera>();
            session.ApplySystemSettings();motor.ApplySystemSettings();camera.ApplySystemSettings();
            check(motor.settings==settings.movement&&session.latestCheckpointWins&&session.ShowRoute==settings.showRoute,"Shared gameplay defaults resolve in "+path);
            check(JsonUtility.ToJson(camera.tuning)==JsonUtility.ToJson(settings.cameraTuning),"Shared full camera tuning resolves in "+path);
        }
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        string output=Path.Combine(SeasidePaths.ReleaseRoot,"Docs/system-editor-checks.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllLines(output,log);
        if(log.Any(line=>line.StartsWith("FAIL")))throw new Exception("System contract checks failed; see "+output);
        Debug.Log("SYSTEM_EDITOR_CHECKS_PASS assertions="+log.Count);
    }
}
