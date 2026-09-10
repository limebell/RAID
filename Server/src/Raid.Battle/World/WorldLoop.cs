namespace Raid.Battle.World;

public sealed class WorldLoop(BattleWorld world, WorldSettings settings)
{
    public void Tick()
    {
        var deltaTime = settings.FixedDeltaTimeSeconds;

        world.Resources.Update(deltaTime);
        world.Cooldowns.Update(deltaTime);
        world.Commands.ProcessQueuedCommands();
        world.Actions.Update(deltaTime);
        world.Orders.Update();
        world.DelayedHits.Update();
        world.Projectiles.Update(deltaTime);
        world.Hits.ProcessPending();
        world.Chains.ResolvePending();
        world.Chains.Update(deltaTime);
        world.Effects.Update(deltaTime);
        world.Zones.Update(deltaTime);
        world.Movement.Update(deltaTime);
        world.AdvanceTick();
    }
}
