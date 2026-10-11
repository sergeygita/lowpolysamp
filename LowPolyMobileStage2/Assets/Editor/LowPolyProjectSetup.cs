#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Первичный импорт применяет платформенные API-настройки один раз.
// Последующие открытия не перезаписывают изменения пользователя.
[InitializeOnLoad]
public static class LowPolyProjectSetup
{
    const string Marker = "ProjectSettings/LowPolySetupVersion.txt";
    public const string ScenePath = "Assets/Scenes/Stage1.unity";
    const string PipelinePath = "Assets/Settings/LowPolyURP.asset";

    static LowPolyProjectSetup() { EditorApplication.delayCall += FirstImport; }

    static void FirstImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += FirstImport;
            return;
        }
        if (File.Exists(Marker) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            Configure();
            if (!Application.isBatchMode && !SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    [MenuItem("LowPoly/Apply Project Settings")]
    public static void Configure()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/LowPolyRenderer.asset");
        if (!pipeline || !renderer) throw new InvalidOperationException("URP assets не импортированы. Проверьте Package Manager и Console.");
        renderer.renderingMode = RenderingMode.Forward;
        renderer.depthPrimingMode = DepthPrimingMode.Disabled;
        pipeline.supportsHDR = false;
        pipeline.msaaSampleCount = 1;
        pipeline.renderScale = 1f;
        pipeline.shadowDistance = 50f;
        pipeline.shadowCascadeCount = 1;
        pipeline.supportsCameraDepthTexture = false;
        pipeline.supportsCameraOpaqueTexture = false;
        GraphicsSettings.renderPipelineAsset = pipeline;
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.renderPipeline = pipeline;
        QualitySettings.antiAliasing = 0;
        QualitySettings.vSyncCount = 0;
        PlayerSettings.companyName = "LowPolyStudio";
        PlayerSettings.productName = "LowPolyMobile";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lowpolystudio.stage1");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Marker, "1\n");
        Debug.Log("LowPoly: проект настроен. Откройте Stage1 и нажмите Play.");
    }

    [MenuItem("LowPoly/Open Stage1")]
    public static void OpenScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("LowPoly/Build Android APK")]
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new BuildFailedException("Установите Android Build Support, SDK/NDK и OpenJDK через Unity Hub.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException("Сначала File > Build Settings > Android > Switch Platform, затем повторите LowPoly > Build Android APK.");
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/LowPolyMobile.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Android build: " + report.summary.result);
        Debug.Log("APK: " + Path.GetFullPath("Builds/Android/LowPolyMobile.apk"));
    }
}

public class LowPolyBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        if (!(GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset))
            throw new BuildFailedException("URP не назначен. LowPoly > Apply Project Settings.");
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(LowPolyProjectSetup.ScenePath))
            throw new BuildFailedException("Stage1.unity отсутствует.");
        var material = Resources.Load<Material>("LowPolyDefault");
        if (!material || !material.shader || material.shader.name != "LowPoly/FlatColor")
            throw new BuildFailedException("Проверьте Resources/LowPolyDefault.mat и шейдер FlatColor.");
    }
}
#endif
