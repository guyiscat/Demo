using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Regression-only fixture: it is never included in a player build or the game scene list.
public static class SeasideTestScenes
{
    public const string Movement="Assets/Editor/Tests/Fixtures/SeasideMovementBaseline.unity";
    [MenuItem("Seaside/Tests/Run shared movement regression (batch only)")]
    public static void RunMovementRegression()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run scene-opening checks in an isolated batch project.");
        EditorSceneManager.OpenScene(Movement);SeasideTests.Run();
        EditorSceneManager.OpenScene(Movement);PlaytestRegression.Run();
    }
}
