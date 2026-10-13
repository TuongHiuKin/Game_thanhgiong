using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// CLI review assemblies survive Play Mode when domain reload is disabled.
// Own temporary devices explicitly instead of relying on a domain reload.
[InitializeOnLoad]
public static class ArenaReviewInputScope
{
    static readonly List<InputDevice> owned = new List<InputDevice>();
    static readonly List<InputDevice> disabled = new List<InputDevice>();
    static readonly List<Action> cleanup = new List<Action>();
    static bool active, previousRunInBackground;
    static InputSettings.BackgroundBehavior previousBackground;
    static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

    static ArenaReviewInputScope()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingPlayMode) End();
        };
        AssemblyReloadEvents.beforeAssemblyReload += End;
        EditorApplication.quitting += End;
    }

    public static void Begin()
    {
        End();
        active = true;
        previousRunInBackground = Application.runInBackground;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    public static T Add<T>(string name) where T : InputDevice
    {
        var device = InputSystem.AddDevice<T>(name);
        owned.Add(device);
        device.MakeCurrent();
        return device;
    }

    public static void IsolateMouse(Mouse mouse)
    {
        foreach (var device in InputSystem.devices.OfType<Mouse>().Where(d => d != mouse && d.enabled).ToArray())
        {
            disabled.Add(device);
            InputSystem.DisableDevice(device);
        }
    }

    public static void OnCleanup(Action action) => cleanup.Add(action);

    public static void End()
    {
        foreach (var action in cleanup) action();
        cleanup.Clear();
        foreach (var device in owned) if (device.added) InputSystem.RemoveDevice(device);
        owned.Clear();
        foreach (var device in disabled) if (device.added) InputSystem.EnableDevice(device);
        disabled.Clear();
        InputSystem.devices.OfType<Mouse>().FirstOrDefault(d => d.native && d.enabled)?.MakeCurrent();
        InputSystem.devices.OfType<Keyboard>().FirstOrDefault(d => d.native && d.enabled)?.MakeCurrent();
        if (!active) return;
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        Application.runInBackground = previousRunInBackground;
        active = false;
    }
}
