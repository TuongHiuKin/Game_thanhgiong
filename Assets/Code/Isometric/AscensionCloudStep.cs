using UnityEngine;

[DisallowMultipleComponent]
public sealed class AscensionCloudStep : MonoBehaviour
{
    [Tooltip("Visual child that floats gently while the parent collider stays stable for horse movement.")]
    public Transform visualRoot;
    [Range(0f, 0.6f)] public float bobAmplitude = 0.18f;
    [Range(0.1f, 4f)] public float bobSpeed = 0.8f;
    [Range(0f, 12f)] public float driftDegrees = 2.5f;
    [Range(0f, 0.25f)] public float sideDrift = 0.08f;
    [Range(0f, 0.15f)] public float breathAmount = 0.045f;
    public float phase;

    Vector3 visualBaseLocalPosition;
    Quaternion visualBaseLocalRotation;
    Vector3 visualBaseLocalScale;

    void Awake()
    {
        if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
        CacheBasePose();
    }

    void OnEnable() => CacheBasePose();

    public void Initialize(Transform visual)
    {
        visualRoot = visual;
        CacheBasePose();
    }

    void CacheBasePose()
    {
        if (visualRoot == null) return;
        visualBaseLocalPosition = visualRoot.localPosition;
        visualBaseLocalRotation = visualRoot.localRotation;
        visualBaseLocalScale = visualRoot.localScale;
        if (Mathf.Approximately(phase, 0f)) phase = Mathf.Abs(GetEntityId().GetHashCode() % 997) * 0.013f;
    }

    void Update()
    {
        if (visualRoot == null || Time.timeScale <= 0f) return;
        float wave = Mathf.Sin(Time.time * bobSpeed + phase);
        float slowWave = Mathf.Sin(Time.time * bobSpeed * .61f + phase * 1.37f);
        visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * (wave * bobAmplitude)
            + Vector3.right * (slowWave * sideDrift);
        visualRoot.localRotation = visualBaseLocalRotation * Quaternion.Euler(0f, wave * driftDegrees, Mathf.Cos(Time.time * bobSpeed * .73f + phase) * driftDegrees * .45f);
        visualRoot.localScale = Vector3.Scale(visualBaseLocalScale,
            new Vector3(1f + slowWave * breathAmount, 1f + wave * breathAmount * .35f, 1f - slowWave * breathAmount * .45f));
    }
}
