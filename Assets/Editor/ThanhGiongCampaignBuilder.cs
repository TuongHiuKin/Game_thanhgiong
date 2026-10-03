using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThanhGiongCampaignBuilder
{
    private const string ScenePath = "Assets/Scenes/AlbionForestMap.unity";
    private const string RootName = "THANH GIONG CAMPAIGN";

    [InitializeOnLoadMethod]
    private static void BuildWhenReady()
    {
        EditorApplication.update -= TryBuild;
        EditorApplication.update += TryBuild;
    }

    private static void TryBuild()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;
        EditorApplication.update -= TryBuild;
        GameObject mounted = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Thanh Giong Mounted 3D");
        bool campaignReady = scene.GetRootGameObjects().Any(item => item.name == RootName) &&
                             mounted != null && mounted.GetComponent<ThanhGiongCampaignAudio>() != null;
        if (campaignReady) return;
        try { BuildCampaign(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("Tools/Thanh Giong/Build Four-Chapter Campaign")]
    public static void BuildCampaign()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RemoveRoot(scene, RootName);

        GameObject player = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Thanh Giong Mounted 3D");
        if (player == null) throw new InvalidOperationException("Missing Thanh Giong Mounted 3D. Rebuild the mounted asset first.");

        Transform spawnMarker = FindTransform(scene, "Player Spawn");
        Vector3 spawn = spawnMarker != null ? spawnMarker.position + Vector3.up * .08f : player.transform.position;
        player.transform.SetPositionAndRotation(spawn, Quaternion.Euler(0f, 40f, 0f));

        ThanhGiongCampaignController old = player.GetComponent<ThanhGiongCampaignController>();
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        ThanhGiongCampaignController campaign = player.AddComponent<ThanhGiongCampaignController>();
        ThanhGiongCampaignAudio oldAudio = player.GetComponent<ThanhGiongCampaignAudio>();
        if (oldAudio != null) UnityEngine.Object.DestroyImmediate(oldAudio);
        player.AddComponent<ThanhGiongCampaignAudio>();

        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        ThanhGiongCampaignVFX vfx = root.AddComponent<ThanhGiongCampaignVFX>();
        ThanhGiongCampaignHUD hud = root.AddComponent<ThanhGiongCampaignHUD>();
        FantasyUIInstaller.Apply(hud);
        campaign.vfx = vfx;
        campaign.hud = hud;
        hud.campaign = campaign;

        Material food = CreateMaterial("Campaign Food", new Color(.95f, .63f, .12f));
        Material green = CreateMaterial("Campaign Bamboo", new Color(.15f, .55f, .16f));
        Material enemy = CreateMaterial("Campaign Enemy", new Color(.45f, .07f, .08f));
        Material boss = CreateMaterial("Campaign Boss", new Color(.12f, .02f, .025f));
        Material village = CreateMaterial("Campaign Village", new Color(.42f, .22f, .08f));
        Material fire = CreateMaterial("Campaign Fire", new Color(1f, .24f, .015f), true);

        GameObject prologue = Child(root, "Màn 1 - Làng Gióng");
        BuildVillage(prologue.transform, spawn, village);
        BuildFoodRoute(prologue.transform, spawn, food);

        Vector3 arenaCenter = spawn + new Vector3(30f, 0f, 26f);
        GameObject battle = Child(root, "Màn 3 - Trận tuyến Núi Sóc");
        BuildBattlefield(battle.transform, arenaCenter, enemy, boss, village, fire);
        campaign.battleRoot = battle;

        GameObject bamboo = BuildBamboo(root.transform, spawn + new Vector3(15f, 0f, 16f), green);
        campaign.bambooRoot = bamboo;

        GameObject sky = new GameObject("Điểm hóa thánh trên mây");
        sky.transform.SetParent(root.transform);
        sky.transform.position = arenaCenter + new Vector3(45f, 48f, 45f);
        campaign.ascensionTarget = sky.transform;
        BuildClouds(root.transform, sky.transform.position, fire);

        Camera camera = Camera.main;
        if (camera != null)
        {
            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>();
            if (follow == null) follow = camera.gameObject.AddComponent<IsometricCameraFollow>();
            follow.target = player.transform;
            follow.offset = new Vector3(-15f, 18f, -15f);
            follow.lookHeight = 1.7f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("[Thanh Giong] Four-chapter playable campaign created. WASD/Shift/Space/F/E/Q.");
    }

    private static void BuildFoodRoute(Transform parent, Vector3 origin, Material material)
    {
        ThanhGiongCollectible.Kind[] kinds =
        {
            ThanhGiongCollectible.Kind.Rice, ThanhGiongCollectible.Kind.Eggplant,
            ThanhGiongCollectible.Kind.Firewood, ThanhGiongCollectible.Kind.Meat
        };
        Vector3[] points =
        {
            new Vector3(3,0,1), new Vector3(6,0,4), new Vector3(4,0,8), new Vector3(0,0,10),
            new Vector3(-5,0,8), new Vector3(-8,0,4), new Vector3(-5,0,-1), new Vector3(0,0,-4),
            new Vector3(8,0,-3), new Vector3(12,0,1), new Vector3(12,0,7), new Vector3(8,0,12)
        };
        for (int i = 0; i < points.Length; i++)
        {
            GameObject item = Primitive(PrimitiveType.Cube, "Lương thực - " + kinds[i % kinds.Length], parent, origin + points[i] + Vector3.up * .55f, new Vector3(.62f, .62f, .62f), material);
            BoxCollider collider = item.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            ThanhGiongCollectible collectible = item.AddComponent<ThanhGiongCollectible>();
            collectible.kind = kinds[i % kinds.Length];
            collectible.foodValue = 25f;
        }
    }

    private static void BuildVillage(Transform parent, Vector3 origin, Material material)
    {
        for (int i = 0; i < 5; i++)
        {
            float angle = i / 5f * Mathf.PI * 2f;
            Vector3 p = origin + new Vector3(Mathf.Cos(angle) * 13f, 1f, Mathf.Sin(angle) * 13f);
            Primitive(PrimitiveType.Cube, "Nhà tranh " + (i + 1), parent, p, new Vector3(4f, 2f, 3.2f), material);
            GameObject roof = Primitive(PrimitiveType.Cube, "Mái tranh", parent, p + Vector3.up * 1.45f, new Vector3(4.8f, .35f, 4f), material);
            roof.transform.rotation = Quaternion.Euler(0f, i * 35f, 6f);
        }
    }

    private static void BuildBattlefield(Transform parent, Vector3 center, Material enemyMaterial, Material bossMaterial, Material campMaterial, Material fireMaterial)
    {
        for (int i = 0; i < 22; i++)
        {
            float a = i * 2.399963f;
            float r = 7f + (i % 6) * 2.25f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 1f, Mathf.Sin(a) * r);
            bool isBoss = i == 21;
            GameObject foe = Primitive(PrimitiveType.Capsule, isBoss ? "Tướng Giặc Ân" : "Giáo binh Ân " + (i + 1), parent, p, isBoss ? new Vector3(2.1f, 2.5f, 2.1f) : new Vector3(1f, 1.25f, 1f), isBoss ? bossMaterial : enemyMaterial);
            ThanhGiongEnemy agent = foe.AddComponent<ThanhGiongEnemy>();
            agent.isBoss = isBoss;
            agent.maxHealth = isBoss ? 180f : 55f;
            agent.moveSpeed = isBoss ? 1.8f : 2.35f;
            GameObject spear = Primitive(PrimitiveType.Cylinder, "Giáo", foe.transform, foe.transform.position + new Vector3(.5f, .55f, 0f), new Vector3(.08f, 1.6f, .08f), campMaterial);
            spear.transform.localPosition = new Vector3(.55f, .3f, 0f);
            spear.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }

        for (int i = 0; i < 4; i++)
        {
            Vector3 p = center + new Vector3((i < 2 ? -1 : 1) * 17f, 1.5f, (i % 2 == 0 ? -1 : 1) * 13f);
            Primitive(PrimitiveType.Cube, "Đồn trại giặc", parent, p, new Vector3(4f, 3f, 4f), campMaterial);
            Primitive(PrimitiveType.Sphere, "Lửa Làng Cháy", parent, p + Vector3.up * 2.3f, new Vector3(1.2f, 2.4f, 1.2f), fireMaterial);
        }
    }

    private static GameObject BuildBamboo(Transform parent, Vector3 position, Material material)
    {
        GameObject root = Child(parent.gameObject, "Khóm Tre Ngà");
        root.transform.position = position;
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            GameObject stalk = Primitive(PrimitiveType.Cylinder, "Tre ngà", root.transform, position + new Vector3(Mathf.Cos(a) * .7f, 2.8f, Mathf.Sin(a) * .7f), new Vector3(.14f, 2.8f + i * .12f, .14f), material);
            if (i == 0)
            {
                CapsuleCollider collider = stalk.GetComponent<CapsuleCollider>();
                collider.isTrigger = true;
                collider.radius = 4f;
                ThanhGiongCollectible collectible = stalk.AddComponent<ThanhGiongCollectible>();
                collectible.kind = ThanhGiongCollectible.Kind.Bamboo;
                collectible.foodValue = 0f;
            }
        }
        return root;
    }

    private static void BuildClouds(Transform parent, Vector3 center, Material material)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = center + new Vector3((i - 4) * 4f, Mathf.Sin(i) * 2f, (i % 3) * 4f);
            GameObject cloud = Primitive(PrimitiveType.Sphere, "Mây hóa thánh", parent, p, new Vector3(5f, 1.4f, 3f), material);
            Collider collider = cloud.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static Material CreateMaterial(string name, Color color, bool emission = false)
    {
        const string folder = "Assets/Materials/Campaign";
        Directory.CreateDirectory(folder);
        string path = folder + "/" + name.Replace(" ", "_") + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.2f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Transform FindTransform(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            if (found != null) return found;
        }
        return null;
    }

    private static void RemoveRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) UnityEngine.Object.DestroyImmediate(root);
    }
}
