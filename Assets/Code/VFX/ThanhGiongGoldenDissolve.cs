using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hiệu ứng Tan Rã Bụi Tro Ánh Sáng Vàng Kim (Golden Noise Dissolve).
/// Lấy cảm hứng từ shader uw_look & noise dissolve của quái vật trong "Understory".
/// </summary>
public class ThanhGiongGoldenDissolve : MonoBehaviour
{
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private Renderer[] renderers;
    private Material[][] originals;
    private ParticleSystem ash;

    public IEnumerator Play(float duration, Color edgeColor = default, int burstParticles = 36, bool keepObjectAlive = false)
    {
        if (edgeColor == default) edgeColor = new Color(1.0f, 0.65f, 0.08f, 1f);

        PrepareMaterials(edgeColor);
        BuildAsh(burstParticles, edgeColor);
        if (ash != null) ash.Play();

        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    runtimeMaterials[i].SetFloat("_Dissolve", t);
                }
            }

            // Shrink gently as particles rise
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.75f, t * t);
            yield return null;
        }

        if (ash != null)
        {
            ash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(0.25f);

        if (!keepObjectAlive)
        {
            gameObject.SetActive(false);
            transform.localScale = initialScale;
        }
    }

    private void PrepareMaterials(Color edgeColor)
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originals = new Material[renderers.Length][];
        Shader shader = ResolveDissolveShader();

        for (int r = 0; r < renderers.Length; r++)
        {
            if (renderers[r] == null) continue;
            originals[r] = renderers[r].sharedMaterials;
            Material[] replacements = new Material[originals[r].Length];

            for (int m = 0; m < replacements.Length; m++)
            {
                Material source = originals[r][m];
                Material material = shader != null ? new Material(shader) : new Material(source);

                if (source != null)
                {
                    if (source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null)
                        material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                    else if (source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null)
                        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));

                    if (source.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    else if (source.HasProperty("_Color"))
                        material.SetColor("_BaseColor", source.GetColor("_Color"));
                }

                if (material.HasProperty("_EdgeColor")) material.SetColor("_EdgeColor", edgeColor);
                if (material.HasProperty("_Dissolve")) material.SetFloat("_Dissolve", 0f);
                if (material.HasProperty("_EdgeWidth")) material.SetFloat("_EdgeWidth", 0.14f);

                replacements[m] = material;
                runtimeMaterials.Add(material);
            }
            renderers[r].materials = replacements;
        }
    }

    private void BuildAsh(int count, Color edgeColor)
    {
        if (ash != null) Destroy(ash.gameObject);

        GameObject go = new GameObject("Bụi Tro Ánh Sáng Vàng");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 1.0f;
        ash = go.AddComponent<ParticleSystem>();
        // A new ParticleSystem plays automatically. Configure it while stopped.
        ash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ash.main;
        main.duration = 1.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.maxParticles = count * 2;
        main.gravityModifier = -0.2f; // Bay bổng lên trời
        main.startColor = new ParticleSystem.MinMaxGradient(edgeColor, new Color(1f, 0.95f, 0.5f, 0.85f));

        var emission = ash.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = ash.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.75f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var noise = ash.noise;
        noise.enabled = true;
        noise.strength = 0.42f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.35f;

        var colorOverLife = ash.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(edgeColor, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = fade;

        ParticleSystemRenderer psRenderer = ash.GetComponent<ParticleSystemRenderer>();
        Shader partShader = ResolveParticleShader();
        if (partShader != null) psRenderer.material = new Material(partShader) { color = edgeColor };
    }

    private static Shader ResolveDissolveShader()
    {
        Shader shader = Shader.Find("ThanhGiong/Golden Noise Dissolve");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return shader;
    }

    private static Shader ResolveParticleShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return shader;
    }

    private void OnDisable()
    {
        RestoreOriginalMaterials();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();
        if (ash != null && ash.gameObject != null) Destroy(ash.gameObject);
    }

    private void RestoreOriginalMaterials()
    {
        if (renderers != null && originals != null)
        {
            for (int i = 0; i < renderers.Length && i < originals.Length; i++)
            {
                if (renderers[i] != null && originals[i] != null)
                    renderers[i].sharedMaterials = originals[i];
            }
        }

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null) Destroy(runtimeMaterials[i]);
        }
        runtimeMaterials.Clear();
    }
}
