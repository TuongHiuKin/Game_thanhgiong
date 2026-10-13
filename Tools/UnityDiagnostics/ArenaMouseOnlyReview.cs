using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Temporary runtime inspection, does not save scene/asset changes.
public static class ArenaMouseOnlyReview
{
    const string Folder = "Artifacts/ArenaValidation/2026-10-13/mouse-only-view/";
    public static string Prepare()
    {
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        ArenaReviewInputScope.Begin();
        var keyboard=ArenaReviewInputScope.Add<Keyboard>("Mouse only review keyboard");
        var mouse=ArenaReviewInputScope.Add<Mouse>("Mouse only review mouse");
        SessionState.SetInt("ArenaMouseOnlyKeyboard",keyboard.deviceId);
        SessionState.SetInt("ArenaMouseOnlyMouse",mouse.deviceId);
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.waveManager.waves[0].count=1; flow.waveManager.waves[0].health=500; flow.BeginCombat();
        EditorApplication.CallbackFunction freeze = () => { if(flow != null && Application.isPlaying) foreach(var e in flow.waveManager.LivingEnemies) e.enabled=false; };
        EditorApplication.update += freeze;
        ArenaReviewInputScope.OnCleanup(() => {
            EditorApplication.update -= freeze;
            if(flow != null) foreach(var e in flow.waveManager.LivingEnemies) e.enabled=true;
        });
        return "Runtime review ready";
    }
    public static string Start()
    {
        var mouse=(Mouse)InputSystem.GetDeviceById(SessionState.GetInt("ArenaMouseOnlyMouse",-1));
        // Isolate scripted review input from physical mouse activity/lock warps.
        ArenaReviewInputScope.IsolateMouse(mouse);
        mouse.MakeCurrent(); InputSystem.QueueStateEvent(mouse,new MouseState());
        var follow = Camera.main.GetComponent<IsometricCameraFollow>();
        follow.SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonClose); follow.arenaYaw=37; follow.SnapToTarget();
        return "View set to 37 degrees";
    }
    public static string Before() => Capture("01-before-move");
    public static string Strafe()
    {
        var keyboard=(Keyboard)InputSystem.GetDeviceById(SessionState.GetInt("ArenaMouseOnlyKeyboard",-1));
        keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
        return "D held, no mouse movement";
    }
    public static string After()
    {
        var keyboard=(Keyboard)InputSystem.GetDeviceById(SessionState.GetInt("ArenaMouseOnlyKeyboard",-1));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        return Capture("02-strafe");
    }
    public static string TurnMouse()
    {
        var mouse=(Mouse)InputSystem.GetDeviceById(SessionState.GetInt("ArenaMouseOnlyMouse",-1));
        mouse.MakeCurrent(); InputSystem.QueueStateEvent(mouse,new MouseState {delta=new Vector2(60,-20)});
        return "Mouse moved, no keyboard input";
    }
    public static string Turned() => Capture("03-mouse-turn");
    static string Capture(string name)
    {
        Directory.CreateDirectory(Folder); ScreenCapture.CaptureScreenshot(Folder+name+".png");
        var follow=Camera.main.GetComponent<IsometricCameraFollow>();
        string state=Newtonsoft.Json.JsonConvert.SerializeObject(new {yaw=follow.arenaYaw,pitch=follow.arenaPitch,
            cameraRotation=Camera.main.transform.eulerAngles.ToString(),player=follow.target.position.ToString(),
            reticle=ThanhGiongArenaHUD.AimScreenPoint.ToString()});
        File.WriteAllText(Folder+name+".json",state); return state;
    }
}
