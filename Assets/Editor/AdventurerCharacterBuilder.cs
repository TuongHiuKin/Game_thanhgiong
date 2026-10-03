using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class AdventurerCharacterBuilder
{
    private const string Root = "Assets/Plugins/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE";
    private const string CharacterPath = Root + "/Characters/fbx/Knight.fbx";
    private const string MovementPath = Root + "/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx";
    private const string GeneralPath = Root + "/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
    private const string EquipmentPath = Root + "/Assets/fbx(unity)";
    private const string OutputFolder = "Assets/Prefabs/Player";
    private const string PrefabPath = OutputFolder + "/ThanhGiongKnight.prefab";
    private const string ControllerPath = "Assets/Animations/ThanhGiongKnight.controller";
    private const string MapScenePath = "Assets/Scenes/AlbionForestMap.unity";

    // Building is intentionally manual. Automatically rebuilding here used to run while
    // model assets were still importing, which could leave a broken nested prefab and
    // prevent other editor setup tasks from completing.

    [MenuItem("Tools/Thanh Giong/Build Adventurer Player")]
    public static void BuildCharacter()
    {
        Directory.CreateDirectory(OutputFolder);
        AssetDatabase.Refresh();

        GameObject characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        if (characterModel == null) throw new InvalidOperationException($"Missing Knight model at {CharacterPath}");

        AnimatorController animatorController = BuildAnimatorController();
        GameObject player = new GameObject("Thanh Giong Knight");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        CharacterController capsule = player.AddComponent<CharacterController>();
        capsule.center = new Vector3(0, 1.05f, 0);
        capsule.height = 2.1f;
        capsule.radius = .42f;
        capsule.stepOffset = .35f;
        capsule.slopeLimit = 50f;

        GameObject visual = PrefabUtility.InstantiatePrefab(characterModel) as GameObject;
        visual.name = "Knight Visual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;

        AdventurerController movement = player.AddComponent<AdventurerController>();
        movement.animator = animator;

        Transform rightHand = FindBone(visual.transform, "hand.r", "righthand", "hand_r", "right_hand");
        Transform leftHand = FindBone(visual.transform, "hand.l", "lefthand", "hand_l", "left_hand");
        AttachEquipment("sword_1handed", rightHand ?? visual.transform, new Vector3(0, .02f, 0), Quaternion.Euler(0, 0, 0));
        AttachEquipment("shield_badge_color", leftHand ?? visual.transform, new Vector3(0, .02f, 0), Quaternion.Euler(0, 0, 0));

        GameObject selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectionRing.name = "Selection Ring";
        selectionRing.transform.SetParent(player.transform, false);
        selectionRing.transform.localPosition = new Vector3(0, .035f, 0);
        selectionRing.transform.localScale = new Vector3(.72f, .018f, .72f);
        UnityEngine.Object.DestroyImmediate(selectionRing.GetComponent<Collider>());
        selectionRing.GetComponent<Renderer>().sharedMaterial = CreateRingMaterial();

        PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
        UnityEngine.Object.DestroyImmediate(player);
        AddPlayerToMap();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log($"Created controllable adventurer prefab at {PrefabPath}");
    }

    [MenuItem("Tools/Thanh Giong/Rebuild Complete Demo")]
    public static void BuildCompleteDemo()
    {
        AlbionInspiredMapGenerator.GenerateMap();
        BuildCharacter();
    }

    private static AnimatorController BuildAnimatorController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        List<AnimationClip> movementClips = LoadClips(MovementPath);
        List<AnimationClip> generalClips = LoadClips(GeneralPath);
        AnimationClip idle = FindClip(generalClips, "idle_a", "idle") ?? movementClips.FirstOrDefault();
        AnimationClip walk = FindClip(movementClips, "walk") ?? FindClip(movementClips, "run") ?? idle;
        AnimationClip run = FindClip(movementClips, "run") ?? walk;
        AnimationClip attack = FindClip(generalClips, "interact", "throw", "use_item", "hit") ?? generalClips.FirstOrDefault() ?? idle;

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        BlendTree blendTree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        if (idle != null) blendTree.AddChild(idle, 0f);
        if (walk != null) blendTree.AddChild(walk, .55f);
        if (run != null) blendTree.AddChild(run, 1f);

        AnimatorState locomotion = machine.AddState("Locomotion");
        locomotion.motion = blendTree;
        machine.defaultState = locomotion;

        if (attack != null)
        {
            AnimatorState attackState = machine.AddState("Attack");
            attackState.motion = attack;
            AnimatorStateTransition toAttack = machine.AddAnyStateTransition(attackState);
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack.hasExitTime = false;
            toAttack.duration = .08f;
            AnimatorStateTransition toMove = attackState.AddTransition(locomotion);
            toMove.hasExitTime = true;
            toMove.exitTime = .88f;
            toMove.duration = .1f;
        }

        Debug.Log("Movement clips: " + string.Join(", ", movementClips.Select(c => c.name)));
        Debug.Log("General clips: " + string.Join(", ", generalClips.Select(c => c.name)));
        return controller;
    }

    private static List<AnimationClip> LoadClips(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static AnimationClip FindClip(IEnumerable<AnimationClip> clips, params string[] keywords)
    {
        return clips.FirstOrDefault(clip => keywords.Any(keyword => clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static Transform FindBone(Transform root, params string[] candidates)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string normalized = child.name.Replace(" ", string.Empty).ToLowerInvariant();
            if (candidates.Any(c => normalized.Contains(c.Replace(" ", string.Empty).ToLowerInvariant()))) return child;
        }
        return null;
    }

    private static void AttachEquipment(string assetName, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{EquipmentPath}/{assetName}.fbx");
        if (asset == null) return;
        GameObject item = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        item.name = assetName;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localRotation = localRotation;
        item.transform.localScale = Vector3.one;
    }

    private static Material CreateRingMaterial()
    {
        string path = OutputFolder + "/SelectionRing.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = new Color(.18f, .85f, .95f, .85f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(.03f, .35f, .5f));
        }
        return material;
    }

    private static void AddPlayerToMap()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MapScenePath) == null) return;
        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            AdventurerController existing = root.GetComponentInChildren<AdventurerController>();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        Transform marker = FindSceneObject("Player Spawn");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        player.transform.position = marker != null ? marker.position + Vector3.up * .1f : new Vector3(-39, .1f, -24);
        player.transform.rotation = Quaternion.Euler(0, 45, 0);

        Camera camera = Camera.main;
        if (camera != null)
        {
            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>() ?? camera.gameObject.AddComponent<IsometricCameraFollow>();
            follow.target = player.transform;
            camera.transform.position = player.transform.position + follow.offset;
            camera.transform.LookAt(player.transform.position + Vector3.up * follow.lookHeight);
            player.GetComponent<AdventurerController>().gameplayCamera = camera;
        }
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform FindSceneObject(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
            if (found != null) return found;
        }
        return null;
    }
}
