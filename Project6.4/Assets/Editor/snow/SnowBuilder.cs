using UnityEditor;
public static class SnowBuilder
{
    [MenuItem("Snow/Create independent whitebox (never overwrites)")]
    public static void CreateScene(){ThermalLevelBuilder.Create(true);}
    public static void Build(){ThermalLevelBuilder.Build(true);}
}
