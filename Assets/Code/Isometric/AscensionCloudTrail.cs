using UnityEngine;

/// <summary>Creates short-lived cloud footholds under the horse during the final ascent.</summary>
[DisallowMultipleComponent]
public sealed class AscensionCloudTrail : MonoBehaviour
{
    public GameObject bigCloudPrefab;
    public GameObject smallCloudPrefab;
    public Material cloudMaterial;
    [Min(.2f)] public float stepDistance = 1.7f;
    [Min(.1f)] public float minInterval = .28f;
    public float footHeight = -.48f;

    bool active;
    int spawned;
    float lastSpawnTime;
    Vector3 lastSpawnPosition;

    public int SpawnedCount => spawned;

    public void BeginAscent()
    {
        active = true;
        spawned = 0;
        lastSpawnTime = -100f;
        Spawn(transform.position, transform.rotation);
    }

    public void FollowHorse(Vector3 position, Quaternion rotation)
    {
        if (!active || Time.time - lastSpawnTime < minInterval) return;
        if (Vector3.Distance(position, lastSpawnPosition) < stepDistance) return;
        Spawn(position, rotation);
    }

    public void EndAscent() => active = false;

    void Spawn(Vector3 position, Quaternion rotation)
    {
        float side = spawned % 2 == 0 ? -.38f : .38f;
        Vector3 at = position + rotation * new Vector3(side, 0f, .55f) + Vector3.up * footHeight;
        GameObject root = new GameObject("Bậc mây xuất hiện dưới vó ngựa " + (spawned + 1).ToString("00"));
        root.transform.SetParent(null);
        root.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));

        GameObject source = spawned % 3 == 1 ? smallCloudPrefab : bigCloudPrefab;
        if (source == null) source = bigCloudPrefab != null ? bigCloudPrefab : smallCloudPrefab;
        GameObject visual = source != null ? Instantiate(source, root.transform) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Mây trồi lên";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, -.16f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, spawned * 37f, 0f);
        visual.transform.localScale = spawned % 3 == 1 ? new Vector3(1.2f, .68f, 1f) : new Vector3(1.5f, .75f, 1.16f);
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        if (cloudMaterial != null)
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = cloudMaterial;

        for (int i = 0; i < 3; i++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Khói mây lan dưới vó";
            puff.transform.SetParent(visual.transform, false);
            puff.transform.localPosition = new Vector3((i - 1) * 1.25f, -.05f, (i % 2 == 0 ? -.3f : .4f));
            puff.transform.localScale = new Vector3(1.5f, .38f, .95f);
            Destroy(puff.GetComponent<Collider>());
            if (cloudMaterial != null) puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
            AscensionCloudPuffMotion puffMotion = puff.AddComponent<AscensionCloudPuffMotion>();
            puffMotion.phase = spawned * 1.37f + i * 2.4f;
            puffMotion.drift = .16f;
            puffMotion.speed = .72f;
        }

        AscensionCloudStep stepMotion = root.AddComponent<AscensionCloudStep>();
        stepMotion.phase = spawned * .63f;
        stepMotion.bobAmplitude = .09f;
        stepMotion.bobSpeed = .8f;
        stepMotion.sideDrift = .06f;
        stepMotion.Initialize(visual.transform);
        root.AddComponent<AscensionCloudGeneratedStep>();

        lastSpawnPosition = position;
        lastSpawnTime = Time.time;
        spawned++;
    }
}
