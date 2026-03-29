using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

public readonly record struct NestedMappingResult(
    bool IsNested,
    string? MappingMethod,
    bool HasCircularReference,
    INamedTypeSymbol? MappedSourceType,
    INamedTypeSymbol? MappedTargetType)
{
    public static NestedMappingResult None => new(false, null, false, null, null);
}