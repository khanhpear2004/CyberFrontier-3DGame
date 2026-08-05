# Kế hoạch hệ thống Item và Planet Buff

## 1. Mục tiêu

Xây dựng một hệ thống data-driven để Item, Planet Buff và Buff tạm thời có thể:

- Dùng chung một hệ thống effect.
- Kết hợp nhiều hiệu ứng trên cùng một đối tượng.
- Stack và gỡ effect chính xác.
- Mở rộng bằng ScriptableObject mà không sửa nhiều code gameplay.
- Tương tác với bắn súng, năng lượng, di chuyển, enemy, encounter và reward.
- Giữ dữ liệu thiết kế tách biệt khỏi trạng thái runtime.

Định hướng gameplay:

```text
Chọn hành tinh
    ↓
Áp dụng luật chơi của hành tinh
    ↓
Khám phá và hoàn thành encounter
    ↓
Mở rương, chọn một trong ba item
    ↓
Ghép build mạnh dần trong một run
    ↓
Đánh boss và chọn hành tinh tiếp theo
```

## 2. Phân biệt vai trò

### Item

- Có hiệu lực đến hết một run.
- Có thể stack.
- Chủ yếu phát triển build của người chơi.
- Ví dụ: tăng năng lượng, bắn rocket, xuyên đạn, chặn projectile.

### Planet Buff

- Chỉ tồn tại trong hành tinh hiện tại.
- Có thể ảnh hưởng player, enemy, môi trường và reward.
- Nên có cả lợi ích và bất lợi để tạo lựa chọn risk/reward.
- Được gỡ hoàn toàn khi rời hành tinh.

### Buff tạm thời

- Có thời hạn hoặc điều kiện kết thúc.
- Có thể đến từ skill, vật thể tương tác hoặc item.
- Cần quy tắc stack riêng như tăng cường độ, làm mới thời gian hoặc cộng thời gian.

## 3. Kiến trúc tổng thể

```text
ItemData ──────────> RunItemManager ───┐
                                       │
PlanetData ──> PlanetBuffManager ──────┼──> EffectController
                                       │           │
BuffData ────> PlanetBuffManager ──────┘           ├──> RuntimeStats
                                                   └──> GameplayEventHub

RuntimeStats ─────> Player / Weapon / Energy / Enemy / Projectile
GameplayEventHub ─> OnShot / OnHit / OnKill / OnDamageTaken / EncounterCompleted
```

### Trách nhiệm của từng thành phần

| Thành phần | Trách nhiệm |
|---|---|
| `RunManager` | Quản lý vòng đời một run và chuyển hành tinh |
| `RunItemManager` | Quản lý item đang sở hữu và số stack |
| `PlanetBuffManager` | Quản lý planet modifier và buff có thời hạn |
| `EffectController` | Tạo, kích hoạt, cập nhật và gỡ effect runtime |
| `RuntimeStats` | Tổng hợp và tính chỉ số cuối cùng |
| `GameplayEventHub` | Phát các sự kiện combat và encounter |
| `RewardManager` | Roll phần thưởng và xử lý lựa chọn item |

`RunManager` chỉ đóng vai trò điều phối. Không đặt logic tính damage, stack item hoặc xử lý từng buff trực tiếp trong manager này.

## 4. Quy tắc stacking

### Item

- Mỗi item có `maxStack`.
- Mặc định mỗi stack tăng cường độ effect.
- Một số effect có thể dùng mốc stack thay vì cộng tuyến tính.
- Khi đạt `maxStack`, item không tiếp tục xuất hiện trong reward hoặc được chuyển thành tài nguyên khác.

### Buff tạm thời

```csharp
public enum BuffStackPolicy
{
    StackIntensity,
    RefreshDuration,
    ExtendDuration,
    IndependentInstances,
    IgnoreIfActive
}
```

Ý nghĩa:

- `StackIntensity`: tăng sức mạnh effect.
- `RefreshDuration`: làm mới timer về thời lượng gốc.
- `ExtendDuration`: cộng thêm thời gian.
- `IndependentInstances`: mỗi stack có timer riêng.
- `IgnoreIfActive`: không áp dụng nếu buff đang tồn tại.

