using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct RootCollectionMappingInfo(
    IPropertySymbol SourceProperty,
    ITypeSymbol SourceElementType,
    INamedTypeSymbol TargetCollectionType,
    INamedTypeSymbol TargetElementType,
    string? ElementMappingMethod,
    bool HasCircularReference,
    INamedTypeSymbol? MappedSourceType,
    INamedTypeSymbol? MappedTargetType);
