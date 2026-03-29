using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct MappingPair(
    INamedTypeSymbol SourceType, 
    INamedTypeSymbol TargetType);