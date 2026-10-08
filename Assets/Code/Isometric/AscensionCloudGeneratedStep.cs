using UnityEngine;

/// <summary>Emerges under a hoof, lingers briefly, then disperses without leaving static stairs.</summary>
public sealed class AscensionCloudGeneratedStep : MonoBehaviour
{
    [Min(.2f)] public float lifetime = 2.7f;
    [Min(.05f)] public float appearTime = .28f;
    [Min(.05f)] public float disperseTime = .85f;

    float born;

    void Awake()
    {
        born = Time.time;
        transform.localScale = Vector3.one * .08f;
    }

    void Update()
    {
        float age = Time.time - born;
        float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / appearTime));
        float disappear = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - lifetime + disperseTime) / disperseTime));
        transform.localScale = Vector3.one * Mathf.Max(.001f, appear * disappear);
        if (age >= lifetime) Destroy(gameObject);
    }
}
