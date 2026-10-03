using UnityEngine;

public class GameAssetAnimationPreview : MonoBehaviour
{
    public enum AssetKind { Warrior, IronHorse }
    public enum MotionState { Idle, Walk, Run }

    [Header("Asset")]
    public AssetKind assetKind;
    public Animator animator;
    public Transform visual;

    [Header("Preview")]
    public bool autoPreview = true;
    public float stateDuration = 3f;
    public float attackEvery = 7f;

    [Header("Horse motion")]
    public float walkFrequency = 5.5f;
    public float runFrequency = 9f;
    public float bobHeight = 0.075f;
    public float pitchAngle = 2.7f;
    public float rollAngle = 1.8f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private MotionState state;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float stateClock;
    private float motionClock;
    private float attackClock;
    private float attackTime = -1f;

    private void Awake()
    {
        if (visual == null)
        {
            Transform child = transform.Find("Visual");
            visual = child != null ? child : transform;
        }
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        basePosition = visual.localPosition;
        baseRotation = visual.localRotation;
        SetMotion(MotionState.Idle);
    }

    private void Update()
    {
        if (autoPreview)
        {
            stateClock += Time.deltaTime;
            attackClock += Time.deltaTime;
            if (stateClock >= stateDuration)
            {
                stateClock = 0f;
                SetMotion((MotionState)(((int)state + 1) % 3));
            }
            if (attackClock >= attackEvery)
            {
                attackClock = 0f;
                TriggerAttack();
            }
        }

        if (assetKind == AssetKind.IronHorse) AnimateHorse();
    }

    public void SetMotion(MotionState nextState)
    {
        state = nextState;
        if (animator != null)
        {
            float speed = state == MotionState.Idle ? 0f : state == MotionState.Walk ? 0.55f : 1f;
            animator.SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
        }
    }

    public void TriggerAttack()
    {
        attackTime = 0f;
        if (animator != null) animator.SetTrigger(AttackHash);
    }

    private void AnimateHorse()
    {
        if (visual == null) return;
        if (attackTime >= 0f)
        {
            attackTime += Time.deltaTime;
            const float duration = 0.72f;
            float t = Mathf.Clamp01(attackTime / duration);
            float rear = Mathf.Sin(t * Mathf.PI);
            Vector3 attackPosition = basePosition + Vector3.up * rear * 0.28f;
            Quaternion attackRotation = baseRotation * Quaternion.Euler(-rear * 18f, 0f, rear * 3f);
            visual.localPosition = Vector3.Lerp(visual.localPosition, attackPosition, 1f - Mathf.Exp(-18f * Time.deltaTime));
            visual.localRotation = Quaternion.Slerp(visual.localRotation, attackRotation, 1f - Mathf.Exp(-18f * Time.deltaTime));
            if (t >= 1f) attackTime = -1f;
            return;
        }

        float frequency = state == MotionState.Run ? runFrequency : walkFrequency;
        float strength = state == MotionState.Idle ? 0.2f : state == MotionState.Walk ? 0.65f : 1f;
        motionClock += Time.deltaTime * frequency;
        float wave = Mathf.Sin(motionClock);
        float doubleWave = Mathf.Sin(motionClock * 2f);
        Vector3 position = basePosition + Vector3.up * Mathf.Abs(wave) * bobHeight * strength;
        Quaternion rotation = baseRotation * Quaternion.Euler(doubleWave * pitchAngle * strength, 0f, wave * rollAngle * strength);
        float damping = 1f - Mathf.Exp(-13f * Time.deltaTime);
        visual.localPosition = Vector3.Lerp(visual.localPosition, position, damping);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, rotation, damping);
    }
}
