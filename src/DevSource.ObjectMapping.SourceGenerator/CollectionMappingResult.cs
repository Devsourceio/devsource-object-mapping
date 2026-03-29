using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct CollectionMappingResult(
    bool IsCollection,
    ITypeSymbol? ElementType,
    string? ElementMappingMethod,
    bool HasCircularReference,
    INamedTypeSymbol? MappedSourceType,
    INamedTypeSymbol? MappedTargetType)
{
    public static CollectionMappingResult None => new(false, null, null, false, null, null);
}