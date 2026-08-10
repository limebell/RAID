namespace Raid.Battle.World;

public sealed class WorldLoop(BattleWorld world, WorldSettings settings)
{
    public void Tick()
    {
        var deltaTime = settings.FixedDeltaTimeSeconds;

        world.Commands.ProcessQueuedCommands();
        world.Actions.Update(deltaTime);
        world.Hits.ProcessPending();
        world.Movement.Update(deltaTime);
        world.AdvanceTick();
    }
}
