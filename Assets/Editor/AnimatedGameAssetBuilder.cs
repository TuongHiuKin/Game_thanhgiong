using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnimatedGameAssetBuilder
{
    private const string OutputFolder = "Assets/Prefabs/Player";
    private const string WarriorSource = "Assets/Prefabs/Player/ThanhGiongKnight.prefab";
    private const string HorseSource = "Assets/Prefabs/Player/ThanhGiong_IronHorse.prefab";
    private const string WarriorOutput = OutputFolder + "/ThanhGiong_Warrior_Animated.prefab";
    private const string HorseOutput = OutputFolder + "/IronHorse_Animated.prefab";
    private const string ScenePath = "Assets/Scenes/AlbionForestMap.unity";

    [InitializeOnLoadMethod]
    private static void BuildWhenReady()
    {
        EditorApplication.update -= TryBuild;
        EditorApplication.update += TryBuild;
    }

    private static void TryBuild()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WarriorSource) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(HorseSource) == null) return;

        EditorApplication.update -= TryBuild;
        try
        {
            GameObject warrior = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorOutput);
            GameObject horse = AssetDatabase.LoadAssetAtPath<GameObject>(HorseOutput);
            if (warrior == null || warrior.GetComponent<GameAssetAnimationPreview>() == null ||
                horse == null || horse.GetComponent<GameAssetAnimationPreview>() == null)
                BuildAndPlace();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Thanh Giong/Build Animated Warrior + Iron Horse Assets")]
    public static void BuildAndPlace()
    {
        Directory.CreateDirectory(OutputFolder);
        CreateWarriorPrefab();
        CreateHorsePrefab();
        PlaceShowcaseInScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Thanh Giong] Created animated warrior and iron horse game assets.");
    }

    private static void CreateWarriorPrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorSource);
        GameObject root = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (root == null) throw new InvalidOperationException("Could not instantiate warrior source.");
        root.name = "Thanh Giong Warrior Animated";

        AdventurerController movement = root.GetComponent<AdventurerController>();
        if (movement != null) UnityEngine.Object.DestroyImmediate(movement);

        Animator animator = root.GetComponentInChildren<Animator>(true);
        GameAssetAnimationPreview preview = root.AddComponent<GameAssetAnimationPreview>();
        preview.assetKind = GameAssetAnimationPreview.AssetKind.Warrior;
        preview.animator = animator;
        preview.visual = animator != null ? animator.transform : root.transform;
        preview.stateDuration = 2.8f;
        preview.attackEvery = 6.4f;

        PrefabUtility.SaveAsPrefabAsset(root, WarriorOutput);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateHorsePrefab()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HorseSource);
        GameObject root = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (root == null) throw new InvalidOperationException("Could not instantiate horse source.");
        root.name = "Iron Horse Animated";

        Transform visual = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "Visual") ?? root.transform;
        GameAssetAnimationPreview preview = root.AddComponent<GameAssetAnimationPreview>();
        preview.assetKind = GameAssetAnimationPreview.AssetKind.IronHorse;
        preview.visual = visual;
        preview.stateDuration = 2.6f;
        preview.attackEvery = 6.8f;

        PrefabUtility.SaveAsPrefabAsset(root, HorseOutput);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void PlaceShowcaseInScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RemoveSceneObject(scene, "Thanh Giong Warrior Animated");
        RemoveSceneObject(scene, "Iron Horse Animated");
        RemoveSceneObject(scene, "Thanh Giong 3D");
        RemoveSceneObject(scene, "Iron Horse 3D");

        Transform marker = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == "Player Spawn");
        Vector3 spawn = marker != null ? marker.position : new Vector3(-39f, 0.2f, -24f);

        GameObject warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorOutput);
        GameObject horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorseOutput);
        GameObject warrior = PrefabUtility.InstantiatePrefab(warriorPrefab, scene) as GameObject;
        GameObject horse = PrefabUtility.InstantiatePrefab(horsePrefab, scene) as GameObject;
        if (warrior == null || horse == null) throw new InvalidOperationException("Could not place animated assets.");

        warrior.name = "Thanh Giong Warrior Animated";
        horse.name = "Iron Horse Animated";
        warrior.transform.SetPositionAndRotation(spawn + new Vector3(-3.2f, 0.05f, 4.5f), Quaternion.Euler(0f, 150f, 0f));
        horse.transform.SetPositionAndRotation(spawn + new Vector3(2.8f, 0.05f, 4.5f), Quaternion.Euler(0f, 150f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.objects = new UnityEngine.Object[] { warrior, horse };
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }

    private static void RemoveSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == objectName) UnityEngine.Object.DestroyImmediate(root);
    }
}
