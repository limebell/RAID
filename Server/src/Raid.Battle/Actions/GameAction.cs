using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Actions;

public sealed class GameAction(
    ActionId id,
    EntityId ownerId,
    string skillId,
    IReadOnlyList<IActionPhase> phases,
    EntityId? targetId = null,
    float damage = 0f)
{
    private readonly IReadOnlyList<IActionPhase> _phases = phases.Count > 0
        ? phases
        : throw new ArgumentException("At least one phase is required.", nameof(phases));
    private int _currentPhaseIndex = -1;
    private ActionContext? _context;

    public ActionId Id { get; } = id;

    public EntityId OwnerId { get; } = ownerId;

    public string SkillId { get; } = skillId;

    public EntityId? TargetId { get; } = targetId;

    public float Damage { get; } = damage;

    public ActionEndReason? EndReason { get; private set; }

    public ActionPhaseKind? CurrentPhaseKind =>
        _currentPhaseIndex >= 0 && _currentPhaseIndex < _phases.Count
            ? _phases[_currentPhaseIndex].Kind
            : null;

    public void Start(ActionContext context)
    {
        _context = context;
        AdvanceToNextPhase();
        context.World.Events.Add(new ActionStartedEvent(
            context.World.Tick,
            EntitySnapshot.FromEntity(context.Owner),
            Id,
            SkillId,
            CurrentPhaseKind));
    }

    public ActionUpdateResult Update(float deltaTime)
    {
        if (_context is null || EndReason is not null)
        {
            return ActionUpdateResult.Completed;
        }

        while (true)
        {
            var phase = _phases[_currentPhaseIndex];
            var result = phase.Update(_context, deltaTime);
            if (result == ActionPhaseResult.Continue)
            {
                return ActionUpdateResult.Running;
            }

            phase.Exit(_context, ActionEndReason.Completed);
            if (!AdvanceToNextPhase())
            {
                Complete(ActionEndReason.Completed);
                return ActionUpdateResult.Completed;
            }
        }
    }

    public void Cancel(ActionEndReason reason)
    {
        if (EndReason is not null || _context is null)
        {
            return;
        }

        if (_currentPhaseIndex >= 0 && _currentPhaseIndex < _phases.Count)
        {
            _phases[_currentPhaseIndex].Exit(_context, reason);
        }

        Complete(reason);
    }

    private bool AdvanceToNextPhase()
    {
        _currentPhaseIndex++;
        if (_currentPhaseIndex >= _phases.Count)
        {
            return false;
        }

        var phase = _phases[_currentPhaseIndex];
        phase.Enter(_context!);
        _context!.World.Events.Add(new ActionPhaseChangedEvent(
            _context.World.Tick,
            EntitySnapshot.FromEntity(_context.Owner),
            Id,
            SkillId,
            phase.Kind));
        return true;
    }

    private void Complete(ActionEndReason reason)
    {
        EndReason = reason;
        _context?.World.Events.Add(new ActionEndedEvent(
            _context.World.Tick,
            EntitySnapshot.FromEntity(_context.Owner),
            Id,
            SkillId,
            reason));
    }
}
