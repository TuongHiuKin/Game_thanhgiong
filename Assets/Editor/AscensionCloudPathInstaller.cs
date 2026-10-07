#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AscensionCloudPathInstaller
{
    const string ScenePath = "Assets/Scenes/ThanhGiongWorld/DinhSocHoaThanh.unity";
    const string CloudBigPath = "Assets/Plugins/KayKit_Medieval_Hexagon_Pack_1.0_FREE/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/decoration/nature/cloud_big.fbx";
    const string CloudSmallPath = "Assets/Plugins/KayKit_Medieval_Hexagon_Pack_1.0_FREE/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/decoration/nature/cloud_small.fbx";
    const string MaterialPath = "Assets/Materials/World/Ascension_Cloud_Walkable.mat";

    [MenuItem("Thanh Giong/Install Ascension Cloud Stairway")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ThanhGiongCampaignController campaign = Object.FindFirstObjectByType<ThanhGiongCampaignController>();
        if (campaign == null)
        {
            Debug.LogError("AscensionCloudPathInstaller: no ThanhGiongCampaignController found in DinhSocHoaThanh.");
            return;
        }

        GameObject old = GameObject.Find("Ascension Cloud Stairway — Horse Steps");
        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject("Ascension Cloud Stairway — Horse Steps");
        root.transform.position = Vector3.zero;
        GameObject visualRoot = new GameObject("KayKit cloud visuals");
        visualRoot.transform.SetParent(root.transform, false);
        GameObject colliderRoot = new GameObject("Walkable divine cloud colliders");
        colliderRoot.transform.SetParent(root.transform, false);

        Material cloudMaterial = LoadOrCreateCloudMaterial();
        GameObject bigCloud = AssetDatabase.LoadAssetAtPath<GameObject>(CloudBigPath);
        GameObject smallCloud = AssetDatabase.LoadAssetAtPath<GameObject>(CloudSmallPath);

        Vector3 start = campaign.transform.position + campaign.transform.forward * 5f + Vector3.up * .55f;
        Vector3 end = campaign.ascensionTarget != null ? campaign.ascensionTarget.position : start + new Vector3(42f, 44f, 42f);
        int steps = 26;
        Vector3 previous = start;
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            Vector3 point = CloudCurve(start, end, t);
            Vector3 tangent = i == 0 ? (CloudCurve(start, end, .04f) - point) : point - previous;
            if (tangent.sqrMagnitude < .001f) tangent = Vector3.forward;
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(tangent, Vector3.up).normalized, Vector3.up);

            GameObject step = new GameObject($"Bậc mây cưỡi ngựa {i + 1:00}");
            step.transform.SetParent(root.transform, true);
            step.transform.SetPositionAndRotation(point, rotation);

            BoxCollider platform = step.AddComponent<BoxCollider>();
            platform.size = new Vector3(6.8f, .55f, 4.4f);
            platform.center = new Vector3(0f, -.12f, 0f);

            AscensionCloudStep motion = step.AddComponent<AscensionCloudStep>();
            motion.bobAmplitude = Mathf.Lerp(.08f, .22f, t);
            motion.bobSpeed = Mathf.Lerp(.55f, .95f, t);
            motion.driftDegrees = Mathf.Lerp(1.2f, 3.5f, t);
            motion.phase = i * .63f;

            GameObject cloud = null;
            if ((i % 3 == 1 || i % 5 == 0) && smallCloud != null) cloud = (GameObject)PrefabUtility.InstantiatePrefab(smallCloud, scene);
            else if (bigCloud != null) cloud = (GameObject)PrefabUtility.InstantiatePrefab(bigCloud, scene);
            if (cloud == null) cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = i % 3 == 1 ? "KayKit Cloud Small — mây bậc" : "KayKit Cloud Big — mây bậc";
            cloud.transform.SetParent(step.transform, false);
            cloud.transform.localPosition = new Vector3(0f, -.22f, 0f);
            cloud.transform.localRotation = Quaternion.Euler(0f, (i * 37f) % 360f, 0f);
            cloud.transform.localScale = i % 3 == 1 ? new Vector3(1.25f, .72f, 1.05f) : new Vector3(1.55f, .78f, 1.18f);
            foreach (Collider c in cloud.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Renderer renderer in cloud.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = cloudMaterial;
            motion.visualRoot = cloud.transform;

            // Soft decorative side puffs make the platform read as a broad cloud, while the collider stays simple.
            for (int p = 0; p < 3; p++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Mây phụ nâng vó ngựa";
                puff.transform.SetParent(cloud.transform, false);
                float side = p - 1;
                puff.transform.localPosition = new Vector3(side * 1.45f, -.08f - p * .02f, Mathf.Sin(i + p) * .75f);
                puff.transform.localScale = new Vector3(1.7f - p * .18f, .45f, 1.05f + p * .12f);
                Object.DestroyImmediate(puff.GetComponent<Collider>());
                puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
            }

            previous = point;
        }

        for (int i = 0; i < steps - 1; i++)
        {
            Transform a = root.transform.Find($"Bậc mây cưỡi ngựa {i + 1:00}");
            Transform b = root.transform.Find($"Bậc mây cưỡi ngựa {i + 2:00}");
            if (a == null || b == null) continue;
            CreateRamp(colliderRoot.transform, a.position, b.position, i);
        }

        CreateHeavenGate(root.transform, end, cloudMaterial);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Installed walkable KayKit cloud stairway for Thanh Giong ascension: " + ScenePath);
    }

    static Vector3 CloudCurve(Vector3 start, Vector3 end, float t)
    {
        Vector3 flat = Vector3.Lerp(start, end, t);
        Vector3 side = Vector3.Cross((end - start).normalized, Vector3.up);
        flat += side * Mathf.Sin(t * Mathf.PI * 2f) * 5.5f;
        flat.y = Mathf.Lerp(start.y, end.y, t * t * (3f - 2f * t));
        return flat;
    }

    static void CreateRamp(Transform parent, Vector3 a, Vector3 b, int index)
    {
        Vector3 mid = (a + b) * .5f;
        Vector3 delta = b - a;
        GameObject ramp = new GameObject($"Thiên lộ vô hình nối mây {index + 1:00}");
        ramp.transform.SetParent(parent, true);
        ramp.transform.position = mid;
        ramp.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        BoxCollider collider = ramp.AddComponent<BoxCollider>();
        collider.size = new Vector3(4.8f, .42f, delta.magnitude + 1.6f);
        collider.center = Vector3.zero;
    }

    static void CreateHeavenGate(Transform parent, Vector3 center, Material material)
    {
        GameObject gate = new GameObject("Cổng mây hóa thánh — đích lên trời");
        gate.transform.SetParent(parent, true);
        gate.transform.position = center;
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * Mathf.PI * 2f;
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Vòng mây thiên giới";
            puff.transform.SetParent(gate.transform, false);
            puff.transform.localPosition = new Vector3(Mathf.Cos(a) * 6.5f, Mathf.Sin(i * 1.7f) * 1.1f, Mathf.Sin(a) * 4.8f);
            puff.transform.localScale = new Vector3(2.5f, .78f, 1.55f);
            Object.DestroyImmediate(puff.GetComponent<Collider>());
            puff.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    static Material LoadOrCreateCloudMaterial()
    {
        Directory.CreateDirectory("Assets/Materials/World");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = "Ascension_Cloud_Walkable" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
        if (targetShader != null && material.shader != targetShader) material.shader = targetShader;
        Color cloudColor = new Color(.82f, .92f, 1f, .86f);
        material.color = cloudColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cloudColor);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .38f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
#endif
