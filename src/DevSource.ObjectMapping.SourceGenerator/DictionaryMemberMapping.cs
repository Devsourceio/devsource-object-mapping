using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct DictionaryMemberMapping(
    string? MappingMethod,
    INamedTypeSymbol? MappedSourceType,
    INamedTypeSymbol? MappedTargetType)
{
    public static DictionaryMemberMapping None => new(null, null, null);
}
