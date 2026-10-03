using System.Collections;
using UnityEngine;

public class ThanhGiongRoofCollapse : MonoBehaviour
{
    private bool collapsed = false;
    private Vector3 initialPos;
    private Quaternion initialRot;

    private void Awake()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    public void Collapse()
    {
        if (collapsed) return;
        collapsed = true;
        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        // Add random initial tilt and drop
        float duration = 0.85f;
        float elapsed = 0f;
        Vector3 targetPos = initialPos + new Vector3(Random.Range(-0.8f, 0.8f), -1.8f, Random.Range(-0.8f, 0.8f));
        Quaternion targetRot = initialRot * Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(-30f, 30f), Random.Range(18f, 35f));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Bounce/gravity easing
            float curve = t * t;
            transform.position = Vector3.Lerp(initialPos, targetPos, curve);
            transform.rotation = Quaternion.Slerp(initialRot, targetRot, t);
            yield return null;
        }

        // Small dust burst or line puff
        ThanhGiongCampaignVFX vfx = FindAnyObjectByType<ThanhGiongCampaignVFX>();
        if (vfx != null)
        {
            vfx.PlayGrowth(transform);
        }
    }
}
