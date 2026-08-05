using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnTransition : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private ParticleSystem spawnVfx;

    [Header("Transition")]
    [SerializeField, Min(0.05f)] private float duration = 0.65f;
    [SerializeField] private AnimationCurve fadeCurve;

    [SerializeField, Min(0.05f)] private float disappearDuration = 0.75f;

    private float currentOpacity = 1f;

    private readonly List<FadeTarget> fadeTargets = new();
    private MaterialPropertyBlock propertyBlock;

    private Coroutine fadeRoutine;

    private sealed class FadeTarget
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public int ColorProperty;
        public Color OriginalColor;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (fadeCurve == null || fadeCurve.length == 0)
        {
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        CacheRenderers();
        SetOpacity(1f);
    }

    public void Play(Action onCompleted)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(onCompleted));
    }

    public void ResetForPool()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        SetOpacity(1f);

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }

        if (spawnVfx != null)
        {
            spawnVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void CacheRenderers()
    {
        fadeTargets.Clear();

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer is ParticleSystemRenderer)
            {
                continue;
            }

            Material[] materials = targetRenderer.sharedMaterials;

            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];

                if (material == null)
                {
                    continue;
                }

                int colorProperty;

                if (material.HasProperty(BaseColorId))
                {
                    colorProperty = BaseColorId;
                }
                else if (material.HasProperty(ColorId))
                {
                    colorProperty = ColorId;
                }
                else
                {
                    continue;
                }

                fadeTargets.Add(new FadeTarget{Renderer = targetRenderer, MaterialIndex = index, ColorProperty = colorProperty, OriginalColor = material.GetColor(colorProperty)});
            }
        }
    }

    private IEnumerator FadeRoutine(Action onCompleted)
    {
        SetOpacity(0f);

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }

        if (spawnVfx != null)
        {
            spawnVfx.Play();
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float opacity = fadeCurve.Evaluate(progress);

            SetOpacity(opacity);
            yield return null;
        }

        SetOpacity(1f);
        fadeRoutine = null;

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(true);
        }

        onCompleted?.Invoke();
    }

    private void SetOpacity(float opacity)
    {
        currentOpacity = Mathf.Clamp01(opacity);

        foreach (FadeTarget target in fadeTargets)
        {
            if (target.Renderer == null)
            {
                continue;
            }

            Color color = target.OriginalColor;
            color.a = target.OriginalColor.a * opacity;

            propertyBlock.Clear();

            target.Renderer.GetPropertyBlock(propertyBlock, target.MaterialIndex);

            propertyBlock.SetColor(target.ColorProperty, color);

            target.Renderer.SetPropertyBlock(propertyBlock, target.MaterialIndex);
        }
    }

    public void HideHealthBar()
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }
    }

    public void PlayDisappear(Action onCompleted)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }

        if (spawnVfx != null)
        {
            spawnVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        fadeRoutine = StartCoroutine(FadeOutRoutine(onCompleted));
    }

    private IEnumerator FadeOutRoutine(Action onCompleted)
    {
        float startOpacity = currentOpacity;
        float elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / disappearDuration);

            float curvedProgress = fadeCurve.Evaluate(progress);

            float opacity = Mathf.Lerp(startOpacity, 0f, curvedProgress);

            SetOpacity(opacity);
            yield return null;
        }

        SetOpacity(0f);
        fadeRoutine = null;

        onCompleted?.Invoke();
    }
}