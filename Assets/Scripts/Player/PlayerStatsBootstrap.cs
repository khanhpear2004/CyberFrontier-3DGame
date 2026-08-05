using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(RuntimeStats))]
public sealed class PlayerStatsBootstrap : MonoBehaviour
{
    [SerializeField] private RuntimeStats runtimeStats;
    [SerializeField] private PlayerEnergy playerEnergy;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAimShooter playerAimShooter;

    private void Awake()
    {
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        if (playerEnergy == null)
        {
            playerEnergy = GetComponent<PlayerEnergy>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerAimShooter == null)
        {
            playerAimShooter = GetComponent<PlayerAimShooter>();
        }
    }

    private void Start()
    {
        RegisterEnergyStats();
        RegisterHealthStats();
        RegisterMovementStats();
        RegisterCombatStats();
    }

    private void RegisterEnergyStats()
    {
        if (playerEnergy == null)
        {
            return;
        }

        runtimeStats.SetBaseValue(StatType.MaxEnergy, playerEnergy.maxEnergy);
        runtimeStats.SetBaseValue(StatType.EnergyRegen, playerEnergy.regenPerSecond);
    }

    private void RegisterHealthStats()
    {
        if (playerHealth == null)
        {
            return;
        }

        runtimeStats.SetBaseValue(StatType.MaxHealth, playerHealth.MaxHealth);
        runtimeStats.SetBaseValue(StatType.DamageTaken, 1f);
    }

    private void RegisterMovementStats()
    {
        if (playerController == null)
        {
            return;
        }

        runtimeStats.SetBaseValue(StatType.WalkSpeed, playerController.walkSpeed);

        runtimeStats.SetBaseValue(StatType.RunSpeed, playerController.runSpeed);

        runtimeStats.SetBaseValue(StatType.FlySpeed, playerController.flySpeed);
    }

    private void RegisterCombatStats()
    {
        if (playerAimShooter == null)
        {
            Debug.LogWarning($"{name}: Không tìm thấy PlayerAimShooter để đăng ký combat stat.", this);
            return;
        }

        runtimeStats.SetBaseValue(StatType.FireRate, playerAimShooter.fireRate);

        if (playerAimShooter.bulletPrefab == null)
        {
            Debug.LogWarning($"{name}: PlayerAimShooter chưa được gắn Bullet Prefab.", playerAimShooter);
            return;
        }

        runtimeStats.SetBaseValue(StatType.Damage, playerAimShooter.bulletPrefab.damage);
        runtimeStats.SetBaseValue(StatType.ProjectileSpeed, playerAimShooter.bulletPrefab.speed);
    }
}