### Planet Buff

- Chỉ có một `PlanetData` active tại một thời điểm.
- Planet mới phải gỡ toàn bộ effect có `sourceId` của planet cũ.
- Planet modifier không sử dụng timer thông thường.
- Planet effect kết thúc khi `RunManager.LeavePlanet()` được gọi.

## 5. Dữ liệu ScriptableObject

### ItemData

Mở rộng `ItemData` hiện tại:

```text
ItemData
├── Id
├── DisplayName
├── Description
├── Icon
├── Rarity
├── MaxStack
├── Tags
└── List<EffectDefinition>
```

Rarity ban đầu:

```csharp
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare
}
```

### BuffData

```text
BuffData
├── Id
├── DisplayName
├── Description
├── Icon
├── Duration
├── StackPolicy
├── MaxStacks
└── List<EffectDefinition>
```

### PlanetData

```text
PlanetData
├── Id
├── DisplayName
├── Description
├── Icon
├── EnvironmentColor
├── List<PlayerEffect>
├── List<EnemyEffect>
├── List<WorldEffect>
└── RewardModifiers
```

Không nên biến toàn bộ planet thành một `BuffData`, vì planet còn ảnh hưởng enemy, môi trường và hệ thống reward.

## 6. Hệ thống Stat

### Các stat MVP

```csharp
public enum StatType
{
    Damage,
    FireRate,
    ProjectileSpeed,
    MaxHealth,
    DamageTaken,
    WalkSpeed,
    RunSpeed,
    FlySpeed,
    MaxEnergy,
    EnergyRegen,
    EnergyCost,
    SkillDamage,
    SkillCooldown
}
```

### Loại modifier

```csharp
public enum StatModifierType
{
    Flat,
    AdditivePercent,
    Multiplicative
}
```

### Công thức chỉ số

```text
FinalStat =
    (BaseStat + tổng Flat)
    × (1 + tổng AdditivePercent)
    × tích các Multiplicative modifier
```

Ví dụ:

```text
Base Damage: 10
Flat: +2
Additive Percent: +30%
Planet Multiplier: ×1.2

Final Damage = (10 + 2) × 1.3 × 1.2 = 18.72
```

Mỗi modifier phải có:

```text
SourceId
StatType
ModifierType
Value
StackCount
```

Ví dụ `sourceId`:

```text
item.blue_core
item.plasma_rocket
planet.voltara
buff.overcharge
```

`sourceId` giúp gỡ đúng effect khi mất buff, rời planet hoặc reset run.

## 7. Hệ thống Effect

### Trigger ban đầu

```csharp
public enum EffectTrigger
{
    Passive,
    OnShot,
    EveryNthShot,
    OnHit,
    OnKill,
    OnDamageTaken,
    OnLowHealth,
    OnLowEnergy,
    OnEncounterCompleted
}
```

### Effect MVP

```text
ModifyStat
Heal
RestoreEnergy
SpawnProjectile
AreaDamage
BlockProjectile
ApplyBuff
AddRewardChoice
```

Một item hoặc planet có thể chứa nhiều effect.

Ví dụ Blue Core:

```text
Effect 1:
Trigger: Passive
Operation: ModifyStat
Stat: MaxEnergy
Type: Flat
Value: 25

Effect 2:
Trigger: Passive
Operation: ModifyStat
Stat: EnergyRegen
Type: Flat
Value: 0.5
```

## 8. Tách Data khỏi Runtime

ScriptableObject chỉ chứa cấu hình. Không lưu trạng thái thay đổi trong:

- `ItemData`
- `BuffData`
- `PlanetData`
- `EffectDefinition`

Không lưu các dữ liệu sau trong ScriptableObject:

- Số phát bắn đã đếm.
- Cooldown còn lại.
- Timer buff.
- Số lần proc.
- Target hiện tại.

Khi effect được áp dụng, `EffectController` tạo một `EffectRuntime`:

```text
EffectRuntime
├── SourceId
├── CurrentStacks
├── RemainingDuration
├── ShotCounter
├── InternalCooldown
└── RuntimeTarget
```

Quy tắc này tránh việc nhiều effect cùng dùng nhầm một trạng thái.

## 9. GameplayEventHub

