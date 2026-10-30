using System.IO;
using UnityEngine;

// The checkout can be moved or cloned. Never write to the original author's D: drive.
public static class SeasidePaths
{
    public static string ReleaseRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
}
