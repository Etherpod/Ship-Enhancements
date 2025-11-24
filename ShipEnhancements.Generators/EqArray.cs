using System.Collections.Immutable;

namespace ShipEnhancements.Generators;

/**
 * value equatable array. 2 instances will be equal so long as they have equal elements.
 *
 * this is needed by the incremental generator framework in order to cache correctly.
 */
internal class EqArray<T> : IEquatable<EqArray<T>>
{
    public static EqArray<T> Empty { get; } = new([]);

    public IList<T> Values { get; }

    public EqArray() : this([]) { }

    public EqArray(IEnumerable<T> source)
    {
        Values = source.ToImmutableArray();
    }

    public bool Equals(EqArray<T>? other)
    {
        if (this == other) return true;
        if (ReferenceEquals(Values, other?.Values)) return true;
        if (Values.Count != other!.Values.Count) return false;
        return Values
            .Select((t, i) => Equals(t, other.Values[i]))
            .All(b => b);
    }
}