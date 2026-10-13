using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

public static class ArenaInputReview
{
    const string Folder = "Artifacts/ArenaValidation/2026-10-13/input-recovery/";
    public static string Ready()
    {
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        return Inspect();
    }
    public static string NativeProbe()
    {
        Recover();
        ArenaReviewInputScope.Begin();
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.RestartRound(); flow.BeginCombat();
        var follow = Camera.main.GetComponent<IsometricCameraFollow>();
        follow.arenaYaw = 0; follow.SnapToTarget();
        SessionState.SetString("ArenaInputProbeStart", JsonUtility.ToJson(flow.transform.position));
        EditorApplication.CallbackFunction freeze = () => {
            if (flow != null) foreach (var enemy in flow.waveManager.LivingEnemies) enemy.enabled = false;
        };
        EditorApplication.update += freeze;
        ArenaReviewInputScope.OnCleanup(() => {
            EditorApplication.update -= freeze;
            if (flow != null) foreach (var enemy in flow.waveManager.LivingEnemies) enemy.enabled = true;
        });
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { delta = new Vector2(40, -20) });
        return Inspect();
    }
    public static string NativeResult()
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        var follow = Camera.main.GetComponent<IsometricCameraFollow>();
        var start = JsonUtility.FromJson<Vector3>(SessionState.GetString("ArenaInputProbeStart", "{}"));
        float travel = Vector3.Distance(start, follow.target.position);
        bool passed = Mouse.current.native && Mouse.current.enabled && Keyboard.current.native && Keyboard.current.enabled &&
            follow.arenaYaw > 5 && travel > .4f;
        System.IO.Directory.CreateDirectory(Folder);
        ScreenCapture.CaptureScreenshot(Folder + "native-input.png");
        string result = Newtonsoft.Json.JsonConvert.SerializeObject(new { passed, yaw=follow.arenaYaw, travel,
            mouseNative=Mouse.current.native, keyboardNative=Keyboard.current.native, state=Inspect() });
        System.IO.File.WriteAllText(Folder+"native-result.json", result);
        return result;
    }
    public static string CleanupResult()
    {
        string result = Inspect();
        System.IO.Directory.CreateDirectory(Folder);
        System.IO.File.WriteAllText(Folder+"after-play-stop.json", result);
        return result;
    }
    public static string Recover()
    {
        foreach (var device in InputSystem.devices.Where(d => !d.native &&
            (d.name.StartsWith("Mouse aim review") || d.name.StartsWith("Mouse only review"))).ToArray())
            InputSystem.RemoveDevice(device);
        foreach (var device in InputSystem.devices.Where(d => d.native && (d is Mouse || d is Keyboard)).ToArray())
            InputSystem.EnableDevice(device);
        InputSystem.devices.OfType<Mouse>().FirstOrDefault(d => d.native)?.MakeCurrent();
        InputSystem.devices.OfType<Keyboard>().FirstOrDefault(d => d.native)?.MakeCurrent();
        return Inspect();
    }
    public static string Inspect()
    {
        var arena = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        var camera = Camera.main != null ? Camera.main.GetComponent<IsometricCameraFollow>() : null;
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            playing = Application.isPlaying, focused = Application.isFocused,
            window = EditorWindow.focusedWindow != null ? EditorWindow.focusedWindow.GetType().Name : "none",
            timeScale = Time.timeScale, cursor = Cursor.lockState.ToString(),
            stage = arena != null ? arena.CurrentStage.ToString() : "none",
            lookActive = arena != null && arena.MouseLookActive,
            yaw = camera != null ? camera.arenaYaw : 0,
            delta = ThanhGiongArenaInput.OrbitDelta.ToString(),
            currentMouse = Mouse.current != null ? Mouse.current.deviceId : -1,
            devices = InputSystem.devices.Select(d => new { d.deviceId, d.name, d.enabled, d.native, layout=d.layout }).ToArray(),
            updateMode = InputSystem.settings.updateMode.ToString(),
            background = InputSystem.settings.backgroundBehavior.ToString(),
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode.ToString()
        });
    }
}
