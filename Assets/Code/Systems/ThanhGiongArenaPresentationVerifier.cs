#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Opt-in development player proof. Controlled enemies keep the art review stable;
// movement, camera switching, scrolling and strikes use real Input System events.
public sealed class ThanhGiongArenaPresentationVerifier : MonoBehaviour
{
    [Serializable] public sealed class Report
    {
        public string verdict;
        public int runtimeErrors;
        public string[] failures, cameraModes;
        public float zoomBefore, zoomAfter, boundaryDistance, movedMeters, eyeHeight, damageDisplacement, damageRise, deathRise;
    }
    readonly Report report = new Report();
    readonly List<string> failures = new List<string>();
    readonly List<string> modes = new List<string>();
    ThanhGiongArenaFlow flow;
    ThanhGiongCampaignController campaign;
    IsometricCameraFollow camera;
    Keyboard keyboard;
    Mouse mouse;
    Key[] keys = Array.Empty<Key>();
    bool attack;
    float scroll;
    string folder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (!Application.isEditor && Debug.isDebugBuild && Environment.GetCommandLineArgs().Contains("--verify-arena-presentation"))
            new GameObject("Arena presentation verification").AddComponent<ThanhGiongArenaPresentationVerifier>();
    }
    void Awake()
    {
        Application.runInBackground = true; Application.targetFrameRate = 60;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>("Presentation keyboard"); mouse = InputSystem.AddDevice<Mouse>("Presentation mouse");
        Application.logMessageReceived += Log;
        folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../PresentationVerification")); Directory.CreateDirectory(folder);
    }
    void Log(string text, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.runtimeErrors++; }
    void Update()
    {
        if (flow != null) foreach (var e in flow.waveManager.LivingEnemies) e.enabled = false;
    }
    void LateUpdate()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, scroll) }.WithButton(MouseButton.Left, attack));
        scroll = 0;
    }
    void Check(bool pass, string message) { if (!pass) failures.Add(message); }
    IEnumerator KeyPress(Key key)
    {
        keys = new[] { key }; yield return null; yield return null;
        keys = Array.Empty<Key>(); yield return new WaitForSeconds(.45f);
    }
    IEnumerator Shot(string name)
    {
        // Hidden Windows swapchains can return black/null ScreenCapture frames.
        // An explicit URP camera render remains valid; HUD is reviewed in Editor.
        yield return new WaitForEndOfFrame();
        var rt = new RenderTexture(1280, 720, 24);
        RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        var previous = RenderTexture.active; RenderTexture.active = rt;
        var frame = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); frame.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), frame.EncodeToPNG());
        RenderTexture.active = previous; rt.Release(); Destroy(rt); Destroy(frame);
    }
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        flow = FindAnyObjectByType<ThanhGiongArenaFlow>(); campaign = flow.GetComponent<ThanhGiongCampaignController>(); camera = Camera.main.GetComponent<IsometricCameraFollow>();
        flow.waveManager.waves[0].count = 3; flow.waveManager.waves[0].health = 500; flow.waveManager.waves[0].spawnInterval = .1f;
        yield return Shot("01-intro"); yield return KeyPress(Key.Enter);
        Check(flow.CanFight, "Enter did not start combat");
        var enemies = flow.waveManager.LivingEnemies.ToArray();
        Check(enemies.Length == 3, "Controlled first wave did not spawn three enemies");
        if (enemies.Length == 0) { Finish(); yield break; }
        for (int i = 0; i < enemies.Length; i++) {
            var rb = enemies[i].GetComponent<Rigidbody>(); rb.position = flow.transform.position + Vector3.forward * (i == 0 ? 3 : 6) + Vector3.right * (i * 3); rb.linearVelocity = Vector3.zero;
        }
        Physics.SyncTransforms(); yield return new WaitForSeconds(.3f);
        modes.Add(camera.arenaCameraMode.ToString()); yield return Shot("02-third-person-far");
        yield return KeyPress(Key.V); modes.Add(camera.arenaCameraMode.ToString()); yield return Shot("03-third-person-close");
        yield return KeyPress(Key.V); modes.Add(camera.arenaCameraMode.ToString());
        report.eyeHeight = camera.transform.position.y - flow.transform.position.y; yield return Shot("04-first-person");
        Check(report.eyeHeight > 3, "First-person camera below rider eye level");
        yield return KeyPress(Key.V); modes.Add(camera.arenaCameraMode.ToString());
        Check(modes.SequenceEqual(new[] { "ThirdPersonFar", "ThirdPersonClose", "FirstPerson", "ThirdPersonFar" }), "V camera cycle failed");
        report.zoomBefore = camera.arenaDistance; scroll = 120; yield return new WaitForSeconds(.3f); report.zoomAfter = camera.arenaDistance;
        Check(Mathf.Abs(report.zoomBefore - report.zoomAfter - 2) < .01f, "A wheel notch did not zoom two meters");
        scroll = -120; yield return new WaitForSeconds(.3f);
        Vector3 start = flow.transform.position;
        Vector3 previous = start;
        foreach (var direction in new[] { Key.W, Key.D, Key.S, Key.A }) {
            keys = new[] { direction }; yield return new WaitForSeconds(2);
            report.movedMeters += Vector3.Distance(previous, flow.transform.position); previous = flow.transform.position;
        }
        keys = Array.Empty<Key>(); Check(report.movedMeters > 2, "WASD did not move the mounted character");
        campaign.TeleportPlayer(new Vector3(0, .08f, -30)); flow.transform.rotation = Quaternion.identity;
        camera.SnapToTarget(); yield return new WaitForSeconds(.5f);
        report.boundaryDistance = Vector3.Distance(camera.transform.position, flow.transform.position + Vector3.up*camera.lookHeight);
        Check(report.boundaryDistance > 14, "Invisible arena boundary collapsed camera"); yield return Shot("05-boundary-background");
        campaign.TeleportPlayer(start); flow.transform.rotation = Quaternion.identity;
        camera.arenaYaw = -90; camera.arenaPitch = 18; camera.arenaDistance = 6; camera.SnapToTarget(); yield return new WaitForSeconds(.3f); yield return Shot("06-left-flank");
        camera.arenaYaw = 90; camera.arenaPitch = 18; camera.arenaDistance = 4.5f; camera.lookHeight = 2.5f; camera.SnapToTarget(); yield return new WaitForSeconds(.3f); yield return Shot("07-weapon-grip");
        camera.SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonClose); camera.arenaYaw = 0; camera.SnapToTarget();
        var enemy = enemies[0]; var body = enemy.GetComponent<Rigidbody>(); body.position = start + Vector3.forward * 3; body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
        yield return new WaitForSeconds(.3f); Vector3 impactStart = body.position; float hp = enemy.HealthRatio;
        Check(campaign.HasMeleeTarget(), "Reticle did not detect melee target");
        attack = true; yield return null; yield return null; attack = false;
        float rise = body.position.y;
        for (float t = 0; t < .4f; t += Time.deltaTime) { rise = Mathf.Max(rise, body.position.y); yield return null; }
        report.damageDisplacement = Vector3.ProjectOnPlane(body.position - impactStart, Vector3.up).magnitude; report.damageRise = rise - impactStart.y;
        Check(enemy.HealthRatio < hp && report.damageDisplacement > .05f && report.damageRise < .1f, "Attack damage / ground knockback failed"); yield return Shot("08-damaged-enemy-health-bar");
        Vector3 deathStart = body.position; enemy.TakeDamage(10000, 0, flow.transform.position, 1, false);
        rise = body.position.y;
        for (float t = 0; t < .4f; t += Time.deltaTime) { rise = Mathf.Max(rise, body.position.y); yield return null; }
        report.deathRise = rise - deathStart.y; Check(report.deathRise < .1f, "Death launched enemy upwards"); yield return Shot("09-flat-death");
        Finish();
    }
    void Finish()
    {
        report.failures = failures.ToArray(); report.cameraModes = modes.ToArray(); report.verdict = failures.Count == 0 && report.runtimeErrors == 0 ? "PASS" : "FAIL";
        File.WriteAllText(Path.Combine(folder, "result.json"), JsonUtility.ToJson(report, true)); Application.Quit(report.verdict == "PASS" ? 0 : 1);
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
    }
}
#endif
