using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ArenaMouseGroundReview
{
    const string Folder = "Artifacts/ArenaValidation/2026-10-13/center-mouse-look-ground/";
    public static string FocusGame()
    {
        Application.runInBackground = true;
        UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        return "Game View focused for rendered-frame tests";
    }
    public static string OpenArena()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ThanhGiongArenaBuilder.ScenePath);
        return "Saved arena opened";
    }
    public static string Probe()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {playing=Application.isPlaying, time=Time.time, scale=Time.timeScale, frame=Time.frameCount,
            scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            runners=Object.FindObjectsByType<MonoBehaviour>().Where(b=>b.GetType().Name.Contains("Runner")).Select(b=>b.GetType().FullName).ToArray()});
    }
    public static string AuditGround()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new[] { "Grass plain", "Walkable ground", "Central battlefield" }.Select(name => {
            var go = GameObject.Find(name); var r = go.GetComponent<Renderer>();
            return new { name, visible = r.enabled, top = r.bounds.max.y, floor = go.GetComponent<Collider>().bounds.max.y };
        }));
    }
    public static string Before()
    {
        Directory.CreateDirectory(Folder);
        ScreenCapture.CaptureScreenshot(Folder + "before-ground.png");
        return AuditGround();
    }
    public static string Prepare()
    {
        ArenaReviewInputScope.Begin();
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.waveManager.waves[0].count = 3; flow.waveManager.waves[0].health = 500; flow.waveManager.waves[0].spawnInterval = .1f;
        flow.BeginCombat();
        flow.GetComponent<MountedHorseController>().enabled = true;
        UnityEditor.EditorApplication.CallbackFunction freeze = () => {
            if (flow != null && Application.isPlaying)
                foreach (var enemy in flow.waveManager.LivingEnemies) enemy.enabled = false;
        };
        UnityEditor.EditorApplication.update += freeze;
        ArenaReviewInputScope.OnCleanup(() => {
            UnityEditor.EditorApplication.update -= freeze;
            if(flow != null) foreach(var enemy in flow.waveManager.LivingEnemies) enemy.enabled=true;
        });
        var mouse = ArenaReviewInputScope.Add<Mouse>("Mouse aim review");
        return AuditGround();
    }
    public static string Left()
    {
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { delta = new Vector2(-80, 20) });
        return "Mouse moved left/up without a button";
    }
    public static string ResetPose()
    {
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.GetComponent<MountedHorseController>().enabled = true;
        var follow = Camera.main.GetComponent<IsometricCameraFollow>();
        follow.arenaYaw = 0; follow.SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonFar); follow.SnapToTarget();
        Mouse.current.MakeCurrent(); InputSystem.QueueStateEvent(Mouse.current, new MouseState());
        return "Live horse animation and initial camera restored";
    }
    public static string Right()
    {
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { delta = new Vector2(140, -40) });
        return "Mouse moved right/down without a button";
    }
    public static string CaptureLeft() => Capture("center-look-left.png");
    public static string CaptureRight() => Capture("center-look-right.png");
    static string Capture(string name)
    {
        Directory.CreateDirectory(Folder); ScreenCapture.CaptureScreenshot(Folder + name);
        var camera = Camera.main; var follow = camera.GetComponent<IsometricCameraFollow>();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { name, width=Screen.width, height=Screen.height, reticle=ThanhGiongArenaHUD.AimScreenPoint.ToString(), yaw=follow.arenaYaw, pitch=follow.arenaPitch, cursorLocked=Cursor.lockState.ToString(), rotation=camera.transform.eulerAngles.ToString() });
    }
}
