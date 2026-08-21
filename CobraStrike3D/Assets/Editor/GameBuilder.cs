using UnityEditor;
using UnityEditor.Build;
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
        PlayerSettings.productName = "Cobra Strike";
        PlayerSettings.companyName = "DavydovCapital";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.davydovcapital.cobrastrike3d");
        PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
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
