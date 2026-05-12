#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class BuildScripts
{
    private const string ARCADE_DEFINE = "ARCADE_BUILD";
    private const string WEB_DEFINE = "WEB_BUILD";

    // Targets we care about: Standalone (arcade build) + WebGL (online build).
    // Setting the define on BOTH means switching active platform doesn't lose it.
    private static readonly NamedBuildTarget[] AllTargets = new[]
    {
        NamedBuildTarget.Standalone,
        NamedBuildTarget.WebGL,
    };

    [MenuItem("Momentum/Build Mode/Set ARCADE_BUILD")]
    public static void SetArcade()
    {
        SetDefinesOnAllTargets(ARCADE_DEFINE);
    }

    [MenuItem("Momentum/Build Mode/Set WEB_BUILD")]
    public static void SetWeb()
    {
        SetDefinesOnAllTargets(WEB_DEFINE);
    }

    [MenuItem("Momentum/Build Mode/Show Current (active target)")]
    public static void ShowCurrent()
    {
        var msg = "";
        foreach (var t in AllTargets)
        {
            msg += $"{t.TargetName}: '{PlayerSettings.GetScriptingDefineSymbols(t)}'\n";
        }
        msg += $"\nActive build target: {EditorUserBuildSettings.activeBuildTarget}";
        EditorUtility.DisplayDialog("Current Defines", msg, "OK");
    }

    private static void SetDefinesOnAllTargets(string symbol)
    {
        foreach (var t in AllTargets)
        {
            PlayerSettings.SetScriptingDefineSymbols(t, symbol);
            Debug.Log($"[BuildScripts] {t.TargetName}: defines set to '{symbol}'");
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
