using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct FlatteningCandidate(
    IPropertySymbol LeafProperty, 
    ImmutableArray<IPropertySymbol> Path, string AccessExpression);