Các effect đặc biệt nên lắng nghe event thay vì tự chạy nhiều `Update()`.

Event MVP:

```text
ShotFired
ProjectileHit
EnemyKilled
PlayerDamaged
EnergySpent
EncounterCompleted
```

Các object phải đăng ký event trong `OnEnable()` và hủy đăng ký trong `OnDisable()` để tránh memory leak hoặc effect chạy nhiều lần.

## 10. DamageInfo

Nâng hệ thống damage từ:

```csharp
TakeDamage(float amount)
```

thành:

```csharp
TakeDamage(DamageInfo info)
```

Cấu trúc dự kiến:

```text
DamageInfo
├── Amount
├── Source
├── Instigator
├── HitPoint
├── DamageType
└── IsCritical
```

Điều này cho phép:

- Biết ai đã giết enemy.
- Kích hoạt `OnKill`.
- Làm lifesteal.
- Phân biệt damage đạn, rocket, skill và môi trường.
- Thêm critical hoặc resistance sau này.

Trong giai đoạn chuyển đổi có thể giữ overload `TakeDamage(float)` để code cũ vẫn hoạt động.

## 11. API dự kiến

### RunItemManager

```csharp
AddItem(ItemData item, int amount = 1)
RemoveItem(ItemData item, int amount = 1)
GetStack(ItemData item)
HasItem(ItemData item)
ClearRunItems()
```

Luồng khi thêm item:

1. Kiểm tra `maxStack`.
2. Cập nhật inventory.
3. Yêu cầu `EffectController` cập nhật effect.
4. Phát `InventoryChanged` cho UI.

### PlanetBuffManager

```csharp
ActivatePlanet(PlanetData planet)
ClearPlanet()
ApplyTimedBuff(BuffData buff)
RemoveBuff(string buffId)
ClearTimedBuffs()
```

Manager này không trực tiếp sửa damage hoặc tốc độ. Nó chỉ đăng ký hoặc gỡ effect qua `EffectController`.

### RunManager

```csharp
StartRun()
EnterPlanet(PlanetData planet)
CompleteEncounter()
CompletePlanet()
LeavePlanet()
EndRun()
```

## 12. Kết nối với code hiện tại

### PlayerEnergy

`PlayerEnergy` sẽ:

- Đọc `MaxEnergy` từ `RuntimeStats`.
- Đọc `EnergyRegen` từ `RuntimeStats`.
- Áp dụng `EnergyCost` multiplier khi tiêu energy.
- Phát `EnergySpent`.

Cần chốt quy tắc khi max energy thay đổi:

- Khi tăng max energy: tăng current energy cùng lượng tăng.
- Khi giảm max energy: clamp current energy về giới hạn mới.

### PlayerController

`PlayerController` sẽ lấy các giá trị cuối từ `RuntimeStats`:

- Walk speed.
- Run speed.
- Fly speed.

Các field Inspector hiện tại vẫn là base stat.

### PlayerAimShooter

`PlayerAimShooter` sẽ:

1. Đọc fire rate từ stats.
2. Đọc energy cost từ stats.
3. Phát `ShotFired`.
4. Truyền `DamageInfo` cho viên đạn.
5. Cho phép effect như Plasma Rocket lắng nghe số phát bắn.

### Bullet

`Bullet` cần phương thức khởi tạo runtime:

```csharp
Initialize(DamageInfo damageInfo, Vector3 direction)
```

Không nên chỉ dựa vào damage cố định trên prefab vì item và planet có thể thay đổi damage theo từng phát bắn.

### Enemy

Planet effect có thể thay đổi:

- Maximum health.
- Movement speed.
- Fire interval.
- Projectile speed.
- Damage.

Khi enemy được lấy lại từ pool:

1. Reset base stat.
2. Áp dụng planet modifier hiện tại.
3. Reset effect runtime và event subscription.

### Encounter

`EnemyEncounterZone` cần theo dõi số enemy còn sống:

```text
Spawn enemy
    ↓
Đăng ký EnemyHealth.Died
    ↓
Active enemy count giảm
    ↓
Count = 0
    ↓
Phát EncounterCompleted
    ↓
Spawn hoặc mở khóa rương
```

