using UnityEditor;
public static class LighthouseBuilder
{
    [MenuItem("Lighthouse/Create independent whitebox (never overwrites)")]
    public static void CreateScene(){ThermalLevelBuilder.Create(false);}
    public static void Build(){ThermalLevelBuilder.Build(false);}
}
