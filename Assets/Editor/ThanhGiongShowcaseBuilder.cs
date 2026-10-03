using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ThanhGiongShowcaseBuilder
{
    private const string ModelFolder = "Assets/Art/Models";
    private const string TextureFolder = "Assets/Art/Textures";
    private const string PrefabFolder = "Assets/Prefabs/Player";
    private const string MaterialFolder = "Assets/Materials/Characters";
    private const string HeroModelPath = ModelFolder + "/ThanhGiong_Hero_base_basic_pbr.fbx";
    private const string HorseModelPath = ModelFolder + "/ThanhGiong_IronHorse_base_basic_pbr.fbx";
    private const string HeroPrefabPath = PrefabFolder + "/ThanhGiong_Hero.prefab";
    private const string HorsePrefabPath = PrefabFolder + "/ThanhGiong_IronHorse.prefab";
    private const string HeroMaterialPath = MaterialFolder + "/ThanhGiong_Hero.mat";
    private const string HorseMaterialPath = MaterialFolder + "/ThanhGiong_IronHorse.mat";
    private const string MapScenePath = "Assets/Scenes/AlbionForestMap.unity";

    [InitializeOnLoadMethod]
    private static void BuildOnce()
    {
        EditorApplication.update -= TryBuildWhenReady;
        EditorApplication.update += TryBuildWhenReady;
    }

    private static void TryBuildWhenReady()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        BoxCollider existingCollider = existingPrefab != null ? existingPrefab.GetComponent<BoxCollider>() : null;
        bool prefabHasUsableSize = existingCollider != null && existingCollider.size.y > 1f;
        if (prefabHasUsableSize)
        {
            EditorApplication.update -= TryBuildWhenReady;
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(HeroModelPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(HorseModelPath) == null) return;

        EditorApplication.update -= TryBuildWhenReady;
        try
        {
            BuildAndPlace();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Thanh Giong/Build and Place 3D Hero + Iron Horse")]
    public static void BuildAndPlace()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        GameObject heroModel = AssetDatabase.LoadAssetAtPath<GameObject>(HeroModelPath);
        GameObject horseModel = AssetDatabase.LoadAssetAtPath<GameObject>(HorseModelPath);
        if (heroModel == null || horseModel == null)
        {
            Debug.LogError("Missing Thanh Giong or Iron Horse FBX in " + ModelFolder);
            return;
        }

        ConfigureNormalTexture(TextureFolder + "/ThanhGiong_Hero_texture_normal.png");
        ConfigureNormalTexture(TextureFolder + "/ThanhGiong_IronHorse_texture_normal.png");

        Material heroMaterial = CreateMaterial("ThanhGiong_Hero", HeroMaterialPath);
        Material horseMaterial = CreateMaterial("ThanhGiong_IronHorse", HorseMaterialPath);
        CreateDisplayPrefab(heroModel, heroMaterial, "Thanh Giong Hero", 2.15f, HeroPrefabPath);
        CreateDisplayPrefab(horseModel, horseMaterial, "Thanh Giong Iron Horse", 2.35f, HorsePrefabPath);

        PlaceInMap();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created and placed Thanh Giong and Iron Horse in AlbionForestMap.");
    }

    private static void ConfigureNormalTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    private static Material CreateMaterial(string prefix, string materialPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            material = new Material(shader) { name = prefix + " Material" };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + prefix + "_texture_diffuse.png");
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + prefix + "_texture_metallic.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + prefix + "_texture_normal.png");
        material.SetTexture("_BaseMap", diffuse);
        material.SetTexture("_MainTex", diffuse);
        material.SetTexture("_MetallicGlossMap", metallic);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_Metallic", 0.55f);
        material.SetFloat("_Smoothness", 0.38f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateDisplayPrefab(GameObject model, Material material, string name, float targetHeight, string prefabPath)
    {
        GameObject root = new GameObject(name);
        GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] replacements = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < replacements.Length; i++) replacements[i] = material;
            renderer.sharedMaterials = replacements;
        }

        // FBX files commonly carry a 0.01 unit conversion on their root transform.
        // Scale multiplicatively and verify the resulting world bounds so that the
        // conversion is preserved instead of being accidentally overwritten.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Bounds currentBounds = CalculateBounds(visual);
            if (currentBounds.size.y <= 0.000001f) break;
            float correction = targetHeight / currentBounds.size.y;
            visual.transform.localScale *= correction;
            if (Mathf.Abs(CalculateBounds(visual).size.y - targetHeight) < 0.01f) break;
        }

        Bounds bounds = CalculateBounds(visual);
        visual.transform.position += Vector3.up * -bounds.min.y;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        Bounds finalBounds = CalculateBounds(visual);
        collider.center = root.transform.InverseTransformPoint(finalBounds.center);
        collider.size = finalBounds.size;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void PlaceInMap()
    {
        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        RemoveExisting(scene, "Thanh Giong 3D");
        RemoveExisting(scene, "Iron Horse 3D");

        Vector3 spawn = new Vector3(-39f, 0.05f, -24f);
        GameObject spawnMarker = GameObject.Find("Player Spawn");
        if (spawnMarker != null) spawn = spawnMarker.transform.position;

        GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        GameObject horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorsePrefabPath);
        GameObject hero = PrefabUtility.InstantiatePrefab(heroPrefab, scene) as GameObject;
        GameObject horse = PrefabUtility.InstantiatePrefab(horsePrefab, scene) as GameObject;
        hero.name = "Thanh Giong 3D";
        horse.name = "Iron Horse 3D";
        hero.transform.SetPositionAndRotation(spawn + new Vector3(-1.25f, 0.05f, 1.5f), Quaternion.Euler(0, 145, 0));
        horse.transform.SetPositionAndRotation(spawn + new Vector3(1.25f, 0.05f, 1.5f), Quaternion.Euler(0, 145, 0));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.objects = new Object[] { hero, horse };
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }

    private static void RemoveExisting(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == objectName) Object.DestroyImmediate(root);
    }
}
