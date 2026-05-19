using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;

public static class CodexAndroidBuild
{
    public static void Build()
    {
        BuildApk();
    }

    public static void BuildApk()
    {
        string outputPath = Environment.GetEnvironmentVariable("CODEX_ANDROID_APK");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine("Builds", "Android", "SymphonyOfRuin-codex.apk");
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
        bool previousDevelopmentBuild = EditorUserBuildSettings.development;
        int previousVersionCode = PlayerSettings.Android.bundleVersionCode;
        string previousBundleVersion = PlayerSettings.bundleVersion;
        string previousSdkRoot = AndroidExternalToolsSettings.sdkRootPath;
        string previousNdkRoot = AndroidExternalToolsSettings.ndkRootPath;
        string previousJdkRoot = AndroidExternalToolsSettings.jdkRootPath;
        string previousGradlePath = AndroidExternalToolsSettings.gradlePath;

        try
        {
            string androidPlayerRoot = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines",
                "AndroidPlayer");

            AndroidExternalToolsSettings.sdkRootPath = Path.Combine(androidPlayerRoot, "SDK");
            AndroidExternalToolsSettings.ndkRootPath = Path.Combine(androidPlayerRoot, "NDK");
            AndroidExternalToolsSettings.jdkRootPath = Path.Combine(androidPlayerRoot, "OpenJDK");
            AndroidExternalToolsSettings.gradlePath = Path.Combine(androidPlayerRoot, "Tools", "gradle");

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
            int buildVersionCode = int.Parse(DateTime.UtcNow.ToString("MMddHHmm"));
            PlayerSettings.Android.bundleVersionCode = buildVersionCode;
            PlayerSettings.bundleVersion = $"1.0.{buildVersionCode}";
            bool developmentBuild = !string.Equals(
                Environment.GetEnvironmentVariable("CODEX_ANDROID_DEVELOPMENT"),
                "0",
                StringComparison.OrdinalIgnoreCase);
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.buildAppBundle = false;
            BuildOptions buildOptions = developmentBuild
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {report.summary.result}");
            }
        }
        finally
        {
            PlayerSettings.Android.targetArchitectures = previousArchitectures;
            EditorUserBuildSettings.development = previousDevelopmentBuild;
            PlayerSettings.Android.bundleVersionCode = previousVersionCode;
            PlayerSettings.bundleVersion = previousBundleVersion;
            AndroidExternalToolsSettings.sdkRootPath = previousSdkRoot;
            AndroidExternalToolsSettings.ndkRootPath = previousNdkRoot;
            AndroidExternalToolsSettings.jdkRootPath = previousJdkRoot;
            AndroidExternalToolsSettings.gradlePath = previousGradlePath;
        }
    }

}
