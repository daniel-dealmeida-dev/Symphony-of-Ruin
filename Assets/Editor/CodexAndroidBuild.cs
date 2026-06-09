using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CodexAndroidBuild
{
    public static void Build()
    {
        BuildApk();
    }

    public static void BuildApk()
    {
        string outputPath = Path.Combine(
            "Builds",
            "Android",
            "SymphonyOfRuin.apk"
        );

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException(
                "Nenhuma cena habilitada foi encontrada em Build Settings."
            );
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("APK gerado com sucesso!");
        }
        else
        {
            UnityEngine.Debug.LogError("Falha ao gerar APK.");
        }
    }
}