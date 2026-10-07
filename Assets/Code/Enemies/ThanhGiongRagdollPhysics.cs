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

    public void ApplyKnockback(Vector3 impactPoint, Vector3 direction, float force, float upwardLift = 1.4f)
    {
        if (isLaunched || !gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impactPoint, direction, force, upwardLift));
    }

    private IEnumerator KnockbackRoutine(Vector3 impactPoint, Vector3 direction, float force, float upwardLift)
    {
        // Unfreeze constraints during impact to allow natural ragdoll tumble
        rb.constraints = RigidbodyConstraints.None;
        Vector3 launchDir = ((transform.position - impactPoint).normalized + Vector3.up * upwardLift).normalized;
        rb.linearVelocity = launchDir * force;
        rb.AddTorque(Random.insideUnitSphere * (force * 3f), ForceMode.Impulse);

        yield return new WaitForSeconds(0.4f);

        // Recover and smoothly return to upright stance
        float recoverTimer = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion uprightRot = Quaternion.Euler(0f, startRot.eulerAngles.y, 0f);

        while (recoverTimer < 0.45f && !isLaunched)
        {
            recoverTimer += Time.deltaTime;
            float t = recoverTimer / 0.45f;
            transform.rotation = Quaternion.Slerp(startRot, uprightRot, t);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, t);
            yield return null;
        }

        if (!isLaunched)
        {
            transform.rotation = uprightRot;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void LaunchOnDeath(Vector3 impactOrigin, float force = 18f, float lift = 4.2f)
    {
        if (isLaunched || !gameObject.activeInHierarchy) return;
        isLaunched = true;
        StopAllCoroutines();
        StartCoroutine(DeathLaunchRoutine(impactOrigin, force, lift));
    }

    private IEnumerator DeathLaunchRoutine(Vector3 impactOrigin, float force, float lift)
    {
        rb.constraints = RigidbodyConstraints.None;
        Vector3 away = (transform.position - impactOrigin).normalized;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;

        Vector3 impulse = (away + Vector3.up * lift).normalized * force;
        rb.linearVelocity = impulse;
        rb.AddTorque(Random.insideUnitSphere * (force * 3.5f), ForceMode.Impulse);

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
