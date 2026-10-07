using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class TreeRedesignBuilder
{
    private const string FoliageFolder = "Assets/Prefabs/Environment/Foliage";
    private const string KayKitFbxFolder = "Assets/Plugins/KayKit_Forest_Nature_Pack_1.0_FREE/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)";
    private const string ForestMatPath = "Assets/Materials/World/forest_0FADB5FA.mat";
    private const string BambooMatPath = "Assets/Materials/Campaign/Campaign_Bamboo.mat";

    [MenuItem("Tools/Thanh Giong/Environment/Build Redesigned Tree Prefabs")]
    public static void BuildAllPrefabs()
    {
        EnsureFolder(FoliageFolder);
        Material forestMat = AssetDatabase.LoadAssetAtPath<Material>(ForestMatPath);
        Material bambooMat = AssetDatabase.LoadAssetAtPath<Material>(BambooMatPath);

        BuildPineCluster(forestMat);
        BuildBroadleafCluster(forestMat);
        BuildLayeredCanopyCluster(forestMat);
        BuildPuffyCrownCluster(forestMat);
        BuildBambooGrove(bambooMat, forestMat);
        BuildUndergrowthCluster(forestMat);
        BuildDriftingLeavesVFX();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TreeRedesignBuilder] Successfully built all 7 redesigned tree & foliage prefabs in " + FoliageFolder);
    }

    [MenuItem("Tools/Thanh Giong/Environment/Enrich Scenes with Redesigned Trees and Atmosphere")]
    public static void EnrichScenes()
    {
        BuildAllPrefabs();

        var campaignMaps = new (string path, Vector3 center)[] {
            ("Assets/Scenes/ThanhGiongWorld/LangGiongTienTuyen.unity", new Vector3(0f, 0f, 7f)),
            ("Assets/Scenes/ThanhGiongWorld/KinhThanhRenThep.unity", new Vector3(0f, 0f, -34f)),
            ("Assets/Scenes/ThanhGiongWorld/ThungLungVuotSong.unity", new Vector3(-24f, 0f, -34f)),
            ("Assets/Scenes/ThanhGiongWorld/PhaoDaiNgamQuanAn.unity", new Vector3(16f, 0f, -16f)),
            ("Assets/Scenes/ThanhGiongWorld/TranTuyenNuiSoc.unity", new Vector3(0f, 0f, -38f)),
            ("Assets/Scenes/ThanhGiongWorld/DinhSocHoaThanh.unity", new Vector3(-10f, 0f, -27f)),
            ("Assets/Scenes/AlbionForestMap.unity", new Vector3(25f, 0f, 20f))
        };

        foreach (var map in campaignMaps)
        {
            EnrichScene(map.path, map.center);
        }

        EditorSceneManager.OpenScene("Assets/Scenes/ThanhGiongWorld/LangGiongTienTuyen.unity");
        Debug.Log("[TreeRedesignBuilder] Successfully enriched all 7 maps with redesigned tree clusters, bamboo groves, and wind atmosphere!");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    private static Mesh LoadMesh(string fbxName)
    {
        string p = $"{KayKitFbxFolder}/{fbxName}.fbx";
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if (go == null) return null;
        var mf = go.GetComponentInChildren<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    private static GameObject CreateSubMesh(GameObject parent, string name, string fbxName, Vector3 localPos, Vector3 localRot, Vector3 localScale, Material mat)
    {
        Mesh mesh = LoadMesh(fbxName);
        if (mesh == null) return null;

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPos;
        child.transform.localRotation = Quaternion.Euler(localRot);
        child.transform.localScale = localScale;

        var mf = child.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = child.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        return child;
    }

    // 1. Pine / Conifer Cluster
    private static void BuildPineCluster(Material mat)
    {
        GameObject root = new GameObject("TreeCluster_Pine_Mossy");

        // Main hero conifer
        CreateSubMesh(root, "Hero_Conifer", "Tree_1_A_Color1", Vector3.zero, Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f), mat);
        // Leaning companion conifer
        CreateSubMesh(root, "Companion_Conifer", "Tree_1_C_Color1", new Vector3(1.1f, 0f, 0.4f), new Vector3(3f, 48f, -4f), new Vector3(0.85f, 0.85f, 0.85f), mat);
        // Skirt bushes
        CreateSubMesh(root, "Skirt_Bush_A", "Bush_1_A_Color1", new Vector3(-0.85f, 0f, 0.6f), new Vector3(0f, 120f, 0f), new Vector3(0.9f, 0.9f, 0.9f), mat);
        CreateSubMesh(root, "Skirt_Bush_B", "Bush_2_E_Color1", new Vector3(0.8f, 0f, -0.7f), new Vector3(0f, -65f, 0f), new Vector3(0.75f, 0.75f, 0.75f), mat);
        // Anchor stone
        CreateSubMesh(root, "Anchor_Rock", "Rock_1_A_Color1", new Vector3(-0.6f, 0f, -0.6f), new Vector3(10f, 25f, 5f), new Vector3(0.65f, 0.65f, 0.65f), mat);
        // Wild grass
        CreateSubMesh(root, "Grass_Tuft_A", "Grass_1_A_Color1", new Vector3(0.5f, 0f, 0.9f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), mat);
        CreateSubMesh(root, "Grass_Tuft_B", "Grass_2_B_Color1", new Vector3(-1.1f, 0f, -0.2f), new Vector3(0f, 75f, 0f), new Vector3(1.1f, 1.1f, 1.1f), mat);

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0.15f, 2.2f, 0.1f);
        col.radius = 0.65f;
        col.height = 4.5f;

        SavePrefab(root, $"{FoliageFolder}/TreeCluster_Pine_Mossy.prefab");
    }

    // 2. Broadleaf Oak Cluster
    private static void BuildBroadleafCluster(Material mat)
    {
        GameObject root = new GameObject("TreeCluster_Broadleaf_Oak");

        // Main hero oak
        CreateSubMesh(root, "Hero_Oak", "Tree_2_B_Color1", Vector3.zero, Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), mat);
        // Leaning sub-tree
        CreateSubMesh(root, "Sibling_Tree", "Tree_2_D_Color1", new Vector3(-1.25f, 0f, 0.3f), new Vector3(-4f, 85f, 6f), new Vector3(0.82f, 0.82f, 0.82f), mat);
        // Low shrubs
        CreateSubMesh(root, "Low_Bush_A", "Bush_3_B_Color1", new Vector3(0.75f, 0f, 0.8f), new Vector3(0f, 40f, 0f), new Vector3(0.95f, 0.95f, 0.95f), mat);
        CreateSubMesh(root, "Low_Bush_B", "Bush_4_C_Color1", new Vector3(-0.5f, 0f, -0.9f), new Vector3(0f, -110f, 0f), new Vector3(0.85f, 0.85f, 0.85f), mat);
        // Embedded stone
        CreateSubMesh(root, "Moss_Stone", "Rock_3_E_Color1", new Vector3(0.9f, 0f, -0.5f), new Vector3(5f, 30f, 0f), new Vector3(0.7f, 0.7f, 0.7f), mat);
        // Wild grass
        CreateSubMesh(root, "Grass_Tuft", "Grass_2_D_Color1", new Vector3(-0.9f, 0f, 0.9f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f), mat);

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(-0.1f, 1.8f, 0.05f);
        col.radius = 0.75f;
        col.height = 3.8f;

        SavePrefab(root, $"{FoliageFolder}/TreeCluster_Broadleaf_Oak.prefab");
    }

    // 3. Layered Canopy Grand Cluster
    private static void BuildLayeredCanopyCluster(Material mat)
    {
        GameObject root = new GameObject("TreeCluster_LayeredCanopy_Grand");

        CreateSubMesh(root, "Hero_GrandCanopy", "Tree_3_A_Color1", Vector3.zero, Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f), mat);
        CreateSubMesh(root, "Mid_Canopy_Tree", "Tree_3_C_Color1", new Vector3(1.35f, 0f, -0.4f), new Vector3(3f, -40f, 4f), new Vector3(0.88f, 0.88f, 0.88f), mat);
        CreateSubMesh(root, "Layered_Bush_A", "Bush_2_A_Color1", new Vector3(-0.9f, 0f, 0.7f), new Vector3(0f, 60f, 0f), new Vector3(0.9f, 0.9f, 0.9f), mat);
        CreateSubMesh(root, "Layered_Bush_B", "Bush_1_D_Color1", new Vector3(0.5f, 0f, -1.0f), new Vector3(0f, -135f, 0f), new Vector3(0.8f, 0.8f, 0.8f), mat);
        CreateSubMesh(root, "River_Stone", "Rock_1_H_Color1", new Vector3(-0.8f, 0f, -0.6f), new Vector3(0f, 45f, 10f), new Vector3(0.75f, 0.75f, 0.75f), mat);
        CreateSubMesh(root, "Tall_Grass", "Grass_1_C_Color1", new Vector3(0.9f, 0f, 0.8f), Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f), mat);

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0.2f, 2.5f, -0.05f);
        col.radius = 0.8f;
        col.height = 5.2f;

        SavePrefab(root, $"{FoliageFolder}/TreeCluster_LayeredCanopy_Grand.prefab");
    }

    // 4. Puffy Crown Painterly Cluster
    private static void BuildPuffyCrownCluster(Material mat)
    {
        GameObject root = new GameObject("TreeCluster_PuffyCrown_Painterly");

        CreateSubMesh(root, "Hero_PuffyTree", "Tree_4_A_Color1", Vector3.zero, Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f), mat);
        CreateSubMesh(root, "Companion_Puffy", "Tree_4_C_Color1", new Vector3(-1.15f, 0f, 0.45f), new Vector3(-2f, 110f, 5f), new Vector3(0.9f, 0.9f, 0.9f), mat);
        CreateSubMesh(root, "Undergrowth_A", "Bush_2_E_Color1", new Vector3(0.85f, 0f, 0.6f), new Vector3(0f, 30f, 0f), new Vector3(0.85f, 0.85f, 0.85f), mat);
        CreateSubMesh(root, "Undergrowth_B", "Bush_3_B_Color1", new Vector3(-0.6f, 0f, -0.8f), new Vector3(0f, -80f, 0f), new Vector3(0.9f, 0.9f, 0.9f), mat);
        CreateSubMesh(root, "Moss_Boulder", "Rock_3_M_Color1", new Vector3(0.7f, 0f, -0.7f), new Vector3(8f, -20f, 0f), new Vector3(0.65f, 0.65f, 0.65f), mat);
        CreateSubMesh(root, "Wild_Grass", "Grass_2_B_Color1", new Vector3(-0.85f, 0f, 0.9f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), mat);

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(-0.1f, 2.1f, 0.1f);
        col.radius = 0.7f;
        col.height = 4.4f;

        SavePrefab(root, $"{FoliageFolder}/TreeCluster_PuffyCrown_Painterly.prefab");
    }

    // 5. Authentic Vietnamese Bamboo Grove
    private static void BuildBambooGrove(Material bambooMat, Material foliageMat)
    {
        GameObject root = new GameObject("BambooGrove_Vietnamese_Legend");

        // Stalk heights and curve offsets
        float[] angles = { -65f, -38f, -12f, 15f, 42f, 70f };
        float[] radii = { 1.1f, 0.9f, 0.75f, 0.8f, 1.0f, 1.25f };
        float[] heights = { 4.8f, 5.8f, 6.5f, 6.2f, 5.3f, 4.2f };
        float[] tilts = { 6f, 4f, 2f, 3f, 5f, 7f };

        for (int i = 0; i < angles.Length; i++)
        {
            float rad = angles[i] * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(rad) * radii[i], 0f, Mathf.Cos(rad) * radii[i]);

            GameObject stalk = new GameObject($"BambooStalk_{i + 1}");
            stalk.transform.SetParent(root.transform, false);
            stalk.transform.localPosition = pos;
            stalk.transform.localRotation = Quaternion.Euler(tilts[i] * Mathf.Cos(rad), angles[i], tilts[i] * Mathf.Sin(rad));

            // Generate segmented stalk nodes
            int segments = Mathf.RoundToInt(heights[i] / 0.85f);
            float segH = heights[i] / segments;
            for (int s = 0; s < segments; s++)
            {
                float t = (float)s / segments;
                float radius = Mathf.Lerp(0.09f, 0.055f, t);

                GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                node.name = $"Segment_{s}";
                node.transform.SetParent(stalk.transform, false);
                node.transform.localPosition = new Vector3(0f, (s + 0.5f) * segH, 0f);
                node.transform.localScale = new Vector3(radius * 2f, segH * 0.49f, radius * 2f);

                var mr = node.GetComponent<MeshRenderer>();
                mr.sharedMaterial = bambooMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;

                Collider c = node.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);

                // Add leaf branches at upper segments
                if (s >= segments / 2)
                {
                    GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    branch.name = $"LeafBranch_{s}";
                    branch.transform.SetParent(node.transform, false);
                    branch.transform.localPosition = new Vector3(radius * 1.2f, 0f, 0f);
                    branch.transform.localRotation = Quaternion.Euler(35f + s * 15f, s * 95f, -25f);
                    branch.transform.localScale = new Vector3(1.8f, 1.2f, 1.0f);

                    var bmr = branch.GetComponent<MeshRenderer>();
                    bmr.sharedMaterial = bambooMat;
                    Collider bc = branch.GetComponent<Collider>();
                    if (bc != null) UnityEngine.Object.DestroyImmediate(bc);
                }
            }

            // Young bamboo shoots at base
            if (i == 1 || i == 3 || i == 5)
            {
                GameObject shoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shoot.name = $"YoungShoot_{i}";
                shoot.transform.SetParent(root.transform, false);
                shoot.transform.localPosition = pos + new Vector3(0.25f, 0f, -0.2f);
                shoot.transform.localRotation = Quaternion.Euler(8f, angles[i] + 45f, 0f);
                shoot.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);

                var smr = shoot.GetComponent<MeshRenderer>();
                smr.sharedMaterial = bambooMat;
                Collider sc = shoot.GetComponent<Collider>();
                if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
            }
        }

        // Understory shrubs and grass beneath bamboo
        CreateSubMesh(root, "Bamboo_Fern_A", "Bush_1_A_Color1", new Vector3(-0.4f, 0f, 0.2f), new Vector3(0f, 45f, 0f), new Vector3(0.7f, 0.7f, 0.7f), foliageMat);
        CreateSubMesh(root, "Bamboo_Fern_B", "Bush_2_A_Color1", new Vector3(0.5f, 0f, 0.3f), new Vector3(0f, -70f, 0f), new Vector3(0.65f, 0.65f, 0.65f), foliageMat);
        CreateSubMesh(root, "Bamboo_Reeds", "Grass_1_A_Color1", new Vector3(0f, 0f, -0.5f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), foliageMat);

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 2.5f, 0.4f);
        col.radius = 1.1f;
        col.height = 5.2f;

        SavePrefab(root, $"{FoliageFolder}/BambooGrove_Vietnamese_Legend.prefab");
    }

    // 6. Undergrowth Wild Cluster
    private static void BuildUndergrowthCluster(Material mat)
    {
        GameObject root = new GameObject("FoliageCluster_Undergrowth_Wild");

        CreateSubMesh(root, "Center_Fern", "Bush_1_A_Color1", Vector3.zero, Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f), mat);
        CreateSubMesh(root, "Flower_Shrub", "Bush_2_A_Color1", new Vector3(0.6f, 0f, 0.3f), new Vector3(0f, 75f, 0f), new Vector3(0.85f, 0.85f, 0.85f), mat);
        CreateSubMesh(root, "Dense_Bush", "Bush_3_B_Color1", new Vector3(-0.55f, 0f, -0.25f), new Vector3(0f, -120f, 0f), new Vector3(0.8f, 0.8f, 0.8f), mat);
        CreateSubMesh(root, "Wild_Reeds_A", "Grass_1_C_Color1", new Vector3(-0.3f, 0f, 0.55f), new Vector3(0f, 30f, 0f), new Vector3(1.3f, 1.3f, 1.3f), mat);
        CreateSubMesh(root, "Wild_Reeds_B", "Grass_2_D_Color1", new Vector3(0.45f, 0f, -0.45f), new Vector3(0f, -45f, 0f), new Vector3(1.2f, 1.2f, 1.2f), mat);
        CreateSubMesh(root, "Edge_Pebble", "Rock_1_A_Color1", new Vector3(0.2f, 0f, -0.6f), new Vector3(0f, 20f, 10f), new Vector3(0.5f, 0.5f, 0.5f), mat);

        SavePrefab(root, $"{FoliageFolder}/FoliageCluster_Undergrowth_Wild.prefab");
    }

    // 7. Ambient Drifting Leaves VFX
    private static void BuildDriftingLeavesVFX()
    {
        GameObject root = new GameObject("PFX_Forest_DriftingLeaves");

        var ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new Color(0.82f, 0.90f, 0.45f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = 8.0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(32f, 6f, 32f);
        shape.position = new Vector3(0f, 6f, 0f);

        // Wind velocity
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.4f, -0.15f);
        vel.z = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);

        // Leaf tumbling noise
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.45f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.5f;

        // Color and Alpha fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.92f, 0.95f, 0.55f), 0f),
                new GradientColorKey(new Color(0.65f, 0.85f, 0.38f), 0.5f),
                new GradientColorKey(new Color(0.85f, 0.70f, 0.30f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.2f),
                new GradientAlphaKey(0.70f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        Shader defaultSpriteShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        if (defaultSpriteShader != null)
        {
            Material partMat = new Material(defaultSpriteShader) { name = "M_DriftingLeaves" };
            partMat.SetColor("_BaseColor", Color.white);
            renderer.sharedMaterial = partMat;
        }

        SavePrefab(root, $"{FoliageFolder}/PFX_Forest_DriftingLeaves.prefab");
    }

    private static void SavePrefab(GameObject instance, string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        else
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        }
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void EnrichScene(string scenePath, Vector3 center)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);
        if (!scene.IsValid()) return;

        // Clean existing generated cluster root if already present
        GameObject existing = GameObject.Find("Stylized Foliage Clusters");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        GameObject root = new GameObject("Stylized Foliage Clusters");

        GameObject pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/TreeCluster_Pine_Mossy.prefab");
        GameObject oakPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/TreeCluster_Broadleaf_Oak.prefab");
        GameObject grandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/TreeCluster_LayeredCanopy_Grand.prefab");
        GameObject puffyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/TreeCluster_PuffyCrown_Painterly.prefab");
        GameObject bambooPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/BambooGrove_Vietnamese_Legend.prefab");
        GameObject undergrowthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/FoliageCluster_Undergrowth_Wild.prefab");
        GameObject leavesVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FoliageFolder}/PFX_Forest_DriftingLeaves.prefab");

        GameObject[] treePrefabs = { pinePrefab, oakPrefab, grandPrefab, puffyPrefab };

        System.Random rng = new System.Random(scenePath.GetHashCode());

        // 1. Foreground Framing Trees (High diagonal 2.5D camera view looking toward +Z/+X)
        Vector3[] foregroundOffsets = new Vector3[] {
            new Vector3(-8.5f, 0f, -8f),
            new Vector3(8.5f, 0f, -7.5f),
            new Vector3(-12.5f, 0f, 4.5f)
        };

        foreach (var offset in foregroundOffsets)
        {
            GameObject p = treePrefabs[rng.Next(treePrefabs.Length)];
            if (p == null) continue;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(p);
            inst.transform.SetParent(root.transform, false);
            inst.transform.position = center + offset;
            inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            inst.transform.localScale = Vector3.one * (1.25f + (float)rng.NextDouble() * 0.25f);
        }

        // 2. Pathway Flanking Bamboo Groves
        Vector3[] bambooOffsets = new Vector3[] {
            new Vector3(6.5f, 0f, 8.5f),
            new Vector3(-6.5f, 0f, 10.5f),
            new Vector3(12.5f, 0f, 2.5f)
        };

        if (bambooPrefab != null)
        {
            foreach (var offset in bambooOffsets)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(bambooPrefab);
                inst.transform.SetParent(root.transform, false);
                inst.transform.position = center + offset;
                inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                inst.transform.localScale = Vector3.one * (0.95f + (float)rng.NextDouble() * 0.2f);
            }
        }

        // 3. Island Edge and Trail Undergrowth Clusters (Breaking up bare terrain)
        Vector3[] undergrowthOffsets = new Vector3[] {
            new Vector3(2.5f, 0f, 3.5f),
            new Vector3(-3.5f, 0f, 5.5f),
            new Vector3(4.5f, 0f, -2.5f),
            new Vector3(-5.5f, 0f, -4.5f),
            new Vector3(7.5f, 0f, 5.5f)
        };

        if (undergrowthPrefab != null)
        {
            foreach (var offset in undergrowthOffsets)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(undergrowthPrefab);
                inst.transform.SetParent(root.transform, false);
                inst.transform.position = center + offset;
                inst.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                inst.transform.localScale = Vector3.one * (0.9f + (float)rng.NextDouble() * 0.35f);
            }
        }

        // 4. Ambient Drifting Leaves Particle System
        if (leavesVfxPrefab != null)
        {
            GameObject vfxInst = (GameObject)PrefabUtility.InstantiatePrefab(leavesVfxPrefab);
            vfxInst.name = "Ambient_Drifting_Leaves_VFX";
            vfxInst.transform.SetParent(root.transform, false);
            vfxInst.transform.position = center + new Vector3(0f, 4.5f, 2f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
