namespace Nexus.Domain.Primitives;

#pragma warning disable S4035
public abstract class ValueObject : IEquatable<ValueObject>
#pragma warning restore S4035
{
    public abstract IEnumerable<object> GetAtomicValues();

    public bool Equals(ValueObject? other)
    {
        return other is not null && ValuesAreEqual(other);
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && ValuesAreEqual(other);
    }

    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Aggregate(default(int), (hashcode, value) => HashCode.Combine(hashcode, value.GetHashCode()));
    }

    private bool ValuesAreEqual(ValueObject other)
    {
        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
