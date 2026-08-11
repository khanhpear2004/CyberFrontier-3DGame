using UnityEditor;
using UnityEngine;

public static class HitImpactPrefabBuilder
{
    private const string HitPrefabFolder = "Assets/Prefab/VFX/Hit";
    private const string PlayerMaterialPath = "Assets/Materials/VFX/M_PlayerHit_Cyan.mat";
    private const string EnemyMaterialPath = "Assets/Materials/VFX/M_EnemyHit_Red.mat";
    private const string DodgeMaterialPath = "Assets/Materials/VFX/M_DogdeHit_CyanWhite.mat";

    private const string PlayerPrefabPath = HitPrefabFolder + "/PlayerHitImpact_URP.prefab";
    private const string EnemyPrefabPath = HitPrefabFolder + "/EnemyHitImpact_URP.prefab";
    private const string DodgePrefabPath = HitPrefabFolder + "/DodgeImpact_URP.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleBuildIfMissing()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    [MenuItem("Tools/Game VFX/Rebuild Hit Impact Prefabs")]
    public static void RebuildPrefabs()
    {
        EnsureFolderExists();

        Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
        Material enemyMaterial = AssetDatabase.LoadAssetAtPath<Material>(EnemyMaterialPath);
        Material dodgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(DodgeMaterialPath);

        if (playerMaterial == null || enemyMaterial == null || dodgeMaterial == null)
        {
            Debug.LogError("Cannot build hit impact prefabs because one or more VFX materials are missing.");
            return;
        }

        CreateImpactPrefab(
            "PlayerHitImpact_URP",
            PlayerPrefabPath,
            playerMaterial,
            coreSize: 0.24f,
            sparkCount: 11,
            sparkSpeedMin: 2.2f,
            sparkSpeedMax: 4.8f
        );

        CreateImpactPrefab(
            "EnemyHitImpact_URP",
            EnemyPrefabPath,
            enemyMaterial,
            coreSize: 0.28f,
            sparkCount: 15,
            sparkSpeedMin: 2.5f,
            sparkSpeedMax: 5.2f
        );

        CreateImpactPrefab(
            "DodgeImpact_URP",
            DodgePrefabPath,
            dodgeMaterial,
            coreSize: 0.38f,
            sparkCount: 20,
            sparkSpeedMin: 3f,
            sparkSpeedMax: 6f
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created three URP hit impact prefabs in Assets/Prefab/VFX/Hit.");
    }

    private static void BuildIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(DodgePrefabPath) != null)
        {
            return;
        }

        RebuildPrefabs();
    }

    private static void EnsureFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefab/VFX"))
        {
            AssetDatabase.CreateFolder("Assets/Prefab", "VFX");
        }

        if (!AssetDatabase.IsValidFolder(HitPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefab/VFX", "Hit");
        }
    }

    private static void CreateImpactPrefab(
        string prefabName,
        string prefabPath,
        Material material,
        float coreSize,
        short sparkCount,
        float sparkSpeedMin,
        float sparkSpeedMax)
    {
        GameObject root = new GameObject(prefabName);

        try
        {
            CreateCoreFlash(root.transform, material, coreSize);
            CreateSparks(
                root.transform,
                material,
                sparkCount,
                sparkSpeedMin,
                sparkSpeedMax
            );

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateCoreFlash(
        Transform parent,
        Material material,
        float coreSize)
    {
        ParticleSystem particleSystem = CreateParticleSystem("CoreFlash", parent);
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 0.15f;
        main.loop = false;
        main.startLifetime = 0.08f;
        main.startSpeed = 0f;
        main.startSize = coreSize;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.maxParticles = 4;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient();

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0f)
            )
        );

        ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        particleRenderer.sharedMaterial = material;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
    }

    private static void CreateSparks(
        Transform parent,
        Material material,
        short sparkCount,
        float speedMin,
        float speedMax)
    {
        ParticleSystem particleSystem = CreateParticleSystem("Sparks", parent);
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.2f;
        main.playOnAwake = true;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, sparkCount) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = 0.02f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient();

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f)
        );

        ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        particleRenderer.lengthScale = 2f;
        particleRenderer.velocityScale = 0.12f;
        particleRenderer.sharedMaterial = material;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
    }

    private static ParticleSystem CreateParticleSystem(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(ParticleSystem));
        child.transform.SetParent(parent, false);
        return child.GetComponent<ParticleSystem>();
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        return new ParticleSystem.MinMaxGradient(gradient);
    }
}
