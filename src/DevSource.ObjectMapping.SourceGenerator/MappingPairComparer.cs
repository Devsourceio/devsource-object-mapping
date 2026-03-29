using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal sealed class MappingPairComparer : IEqualityComparer<MappingPair>
{
    public static MappingPairComparer Instance { get; } = new();

    public bool Equals(MappingPair x, MappingPair y)
    {
        return SymbolEqualityComparer.Default.Equals(x.SourceType, y.SourceType) &&
               SymbolEqualityComparer.Default.Equals(x.TargetType, y.TargetType);
    }

    public int GetHashCode(MappingPair obj)
    {
        unchecked
        {
            return (SymbolEqualityComparer.Default.GetHashCode(obj.SourceType) * 397) ^
                   SymbolEqualityComparer.Default.GetHashCode(obj.TargetType);
        }
    }
}