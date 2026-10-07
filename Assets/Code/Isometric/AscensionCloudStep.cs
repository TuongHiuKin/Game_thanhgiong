using UnityEngine;

[DisallowMultipleComponent]
public sealed class AscensionCloudStep : MonoBehaviour
{
    [Tooltip("Visual child that floats gently while the parent collider stays stable for horse movement.")]
    public Transform visualRoot;
    [Range(0f, 0.6f)] public float bobAmplitude = 0.18f;
    [Range(0.1f, 4f)] public float bobSpeed = 0.8f;
    [Range(0f, 12f)] public float driftDegrees = 2.5f;
    public float phase;

    Vector3 visualBaseLocalPosition;
    Quaternion visualBaseLocalRotation;

    void Awake()
    {
        if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        CacheBasePose();
    }

    void OnEnable() => CacheBasePose();

    void CacheBasePose()
    {
        if (visualRoot == null) return;
        visualBaseLocalPosition = visualRoot.localPosition;
        visualBaseLocalRotation = visualRoot.localRotation;
        if (Mathf.Approximately(phase, 0f)) phase = Mathf.Abs(GetEntityId().GetHashCode() % 997) * 0.013f;
    }

    void Update()
    {
        if (visualRoot == null || Time.timeScale <= 0f) return;
        float wave = Mathf.Sin(Time.time * bobSpeed + phase);
        visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * (wave * bobAmplitude);
        visualRoot.localRotation = visualBaseLocalRotation * Quaternion.Euler(0f, wave * driftDegrees, Mathf.Cos(Time.time * bobSpeed * .73f + phase) * driftDegrees * .45f);
    }
}
