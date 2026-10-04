using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build the saved scene without regenerating it, so later student edits are preserved.
public static class QuietCornerCheckpoint
{
    public const string ScenePath = "Assets/Scenes/QuietCorner.unity";

    [MenuItem("Quiet Corner/Open checkpoint scene")]
    public static void OpenScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.delayCall += () =>
        {
            var view = EditorWindow.GetWindow<SceneView>();
            view.in2DMode = false;
            view.sceneLighting = true;
            view.drawGizmos = false;
            view.LookAt(new Vector3(0, 1.7f, 1), Quaternion.Euler(16, 0, 0), 10.5f, false, true);
            view.Focus();
            view.Repaint();
            Debug.Log("CHECKPOINT_SCENE_OPEN " + ScenePath);
        };
    }

    [MenuItem("Quiet Corner/Build WebGL checkpoint")]
    public static void BuildWebGL()
    {
        string output = Path.GetFullPath("../Checkpoint_Submission/WebGL");
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-qc-webgl-output");
        if (i >= 0 && i + 1 < args.Length) output = Path.GetFullPath(args[i + 1]);
        EditorSceneManager.OpenScene(ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.template = "PROJECT:QuietCorner";
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.WebGL.maximumMemorySize = 512;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = output,
            target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("WebGL build failed: " + report.summary.result);
        File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
        File.WriteAllText(Path.Combine(output, "build-info.json"),
            "{\"scene\":\"QuietCorner\",\"scenePath\":\"" + ScenePath +
            "\",\"unityVersion\":\"" + Application.unityVersion + "\",\"target\":\"WebGL\"}");
        Debug.Log("QUIET_CORNER_WEBGL_BUILD_SUCCEEDED " + output);
    }
}
