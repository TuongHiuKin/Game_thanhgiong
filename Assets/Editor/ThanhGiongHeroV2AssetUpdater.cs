using System.IO;
using UnityEditor;
using UnityEngine;

public static class ThanhGiongHeroV2AssetUpdater
{
    private const string SourceFolder = "Assets/fbcd2ed9-1e6d-4f6a-b216-a1d89c359a7d";
    private const string ModelFolder = "Assets/Art/Models/ThanhGiong_Hero_V2";
    private const string TextureFolder = "Assets/Art/Textures/ThanhGiong_Hero_V2";
    private const string MaterialFolder = "Assets/Art/Materials/ThanhGiong_Hero_V2";
    private const string SourceBaseFbx = SourceFolder + "/base.fbx";
    private const string ArtBaseFbx = ModelFolder + "/ThanhGiong_Hero_V2_base.fbx";

    [MenuItem("Tools/Thanh Giong/Update Hero V2 Asset Pack")]
    public static void UpdateHeroV2AssetPack()
    {
        EnsureFolder("Assets/Art/Materials");
        EnsureFolder(MaterialFolder);
        ConfigureModelImporter(SourceBaseFbx);
        ConfigureModelImporter(ArtBaseFbx);
        ConfigureTexture(TextureFolder + "/ThanhGiong_Hero_V2_diffuse.png", false, TextureImporterType.Default, true);
        ConfigureTexture(TextureFolder + "/ThanhGiong_Hero_V2_shaded.png", false, TextureImporterType.Default, true);
        ConfigureTexture(TextureFolder + "/ThanhGiong_Hero_V2_normal.png", false, TextureImporterType.NormalMap, false);
        ConfigureTexture(TextureFolder + "/ThanhGiong_Hero_V2_metallic_smoothness.png", false, TextureImporterType.Default, false);
        ConfigureTexture(TextureFolder + "/ThanhGiong_Hero_V2_occlusion.png", false, TextureImporterType.Default, false);

        Material painted = CreatePaintedMaterial();
        Material pbr = CreatePbrMaterial();
        Material shaded = CreateShadedMaterial();

        CreateHeroPrefab("ThanhGiong_Hero_V2_Painted", painted, false);
        CreateHeroPrefab("ThanhGiong_Hero_V2_PBR", pbr, false);
        CreateHeroPrefab("ThanhGiong_Hero_V2_Shaded", shaded, false);
        CreateHeroPrefab("ThanhGiong_Hero_V2_Monument", painted, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ThanhGiongHeroV2AssetUpdater: Updated Hero V2 model importers, textures, materials and Art prefabs.");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void ConfigureModelImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        importer.globalScale = 0.01f;
        importer.useFileScale = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importAnimation = false;
        importer.meshOptimizationFlags = MeshOptimizationFlags.Everything;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path, bool alpha, TextureImporterType type, bool srgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static Material CreatePaintedMaterial()
    {
        Shader shader = Shader.Find("ThanhGiong/LegendSurface");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = CreateOrLoadMaterial("M_ThanhGiong_Hero_V2_Painted", shader);
        SetTexture(material, "_BaseMap", TextureFolder + "/ThanhGiong_Hero_V2_diffuse.png");
        SetColor(material, "_BaseColor", Color.white);
        SetFloat(material, "_Mode", 0f);
        SetFloat(material, "_Visibility", 1f);
        SetFloat(material, "_UseVertexColors", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreatePbrMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = CreateOrLoadMaterial("M_ThanhGiong_Hero_V2_PBR", shader);
        SetTexture(material, "_BaseMap", TextureFolder + "/ThanhGiong_Hero_V2_diffuse.png");
        SetTexture(material, "_BumpMap", TextureFolder + "/ThanhGiong_Hero_V2_normal.png");
        SetTexture(material, "_MetallicGlossMap", TextureFolder + "/ThanhGiong_Hero_V2_metallic_smoothness.png");
        SetTexture(material, "_OcclusionMap", TextureFolder + "/ThanhGiong_Hero_V2_occlusion.png");
        SetColor(material, "_BaseColor", Color.white);
        SetFloat(material, "_Metallic", 1f);
        SetFloat(material, "_Smoothness", 1f);
        SetFloat(material, "_BumpScale", 1f);
        SetFloat(material, "_OcclusionStrength", 1f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateShadedMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = CreateOrLoadMaterial("M_ThanhGiong_Hero_V2_Shaded", shader);
        SetTexture(material, "_BaseMap", TextureFolder + "/ThanhGiong_Hero_V2_shaded.png");
        SetColor(material, "_BaseColor", Color.white);
        SetFloat(material, "_Smoothness", 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrLoadMaterial(string name, Shader shader)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }
        material.name = name;
        return material;
    }

    private static void SetTexture(Material material, string property, string texturePath)
    {
        if (material == null || !material.HasProperty(property)) return;
        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        if (texture != null) material.SetTexture(property, texture);
    }

    private static void SetColor(Material material, string property, Color color)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, color);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    private static void CreateHeroPrefab(string name, Material material, bool monument)
    {
        Mesh mesh = LoadPrimaryMesh(ArtBaseFbx);
        if (mesh == null) mesh = LoadPrimaryMesh(SourceBaseFbx);
        if (mesh == null)
        {
            Debug.LogWarning("ThanhGiongHeroV2AssetUpdater: Could not find primary mesh for " + name);
            return;
        }

        GameObject root = new GameObject(name);
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = 1.84f;
        capsule.radius = 0.45f;
        capsule.center = new Vector3(0f, 0.92f, 0f);

        GameObject model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        model.transform.localScale = Vector3.one * 100f;
        MeshFilter filter = model.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;

        if (monument)
        {
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "DongSon_Plinth";
            plinth.transform.SetParent(root.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            plinth.transform.localScale = new Vector3(1.08f, 0.07f, 1.08f);
            MeshRenderer plinthRenderer = plinth.GetComponent<MeshRenderer>();
            plinthRenderer.sharedMaterial = CreatePlinthMaterial();
        }

        string path = ModelFolder + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Material CreatePlinthMaterial()
    {
        Material material = CreateOrLoadMaterial("M_DongSon_Stone_Plinth", Shader.Find("Universal Render Pipeline/Lit"));
        SetColor(material, "_BaseColor", new Color(0.48f, 0.39f, 0.27f, 1f));
        SetFloat(material, "_Smoothness", 0.22f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh LoadPrimaryMesh(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            Mesh mesh = asset as Mesh;
            if (mesh != null && !mesh.name.StartsWith("__", System.StringComparison.Ordinal)) return mesh;
        }
        return null;
    }
}
