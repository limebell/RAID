using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;

namespace Raid.Battle.Resources;

public sealed class ResourceSystem(BattleWorld world)
{
    private float _manaRegenElapsedSeconds;

    public bool TrySpendMana(PlayerEntity player, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (player.CurrentMana < amount)
        {
            return false;
        }

        player.CurrentMana -= amount;
        return true;
    }

    public void RestoreMana(PlayerEntity player, float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        var before = player.CurrentMana;
        player.CurrentMana = MathF.Min(player.MaxMana, player.CurrentMana + amount);
        if (player.CurrentMana == before)
        {
            return;
        }

        world.Events.Add(new ResourceChangedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(player)));
    }

    public void Update(float deltaTime)
    {
        _manaRegenElapsedSeconds += deltaTime;
        while (_manaRegenElapsedSeconds >= 1f)
        {
            _manaRegenElapsedSeconds -= 1f;
            ApplyManaRegenPulse();
        }
    }

    private void ApplyManaRegenPulse()
    {
        foreach (var player in world.Entities.Players())
        {
            if (!CanRegenMana(player))
            {
                continue;
            }

            var regenRate = GetEffectiveManaRegenPerSecond(player);
            if (regenRate <= 0f)
            {
                continue;
            }

            var before = player.CurrentMana;
            player.CurrentMana = MathF.Min(player.MaxMana, player.CurrentMana + regenRate);
            if (player.CurrentMana == before)
            {
                continue;
            }

            world.Events.Add(new ResourceChangedEvent(
                world.Tick,
                EntitySnapshot.FromEntity(player)));
        }
    }

    private static bool CanRegenMana(PlayerEntity player)
    {
        return player.CurrentMana < player.MaxMana
            && !player.IsDowned
            && player.CurrentHealth > 0f;
    }

    private float GetEffectiveManaRegenPerSecond(PlayerEntity player)
    {
        if (world.Settings.Practice.HighManaRegen)
        {
            return WorldSettings.PracticeHighManaRegenPerSecond;
        }

        return player.ManaRegenPerSecond;
    }
}
