using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyDamageFeedback : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.85f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<MaterialSlot> materialSlots = new List<MaterialSlot>();
    private MaterialPropertyBlock propertyBlock;

    private EnemyHealth enemyHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        CacheMaterialSlots();
    }

    private void OnEnable()
    {
        enemyHealth.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        enemyHealth.Damaged -= HandleDamaged;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreOriginalColors();
    }

    private void HandleDamaged(float damage)
    {
        if (damage <= 0f || materialSlots.Count == 0)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreOriginalColors();
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyFlashColor();

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreOriginalColors();
        flashRoutine = null;
    }

    private void CacheMaterialSlots()
    {
        materialSlots.Clear();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                if (material == null)
                {
                    continue;
                }

                int colorPropertyId;

                if (material.HasProperty(BaseColorId))
                {
                    colorPropertyId = BaseColorId;
                }
                else if (material.HasProperty(ColorId))
                {
                    colorPropertyId = ColorId;
                }
                else
                {
                    continue;
                }

                materialSlots.Add(new MaterialSlot(
                    targetRenderer,
                    materialIndex,
                    colorPropertyId,
                    material.GetColor(colorPropertyId)
                ));
            }
        }
    }

    private void ApplyFlashColor()
    {
        foreach (MaterialSlot slot in materialSlots)
        {
            Color targetColor = Color.Lerp(slot.OriginalColor, flashColor, flashStrength);
            targetColor.a = slot.OriginalColor.a;
            SetColor(slot, targetColor);
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (MaterialSlot slot in materialSlots)
        {
            SetColor(slot, slot.OriginalColor);
        }
    }

    private void SetColor(MaterialSlot slot, Color color)
    {
        if (slot.Renderer == null || propertyBlock == null)
        {
            return;
        }

        slot.Renderer.GetPropertyBlock(propertyBlock, slot.MaterialIndex);
        propertyBlock.SetColor(slot.ColorPropertyId, color);
        slot.Renderer.SetPropertyBlock(propertyBlock, slot.MaterialIndex);
        propertyBlock.Clear();
    }

    private sealed class MaterialSlot
    {
        public MaterialSlot(Renderer renderer, int materialIndex, int colorPropertyId, Color originalColor)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            ColorPropertyId = colorPropertyId;
            OriginalColor = originalColor;
        }

        public Renderer Renderer { get; }
        public int MaterialIndex { get; }
        public int ColorPropertyId { get; }
        public Color OriginalColor { get; }
    }
}
