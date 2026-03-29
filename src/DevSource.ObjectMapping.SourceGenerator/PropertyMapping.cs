using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal record PropertyMapping(IPropertySymbol SourceProperty, IPropertySymbol TargetProperty)
{
    public string SourceAccessExpression { get; init; } = string.Empty;
    public string? SourceNullGuardCondition { get; init; }
    public bool IsFlattened { get; init; }
    public bool IsNested { get; init; }
    public string? MappingMethod { get; init; }
    public bool IsDictionary { get; init; }
    public ITypeSymbol? DictionaryKeyType { get; init; }
    public ITypeSymbol? DictionaryValueType { get; init; }
    public string? DictionaryKeyMappingMethod { get; init; }
    public string? DictionaryValueMappingMethod { get; init; }
    public bool IsCollection { get; init; }
    public ITypeSymbol? ElementType { get; init; }
    public string? ElementMappingMethod { get; init; }
    public bool IsCircularReference { get; init; }
}