namespace Raid.Battle.Actions;

public readonly record struct ActionId(long Value)
{
    public override string ToString() => Value.ToString();
}
