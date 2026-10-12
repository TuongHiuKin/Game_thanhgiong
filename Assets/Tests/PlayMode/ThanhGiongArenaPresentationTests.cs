using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ThanhGiongArenaPresentationTests
{
    Component arena, campaign, waves, camera;
    Keyboard keyboard;
    Mouse mouse;
    InputSettings.BackgroundBehavior previousBackground;
    InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    static object P(object o, string name) => o.GetType().GetProperty(name).GetValue(o);
    static object F(object o, string name) => o.GetType().GetField(name).GetValue(o);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name).Invoke(o, args);
    Component[] Living => ((IEnumerable)P(waves, "LivingEnemies")).Cast<Component>().ToArray();

    [UnitySetUp] public IEnumerator Load()
    {
        Time.timeScale = 1;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        yield return SceneManager.LoadSceneAsync("PhuDongArena");
        yield return null;
        arena = UnityEngine.Object.FindFirstObjectByType(T("ThanhGiongArenaFlow")) as Component;
        campaign = arena.GetComponent(T("ThanhGiongCampaignController"));
        waves = (Component)F(arena, "waveManager");
        camera = Camera.main.GetComponent(T("IsometricCameraFollow"));
        foreach (object wave in (Array)F(waves, "waves")) { wave.GetType().GetField("count").SetValue(wave, 1); wave.GetType().GetField("health").SetValue(wave, 500f); }
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        keyboard.MakeCurrent(); mouse.MakeCurrent();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        Time.timeScale = 1; Call(arena, "RestartRound"); yield return null;
    }
    void Mode(int index) => Call(camera, "SetArenaCameraMode", Enum.ToObject(camera.GetType().GetField("arenaCameraMode").FieldType, index));
    IEnumerator Enemy()
    {
        Call(arena, "BeginCombat");
        float deadline = Time.realtimeSinceStartup + 3;
        while (Living.Length == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.Greater(Living.Length, 0);
        ((Behaviour)Living[0]).enabled = false;
    }

    [UnityTest] public IEnumerator VInputSurvivesLateFrameAndCyclesExactlyOnce()
    {
        Mode(0);
        for (int expected = 1; expected <= 3; expected++) {
            yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.V));
            InputSystem.Update(); // Same explicit flush used by Unity CLI.
            yield return null; yield return null;
            Assert.AreEqual(expected % 3, Convert.ToInt32(F(camera, "arenaCameraMode")));
            yield return null;
            Assert.AreEqual(expected % 3, Convert.ToInt32(F(camera, "arenaCameraMode")), "A held key must not cycle repeatedly.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
    }

    [UnityTest] public IEnumerator OneWheelNotchZoomsTwoMetersAndFirstPersonKeepsEyeHeight()
    {
        Mode(0);
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) });
        yield return null; yield return null;
        Assert.AreEqual(13.5f, (float)F(camera, "arenaDistance"), .01f);
        Mode(2); Call(camera, "SnapToTarget");
        Assert.Greater(camera.transform.position.y - arena.transform.position.y, 3f, "The eye must clear the mounted rider's chest.");
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) });
        yield return null; yield return null;
        Assert.AreEqual(0f, (float)F(camera, "arenaDistance"));
    }

    [UnityTest] public IEnumerator ArenaBoundaryDoesNotCollapseCameraDistance()
    {
        yield return Enemy();
        Call(campaign, "TeleportPlayer", new Vector3(0, .08f, -30));
        Mode(0); Call(camera, "SnapToTarget");
        yield return new WaitForSeconds(.7f);
        var focus = arena.transform.position + Vector3.up * (float)F(camera, "lookHeight");
        Assert.AreEqual(15.5f, Vector3.Distance(camera.transform.position, focus), .1f);
        Assert.GreaterOrEqual(Camera.main.farClipPlane, 1000f);
    }

    [UnityTest] public IEnumerator DamageAndDeathPushEnemyAlongGround()
    {
        yield return Enemy(); var e = Living[0]; var rb = e.GetComponent<Rigidbody>();
        rb.position = new Vector3(5, .1f, 5); Physics.SyncTransforms();
        yield return new WaitForSeconds(.3f);
        Vector3 start = rb.position;
        Call(e, "TakeDamage", 20f, 1f, start - Vector3.forward * 2, 1f, false);
        Assert.AreEqual(0f, rb.linearVelocity.y, .001f);
        yield return new WaitForSeconds(.15f);
        Assert.Greater(rb.position.z - start.z, .05f);
        Assert.LessOrEqual(rb.position.y - start.y, .08f);
        Call(e, "TakeDamage", 10000f, 0f, rb.position - Vector3.forward * 2, 1f, false);
        Assert.AreEqual(0f, rb.linearVelocity.y, .001f);
        Assert.Greater(rb.linearVelocity.z, 0);
    }

    [UnityTest] public IEnumerator ReticleAgreesWithActualMeleeBeyondOldHardcodedRange()
    {
        yield return Enemy(); var e = Living[0]; var rb = e.GetComponent<Rigidbody>();
        arena.transform.rotation = Quaternion.identity;
        rb.position = arena.transform.position + Vector3.forward * 5f;
        Physics.SyncTransforms();
        Assert.IsTrue((bool)Call(campaign, "HasMeleeTarget"));
        float before = (float)P(e, "HealthRatio");
        Assert.IsTrue((bool)Call(campaign, "TryMeleeAttack"));
        yield return new WaitForSeconds(.25f);
        Assert.Less((float)P(e, "HealthRatio"), before);
        rb.position = arena.transform.position - Vector3.forward * 5f; Physics.SyncTransforms();
        Assert.IsFalse((bool)Call(campaign, "HasMeleeTarget"));
    }
}
