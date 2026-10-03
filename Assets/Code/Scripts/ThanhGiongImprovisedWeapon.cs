using System.Collections;
using UnityEngine;

public class ThanhGiongImprovisedWeapon : MonoBehaviour
{
    public enum WeaponType { Boulder, TreeLog }
    public WeaponType type = WeaponType.Boulder;
    public float throwDamage = 120f;
    public float stunDuration = 2.5f;
    public float aoeRadius = 5.5f;

    private bool isCarried;
    public bool IsCarried => isCarried;
    private Transform carrier;
    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        col = GetComponent<Collider>();
    }

    public void PickUp(Transform player)
    {
        isCarried = true;
        carrier = player;
        rb.isKinematic = true;
        if (col != null) col.enabled = false;
        transform.SetParent(player);
        transform.localPosition = new Vector3(0f, 3.2f, 0.4f);
        transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
    }

    public void Throw(Vector3 direction, float force = 18f)
    {
        isCarried = false;
        transform.SetParent(null);
        if (col != null) col.enabled = true;
        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * force + Vector3.up * 4.5f;
        rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
        StartCoroutine(DetectImpact());
    }

    private IEnumerator DetectImpact()
    {
        yield return new WaitForSeconds(0.15f);
        float timer = 0f;
        bool impacted = false;

        while (timer < 4f && !impacted)
        {
            timer += Time.deltaTime;
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.8f);
            foreach (var h in hits)
            {
                if (h.transform == carrier || h.transform.IsChildOf(carrier)) continue;
                if (h.GetComponentInParent<ThanhGiongEnemy>() != null || timer > 1.2f)
                {
                    impacted = true;
                    ExplodeImpact();
                    break;
                }
            }
            yield return null;
        }

        if (!impacted) ExplodeImpact();
    }

    private void ExplodeImpact()
    {
        // Camera shake
        IsometricCameraFollow.Instance?.Shake(0.4f, 0.8f);

        // AOE Damage & Stun
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius);
        foreach (var h in hits)
        {
            ThanhGiongEnemy enemy = h.GetComponentInParent<ThanhGiongEnemy>();
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(throwDamage, stunDuration);
            }
        }

        // VFX Ring
        ThanhGiongCampaignVFX vfx = FindAnyObjectByType<ThanhGiongCampaignVFX>();
        vfx?.PlayGrowth(transform);

        Destroy(gameObject, 0.2f);
    }
}
