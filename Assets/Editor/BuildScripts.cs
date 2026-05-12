#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class BuildScripts
{
    private const string ARCADE_DEFINE = "ARCADE_BUILD";
    private const string WEB_DEFINE = "WEB_BUILD";

    [MenuItem("Momentum/Build Mode/Set ARCADE_BUILD")]
    public static void SetArcade()
    {
        SetDefines(ARCADE_DEFINE);
    }

    [MenuItem("Momentum/Build Mode/Set WEB_BUILD")]
    public static void SetWeb()
    {
        SetDefines(WEB_DEFINE);
    }

    [MenuItem("Momentum/Build Mode/Show Current")]
    public static void ShowCurrent()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        var current = PlayerSettings.GetScriptingDefineSymbols(target);
        EditorUtility.DisplayDialog("Current Defines", $"Defines: {current}", "OK");
    }

    private static void SetDefines(string symbol)
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        PlayerSettings.SetScriptingDefineSymbols(target, symbol);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildScripts] Defines set to: {symbol}");
    }
}
#endif
