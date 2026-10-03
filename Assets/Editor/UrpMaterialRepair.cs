using System;
using UnityEditor;
using UnityEngine;

public static class UrpMaterialRepair
{
    [InitializeOnLoadMethod]
    private static void RepairWhenReady()
    {
        EditorApplication.update -= TryRepair;
        EditorApplication.update += TryRepair;
    }

    private static void TryRepair()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= TryRepair;
        RepairAllMaterials();
    }

    [MenuItem("Tools/Thanh Giong/Repair Pink Materials for URP")]
    public static void RepairAllMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (lit == null)
        {
            Debug.LogError("[URP Repair] Universal Render Pipeline/Lit shader is unavailable.");
            return;
        }

        int repaired = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || !NeedsRepair(material)) continue;

            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : .25f;
            bool transparent = material.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               material.name.IndexOf("ring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               color.a < .99f;

            material.shader = transparent && unlit != null ? unlit : lit;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (mainTexture != null && material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", mainTexture);
            if (mainTexture != null && material.HasProperty("_MainTex")) material.SetTexture("_MainTex", mainTexture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);

            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", 5f);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", 10f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            else
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = -1;
            }

            EditorUtility.SetDirty(material);
            repaired++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[URP Repair] Repaired " + repaired + " incompatible material(s). Pink assets should now render normally.");
    }

    private static bool NeedsRepair(Material material)
    {
        if (material.shader == null) return true;
        string shaderName = material.shader.name;
        return shaderName == "Standard" ||
               shaderName.StartsWith("Legacy Shaders/", StringComparison.Ordinal) ||
               shaderName == "Hidden/InternalErrorShader";
    }
}
