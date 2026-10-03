using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlbionInspiredMapGenerator
{
    private const string ScenePath = "Assets/Scenes/AlbionForestMap.unity";
    private const string MaterialFolder = "Assets/Generated/AlbionForest/Materials";
    private const string ModelFolder = "Assets/KayKit_Forest_Nature_Pack_1.0_FREE/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)";
    private const int Seed = 20260929;

    [InitializeOnLoadMethod]
    private static void GenerateOnceAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            GenerateMap();
        };
    }

    [MenuItem("Tools/Thanh Giong/Generate Albion-style Forest Map")]
    public static void GenerateMap()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

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
        ScatterNature(nature.transform, rng);
        CreateBoundaryCliffs(nature.transform, rng);
        CreateLightingAndCamera();
        CreateGameplayMarkers();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created Albion-inspired forest map at {ScenePath}");
    }

    private static Dictionary<string, Material> CreatePalette()
    {
        return new Dictionary<string, Material>
        {
            ["grass"] = MaterialAsset("Grass", new Color(0.25f, 0.46f, 0.18f), 0.1f),
            ["grassDark"] = MaterialAsset("GrassDark", new Color(0.16f, 0.34f, 0.13f), 0.05f),
            ["road"] = MaterialAsset("Road", new Color(0.46f, 0.34f, 0.20f), 0.15f),
            ["roadEdge"] = MaterialAsset("RoadEdge", new Color(0.32f, 0.25f, 0.16f), 0.1f),
            ["water"] = MaterialAsset("Water", new Color(0.08f, 0.42f, 0.56f, 0.82f), 0.75f),
            ["wood"] = MaterialAsset("Wood", new Color(0.34f, 0.18f, 0.08f), 0.12f),
            ["stone"] = MaterialAsset("Stone", new Color(0.42f, 0.43f, 0.40f), 0.08f),
            ["banner"] = MaterialAsset("Banner", new Color(0.62f, 0.09f, 0.08f), 0.15f),
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

        material.color = color;
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
        CreatePrimitive("Main Meadow", PrimitiveType.Cube, parent, new Vector3(0, -0.6f, 0), new Vector3(96, 1, 96), p["grass"]);

        Vector3[] darkerPatches =
        {
            new(-36, -0.03f, 31), new(34, -0.03f, 29), new(-35, -0.03f, -30), new(34, -0.03f, -33)
        };
        foreach (Vector3 pos in darkerPatches)
        {
            GameObject patch = CreatePrimitive("Dark Forest Floor", PrimitiveType.Cylinder, parent, pos, new Vector3(17, 0.08f, 15), p["grassDark"]);
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
        GameObject lake = CreatePrimitive("Moon Lake", PrimitiveType.Cylinder, waterParent, new Vector3(-27, -0.03f, 17), new Vector3(12, .12f, 8), p["water"]);
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
        CreatePrimitive("Shrine Base", PrimitiveType.Cylinder, poi.transform, new Vector3(5, .18f, 4), new Vector3(3.5f, .35f, 3.5f), p["stone"]);
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

    private static void ScatterNature(Transform parent, System.Random rng)
    {
        string[] trees = { "Tree_1_A_Color1", "Tree_1_B_Color1", "Tree_2_A_Color1", "Tree_2_C_Color1", "Tree_3_A_Color1", "Tree_4_B_Color1", "Tree_Bare_1_A_Color1" };
        string[] bushes = { "Bush_1_A_Color1", "Bush_1_D_Color1", "Bush_2_B_Color1", "Bush_3_A_Color1", "Bush_4_C_Color1" };
        string[] grasses = { "Grass_1_A_Color1", "Grass_1_C_Color1", "Grass_2_B_Color1", "Grass_2_D_Color1" };
        string[] rocks = { "Rock_1_A_Color1", "Rock_1_G_Color1", "Rock_2_B_Color1", "Rock_2_H_Color1", "Rock_3_C_Color1" };

        Scatter(parent, trees, 105, 15f, 47f, .85f, 1.35f, rng, true);
        Scatter(parent, bushes, 70, 11f, 45f, .75f, 1.3f, rng, false);
        Scatter(parent, grasses, 130, 9f, 46f, .65f, 1.25f, rng, false);
        Scatter(parent, rocks, 48, 12f, 46f, .65f, 1.4f, rng, true);
    }

    private static void Scatter(Transform parent, string[] names, int count, float minRadius, float maxRadius, float minScale, float maxScale, System.Random rng, bool collider)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Range(rng, 0, Mathf.PI * 2);
            float radius = Mathf.Sqrt(Range(rng, minRadius * minRadius, maxRadius * maxRadius));
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            if (DistanceToRoad(pos) < 5.2f || Vector2.Distance(new Vector2(pos.x + 27, pos.z - 17), Vector2.zero) < 13f)
            {
                i--;
                continue;
            }

            GameObject instance = InstantiateModel(names[rng.Next(names.Length)], parent);
            if (instance == null) continue;
            float scale = Range(rng, minScale, maxScale);
            instance.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Range(rng, 0, 360), 0));
            instance.transform.localScale = Vector3.one * scale;
            if (collider)
            {
                CapsuleCollider c = instance.AddComponent<CapsuleCollider>();
                c.center = new Vector3(0, 1.2f, 0);
                c.height = 2.4f;
                c.radius = .45f;
            }
        }
    }

    private static void CreateBoundaryCliffs(Transform parent, System.Random rng)
    {
        string[] rocks = { "Rock_3_A_Color1", "Rock_3_F_Color1", "Rock_3_M_Color1", "Rock_3_R_Color1" };
        for (int i = 0; i < 44; i++)
        {
            float angle = i * Mathf.PI * 2 / 44f + Range(rng, -.04f, .04f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * Range(rng, 46f, 49f), 0, Mathf.Sin(angle) * Range(rng, 46f, 49f));
            GameObject rock = InstantiateModel(rocks[rng.Next(rocks.Length)], parent);
            if (rock == null) continue;
            rock.name = "Boundary Rock";
            rock.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Range(rng, 0, 360), Range(rng, -8, 8)));
            rock.transform.localScale = Vector3.one * Range(rng, 1.6f, 2.6f);
        }
    }

    private static float DistanceToRoad(Vector3 p)
    {
        Vector3 a = new(-46, 0, -28);
        Vector3 b = new(46, 0, 28);
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(p, a + ab * t);
    }

    private static GameObject InstantiateModel(string modelName, Transform parent)
    {
        string path = $"{ModelFolder}/{modelName}.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"KayKit model not found: {path}");
            return null;
        }
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance != null) instance.name = modelName.Replace("_Color1", string.Empty);
        return instance;
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
        RenderSettings.fogEndDistance = 105;

        GameObject lightObject = new GameObject("Warm Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .86f, .66f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
        RenderSettings.sun = sun;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 34;
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 180;
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
        child.transform.SetParent(parent.transform);
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
