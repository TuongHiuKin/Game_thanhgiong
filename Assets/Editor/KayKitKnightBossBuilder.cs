using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the An general from the licensed KayKit Adventurers Knight and applies
/// the visual to every ThanhGiongEnemy marked as a boss. Gameplay remains owned
/// by ThanhGiongEnemy; this utility only creates presentation assets and wiring.
/// </summary>
public static class KayKitKnightBossBuilder
{
    private const string KayKitRoot = "Assets/Plugins/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE";
    private const string KnightPath = KayKitRoot + "/Characters/fbx/Knight.fbx";
    private const string GeneralAnimations = KayKitRoot + "/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
    private const string MovementAnimations = KayKitRoot + "/Animations/fbx/Rig_Medium/Rig_Medium_MovementBasic.fbx";
    private const string WeaponPath = KayKitRoot + "/Assets/fbx(unity)/sword_2handed_color.fbx";
    private const string ShieldPath = KayKitRoot + "/Assets/fbx(unity)/shield_spikes_color.fbx";
    private const string TexturePath = KayKitRoot + "/Textures/knight_texture.png";

    private const string OutputFolder = "Assets/Prefabs/Enemies/KayKitKnightBoss";
    private const string ControllerPath = OutputFolder + "/KayKit_Knight_Boss.controller";
    private const string MaterialPath = OutputFolder + "/KayKit_Knight_Boss_DarkIron.mat";
    private const string VisualPrefabPath = OutputFolder + "/KayKit_Knight_Boss_Visual.prefab";
    private const string BossPrefabPath = "Assets/Prefabs/Enemies/KayKit_Knight_Boss_An.prefab";

    [MenuItem("Thanh Giong/Boss/Build KayKit Knight Boss")]
    public static void BuildAndApply()
    {
        EnsureFolder(OutputFolder);
        AnimatorController controller = BuildController();
        Material material = BuildBossMaterial();
        GameObject visualPrefab = BuildVisualPrefab(controller, material);
        BuildGameplayPrefab(visualPrefab);
        int updated = ApplyToOpenScene(visualPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (updated > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"KAYKIT_BOSS_READY prefab={BossPrefabPath} sceneBossesUpdated={updated}");
    }

    private static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Stuck", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimationClip idle = FindClip(GeneralAnimations, "Idle_A");
        AnimationClip run = FindClip(MovementAnimations, "Running_A");
        AnimationClip attack = FindClip(GeneralAnimations, "Throw");
        AnimationClip hit = FindClip(GeneralAnimations, "Hit_A");
        AnimationClip dead = FindClip(GeneralAnimations, "Death_B");
        AnimationClip stuck = FindClip(GeneralAnimations, "PickUp");

        BlendTree locomotionTree;
        AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out locomotionTree);
        locomotionTree.blendParameter = "Speed";
        locomotionTree.useAutomaticThresholds = false;
        locomotionTree.AddChild(idle, 0f);
        locomotionTree.AddChild(run, 1f);
        machine.defaultState = locomotion;

        AnimatorState attackState = AddState(machine, "Heavy_Slam", attack, 1.05f, new Vector3(430, 20));
        AnimatorState hitState = AddState(machine, "Hit_Reaction", hit, 1.1f, new Vector3(430, 105));
        AnimatorState deadState = AddState(machine, "Defeated", dead, 0.9f, new Vector3(430, 190));
        AnimatorState stuckState = AddState(machine, "Blade_Stuck", stuck, 0.72f, new Vector3(170, 190));

        AddAnyTrigger(machine, attackState, "Attack", 0.06f);
        AddAnyTrigger(machine, hitState, "Hit", 0.05f);
        AddAnyTrigger(machine, deadState, "Dead", 0.06f);

        AnimatorStateTransition toStuck = machine.AddAnyStateTransition(stuckState);
        toStuck.hasExitTime = false;
        toStuck.duration = 0.08f;
        toStuck.AddCondition(AnimatorConditionMode.If, 0, "Stuck");

