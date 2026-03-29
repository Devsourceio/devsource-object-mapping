using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct DictionaryMappingResult(
    bool IsDictionary,
    ITypeSymbol? KeyType,
    ITypeSymbol? ValueType,
    string? KeyMappingMethod,
    string? ValueMappingMethod,
    bool HasCircularReference,
    INamedTypeSymbol? MappedSourceType,
    INamedTypeSymbol? MappedTargetType)
{
    public static DictionaryMappingResult None => new(false, null, null, null, null, false, null, null);
}