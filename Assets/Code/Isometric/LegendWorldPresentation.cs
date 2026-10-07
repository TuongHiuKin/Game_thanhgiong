using UnityEngine;

// Labels belong to nearby interactions; the landscape remains readable at a distance.
public sealed class LegendWorldPresentation : MonoBehaviour
{
    public Transform player;
    public float labelDistance = 9f;
    TextMesh[] labels;
    void Start()
    {
        if (player == null) player = FindFirstObjectByType<ThanhGiongCampaignController>()?.transform;
        labels = FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
    void LateUpdate()
    {
        if (player == null || Time.timeScale <= 0f) return;
        foreach (var label in labels) {
            if (!label) continue;
            var renderer = label.GetComponent<Renderer>();
            if (renderer == null) continue;
            Vector3 delta = label.transform.position - player.position; delta.y = 0;
            renderer.enabled = delta.sqrMagnitude < labelDistance * labelDistance;
        }
    }
    void OnDisable() { if (labels == null) return; foreach (var label in labels) if (label && label.GetComponent<Renderer>()) label.GetComponent<Renderer>().enabled = true; }
}