## 13. Reward flow

Popup hiện tại cần chuyển từ hiển thị toàn bộ reward sang chọn một item:

```text
Chest yêu cầu RewardManager roll 3 item
    ↓
RewardPopupUI hiển thị 3 lựa chọn
    ↓
Người chơi chọn 1
    ↓
RunItemManager.AddItem()
    ↓
Popup đóng và gameplay tiếp tục
```

Reward roll cần xét:

- Rarity weight.
- Item đã đạt max stack.
- Planet reward modifier.
- Tag ưu tiên như Energy, Weapon hoặc Mobility.
- Số lựa chọn được cộng thêm.

## 14. Vertical slice đầu tiên

### Item 1: Blue Core

```text
Rarity: Common
Max stack: 10

Passive:
+25 Max Energy mỗi stack
+0.5 Energy Regen mỗi stack
```

### Item 2: Cyberphorse

```text
Rarity: Common
Max stack: 5

Passive:
+8% Run Speed mỗi stack
+8% Fly Speed mỗi stack
```

### Item 3: Plasma Rocket

```text
Rarity: Uncommon
Max stack: 5

EveryNthShot:
Mỗi 10 phát tạo một rocket
Rocket gây 25 AoE damage
Mỗi stack sau tăng 10 damage
```

### Planet 1: Voltara

```text
Player:
+3 Energy Regen

Enemy:
+25% Fire Rate

Reward:
Tăng trọng số item có tag Energy
```

### Planet 2: Crimson Forge

```text
Player:
+30% Damage
+25% Damage Taken

Reward:
+15% cơ hội xuất hiện Rare
```

### Planet 3: Verdant-9

```text
Player:
Hồi 10 HP khi hoàn thành encounter

Enemy:
+30% Max Health

Reward:
Thêm một lựa chọn item trong popup
```

Vertical slice này kiểm chứng được:

- Flat modifier.
- Percent modifier.
- Effect theo event.
- Projectile đặc biệt.
- Player modifier.
- Enemy modifier.
- Encounter effect.
- Reward modifier.
- Gỡ effect khi đổi planet.

## 15. Các giai đoạn triển khai

### Giai đoạn 1: Nền móng Stat và Effect

Tạo:

```text
StatType
StatModifier
RuntimeStats
EffectDefinition
EffectRuntime
EffectController
GameplayEventHub
```

Điều kiện hoàn thành:

- Thêm modifier bằng `sourceId`.
- Stack modifier đúng.
- Gỡ một source không ảnh hưởng source khác.
- Công thức flat, percent và multiplier hoạt động đúng.
- Có Edit Mode tests cho công thức stat.

### Giai đoạn 2: RunItemManager

Tạo `RunItemManager` và kết nối:

- Blue Core.
- Cyberphorse.
- PlayerEnergy.
- PlayerController.

Điều kiện hoàn thành:

- Nhận item cập nhật đúng stack.
- Max stack hoạt động.
- Energy và tốc độ cập nhật ngay.
- UI nhận event `InventoryChanged`.
- Reset run gỡ sạch item effect.

### Giai đoạn 3: Combat Event và Plasma Rocket

Thêm:

- `DamageInfo`.
- `ShotFired`.
- `ProjectileHit`.
- `EnemyKilled`.
- Plasma Rocket runtime.

Điều kiện hoàn thành:

- Mỗi phát thứ 10 tạo đúng một rocket.
- Counter không dùng chung giữa nhiều effect.
- Stack tăng damage đúng.
- Rocket không gây damage nhầm chủ sở hữu.

### Giai đoạn 4: PlanetBuffManager

Tạo:

- `BuffData`.
- `PlanetData`.
- `PlanetBuffManager`.
- Voltara.
- Crimson Forge.
- Verdant-9.

Điều kiện hoàn thành:

- Chỉ một planet active.
- Đổi planet gỡ sạch modifier cũ.
- Player và enemy nhận đúng modifier.
- Enemy pool không giữ stat của planet trước.

### Giai đoạn 5: Reward System

Nâng cấp:

- `RewardManager`.
- Reward roll theo rarity.
- Popup chọn một item.
- Planet reward modifier.

