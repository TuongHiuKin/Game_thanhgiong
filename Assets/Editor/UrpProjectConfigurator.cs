using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering;

public static class UrpProjectConfigurator
{
    private const string PcAssetPath = "Assets/Settings/PC_RPAsset.asset";
    private const string MobileAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

    [InitializeOnLoadMethod]
    private static void ConfigureWhenReady()
    {
        EditorApplication.update -= TryConfigure;
        EditorApplication.update += TryConfigure;
    }

    private static void TryConfigure()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= TryConfigure;
        Configure();
    }

    [MenuItem("Tools/Thanh Giong/Configure URP Project")]
    public static void Configure()
    {
        UniversalRenderPipelineAsset pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcAssetPath);
        UniversalRenderPipelineAsset mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileAssetPath);
        if (pc == null)
        {
            Debug.LogError("[URP Setup] Missing " + PcAssetPath);
            return;
        }

        GraphicsSettings.defaultRenderPipeline = pc;
        int originalQuality = QualitySettings.GetQualityLevel();
        string[] qualityNames = QualitySettings.names;
        for (int i = 0; i < qualityNames.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = i <= 1 && mobile != null ? mobile : pc;
        }
        QualitySettings.SetQualityLevel(originalQuality, true);
        QualitySettings.renderPipeline = originalQuality <= 1 && mobile != null ? mobile : pc;

        EditorUtility.SetDirty(pc);
        if (mobile != null) EditorUtility.SetDirty(mobile);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        Debug.Log("[URP Setup] Active pipeline: " + GraphicsSettings.currentRenderPipeline.name +
                  " | quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                  " | BRG variants: " + EditorGraphicsSettings.batchRendererGroupShaderStrippingMode);
    }
}
