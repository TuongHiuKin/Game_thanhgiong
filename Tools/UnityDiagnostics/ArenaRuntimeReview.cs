using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Controlled, temporary Play Mode review. Never writes scene or prefab assets.
public static class ArenaRuntimeReview
{
    static ThanhGiongArenaFlow Flow => Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
    public static string Prepare()
    {
        if (!Application.isPlaying) return "Play Mode required";
        Application.runInBackground = true;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(gameView).Show();
        EditorWindow.GetWindow(gameView).Focus();
        var sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupMethod = sizesType.GetMethod("GetGroup");
        var group = groupMethod.Invoke(sizes, new[] { System.Enum.ToObject(groupMethod.GetParameters()[0].ParameterType, 0) });
        var sizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
        var ctor = sizeType.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).First(c => c.GetParameters().Length == 4);
        var size = ctor.Invoke(new object[] { System.Enum.ToObject(ctor.GetParameters()[0].ParameterType, 1), 1280, 720, "Arena verification" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        gameView.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(EditorWindow.GetWindow(gameView), index);
        Flow.RestartRound();
        Flow.waveManager.waves[0].count = 3;
        Flow.waveManager.waves[0].health = 500;
        Flow.waveManager.waves[0].spawnInterval = .1f;
        EditorApplication.update -= HoldEnemies;
        EditorApplication.update += HoldEnemies;
        return State();
    }
    static void HoldEnemies()
    {
        if (!Application.isPlaying) { EditorApplication.update -= HoldEnemies; return; }
        foreach (var e in Object.FindObjectsByType<ThanhGiongEnemy>()) e.enabled = false;
    }
    public static string State()
    {
        var f = Flow;
        var c = Camera.main.GetComponent<IsometricCameraFollow>();
        var rig = f.GetComponent<MountedHorseMeshMotion>();
        var skin = f.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            frame = Time.frameCount, time = Time.time, paused = EditorApplication.isPaused, timescale = Time.timeScale, input = ThanhGiongArenaInput.Current != null,
            stage = f.CurrentStage.ToString(), health = f.GetComponent<ThanhGiongCampaignController>().Health,
            player = f.transform.position.ToString(), mode = c.arenaCameraMode.ToString(), c.arenaDistance,
            camera = c.transform.position.ToString(), actualDistance = Vector3.Distance(c.transform.position, f.transform.position + Vector3.up*c.lookHeight),
            c.arenaPitch, c.arenaYaw, c.fieldOfView, farClip = Camera.main.farClipPlane,
            skinBounds = new { min = skin.bounds.min.ToString(), max = skin.bounds.max.ToString() },
            bones = rig.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("Rider") || t.name.Contains("Hand")).Select(t => new { t.name, position = t.position.ToString() }).ToArray(),
            enemies = f.waveManager.LivingEnemies.Select(e => new { e.name, hp = e.HealthRatio, position = e.transform.position.ToString(), velocity = e.GetComponent<Rigidbody>().linearVelocity.ToString() }).ToArray()
        });
    }
    public static string PositionEnemies()
    {
        int i = 0;
        foreach (var e in Flow.waveManager.LivingEnemies) {
            var rb = e.GetComponent<Rigidbody>(); rb.position = Flow.transform.position + Flow.transform.forward * (i == 0 ? 3f : 6f) + Flow.transform.right * (i++ == 0 ? 0f : 3f);
            rb.linearVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        return State();
    }
    public static string WheelIn()
    {
        InputSystem.QueueDeltaStateEvent(Mouse.current.scroll, new Vector2(0,120));
        return "Queued one upward wheel notch (120 units)";
    }
    public static string CaptureHud()
    {
        ScreenCapture.CaptureScreenshot("Artifacts/ArenaValidation/2026-10-12/editor-hud.png");
        return "Screenshot queued for next frame";
    }
    public static string Boundary()
    {
        Flow.GetComponent<ThanhGiongCampaignController>().TeleportPlayer(new Vector3(30, .08f, 30));
        Camera.main.GetComponent<IsometricCameraFollow>().SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonFar);
        return State();
    }
    public static string Attack()
    {
        return Flow.GetComponent<ThanhGiongCampaignController>().TryMeleeAttack().ToString();
    }
    public static string Unpause()
    {
        EditorApplication.isPaused = false;
        return State();
    }
    public static string CancelTests()
    {
        var api = typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi);
        var holder = api.GetProperty("m_testJobDataHolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
        var runners = (System.Array)holder.GetType().GetMethod("GetAllRunners").Invoke(holder, null);
        int cancelled = 0;
        foreach (var runner in runners) {
            var data = runner.GetType().GetMethod("GetData").Invoke(runner, null);
            var guid = (string)data.GetType().GetField("guid").GetValue(data);
            if (UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.CancelTestRun(guid)) cancelled++;
        }
        return "Cancelled outstanding test jobs: " + cancelled;
    }
    public static string FirstPerson()
    {
        Camera.main.GetComponent<IsometricCameraFollow>().SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.FirstPerson);
        return State();
    }
    public static string HudMetrics()
    {
        var p = Camera.main.WorldToScreenPoint(Flow.transform.position + Vector3.up * 1.25f + Flow.transform.forward * 3.4f);
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { target = Flow.GetComponent<ThanhGiongCampaignController>().HasMeleeTarget(), x = p.x, y = Screen.height - p.y, depth = p.z, Screen.width, Screen.height });
    }
    public static string LeftFlank()
    {
        var movement = Flow.GetComponent<MountedHorseController>(); movement.ResetMovementState(); movement.enabled = false;
        Flow.transform.rotation = Quaternion.identity;
        var rig = Flow.GetComponent<MountedHorseMeshMotion>(); rig.Pose(0, 0, false, -1, true);
        var cam = Camera.main; cam.GetComponent<IsometricCameraFollow>().enabled = false;
        cam.fieldOfView = 45; cam.transform.position = Flow.transform.position + new Vector3(-6, 2.2f, .4f); cam.transform.LookAt(Flow.transform.position + Vector3.up * 1.5f);
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { footSpread = rig.hooves.Max(f => f.position.y) - rig.hooves.Min(f => f.position.y), feet = rig.hooves.Select(f => f.position.ToString()).ToArray() });
    }
    public static string Grip()
    {
        var weapon = Flow.GetComponentsInChildren<Transform>().First(t => t.name == "Weapon_IronSword");
        var cam = Camera.main; cam.GetComponent<IsometricCameraFollow>().enabled = false;
        var grip = weapon.position - Vector3.up * .62f;
        cam.fieldOfView = 45; cam.transform.position = grip + new Vector3(1.2f, .3f, 1.2f); cam.transform.LookAt(grip);
        return weapon.position.ToString();
    }
    public static string SourceGrip()
    {
        Flow.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh").sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_HorseSkin.asset");
        return Grip();
    }
    public static string ArenaGrip()
    {
        Flow.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh").sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_ArenaSkin.asset");
        return Grip();
    }
    public static string HudSetup()
    {
        var c = Camera.main.GetComponent<IsometricCameraFollow>(); c.enabled = true;
        c.SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonClose); c.arenaYaw = 0;
        Flow.transform.rotation = Quaternion.identity; c.SnapToTarget();
        return PositionEnemies();
    }
}