        AddReturn(attackState, locomotion, 0.82f, 0.09f);
        AddReturn(hitState, locomotion, 0.72f, 0.08f);
        AnimatorStateTransition stuckReturn = stuckState.AddTransition(locomotion);
        stuckReturn.hasExitTime = false;
        stuckReturn.duration = 0.12f;
        stuckReturn.AddCondition(AnimatorConditionMode.IfNot, 0, "Stuck");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Material BuildBossMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = "KayKit Knight Boss - Dark Iron" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else material.shader = shader;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        material.mainTexture = texture;
        Color iron = new Color(0.46f, 0.33f, 0.34f, 1f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", iron);
        if (material.HasProperty("_Color")) material.SetColor("_Color", iron);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.28f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildVisualPrefab(AnimatorController controller, Material material)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
        if (source == null) throw new FileNotFoundException("KayKit Knight was not imported", KnightPath);

        GameObject root = new GameObject("Visual");
        GameObject knight = (GameObject)PrefabUtility.InstantiatePrefab(source);
        knight.name = "KayKit_Knight_General";
        knight.transform.SetParent(root.transform, false);
        knight.transform.localPosition = Vector3.zero;
        knight.transform.localRotation = Quaternion.identity;
        knight.transform.localScale = Vector3.one * 1.22f;

        Animator animator = knight.GetComponent<Animator>();
        if (animator == null) animator = knight.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        foreach (SkinnedMeshRenderer renderer in knight.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        Transform rightHand = FindDeep(knight.transform, "handslot.r");
        Transform leftHand = FindDeep(knight.transform, "handslot.l");
        AttachProp(WeaponPath, "Đại Đao Tướng Ân", rightHand, new Vector3(0f, 0.02f, 0.02f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.35f);
        AttachProp(ShieldPath, "Khiên Gai Tướng Ân", leftHand, new Vector3(0f, 0f, 0.02f), Quaternion.Euler(0f, -90f, 90f), Vector3.one * 1.18f);

        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aura.name = "Boss_Ground_Aura";
        aura.transform.SetParent(root.transform, false);
        aura.transform.localPosition = new Vector3(0, 0.025f, 0);
        aura.transform.localScale = new Vector3(0.9f, 0.018f, 0.9f);
        Object.DestroyImmediate(aura.GetComponent<Collider>());
        MeshRenderer auraRenderer = aura.GetComponent<MeshRenderer>();
        Material auraMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        auraMaterial.name = "Boss Aura Runtime";
        auraMaterial.color = new Color(0.75f, 0.06f, 0.02f, 0.45f);
        auraRenderer.sharedMaterial = auraMaterial;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildGameplayPrefab(GameObject visualPrefab)
    {
        GameObject root = new GameObject("Tướng Giặc Ân - KayKit Knight Boss");
        root.AddComponent<CapsuleCollider>();
        ThanhGiongEnemy enemy = root.AddComponent<ThanhGiongEnemy>();
        enemy.isBoss = true;
        enemy.enemyName = "Tướng Giặc Ân - Thiết Kỵ Đại Đao";
        enemy.maxHealth = 480f;
        enemy.moveSpeed = 3f;
        enemy.heavyAttackRange = 4.5f;
        enemy.stuckDuration = 1.5f;
        enemy.vulnerabilityMultiplier = 2f;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
        visual.transform.SetParent(root.transform, false);
        PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static int ApplyToOpenScene(GameObject visualPrefab)
    {
        int count = 0;
        foreach (ThanhGiongEnemy enemy in Object.FindObjectsByType<ThanhGiongEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!enemy.isBoss || !enemy.gameObject.scene.IsValid()) continue;

            Transform oldVisual = enemy.transform.Find("Visual");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
            else
            {
                for (int i = enemy.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = enemy.transform.GetChild(i);
                    if (child.GetComponentInChildren<Animator>(true) != null || child.name.Contains("Visual"))
                        Object.DestroyImmediate(child.gameObject);
                }
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, enemy.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            enemy.enemyName = "Tướng Giặc Ân - Thiết Kỵ Đại Đao";
            enemy.maxHealth = 480f;
            enemy.moveSpeed = 3f;
            enemy.heavyAttackRange = 4.5f;
            enemy.stuckDuration = 1.5f;
            enemy.vulnerabilityMultiplier = 2f;
            enemy.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(enemy);
            count++;
        }
        return count;
    }

    private static AnimationClip FindClip(string path, string clipName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip clip && clip.name == clipName) return clip;
        throw new FileNotFoundException($"Animation clip {clipName} was not found", path);
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, float speed, Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);
        state.motion = motion;
        state.speed = speed;
        return state;
    }

    private static void AddAnyTrigger(AnimatorStateMachine machine, AnimatorState target, string parameter, float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(target);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0, parameter);
    }

    private static void AddReturn(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static void AttachProp(string path, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null || parent == null) return;
        GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(source);
        prop.name = name;
        prop.transform.SetParent(parent, false);
        prop.transform.localPosition = position;
        prop.transform.localRotation = rotation;
        prop.transform.localScale = scale;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
