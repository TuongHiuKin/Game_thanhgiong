using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThanhGiongCombatEffectsInstaller
{
    private const string Key = "ThanhGiong.CombatEffects.2026-10-02-v2-smooth-animation";

    [InitializeOnLoadMethod]
    private static void Queue()
    {
        if (EditorPrefs.GetBool(Key, false)) return;
        EditorApplication.delayCall += InstallWhenReady;
    }

    private static void InstallWhenReady()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += InstallWhenReady; return; }
        EditorPrefs.SetBool(Key, true);
        Install();
    }

    [MenuItem("Tools/Thanh Giong/Combat/Install Boss Window, Ribbon and Golden Dissolve")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveOpenScenes()) return;
        string activePath = SceneManager.GetActiveScene().path;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = ConfigureHierarchy(root);
            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;
            foreach (GameObject root in scene.GetRootGameObjects()) dirty |= ConfigureHierarchy(root);
            if (dirty) EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(activePath)) EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[Thanh Giong] Installed 1.5s boss vulnerability, speed ribbons and golden dissolve across enemy/player assets.");
    }

    private static bool ConfigureHierarchy(GameObject root)
    {
        bool dirty = false;
        foreach (ThanhGiongEnemy enemy in root.GetComponentsInChildren<ThanhGiongEnemy>(true))
        {
            if (enemy.isBoss)
            {
                enemy.stuckDuration = 1.5f;
                enemy.vulnerabilityMultiplier = 2f;
                EditorUtility.SetDirty(enemy);
                dirty = true;
            }
            ThanhGiongGoldenDissolve dissolve = enemy.GetComponent<ThanhGiongGoldenDissolve>();
            if (dissolve == null) enemy.gameObject.AddComponent<ThanhGiongGoldenDissolve>();
            ThanhGiongEnemyAnimationDriver driver = enemy.GetComponent<ThanhGiongEnemyAnimationDriver>();
            if (driver == null) driver = enemy.gameObject.AddComponent<ThanhGiongEnemyAnimationDriver>();
            driver.speedBlendTime = .2f;
            driver.stateBlendTime = .1f;
            enemy.locomotionSmoothTime = .16f;
            EditorUtility.SetDirty(enemy);
            EditorUtility.SetDirty(driver);
            dirty = true;
        }

        foreach (MountedHorseController horse in root.GetComponentsInChildren<MountedHorseController>(true))
        {
            ThanhGiongSpeedRibbon ribbon = horse.GetComponent<ThanhGiongSpeedRibbon>();
            if (ribbon == null) ribbon = horse.gameObject.AddComponent<ThanhGiongSpeedRibbon>();
            ribbon.motionSource = horse.transform;
            ribbon.ribbonAnchor = horse.visual != null ? horse.visual : horse.transform;
            EditorUtility.SetDirty(ribbon);

            ThanhGiongRibbonCape cape = horse.GetComponent<ThanhGiongRibbonCape>();
            if (cape == null) cape = horse.gameObject.AddComponent<ThanhGiongRibbonCape>();
            cape.anchorTransform = horse.visual != null ? horse.visual : horse.transform;
            EditorUtility.SetDirty(cape);

            dirty = true;
        }
        return dirty;
    }
}
