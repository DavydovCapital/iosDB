using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GameBuilder
{
    [MenuItem("Cobra/Build Windows")]
    public static void BuildWindows()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/CobraStrike3D.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log("Windows build result: " + report.summary.result);
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    [MenuItem("Cobra/Build iOS")]
    public static void BuildiOS()
    {
        PlayerSettings.productName = "Cobra Strike 3D";
        PlayerSettings.companyName = "DavydovCapital";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.davydovcapital.cobrastrike3d");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        });
        Debug.Log("iOS build result: " + report.summary.result);
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
