using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ThanhGiongRagdollPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    public float defaultMass = 80f;
    public float drag = 0.8f;
    public float angularDrag = 2.0f;
    public float maxImpactSpeed = 18f;
    public float maxDeathLaunchSpeed = 24f;

    private Rigidbody rb;
    private Collider col;
    private bool isLaunched;
    private Vector3 originalLocalScale;

    private void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = defaultMass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        originalLocalScale = transform.localScale;
    }

    public void ResetForBattle()
    {
        StopAllCoroutines();
        isLaunched = false;
        if (rb != null) {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        }
        if (col != null) col.enabled = true;
    }

    public void ApplyKnockback(Vector3 impactPoint, Vector3 direction, float force, float upwardLift = 0f)
    {
        if (isLaunched || !gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impactPoint, direction, force, upwardLift));
    }

    private IEnumerator KnockbackRoutine(Vector3 impactPoint, Vector3 direction, float force, float upwardLift)
    {
        // Maintain upright posture during knockback (stumble backwards, no tumbling or flying up)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        Vector3 flatAway = Vector3.ProjectOnPlane(transform.position - impactPoint, Vector3.up);
        if (flatAway.sqrMagnitude < 0.01f) flatAway = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flatAway.sqrMagnitude < 0.01f) flatAway = -transform.forward;
        Vector3 launchDir = flatAway.normalized;
        float cappedForce = Mathf.Min(Mathf.Max(0f, force), maxImpactSpeed * 0.75f);
        rb.linearVelocity = new Vector3(launchDir.x * cappedForce, 0f, launchDir.z * cappedForce);

        // Smoothly slide / stumble backwards and recover
        float timer = 0f;
        while (timer < 0.45f && !isLaunched)
        {
            timer += Time.deltaTime;
            float t = timer / 0.45f;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, t * 2f);
            yield return null;
        }

        if (!isLaunched)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void LaunchOnDeath(Vector3 impactOrigin, float force = 12f, float lift = 0f)
    {
        if (isLaunched || !gameObject.activeInHierarchy) return;
        isLaunched = true;
        StopAllCoroutines();
        StartCoroutine(DeathLaunchRoutine(impactOrigin, force, Mathf.Min(lift, 0.5f)));
    }

    private IEnumerator DeathLaunchRoutine(Vector3 impactOrigin, float force, float lift)
    {
        Vector3 away = (transform.position - impactOrigin).normalized;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;

        float cappedForce = Mathf.Min(Mathf.Max(0f, force), 14f);
        Vector3 impulse = away.normalized * cappedForce;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.linearVelocity = impulse;
        rb.AddTorque(Vector3.up * Random.Range(-cappedForce, cappedForce), ForceMode.Impulse);

        // A short physical impact preserves hit weight, then every enemy dissolves
        // into rising golden ash instead of ending as a plain ragdoll.
        ThanhGiongEnemy enemy = GetComponent<ThanhGiongEnemy>();
        bool isBoss = enemy != null && enemy.isBoss;
        float waitBeforeDissolve = isBoss ? 0.85f : 0.6f;
        yield return new WaitForSeconds(waitBeforeDissolve);

        ThanhGiongGoldenDissolve dissolve = GetComponent<ThanhGiongGoldenDissolve>();
        if (dissolve == null) dissolve = gameObject.AddComponent<ThanhGiongGoldenDissolve>();

        float dissolveDuration = isBoss ? 2.2f : 1.15f;
        int particleCount = isBoss ? 80 : 36;
        Color edgeColor = isBoss ? new Color(1.0f, 0.45f, 0.05f, 1f) : new Color(1.0f, 0.72f, 0.15f, 1f);

        yield return dissolve.Play(dissolveDuration, edgeColor, particleCount);

        gameObject.SetActive(false);
        transform.localScale = originalLocalScale;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isLaunched = false;
    }
}

