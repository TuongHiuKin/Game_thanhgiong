using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Builds the KayKit enemy motion graphs without replacing prefab or scene objects.</summary>
public static class EnemyMotionGraphBuilder
{
    private const string Animations = "Assets/Plugins/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Animations/fbx/Rig_Medium/";
    private const string SoldierController = "Assets/Animations/ThanhGiongEnemySoldier.controller";
    private const string BossController = "Assets/Prefabs/Enemies/KayKitKnightBoss/KayKit_Knight_Boss.controller";
    private const string SoldierPrefab = "Assets/Prefabs/Enemies/KayKit_Barbarian_Soldier.prefab";

    [MenuItem("Tools/Thanh Giong/Enemies/Build Motion Graphs")]
    public static void Build()
    {
        BuildController(SoldierController, false);
        BuildController(BossController, true);
        GameObject soldier = PrefabUtility.LoadPrefabContents(SoldierPrefab);
        try
        {
            Animator animator = soldier.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Soldier prefab has no Animator");
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SoldierController);
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(soldier, SoldierPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(soldier); }
        AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_MOTION_GRAPHS_READY soldier and boss controllers built");
    }

    private static void BuildController(string path, bool boss)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (var parameter in controller.parameters.ToArray()) controller.RemoveParameter(parameter);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Windup", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Strike", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Recovery", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Stuck", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states.ToArray()) machine.RemoveState(state.state);
        foreach (var transition in machine.anyStateTransitions.ToArray()) machine.RemoveAnyStateTransition(transition);

        BlendTree tree = new BlendTree { name = boss ? "Boss Idle Walk Run" : "Soldier Idle Walk Run", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(Clip("Rig_Medium_General.fbx", "Idle_A"), 0f);
        tree.AddChild(Clip("Rig_Medium_MovementBasic.fbx", boss ? "Walking_C" : "Walking_A"), .42f);
        tree.AddChild(Clip("Rig_Medium_MovementBasic.fbx", boss ? "Running_B" : "Running_A"), 1f);

        AnimatorState locomotion = State(machine, "Idle Walk Run", tree, 1f, 220, 40);
        machine.defaultState = locomotion;
        AnimatorState windup = State(machine, "Windup", Clip("Rig_Medium_General.fbx", "Use_Item"), boss ? .85f : 1.7f, 490, -65);
        AnimatorState strike = State(machine, boss ? "Heavy Slam" : "Weapon Strike", Clip("Rig_Medium_General.fbx", "Throw"), boss ? 1.35f : 2.4f, 740, -65);
        AnimatorState recovery = State(machine, "Recovery", Clip("Rig_Medium_General.fbx", "Interact"), boss ? 1.8f : 3f, 740, 95);
        AnimatorState stuck = State(machine, "Blade Stuck", Clip("Rig_Medium_General.fbx", "PickUp"), .75f, 490, 230);
        AnimatorState hit = State(machine, "Hit", Clip("Rig_Medium_General.fbx", "Hit_A"), 1.2f, 220, 230);
        AnimatorState dead = State(machine, "Death", Clip("Rig_Medium_General.fbx", "Death_B"), 1f, 0, 230);

        Condition(locomotion.AddTransition(windup), "Windup", AnimatorConditionMode.If, .1f);
        Condition(windup.AddTransition(locomotion), "Windup", AnimatorConditionMode.IfNot, .08f);
        Condition(strike.AddTransition(recovery), "Recovery", AnimatorConditionMode.If, .08f);
        Condition(recovery.AddTransition(locomotion), "Recovery", AnimatorConditionMode.IfNot, .12f);
        Condition(stuck.AddTransition(recovery), "Stuck", AnimatorConditionMode.IfNot, .1f);
        Any(machine, strike, "Strike", .04f);
        Any(machine, stuck, "Stuck", .06f);
        Any(machine, hit, "Hit", .04f);
        Any(machine, dead, "Dead", .04f);
        Exit(hit, locomotion, .8f, .08f);
        Exit(strike, locomotion, .9f, .08f);
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip Clip(string file, string name)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(Animations + file).OfType<AnimationClip>().FirstOrDefault(x => x.name == name);
        if (clip == null) throw new InvalidOperationException("Missing KayKit animation " + name);
        return clip;
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, float speed, float x, float y)
    {
        AnimatorState state = machine.AddState(name, new Vector3(x, y));
        state.motion = motion;
        state.speed = speed;
        return state;
    }

    private static void Condition(AnimatorStateTransition transition, string parameter, AnimatorConditionMode mode, float duration)
    {
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(mode, 0f, parameter);
    }

    private static void Any(AnimatorStateMachine machine, AnimatorState state, string parameter, float duration)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
        Condition(transition, parameter, AnimatorConditionMode.If, duration);
        transition.canTransitionToSelf = false;
    }

    private static void Exit(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }
}
