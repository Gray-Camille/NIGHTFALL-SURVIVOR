using UnityEditor;
using System.IO;
using System.Linq;

public class BuildScript
{
    public static void BuildWindows()
    {
        // Ensure MainScene is in build settings
        string scenePath = "Assets/Scenes/MainScene.unity";
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        string outDir = "E:/Unity CSharp Workplace/demo/打包";
        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        string exePath = Path.Combine(outDir, "PixelSurvivor.exe");

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = new[] { scenePath };
        options.locationPathName = exePath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("BUILD SUCCESS: " + exePath);
        }
        else
        {
            UnityEngine.Debug.LogError("BUILD FAILED: " + report.summary.totalErrors + " errors");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                    UnityEngine.Debug.Log(msg.content);
            }
        }
    }
}