using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Triển khai bản đồ chiến dịch đầu tiên (AlbionForestMap) với toàn bộ tài nguyên
/// phong phú từ gói KayKit - Forest Nature Pack (Trees, Rocks, Bushes, Grass, Cliffs).
/// </summary>
public static class AlbionInspiredMapGenerator
{
    private const string ScenePath = "Assets/Scenes/AlbionForestMap.unity";
    private const string MaterialFolder = "Assets/Materials/World";
    private const string ModelFolder = "Assets/Plugins/KayKit_Forest_Nature_Pack_1.0_FREE/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)";
    private const int Seed = 20260929;

    private static Material s_forestMaterial;

    [InitializeOnLoadMethod]
    private static void RegisterUpdate()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            TryPopulateNatureInScene(false);
        };
    }

    private static void UpdateCheck()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= UpdateCheck;
        Debug.Log("[AlbionMap] Executing KayKit Forest Nature deployment check...");
        try
        {
            TryPopulateNatureInScene(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [MenuItem("Tools/Thanh Giong/Populate KayKit Forest Nature into Map")]
    public static void ManualPopulate()
    {
        TryPopulateNatureInScene(true);
    }

    [MenuItem("Tools/Thanh Giong/Remap KayKit Forest Models to URP Material")]
    public static void ManualRemapModels()
    {
        RemapAllKayKitForestModelsToUrp();
    }

    [MenuItem("Tools/Thanh Giong/Generate Albion-style Forest Map (Full Rebuild)")]
    public static void GenerateMap()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        RemapAllKayKitForestModelsToUrp();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var palette = CreatePalette();
        var rng = new System.Random(Seed);

        GameObject environment = new GameObject("ENVIRONMENT");
        GameObject terrain = Child(environment, "Terrain");
        GameObject roads = Child(environment, "Roads");
        GameObject water = Child(environment, "Water");
        GameObject nature = Child(environment, "KayKit Nature");
        GameObject landmarks = Child(environment, "Landmarks");

        CreateGround(terrain.transform, palette);
        CreateRoadNetwork(roads.transform, palette, rng);
        CreateLakeAndBridge(water.transform, landmarks.transform, palette);
        CreateCentralClearing(landmarks.transform, palette);
        CreateLightingAndCamera();
        CreateGameplayMarkers();

        PopulateForestNature(nature.transform, rng);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        // Đảm bảo nhân vật và chiến dịch được tạo đầy đủ
        MountedThanhGiongAssetBuilder.Rebuild();
        AnimatedGameAssetBuilder.BuildAndPlace();
        ThanhGiongCampaignBuilder.BuildCampaign();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Thanh Giong] Rebuilt complete Albion-inspired forest map with KayKit Forest Nature at {ScenePath}");
    }

    public static void RemapAllKayKitForestModelsToUrp()
    {
        Material forestMat = GetForestMaterial();
        if (forestMat == null) return;

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "forest"), forestMat);
                importer.SaveAndReimport();
                count++;
            }
        }
        if (count > 0)
        {
            Debug.Log($"[KayKit Nature] Remapped {count} FBX models to URP material: {forestMat.name}");
        }
    }

    public static void TryPopulateNatureInScene(bool force = false)
    {
        // The import-time check must never navigate away from the scene the designer
        // is currently editing. Manual population may still open Albion explicitly.
        if (!force && SceneManager.GetActiveScene().path != ScenePath) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            GenerateMap();
            return;
        }

        RemapAllKayKitForestModelsToUrp();

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject environment = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "ENVIRONMENT");
        if (environment == null)
        {
            environment = new GameObject("ENVIRONMENT");
        }

        Transform natureTransform = environment.transform.Find("KayKit Nature");
        if (natureTransform == null)
        {
            GameObject natureGo = new GameObject("KayKit Nature");
            natureGo.transform.SetParent(environment.transform, false);
            natureTransform = natureGo.transform;
        }

        bool hasStructuredSubfolders = natureTransform.Find("Trees") != null && natureTransform.Find("Boundary Cliffs") != null;
        int totalSubChildren = CountTotalDescendants(natureTransform);
        if (!force && hasStructuredSubfolders && totalSubChildren > 150)
        {
            Debug.Log($"[KayKit Nature] Nature hierarchy already fully populated ({totalSubChildren} elements). Skipping auto-populate.");
            return;
        }

        // Dọn sạch vật thể cũ trong KayKit Nature trước khi sinh mới
        for (int i = natureTransform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(natureTransform.GetChild(i).gameObject);
        }

        var rng = new System.Random(Seed);
        PopulateForestNature(natureTransform, rng);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[KayKit Nature] Successfully populated {CountTotalDescendants(natureTransform)} nature assets into {ScenePath}!");
    }

    private static int CountTotalDescendants(Transform root)
    {
        int count = 0;
        foreach (Transform child in root)
        {
            count += 1 + CountTotalDescendants(child);
        }
        return count;
    }

    public static void PopulateForestNature(Transform natureRoot, System.Random rng = null)
    {
        rng ??= new System.Random(Seed);

        Transform treesRoot = Child(natureRoot.gameObject, "Trees").transform;
        Transform bushesRoot = Child(natureRoot.gameObject, "Bushes").transform;
        Transform grassesRoot = Child(natureRoot.gameObject, "Grasses").transform;
        Transform rocksRoot = Child(natureRoot.gameObject, "Rocks").transform;
        Transform cliffsRoot = Child(natureRoot.gameObject, "Boundary Cliffs").transform;
        Transform battleRoot = Child(natureRoot.gameObject, "Battlefield Bare Trees").transform;
        Transform lakeRoot = Child(natureRoot.gameObject, "Lakeside Flora").transform;
        Transform villageRoot = Child(natureRoot.gameObject, "Village Flora").transform;

        // Danh sách tài nguyên FBX KayKit - Forest Nature Pack
        string[] greenTrees =
        {
            "Tree_1_A_Color1", "Tree_1_B_Color1", "Tree_1_C_Color1",
            "Tree_2_A_Color1", "Tree_2_B_Color1", "Tree_2_C_Color1", "Tree_2_D_Color1", "Tree_2_E_Color1",
            "Tree_3_A_Color1", "Tree_3_B_Color1", "Tree_3_C_Color1",
            "Tree_4_A_Color1", "Tree_4_B_Color1", "Tree_4_C_Color1"
        };

        string[] bareTrees =
        {
            "Tree_Bare_1_A_Color1", "Tree_Bare_1_B_Color1", "Tree_Bare_1_C_Color1",
            "Tree_Bare_2_A_Color1", "Tree_Bare_2_B_Color1", "Tree_Bare_2_C_Color1"
        };

        string[] bushes =
        {
            "Bush_1_A_Color1", "Bush_1_B_Color1", "Bush_1_C_Color1", "Bush_1_D_Color1",
            "Bush_2_A_Color1", "Bush_2_B_Color1", "Bush_2_C_Color1", "Bush_2_D_Color1",
            "Bush_3_A_Color1", "Bush_3_B_Color1",
            "Bush_4_A_Color1", "Bush_4_B_Color1", "Bush_4_C_Color1"
        };

        string[] grasses =
        {
            "Grass_1_A_Color1", "Grass_1_B_Color1", "Grass_1_C_Color1", "Grass_1_D_Color1",
            "Grass_2_A_Color1", "Grass_2_B_Color1", "Grass_2_C_Color1", "Grass_2_D_Color1"
        };

        string[] fieldRocks =
        {
            "Rock_1_A_Color1", "Rock_1_C_Color1", "Rock_1_G_Color1", "Rock_1_H_Color1",
            "Rock_2_A_Color1", "Rock_2_B_Color1", "Rock_2_C_Color1", "Rock_2_H_Color1",
            "Rock_3_C_Color1", "Rock_3_E_Color1"
        };

        string[] cliffRocks =
        {
            "Rock_3_A_Color1", "Rock_3_F_Color1", "Rock_3_M_Color1", "Rock_3_R_Color1",
            "Rock_3_H_Color1", "Rock_3_K_Color1", "Rock_3_L_Color1"
        };

        // 1. Rặng vách đá bao quanh thung lũng (Boundary Cliffs)
        CreateBoundaryCliffRing(cliffsRoot, cliffRocks, rng, 46);

        // 2. Cây xanh rừng đại ngàn (Green Valley Trees)
        ScatterNatureGroup(treesRoot, greenTrees, 115, 12f, 45.5f, 0.85f, 1.4f, rng, true);

        // 3. Bụi rậm thảm thực vật (Bushes)
        ScatterNatureGroup(bushesRoot, bushes, 75, 9f, 45f, 0.75f, 1.35f, rng, false);

        // 4. Đồng cỏ hoang & hoa dại (Wild Grass Tufts)
        ScatterNatureGroup(grassesRoot, grasses, 140, 6f, 46f, 0.65f, 1.3f, rng, false);

        // 5. Đá tảng & sỏi đá tự nhiên rải rác (Rocks)
        ScatterNatureGroup(rocksRoot, fieldRocks, 52, 10f, 45.5f, 0.65f, 1.45f, rng, true);

        // 6. Khu vực chiến trường Núi Sóc (Cây khô cháy sém & đá nhọn)
        CreateBattlefieldNature(battleRoot, bareTrees, cliffRocks, rng);

        // 7. Khu vực hồ Nguyệt Đãng (Cỏ nước, lau sậy & đá ven bờ)
        CreateLakesideNature(lakeRoot, grasses, bushes, fieldRocks, rng);

        // 8. Khu vực Làng Phù Đổng (Màn 1: Cây bóng mát, bụi rào & cỏ sân tộc)
        CreateVillageFlora(villageRoot, greenTrees, bushes, grasses, rng);
    }

    private static void CreateVillageFlora(Transform parent, string[] trees, string[] bushes, string[] grasses, System.Random rng)
    {
        Vector3 villageCenter = new Vector3(-39f, 0f, -24f);

        // Cây bóng mát đầu làng và sau các mái tranh
        Vector3[] treeOffsets =
        {
            new Vector3(-9f, 0f, -8f),
            new Vector3(-11f, 0f, 5f),
            new Vector3(8f, 0f, -10f),
            new Vector3(-5f, 0f, 11f),
            new Vector3(10f, 0f, 9f),
            new Vector3(-13f, 0f, -1f)
        };
        for (int i = 0; i < treeOffsets.Length; i++)
        {
            GameObject tree = InstantiateModel(trees[rng.Next(trees.Length)], parent);
            if (tree == null) continue;
            tree.name = "Village Shade Tree " + (i + 1);
            tree.transform.SetPositionAndRotation(villageCenter + treeOffsets[i], Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            tree.transform.localScale = Vector3.one * Range(rng, 1.0f, 1.35f);
            CapsuleCollider c = tree.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.3f, 0f);
            c.height = 2.6f;
            c.radius = 0.42f;
        }

        // Bụi rậm hàng rào làng
        for (int i = 0; i < 14; i++)
        {
            float a = i * Mathf.PI * 2f / 14f;
            float r = Range(rng, 10f, 13.5f);
            Vector3 pos = villageCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (DistanceToRoad(pos) < 3.5f) continue;
            GameObject bush = InstantiateModel(bushes[rng.Next(bushes.Length)], parent);
            if (bush == null) continue;
            bush.name = "Village Hedge Bush " + (i + 1);
            bush.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            bush.transform.localScale = Vector3.one * Range(rng, 0.8f, 1.25f);
        }

        // Khóm cỏ sân làng
        for (int i = 0; i < 18; i++)
        {
            float a = Range(rng, 0f, Mathf.PI * 2f);
            float r = Range(rng, 4.5f, 12f);
            Vector3 pos = villageCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (DistanceToRoad(pos) < 3f) continue;
            GameObject grass = InstantiateModel(grasses[rng.Next(grasses.Length)], parent);
            if (grass == null) continue;
            grass.name = "Village Grass " + (i + 1);
            grass.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            grass.transform.localScale = Vector3.one * Range(rng, 0.7f, 1.1f);
        }
    }

    private static void CreateBoundaryCliffRing(Transform parent, string[] rockModels, System.Random rng, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count + Range(rng, -0.04f, 0.04f);
            float radius = Range(rng, 46.5f, 49.5f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject rock = InstantiateModel(rockModels[rng.Next(rockModels.Length)], parent);
            if (rock == null) continue;
            rock.name = "Cliff Rock " + (i + 1);
            rock.transform.SetPositionAndRotation(pos, Quaternion.Euler(Range(rng, -4, 4), Range(rng, 0, 360), Range(rng, -6, 6)));
            rock.transform.localScale = Vector3.one * Range(rng, 1.7f, 2.7f);

            BoxCollider box = rock.AddComponent<BoxCollider>();
            box.size = new Vector3(2.5f, 4f, 2.5f);
            box.center = new Vector3(0f, 2f, 0f);
        }
    }

    private static void ScatterNatureGroup(Transform parent, string[] names, int targetCount, float minRadius, float maxRadius, float minScale, float maxScale, System.Random rng, bool isSolid)
    {
        int placed = 0;
        int maxAttempts = targetCount * 25;
        int attempts = 0;

        while (placed < targetCount && attempts < maxAttempts)
        {
            attempts++;
            float angle = Range(rng, 0, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Range(rng, minRadius * minRadius, maxRadius * maxRadius));
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            // Kiểm tra khoảng cách tránh đường sá và các khu vực gameplay trọng điểm
            if (IsBlockedByGameplay(pos)) continue;

            GameObject instance = InstantiateModel(names[rng.Next(names.Length)], parent);
            if (instance == null) continue;

            float scale = Range(rng, minScale, maxScale);
            instance.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            instance.transform.localScale = Vector3.one * scale;

            if (isSolid)
            {
                CapsuleCollider c = instance.AddComponent<CapsuleCollider>();
                c.center = new Vector3(0f, 1.25f, 0f);
                c.height = 2.5f;
                c.radius = 0.42f;
            }

            placed++;
        }
    }

    private static void CreateBattlefieldNature(Transform parent, string[] bareTrees, string[] darkRocks, System.Random rng)
    {
        Vector3 battleCenter = new Vector3(30f, 0f, 26f);

        // Vành đai cây khô cháy quanh đấu trường Núi Sóc
        for (int i = 0; i < 18; i++)
        {
            float angle = i * Mathf.PI * 2f / 18f + Range(rng, -0.1f, 0.1f);
            float radius = Range(rng, 15f, 23f);
            Vector3 pos = battleCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            if (pos.magnitude > 46f) continue;

            GameObject tree = InstantiateModel(bareTrees[rng.Next(bareTrees.Length)], parent);
            if (tree == null) continue;
            tree.name = "Battle Bare Tree " + (i + 1);
            tree.transform.SetPositionAndRotation(pos, Quaternion.Euler(Range(rng, -3, 3), Range(rng, 0, 360), Range(rng, -3, 3)));
            tree.transform.localScale = Vector3.one * Range(rng, 0.9f, 1.4f);

            CapsuleCollider c = tree.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.2f, 0f);
            c.height = 2.4f;
            c.radius = 0.38f;
        }

        // Tảng đá trận địa
        for (int i = 0; i < 10; i++)
        {
            float angle = Range(rng, 0f, Mathf.PI * 2f);
            float radius = Range(rng, 16f, 22f);
            Vector3 pos = battleCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            if (pos.magnitude > 46f) continue;

            GameObject rock = InstantiateModel(darkRocks[rng.Next(darkRocks.Length)], parent);
            if (rock == null) continue;
            rock.name = "Battlefield Jagged Rock " + (i + 1);
            rock.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Range(rng, 0, 360), Range(rng, -5, 5)));
            rock.transform.localScale = Vector3.one * Range(rng, 0.8f, 1.5f);

            BoxCollider b = rock.AddComponent<BoxCollider>();
            b.size = new Vector3(1.6f, 1.8f, 1.6f);
            b.center = new Vector3(0f, 0.9f, 0f);
        }
    }

    private static void CreateLakesideNature(Transform parent, string[] grasses, string[] bushes, string[] rocks, System.Random rng)
    {
        Vector3 lakeCenter = new Vector3(-27f, 0f, 17f);

        // Lau sậy ven hồ
        for (int i = 0; i < 28; i++)
        {
            float angle = i * Mathf.PI * 2f / 28f + Range(rng, -0.08f, 0.08f);
            float radiusX = 6.8f + Range(rng, -0.6f, 1.4f);
            float radiusZ = 4.6f + Range(rng, -0.5f, 1.2f);
            Vector3 offset = Quaternion.Euler(0f, -18f, 0f) * new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
            Vector3 pos = lakeCenter + offset;

            // Đặt sát mép hồ
            string model = (i % 3 == 0) ? bushes[rng.Next(bushes.Length)] : grasses[rng.Next(grasses.Length)];
            GameObject plant = InstantiateModel(model, parent);
            if (plant == null) continue;
            plant.name = "Lakeside Flora " + (i + 1);
            plant.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            plant.transform.localScale = Vector3.one * Range(rng, 0.8f, 1.3f);
        }

        // Đá cuội mép nước
        for (int i = 0; i < 12; i++)
        {
            float angle = Range(rng, 0f, Mathf.PI * 2f);
            Vector3 offset = Quaternion.Euler(0f, -18f, 0f) * new Vector3(Mathf.Cos(angle) * 6.5f, 0f, Mathf.Sin(angle) * 4.3f);
            GameObject pebble = InstantiateModel(rocks[rng.Next(rocks.Length)], parent);
            if (pebble == null) continue;
            pebble.name = "Lake Stone " + (i + 1);
            pebble.transform.SetPositionAndRotation(lakeCenter + offset, Quaternion.Euler(0f, Range(rng, 0, 360), 0f));
            pebble.transform.localScale = Vector3.one * Range(rng, 0.4f, 0.85f);
        }
    }

    private static bool IsBlockedByGameplay(Vector3 pos)
    {
        // 1. Tránh đường trục chính
        if (DistanceToRoad(pos) < 5.0f) return true;

        // 2. Tránh khu vực Làng Gióng (Màn 1)
        Vector3 village = new Vector3(-39f, 0f, -24f);
        if (Vector3.Distance(pos, village) < 14f) return true;

        // 3. Tránh khu vực Đấu trường giặc Ân (Màn 3)
        Vector3 battle = new Vector3(30f, 0f, 26f);
        if (Vector3.Distance(pos, battle) < 17f) return true;

        // 4. Tránh lòng hồ Nguyệt Đãng
        Vector3 lake = new Vector3(-27f, 0f, 17f);
        if (Vector3.Distance(pos, lake) < 9.5f) return true;

        // 5. Tránh Đền thờ / Đồi trung tâm
        Vector3 shrine = new Vector3(5f, 0f, 4f);
        if (Vector3.Distance(pos, shrine) < 7.8f) return true;

        // 6. Tránh khóm tre ngà
        Vector3 bamboo = village + new Vector3(15f, 0f, 16f);
        if (Vector3.Distance(pos, bamboo) < 6.5f) return true;

        return false;
    }

    private static float DistanceToRoad(Vector3 p)
    {
        Vector3 a = new Vector3(-46f, 0f, -28f);
        Vector3 b = new Vector3(46f, 0f, 28f);
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        float d1 = Vector3.Distance(p, a + ab * t);

        Vector3 c = new Vector3(-4f, 0f, -3f);
        Vector3 d = new Vector3(14f, 0f, -47f);
        Vector3 cd = d - c;
        float t2 = Mathf.Clamp01(Vector3.Dot(p - c, cd) / cd.sqrMagnitude);
        float d2 = Vector3.Distance(p, c + cd * t2);

        return Mathf.Min(d1, d2);
    }

    private static Material GetForestMaterial()
    {
        if (s_forestMaterial != null) return s_forestMaterial;

        // Ưu tiên load material URP có sẵn
        s_forestMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/forest_0FADB5FA.mat");
        if (s_forestMaterial != null && s_forestMaterial.GetTexture("_BaseMap") != null)
            return s_forestMaterial;

        string[] guids = AssetDatabase.FindAssets("forest_ t:Material", new[] { MaterialFolder });
        foreach (string guid in guids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null && m.GetTexture("_BaseMap") != null)
            {
                s_forestMaterial = m;
                return s_forestMaterial;
            }
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Plugins/KayKit_Forest_Nature_Pack_1.0_FREE/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)/forest_texture.png");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        s_forestMaterial = new Material(shader) { name = "KayKit_Forest_Nature_URP" };
        if (texture != null)
        {
            s_forestMaterial.SetTexture("_BaseMap", texture);
            s_forestMaterial.SetTexture("_MainTex", texture);
        }
        s_forestMaterial.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(s_forestMaterial, MaterialFolder + "/KayKit_Forest_Nature_URP.mat");
        return s_forestMaterial;
    }

    private static GameObject InstantiateModel(string modelName, Transform parent)
    {
        string path = $"{ModelFolder}/{modelName}.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[KayKit Nature] Model not found: {path}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance != null)
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = modelName.Replace("_Color1", string.Empty);
            Material forestMat = GetForestMaterial();
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (forestMat != null)
                {
                    r.sharedMaterial = forestMat;
                }
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }
        return instance;
    }

    private static Dictionary<string, Material> CreatePalette()
    {
        return new Dictionary<string, Material>
        {
            ["grass"] = MaterialAsset("Grass", new Color(0.42f, 0.68f, 0.28f), 0.12f),
            ["grassDark"] = MaterialAsset("GrassDark", new Color(0.18f, 0.36f, 0.14f), 0.08f),
            ["road"] = MaterialAsset("Road", new Color(0.52f, 0.38f, 0.22f), 0.15f),
            ["roadEdge"] = MaterialAsset("RoadEdge", new Color(0.36f, 0.28f, 0.18f), 0.1f),
            ["water"] = MaterialAsset("Water", new Color(0.12f, 0.48f, 0.64f, 0.85f), 0.78f),
            ["wood"] = MaterialAsset("Wood", new Color(0.38f, 0.22f, 0.10f), 0.12f),
            ["stone"] = MaterialAsset("Stone", new Color(0.45f, 0.46f, 0.44f), 0.08f),
            ["banner"] = MaterialAsset("Banner", new Color(0.72f, 0.12f, 0.10f), 0.15f),
        };
    }

    private static Material MaterialAsset(string name, Color color, float smoothness)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        Shader expectedShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (expectedShader != null && material.shader != expectedShader)
            material.shader = expectedShader;

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);

        if (name == "Water")
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateGround(Transform parent, Dictionary<string, Material> p)
    {
        CreatePrimitive("Main Meadow", PrimitiveType.Cube, parent, new Vector3(0, -0.6f, 0), new Vector3(98, 1, 98), p["grass"]);

        Vector3[] darkerPatches =
        {
            new(-36, -0.03f, 31), new(34, -0.03f, 29), new(-35, -0.03f, -30), new(34, -0.03f, -33)
        };
        foreach (Vector3 pos in darkerPatches)
        {
            GameObject patch = CreatePrimitive("Dark Forest Floor", PrimitiveType.Cylinder, parent, pos, new Vector3(18, 0.08f, 16), p["grassDark"]);
            patch.transform.rotation = Quaternion.Euler(0, pos.x + pos.z, 0);
            UnityEngine.Object.DestroyImmediate(patch.GetComponent<Collider>());
        }
    }

    private static void CreateRoadNetwork(Transform parent, Dictionary<string, Material> p, System.Random rng)
    {
        Vector3[] mainRoad =
        {
            new(-46, 0.03f, -28), new(-35, 0.03f, -22), new(-25, 0.03f, -14), new(-14, 0.03f, -8),
            new(-3, 0.03f, -3), new(8, 0.03f, 4), new(18, 0.03f, 12), new(29, 0.03f, 21), new(46, 0.03f, 28)
        };
        CreateRoadSegments(parent, mainRoad, p["road"], 6.2f);

        Vector3[] branchRoad =
        {
            new(-4, 0.025f, -3), new(-1, 0.025f, -14), new(3, 0.025f, -25), new(9, 0.025f, -36), new(14, 0.025f, -47)
        };
        CreateRoadSegments(parent, branchRoad, p["road"], 4.4f);

        for (int i = 0; i < 55; i++)
        {
            float t = (float)rng.NextDouble();
            Vector3 pos = Vector3.Lerp(mainRoad[0], mainRoad[^1], t);
            pos += new Vector3(Range(rng, -2.2f, 2.2f), 0.08f, Range(rng, -2f, 2f));
            GameObject pebble = CreatePrimitive("Road Pebble", PrimitiveType.Cylinder, parent, pos, new Vector3(Range(rng, .12f, .35f), .04f, Range(rng, .12f, .35f)), p["roadEdge"]);
            UnityEngine.Object.DestroyImmediate(pebble.GetComponent<Collider>());
        }
    }

    private static void CreateRoadSegments(Transform parent, Vector3[] points, Material material, float width)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 delta = b - a;
            GameObject segment = CreatePrimitive("Dirt Road", PrimitiveType.Cube, parent, (a + b) * 0.5f, new Vector3(width, .12f, delta.magnitude + 1.2f), material);
            segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            UnityEngine.Object.DestroyImmediate(segment.GetComponent<Collider>());
        }
    }

    private static void CreateLakeAndBridge(Transform waterParent, Transform landmarkParent, Dictionary<string, Material> p)
    {
        GameObject lake = CreatePrimitive("Moon Lake", PrimitiveType.Cylinder, waterParent, new Vector3(-27, -0.03f, 17), new Vector3(13, .12f, 9), p["water"]);
        lake.transform.rotation = Quaternion.Euler(0, -18, 0);
        UnityEngine.Object.DestroyImmediate(lake.GetComponent<Collider>());

        GameObject bridge = Child(landmarkParent.gameObject, "Wooden Bridge");
        for (int i = -4; i <= 4; i++)
            CreatePrimitive("Plank", PrimitiveType.Cube, bridge.transform, new Vector3(-27 + i * 1.15f, .42f, 17), new Vector3(1.05f, .22f, 4.2f), p["wood"]);
        CreatePrimitive("North Rail", PrimitiveType.Cube, bridge.transform, new Vector3(-27, 1.02f, 19.05f), new Vector3(11, .18f, .18f), p["wood"]);
        CreatePrimitive("South Rail", PrimitiveType.Cube, bridge.transform, new Vector3(-27, 1.02f, 14.95f), new Vector3(11, .18f, .18f), p["wood"]);
    }

    private static void CreateCentralClearing(Transform parent, Dictionary<string, Material> p)
    {
        GameObject poi = Child(parent.gameObject, "Traveler Shrine");
        CreatePrimitive("Shrine Base", PrimitiveType.Cylinder, poi.transform, new Vector3(5, .18f, 4), new Vector3(3.8f, .35f, 3.8f), p["stone"]);
        CreatePrimitive("Shrine Pillar", PrimitiveType.Cube, poi.transform, new Vector3(5, 2.1f, 4), new Vector3(1.1f, 3.6f, 1.1f), p["stone"]);
        GameObject crystal = CreatePrimitive("Red Crystal", PrimitiveType.Cube, poi.transform, new Vector3(5, 4.45f, 4), new Vector3(1.3f, 1.8f, 1.3f), p["banner"]);
        crystal.transform.rotation = Quaternion.Euler(0, 45, 0);

        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            Vector3 pos = new Vector3(5 + Mathf.Cos(angle) * 7.5f, .12f, 4 + Mathf.Sin(angle) * 7.5f);
            CreatePrimitive("Clearing Stone", PrimitiveType.Cylinder, poi.transform, pos, new Vector3(.55f, .35f, .55f), p["stone"]);
        }

        CreatePrimitive("Banner Pole", PrimitiveType.Cylinder, poi.transform, new Vector3(10, 2.2f, 6), new Vector3(.12f, 2.2f, .12f), p["wood"]);
        CreatePrimitive("Guild Banner", PrimitiveType.Cube, poi.transform, new Vector3(10, 3.7f, 6), new Vector3(.12f, 1.5f, 1.15f), p["banner"]);
    }

    private static void CreateLightingAndCamera()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.55f, .68f, .75f);
        RenderSettings.ambientEquatorColor = new Color(.32f, .39f, .31f);
        RenderSettings.ambientGroundColor = new Color(.12f, .16f, .10f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.55f, .68f, .62f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 55;
        RenderSettings.fogEndDistance = 110;

        GameObject lightObject = new GameObject("Warm Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .88f, .68f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
        RenderSettings.sun = sun;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 38;
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 220;
        cameraObject.transform.position = new Vector3(-32, 46, -42);
        cameraObject.transform.LookAt(new Vector3(2, 0, 3));
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateGameplayMarkers()
    {
        GameObject gameplay = new GameObject("GAMEPLAY MARKERS");
        GameObject spawn = new GameObject("Player Spawn");
        spawn.transform.SetParent(gameplay.transform);
        spawn.transform.position = new Vector3(-39, .2f, -24);

        GameObject exit = new GameObject("Map Exit - North East");
        exit.transform.SetParent(gameplay.transform);
        exit.transform.position = new Vector3(43, .2f, 27);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
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

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
