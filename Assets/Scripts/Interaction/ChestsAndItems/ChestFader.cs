using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ChestFader : MonoBehaviour
{
    [Serializable]
    private class FadePart
    {
        public Renderer renderer;
        public Material opaqueMaterial;
        public Material fadeMaterial;
    }

    [SerializeField] private FadePart[] fadeParts;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    public void ResetFade()
    {
        foreach (FadePart part in fadeParts)
        {
            if (part.renderer == null)
            {
                continue;
            }

            part.renderer.SetPropertyBlock(null);
            if (part.opaqueMaterial != null)
            {
                part.renderer.sharedMaterial = part.opaqueMaterial;
            }
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        foreach (FadePart part in fadeParts)
        {
            if (part.renderer != null && part.fadeMaterial != null)
            {
                part.renderer.sharedMaterial = part.fadeMaterial;
            }
        }

        if (duration <= 0f)
        {
            SetAlpha(0f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);

        yield return null;
    }

    private void SetAlpha(float normalizedAlpha)
    {
        normalizedAlpha = Mathf.Clamp01(normalizedAlpha);

        foreach (FadePart part in fadeParts)
        {
            if (part.renderer == null ||
                part.fadeMaterial == null ||
                !part.fadeMaterial.HasProperty(BaseColorId))
            {
                continue;
            }

            part.renderer.GetPropertyBlock(propertyBlock);

            Color baseColor = part.fadeMaterial.GetColor(BaseColorId);

            baseColor.a *= normalizedAlpha;

            propertyBlock.SetColor(BaseColorId, baseColor);

            if (part.fadeMaterial.HasProperty(EmissionColorId))
            {
                Color emissionColor = part.fadeMaterial.GetColor(EmissionColorId);

                emissionColor *= normalizedAlpha;

                propertyBlock.SetColor(
                    EmissionColorId,
                    emissionColor
                );
            }

            part.renderer.SetPropertyBlock(propertyBlock);
        }
    }
}