using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasyUIInstaller
{
    private const string UiRoot = "Assets/UI/kenney_fantasy-ui-borders/PNG/";
    private const string PanelPath = UiRoot + "Default/Transparent center/panel-transparent-center-012.png";
    private const string BannerPath = UiRoot + "Double/Transparent center/panel-transparent-center-020.png";
    private const string DividerPath = UiRoot + "Double/Divider Fade/divider-fade-002.png";
    private const string InstallVersion = "2026.10.02-fantasy-ui-v1";

    [InitializeOnLoadMethod]
    private static void InstallWhenReady()
    {
        EditorApplication.delayCall += TryInstall;
    }

    private static void TryInstall()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryInstall;
            return;
        }

        string key = "ThanhGiong.FantasyUI." + Application.dataPath;
        if (EditorPrefs.GetString(key) == InstallVersion) return;
        try
        {
            InstallInAllScenes();
            EditorPrefs.SetString(key, InstallVersion);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Thanh Giong/Install Kenney Fantasy UI")]
    public static void InstallInAllScenes()
    {
        ConfigureTexture(PanelPath);
        ConfigureTexture(BannerPath);
        ConfigureTexture(DividerPath);
        AssetDatabase.Refresh();

        Scene current = SceneManager.GetActiveScene();
        string originalPath = current.path;
        EditorSceneManager.SaveOpenScenes();

        string[] scenePaths =
        {
            "Assets/Scenes/AlbionForestMap.unity",
            "Assets/Scenes/ThanhGiongWorld/LangGiongTienTuyen.unity",
            "Assets/Scenes/ThanhGiongWorld/KinhThanhRenThep.unity",
            "Assets/Scenes/ThanhGiongWorld/PhaoDaiNgamQuanAn.unity",
            "Assets/Scenes/ThanhGiongWorld/ThungLungVuotSong.unity",
            "Assets/Scenes/ThanhGiongWorld/TranTuyenNuiSoc.unity",
            "Assets/Scenes/ThanhGiongWorld/DinhSocHoaThanh.unity"
        };

        foreach (string path in scenePaths)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ThanhGiongCampaignHUD hud in root.GetComponentsInChildren<ThanhGiongCampaignHUD>(true)) Apply(hud);
                foreach (KayKitMapGuide guide in root.GetComponentsInChildren<KayKitMapGuide>(true)) Apply(guide);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<SceneAsset>(originalPath) != null)
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[Thanh Giong UI] Installed Kenney fantasy frames in all campaign scenes.");
    }

    public static void Apply(ThanhGiongCampaignHUD hud)
    {
        if (hud == null) return;
        hud.panelFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(PanelPath);
        hud.bannerFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(BannerPath);
        hud.dividerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DividerPath);
        EditorUtility.SetDirty(hud);
    }

    public static void Apply(KayKitMapGuide guide)
    {
        if (guide == null) return;
        guide.panelFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(PanelPath);
        guide.dividerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DividerPath);
        EditorUtility.SetDirty(guide);
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
    }
}
