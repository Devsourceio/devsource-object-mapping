using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct SourcePropertyResolution(
    IPropertySymbol SourceProperty,
    string SourceAccessExpression,
    string? SourceNullGuardCondition,
    FlatteningCandidate? FlatteningCandidate)
{
    public bool IsFlattened => FlatteningCandidate is not null;

    public static SourcePropertyResolution Direct(IPropertySymbol sourceProperty)
        => new(sourceProperty, $"source.{sourceProperty.Name}", null, null);

    public static SourcePropertyResolution Flattened(
        IPropertySymbol sourceProperty,
        FlatteningCandidate flatteningCandidate,
        string sourceAccessExpression,
        string? sourceNullGuardCondition)
        => new(sourceProperty, sourceAccessExpression, sourceNullGuardCondition, flatteningCandidate);

    public bool RequiresNullableFlatteningWarning(IPropertySymbol targetProperty)
    {
        return IsFlattened &&
               SourceNullGuardCondition is not null &&
               targetProperty.NullableAnnotation != NullableAnnotation.Annotated;
    }
}
