using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>One arena input source, also readable by Unity CLI's Input System simulation.</summary>
[DefaultExecutionOrder(1000)]
public sealed class ThanhGiongArenaInput : MonoBehaviour
{
    public static ThanhGiongArenaInput Current { get; private set; }
    readonly Dictionary<KeyCode, int> pressed = new Dictionary<KeyCode, int>();
    readonly List<KeyCode> expired = new List<KeyCode>();
    bool attack;
    int attackFrame;
#if ENABLE_INPUT_SYSTEM
    InputActionMap actions;
    void Awake()
    {
        Current = this; actions = new InputActionMap("PhuDongArena");
        Bind(KeyCode.Return, "enter"); Bind(KeyCode.Escape, "escape"); Bind(KeyCode.E, "e"); Bind(KeyCode.R, "r");
        Bind(KeyCode.C, "c"); Bind(KeyCode.Space, "space"); Bind(KeyCode.LeftAlt, "leftAlt"); Bind(KeyCode.LeftControl, "leftCtrl");
        Bind(KeyCode.V, "v");
        actions.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton").performed += _ => { attack = true; attackFrame = Time.frameCount; };
        actions.Enable();
    }
    void Bind(KeyCode key, string control) => actions.AddAction(key.ToString(), InputActionType.Button, "<Keyboard>/" + control).performed += _ => pressed[key] = Time.frameCount;
    void OnDestroy() { actions?.Dispose(); if (Current == this) Current = null; }
    public static Vector2 Move
    {
        get
        {
            var k = Keyboard.current; if (k == null) return Vector2.zero;
            return new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
        }
    }
    public static bool Running => Keyboard.current?.leftShiftKey.isPressed == true;
    public static bool OrbitHeld => Mouse.current?.rightButton.isPressed == true;
    public static Vector2 OrbitDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() * .12f : Vector2.zero;
    public static float ScrollDelta => Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#else
    void Awake() { Current = this; }
    void OnDestroy() { if (Current == this) Current = null; }
    public static Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public static bool Running => Input.GetKey(KeyCode.LeftShift);
    public static bool OrbitHeld => Input.GetMouseButton(1);
    public static Vector2 OrbitDelta => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    public static float ScrollDelta => Input.mouseScrollDelta.y;
#endif
    public static bool Down(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return Current != null && Current.pressed.Remove(key);
#else
        return Input.GetKeyDown(key);
#endif
    }
    public static bool AttackDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Current == null || !Current.attack) return false;
        Current.attack = false; return true;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
    // CLI events can arrive after gameplay Update. Retain them until consumers
    // have had the following frame, then discard unused paused/locked inputs.
    void LateUpdate()
    {
        expired.Clear();
        foreach (var key in pressed) if (key.Value < Time.frameCount) expired.Add(key.Key);
        foreach (var key in expired) pressed.Remove(key);
        if (attackFrame < Time.frameCount) attack = false;
    }
}
