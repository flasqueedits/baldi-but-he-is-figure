#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds a headless Linux server for Render.com. Requires the Unity
/// "Linux Build Support (Mono/IL2CPP)" module installed via Unity Hub.
/// </summary>
public static class DedicatedServerBuilder
{
    private const string OutputPath = "Builds/LinuxServer/FigureServer.x86_64";

    [MenuItem("Tools/Figure Multiplayer/Build Linux Dedicated Server")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[ServerBuild] Exit Play mode first.");
            return;
        }

        StandaloneBuildSubtarget previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scene/School.unity" },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log("[ServerBuild] Linux server built at " + OutputPath + " (" + summary.totalSize + " bytes)");
        else
            Debug.LogError("[ServerBuild] Build failed: " + summary.result);
    }
}
#endif
