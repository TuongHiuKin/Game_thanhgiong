using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds a reusable KayKit Barbarian enemy and deploys a deterministic army.</summary>
public static class BarbarianArmyBuilder
{
    private const string KayKitRoot = "Assets/Plugins/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE";
    private const string ModelPath = KayKitRoot + "/Characters/fbx/Barbarian.fbx";
    private const string AxePath = KayKitRoot + "/Assets/fbx(unity)/axe_1handed.fbx";
    private const string ShieldPath = KayKitRoot + "/Assets/fbx(unity)/shield_round_barbarian.fbx";
    private const string ControllerPath = "Assets/Animations/ThanhGiongKnight.controller";
    private const string PrefabFolder = "Assets/Prefabs/Enemies";
    private const string PrefabPath = PrefabFolder + "/KayKit_Barbarian_Soldier.prefab";
    private const string TargetScene = "Assets/Scenes/ThanhGiongWorld/PhaoDaiNgamQuanAn.unity";
    private const string ArmyRootName = "DOI QUAN BARBARIAN";
    private const string AutoBuildKey = "ThanhGiong.BarbarianArmy.2026-10-02-v1";

    [InitializeOnLoadMethod]
    private static void BuildRequestedArmyOnce()
    {
        if (EditorPrefs.GetBool(AutoBuildKey, false)) return;
        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void TryAutoBuild()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuild;
            return;
        }

        if (SceneManager.GetActiveScene().path != TargetScene) return;
        EditorPrefs.SetBool(AutoBuildKey, true);
        BuildAndDeploy();
    }

    [MenuItem("Tools/Thanh Giong/Enemies/Build and Deploy Barbarian Army")]
    public static void BuildAndDeploy()
    {
        GameObject prefab = BuildPrefab();
        DeployArmy(prefab, SceneManager.GetActiveScene());
    }

    private static GameObject BuildPrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (source == null) throw new InvalidOperationException("Missing KayKit Barbarian model: " + ModelPath);

        Directory.CreateDirectory(PrefabFolder);
        GameObject root = new GameObject("KayKit Barbarian Soldier");
        root.layer = LayerMask.NameToLayer("Default");

        GameObject visual = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (visual == null) throw new InvalidOperationException("Could not instantiate Barbarian model.");
        visual.name = "Barbarian Visual";
        visual.transform.SetParent(root.transform, false);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.AddComponent<Animator>();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        Transform rightHand = FindBone(visual.transform, "hand.r", "righthand", "hand_r", "right_hand");
        Transform leftHand = FindBone(visual.transform, "hand.l", "lefthand", "hand_l", "left_hand");
        AttachProp(AxePath, rightHand != null ? rightHand : visual.transform, "Barbarian Axe");
        AttachProp(ShieldPath, leftHand != null ? leftHand : visual.transform, "Barbarian Shield");

        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = .42f;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 85f;
        body.linearDamping = .8f;
        body.angularDamping = 2.5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        ThanhGiongEnemy enemy = root.AddComponent<ThanhGiongEnemy>();
        enemy.enemyName = "Chiến Binh Barbarian";
        enemy.maxHealth = 75f;
        enemy.moveSpeed = 2.7f;
        enemy.engageDistance = 28f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void DeployArmy(GameObject prefab, Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("No loaded scene for Barbarian army.");

        GameObject oldRoot = scene.GetRootGameObjects().FirstOrDefault(item => item.name == ArmyRootName);
        if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

        GameObject army = new GameObject(ArmyRootName);
        SceneManager.MoveGameObjectToScene(army, scene);

        Vector3 center = FindDeploymentCenter(scene);
        const int rows = 4;
        const int columns = 6;
        const float xSpacing = 2.2f;
        const float zSpacing = 2.4f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                GameObject soldier = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (soldier == null) continue;
                soldier.name = $"Barbarian_{row + 1:00}_{column + 1:00}";
                soldier.transform.SetParent(army.transform, true);
                float x = (column - (columns - 1) * .5f) * xSpacing;
                float z = row * zSpacing;
                soldier.transform.position = GroundPoint(center + new Vector3(x, 0f, z));
                soldier.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        // A larger commander anchors the rear rank without changing the reusable prefab.
        GameObject commander = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (commander != null)
        {
            commander.name = "Barbarian Commander";
            commander.transform.SetParent(army.transform, true);
            commander.transform.position = GroundPoint(center + new Vector3(0f, 0f, rows * zSpacing + 1.8f));
            commander.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            commander.transform.localScale = Vector3.one * 1.35f;
            ThanhGiongEnemy commanderEnemy = commander.GetComponent<ThanhGiongEnemy>();
            commanderEnemy.enemyName = "Thủ Lĩnh Barbarian";
            commanderEnemy.maxHealth = 180f;
            commanderEnemy.moveSpeed = 2.9f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = army;
        Debug.Log($"[Thanh Giong] Deployed 24 Barbarian soldiers and 1 commander in {scene.name}.");
    }

    private static Vector3 FindDeploymentCenter(Scene scene)
    {
        Transform player = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.CompareTag("Player") || item.name.Contains("Player Spawn"));
        return player != null ? player.position + new Vector3(0f, 0f, 18f) : new Vector3(0f, 0f, 12f);
    }

    private static Vector3 GroundPoint(Vector3 point)
    {
        if (Physics.Raycast(point + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 180f, ~0, QueryTriggerInteraction.Ignore))
            point.y = hit.point.y + .03f;
        return point;
    }

    private static void AttachProp(string assetPath, Transform parent, string name)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null) return;
        GameObject prop = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (prop == null) return;
        prop.name = name;
        prop.transform.SetParent(parent, false);
        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;
    }

    private static Transform FindBone(Transform root, params string[] candidates)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            string normalized = item.name.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "").ToLowerInvariant();
            if (candidates.Any(candidate => normalized.Contains(candidate.Replace("_", "").Replace(".", "")))) return item;
        }
        return null;
    }
}