Điều kiện hoàn thành:

- Rương đưa ra đúng số lựa chọn.
- Chỉ item được chọn được thêm vào inventory.
- Item đạt max stack không tiếp tục xuất hiện.
- Verdant-9 thêm đúng một lựa chọn.

### Giai đoạn 6: Encounter Loop

Nâng cấp `EnemyEncounterZone`:

- Theo dõi enemy đã spawn.
- Biết khi encounter hoàn thành.
- Phát `EncounterCompleted`.
- Spawn hoặc mở khóa rương.

Điều kiện hoàn thành:

- Encounter không hoàn thành sớm.
- Enemy despawn do pool vẫn được xử lý chính xác.
- Reward chỉ xuất hiện một lần.

### Giai đoạn 7: UI và cân bằng

Thêm:

- Danh sách item và số stack.
- Tooltip mô tả effect.
- Planet modifier đang hoạt động.
- Buff tạm thời và timer.
- Màu rarity.

Điều kiện hoàn thành:

- Người chơi hiểu vì sao stat thay đổi.
- Tooltip thể hiện giá trị theo stack hiện tại.
- Buff hết hạn biến mất khỏi UI.

## 16. Cấu trúc folder đề xuất

```text
Assets/Scripts/
├── Stats/
│   ├── StatType.cs
│   ├── StatModifier.cs
│   └── RuntimeStats.cs
├── Effects/
│   ├── EffectDefinition.cs
│   ├── EffectRuntime.cs
│   ├── EffectController.cs
│   └── GameplayEventHub.cs
├── Item/
│   ├── ItemData.cs
│   ├── ItemEntry.cs
│   └── RunItemManager.cs
├── Buffs/
│   ├── BuffData.cs
│   ├── PlanetData.cs
│   └── PlanetBuffManager.cs
├── Run/
│   ├── RunManager.cs
│   └── RewardManager.cs
└── Combat/
    └── DamageInfo.cs
```

## 17. Checklist kỹ thuật

### Data

- [ ] ID của item, buff và planet là duy nhất.
- [ ] ScriptableObject không giữ runtime state.
- [ ] Mỗi effect có `sourceId`.
- [ ] Mỗi item có rarity và max stack.
- [ ] Planet tách player, enemy, world và reward effects.

### Runtime

- [ ] Modifier được gỡ chính xác theo source.
- [ ] Reset run xóa toàn bộ item và buff.
- [ ] Rời planet xóa toàn bộ planet effect.
- [ ] Enemy pool reset stat trước khi spawn.
- [ ] Event luôn unsubscribe trong `OnDisable`.
- [ ] Counter của `EveryNthShot` thuộc effect runtime.

### Gameplay

- [ ] Reward popup cho chọn đúng một item.
- [ ] Item đạt max stack không tiếp tục xuất hiện.
- [ ] Encounter chỉ hoàn thành khi toàn bộ enemy đã chết.
- [ ] Planet có cả lợi ích và bất lợi.
- [ ] Người chơi có thể xem item và planet effect hiện tại.

### Test

- [ ] Flat modifier.
- [ ] Additive percent modifier.
- [ ] Multiplicative modifier.
- [ ] Thêm và gỡ nhiều source.
- [ ] Item stack và max stack.
- [ ] Buff refresh và hết thời hạn.
- [ ] Đổi planet.
- [ ] Enemy pool reset.
- [ ] Every N shots.
- [ ] Reward rarity và số lựa chọn.

## 18. Thứ tự thực hiện khuyến nghị

Thứ tự phát triển:

```text
RuntimeStats
    ↓
EffectController
    ↓
RunItemManager
    ↓
Blue Core + Cyberphorse
    ↓
DamageInfo + GameplayEventHub
    ↓
Plasma Rocket
    ↓
PlanetBuffManager
    ↓
Voltara + Crimson Forge + Verdant-9
    ↓
Reward selection
    ↓
Encounter completion
    ↓
UI + cân bằng
```

Bước code đầu tiên nên là `RuntimeStats`, `StatModifier` và các Edit Mode test. Sau khi nền stat hoạt động ổn định, kết nối Blue Core và Cyberphorse trước khi triển khai các effect phức tạp hơn.

