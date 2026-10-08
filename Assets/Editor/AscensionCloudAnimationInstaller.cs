using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AscensionCloudAnimationInstaller
{
    const string ScenePath = "Assets/Scenes/ThanhGiongWorld/DinhSocHoaThanh.unity";
    const string CloudFolder = "Assets/Plugins/KayKit_Medieval_Hexagon_Pack_1.0_FREE/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/decoration/nature/";
    const string MaterialPath = "Assets/Materials/World/Ascension_Cloud_Walkable.mat";

    [MenuItem("Thanh Giong/Install Horse Cloud Trail")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open DinhSocHoaThanh before installing the horse cloud trail.");
            return;
        }

        ThanhGiongCampaignController campaign = null;
        foreach (ThanhGiongCampaignController candidate in Object.FindObjectsByType<ThanhGiongCampaignController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.scene == scene) { campaign = candidate; break; }
        if (campaign == null)
        {
            Debug.LogError("No ThanhGiongCampaignController found in DinhSocHoaThanh.");
            return;
        }

        GameObject big = AssetDatabase.LoadAssetAtPath<GameObject>(CloudFolder + "cloud_big.fbx");
        GameObject small = AssetDatabase.LoadAssetAtPath<GameObject>(CloudFolder + "cloud_small.fbx");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (big == null || small == null || material == null)
        {
            Debug.LogError("Cloud models or material are missing; the static stairway was left untouched.");
            return;
        }

        GameObject oldStairway = GameObject.Find("Ascension Cloud Stairway — Horse Steps");
        if (oldStairway != null && oldStairway.scene == scene) Object.DestroyImmediate(oldStairway);

        AscensionCloudTrail trail = campaign.GetComponent<AscensionCloudTrail>();
        if (trail == null) trail = campaign.gameObject.AddComponent<AscensionCloudTrail>();
        trail.bigCloudPrefab = big;
        trail.smallCloudPrefab = small;
        trail.cloudMaterial = material;
        EditorUtility.SetDirty(trail);

        int removedAmbient = 0;
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.gameObject.scene != scene || item.name != "Mây hóa thánh") continue;
            Object.DestroyImmediate(item.gameObject);
            removedAmbient++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HORSE_CLOUD_TRAIL_READY staticStairwayRemoved=" + (oldStairway != null) + " ambientCloudsRemoved=" + removedAmbient);
    }
}
