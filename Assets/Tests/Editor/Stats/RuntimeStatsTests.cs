using System.Text.RegularExpressions;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class RuntimeStatsTests
{
    private GameObject testObject;
    private RuntimeStats runtimeStats;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject(
            "RuntimeStats Test Object"
        );

        runtimeStats =
            testObject.AddComponent<RuntimeStats>();
    }

    [TearDown]
    public void TearDown()
    {
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void GetValue_WithNoModifier_ReturnsBaseValue()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        float result = runtimeStats.GetValue(
            StatType.Damage
        );

        Assert.AreEqual(
            10f,
            result,
            0.001f
        );
    }

    [Test]
    public void GetValue_WithFlatModifier_AddsToBaseValue()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.flat",
                "damage",
                StatType.Damage,
                StatModifierType.Flat,
                5f
            )
        );

        Assert.AreEqual(
            15f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );
    }

    [Test]
    public void GetValue_WithAdditivePercent_AddsPercentTogether()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            100f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.first",
                "damage-percent",
                StatType.Damage,
                StatModifierType.AdditivePercent,
                0.2f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.second",
                "damage-percent",
                StatType.Damage,
                StatModifierType.AdditivePercent,
                0.3f
            )
        );

        Assert.AreEqual(
            150f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );
    }

    [Test]
    public void GetValue_WithMultipliers_MultipliesSequentially()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            100f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "planet.first",
                "damage-multiplier",
                StatType.Damage,
                StatModifierType.Multiplicative,
                1.2f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "buff.second",
                "damage-multiplier",
                StatType.Damage,
                StatModifierType.Multiplicative,
                1.5f
            )
        );

        Assert.AreEqual(
            180f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );
    }

    [Test]
    public void GetValue_WithMixedModifiers_UsesCorrectOrder()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.flat",
                "damage",
                StatType.Damage,
                StatModifierType.Flat,
                2f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.percent",
                "damage",
                StatType.Damage,
                StatModifierType.AdditivePercent,
                0.3f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "planet.multiplier",
                "damage",
                StatType.Damage,
                StatModifierType.Multiplicative,
                1.2f
            )
        );

        Assert.AreEqual(
            18.72f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );
    }

    [Test]
    public void AddModifier_WithSameIdentity_ReplacesOldValue()
    {
        runtimeStats.SetBaseValue(
            StatType.MaxEnergy,
            200f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.blue-core",
                "max-energy",
                StatType.MaxEnergy,
                StatModifierType.Flat,
                25f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.blue-core",
                "max-energy",
                StatType.MaxEnergy,
                StatModifierType.Flat,
                50f
            )
        );

        Assert.AreEqual(
            250f,
            runtimeStats.GetValue(StatType.MaxEnergy),
            0.001f
        );
    }

    [Test]
    public void RemoveModifiersBySource_LeavesOtherSourcesActive()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.damage",
                "flat-damage",
                StatType.Damage,
                StatModifierType.Flat,
                2f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "planet.crimson",
                "damage-multiplier",
                StatType.Damage,
                StatModifierType.Multiplicative,
                1.5f
            )
        );

        int removedCount =
            runtimeStats.RemoveModifiersBySource(
                "item.damage"
            );

        Assert.AreEqual(1, removedCount);

        Assert.AreEqual(
            15f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );
    }

    [Test]
    public void ClearAllModifiers_ReturnsStatsToBaseValues()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        runtimeStats.SetBaseValue(
            StatType.RunSpeed,
            6f
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.damage",
                "flat-damage",
                StatType.Damage,
                StatModifierType.Flat,
                5f
            )
        );

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.speed",
                "run-speed",
                StatType.RunSpeed,
                StatModifierType.AdditivePercent,
                0.5f
            )
        );

        runtimeStats.ClearAllModifiers();

        Assert.AreEqual(
            10f,
            runtimeStats.GetValue(StatType.Damage),
            0.001f
        );

        Assert.AreEqual(
            6f,
            runtimeStats.GetValue(StatType.RunSpeed),
            0.001f
        );
    }

    [Test]
    public void StatChanged_WhenValueChanges_ReportsOldAndNewValue()
    {
        runtimeStats.SetBaseValue(
            StatType.Damage,
            10f
        );

        StatType receivedType = default;
        float receivedOldValue = 0f;
        float receivedNewValue = 0f;
        int invocationCount = 0;

        runtimeStats.StatChanged += (
            statType,
            oldValue,
            newValue) =>
        {
            receivedType = statType;
            receivedOldValue = oldValue;
            receivedNewValue = newValue;
            invocationCount++;
        };

        runtimeStats.AddOrReplaceModifier(
            CreateModifier(
                "item.damage",
                "flat-damage",
                StatType.Damage,
                StatModifierType.Flat,
                5f
            )
        );

        Assert.AreEqual(1, invocationCount);
        Assert.AreEqual(StatType.Damage, receivedType);

        Assert.AreEqual(
            10f,
            receivedOldValue,
            0.001f
        );

        Assert.AreEqual(
            15f,
            receivedNewValue,
            0.001f
        );
    }

    [Test]
    public void InitializeBaseStats_WithDuplicateBaseStat_KeepsFirstValue()
    {
        GameObject duplicateObject =
            new GameObject("Duplicate Base Stats Test");

        duplicateObject.SetActive(false);

        RuntimeStats duplicateStats =
            duplicateObject.AddComponent<RuntimeStats>();

        SerializedObject serializedStats =
            new SerializedObject(duplicateStats);

        SerializedProperty baseStatsProperty =
            serializedStats.FindProperty("baseStats");

        baseStatsProperty.arraySize = 2;

        SetBaseStatEntry(
            baseStatsProperty.GetArrayElementAtIndex(0),
            StatType.Damage,
            10f
        );

        SetBaseStatEntry(
            baseStatsProperty.GetArrayElementAtIndex(1),
            StatType.Damage,
            99f
        );

        serializedStats.ApplyModifiedPropertiesWithoutUndo();

        LogAssert.Expect(
            LogType.Warning,
            new Regex(
                "Stat Damage bị khai báo trùng"
            )
        );

        MethodInfo initializeMethod =
            typeof(RuntimeStats).GetMethod(
                "InitializeBaseStats",
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        Assert.IsNotNull(
            initializeMethod,
            "Không tìm thấy InitializeBaseStats."
        );

        initializeMethod.Invoke(
            duplicateStats,
            null
        );

        Assert.AreEqual(
            10f,
            duplicateStats.GetValue(StatType.Damage),
            0.001f
        );

        Object.DestroyImmediate(duplicateObject);
    }

    private static StatModifier CreateModifier(
        string sourceId,
        string modifierId,
        StatType statType,
        StatModifierType modifierType,
        float value)
    {
        return new StatModifier(
            sourceId,
            modifierId,
            statType,
            modifierType,
            value
        );
    }

    private static void SetBaseStatEntry(
        SerializedProperty entry,
        StatType statType,
        float baseValue)
    {
        entry
            .FindPropertyRelative("statType")
            .enumValueIndex = (int)statType;

        entry
            .FindPropertyRelative("baseValue")
            .floatValue = baseValue;
    }
}
