using UnityEngine;

/// <summary>Slow, offset motion for decorative cloud and smoke puffs. Colliders stay untouched.</summary>
[DisallowMultipleComponent]
public sealed class AscensionCloudPuffMotion : MonoBehaviour
{
    [Min(0f)] public float drift = .18f;
    [Min(0.1f)] public float speed = .65f;
    [Range(0f, .25f)] public float breath = .08f;
    [Range(0f, 15f)] public float turnDegrees = 4f;
    public float phase;

    Vector3 basePosition;
    Vector3 baseScale;
    Quaternion baseRotation;

    void Awake()
    {
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (Time.timeScale <= 0f) return;
        float t = Time.time * speed + phase;
        float slow = Mathf.Sin(t);
        float cross = Mathf.Sin(t * .73f + 1.4f);
        transform.localPosition = basePosition + new Vector3(cross * drift, slow * drift * .65f, Mathf.Cos(t * .83f) * drift * .42f);
        transform.localScale = Vector3.Scale(baseScale,
            new Vector3(1f + slow * breath, 1f + cross * breath * .42f, 1f - slow * breath * .45f));
        transform.localRotation = baseRotation * Quaternion.Euler(0f, slow * turnDegrees, cross * turnDegrees * .28f);
    }
}
