namespace Nexus.Domain.Primitives;

#pragma warning disable S4035
public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>>
#pragma warning restore S4035
    where TId : notnull
{
    public TId Id { get; private init; } = id;

    public static bool operator ==(Entity<TId>? first, Entity<TId>? second)
    {
        return first is not null && second is not null && first.Equals(second);
    }

    public static bool operator !=(Entity<TId>? first, Entity<TId>? second)
    {
        return !(first == second);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity<TId>)obj);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
