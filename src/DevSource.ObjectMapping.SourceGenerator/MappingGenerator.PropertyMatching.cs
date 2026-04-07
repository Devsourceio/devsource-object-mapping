using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace DevSource.ObjectMapping.SourceGenerator;

public partial class MappingGenerator
{
    private static ImmutableArray<PropertyMapping> MatchProperties(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        ImmutableArray<IPropertySymbol> sourceProperties,
        ImmutableArray<IPropertySymbol> targetProperties,
        TypeDeclarationSyntax syntaxNode)
    {
        var sourceDict = sourceProperties.ToDictionary(p => p.Name, p => p);
        var mappings = new List<PropertyMapping>();

        foreach (var targetProperty in targetProperties)
        {
            if (!TryResolveSourceProperty(sourceType, sourceDict, targetProperty, syntaxNode, context, out var sourceResolution))
                continue;

            if (!ValidatePropertyMapping(context, sourceResolution.SourceProperty, targetProperty, targetType, syntaxNode, sourceResolution))
                continue;

            mappings.Add(CreatePropertyMapping(
                context,
                compilation,
                sourceType,
                targetType,
                sourceResolution,
                targetProperty,
                syntaxNode));
        }

        return [.. mappings];
    }

    private static bool TryResolveSourceProperty(
        INamedTypeSymbol sourceType,
        Dictionary<string, IPropertySymbol> sourceDict,
        IPropertySymbol targetProperty,
        TypeDeclarationSyntax syntaxNode,
        SourceProductionContext context,
        out SourcePropertyResolution resolution)
    {
        if (sourceDict.TryGetValue(targetProperty.Name, out var directSourceProperty))
        {
            resolution = SourcePropertyResolution.Direct(directSourceProperty);
            return true;
        }

        var flatteningCandidates = FindFlatteningCandidates(sourceType, targetProperty.Name);
        if (flatteningCandidates.Length == 0)
        {
            resolution = default;
            return false;
        }

        if (flatteningCandidates.Length > 1)
        {
            ReportDiagnostic(context, MappingDiagnostics.DSM007, targetProperty, syntaxNode, targetProperty.Name);
            resolution = default;
            return false;
        }

        var flatteningCandidate = flatteningCandidates[0];
        if (!TryBuildFlatteningExpressions(flatteningCandidate, targetProperty.Type, out var sourceAccessExpression, out var sourceNullGuardCondition))
        {
            ReportDiagnostic(context, MappingDiagnostics.DSM004, targetProperty, syntaxNode, flatteningCandidate.AccessExpression, targetProperty.Name);
            resolution = default;
            return false;
        }

        resolution = SourcePropertyResolution.Flattened(
            flatteningCandidate.LeafProperty,
            flatteningCandidate,
            sourceAccessExpression,
            sourceNullGuardCondition);
        return true;
    }

    private static bool ValidatePropertyMapping(
        SourceProductionContext context,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
        INamedTypeSymbol targetType,
        TypeDeclarationSyntax syntaxNode,
        SourcePropertyResolution resolution)
    {
        if (targetProperty.SetMethod is null)
        {
            ReportDiagnostic(context, MappingDiagnostics.DSM006, targetProperty, syntaxNode, sourceProperty.Name, targetType.Name);
            return false;
        }

        if (!AreTypesCompatible(sourceProperty.Type, targetProperty.Type))
        {
            ReportDiagnostic(context, MappingDiagnostics.DSM002, targetProperty, syntaxNode, sourceProperty.Name, sourceProperty.Type.Name, targetProperty.Type.Name);
            return false;
        }

        if (IsNullableMismatch(sourceProperty.Type, targetProperty.Type))
            ReportDiagnostic(context, MappingDiagnostics.DSM004, targetProperty, syntaxNode, sourceProperty.Name, targetProperty.Name);

        if (resolution.RequiresNullableFlatteningWarning(targetProperty))
            ReportDiagnostic(context, MappingDiagnostics.DSM004, targetProperty, syntaxNode, resolution.FlatteningCandidate!.Value.AccessExpression, targetProperty.Name);

        return true;
    }

    private static PropertyMapping CreatePropertyMapping(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        SourcePropertyResolution resolution,
        IPropertySymbol targetProperty,
        TypeDeclarationSyntax syntaxNode)
    {
        var sourceProperty = resolution.SourceProperty;
        var nestedMapping = TryGetNestedMapping(compilation, sourceProperty, targetProperty);
        var dictionaryMapping = TryGetDictionaryMapping(compilation, sourceProperty, targetProperty);
        var collectionMapping = TryGetCollectionMapping(compilation, sourceProperty, targetProperty);
        var suppressCircularReference = ShouldSuppressCircularReference(sourceType, targetType, nestedMapping, dictionaryMapping, collectionMapping);

        if (nestedMapping.HasCircularReference || dictionaryMapping.HasCircularReference || collectionMapping.HasCircularReference)
            ReportDiagnostic(context, MappingDiagnostics.DSM009, sourceProperty, syntaxNode, sourceProperty.Type.Name);

        return new PropertyMapping(sourceProperty, targetProperty)
        {
            SourceAccessExpression = resolution.SourceAccessExpression,
            SourceNullGuardCondition = resolution.SourceNullGuardCondition,
            IsFlattened = resolution.IsFlattened,
            IsNested = nestedMapping.IsNested,
            MappingMethod = nestedMapping.MappingMethod,
            IsDictionary = dictionaryMapping.IsDictionary,
            DictionaryKeyType = dictionaryMapping.KeyType,
            DictionaryValueType = dictionaryMapping.ValueType,
            DictionaryKeyMappingMethod = dictionaryMapping.KeyMappingMethod,
            DictionaryValueMappingMethod = dictionaryMapping.ValueMappingMethod,
            IsCollection = collectionMapping.IsCollection,
            ElementType = collectionMapping.ElementType,
            ElementMappingMethod = collectionMapping.ElementMappingMethod,
            IsCircularReference = suppressCircularReference
        };
    }

    private static bool ShouldSuppressCircularReference(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        NestedMappingResult nestedMapping,
        DictionaryMappingResult dictionaryMapping,
        CollectionMappingResult collectionMapping)
    {
        return nestedMapping.HasCircularReference && ShouldSuppressCircularBranch(sourceType, targetType, nestedMapping.MappedSourceType, nestedMapping.MappedTargetType) ||
               dictionaryMapping.HasCircularReference && ShouldSuppressCircularBranch(sourceType, targetType, dictionaryMapping.MappedSourceType, dictionaryMapping.MappedTargetType) ||
               collectionMapping.HasCircularReference && ShouldSuppressCircularBranch(sourceType, targetType, collectionMapping.MappedSourceType, collectionMapping.MappedTargetType);
    }

    private static void ReportDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
    {
        var diagnostic = Diagnostic.Create(
            descriptor,
            location,
            messageArgs);
        context.ReportDiagnostic(diagnostic);
    }

    private static void ReportDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        IPropertySymbol property,
        TypeDeclarationSyntax syntaxNode,
        params object[] messageArgs)
    {
        ReportDiagnostic(
            context,
            descriptor,
            property.Locations.FirstOrDefault() ?? syntaxNode.GetLocation(),
            messageArgs);
    }

    private static ImmutableArray<FlatteningCandidate> FindFlatteningCandidates(
        INamedTypeSymbol sourceType,
        string targetPropertyName)
    {
        var candidates = new List<FlatteningCandidate>();
        FindFlatteningCandidates(sourceType, targetPropertyName, "source", [], candidates, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
        return [.. candidates];
    }

    private static void FindFlatteningCandidates(
        INamedTypeSymbol currentType,
        string targetPropertyName,
        string accessPrefix,
        List<IPropertySymbol> path,
        List<FlatteningCandidate> candidates,
        HashSet<INamedTypeSymbol> visitedTypes)
    {
        if (!visitedTypes.Add(currentType))
            return;

        foreach (var property in GetPublicProperties(currentType))
        {
            if (IsCollectionType(property.Type) || IsDictionaryType(property.Type))
                continue;

            var nextPath = new List<IPropertySymbol>(path) { property };
            var flattenedName = string.Concat(nextPath.Select(static p => p.Name));
            if (!targetPropertyName.StartsWith(flattenedName, StringComparison.Ordinal))
                continue;

            var accessExpression = $"{accessPrefix}.{property.Name}";
            if (flattenedName == targetPropertyName)
            {
                candidates.Add(new FlatteningCandidate(property, nextPath.ToImmutableArray(), accessExpression));
                continue;
            }

            var nestedType = property.Type as INamedTypeSymbol ?? property.Type.OriginalDefinition as INamedTypeSymbol;
            if (nestedType is null || IsPrimitiveType(property.Type))
                continue;

            FindFlatteningCandidates(nestedType, targetPropertyName, accessExpression, nextPath, candidates, visitedTypes);
        }

        visitedTypes.Remove(currentType);
    }

    private static bool TryBuildFlatteningExpressions(
        FlatteningCandidate candidate,
        ITypeSymbol targetType,
        out string accessExpression,
        out string? nullGuardCondition)
    {
        accessExpression = candidate.AccessExpression;
        nullGuardCondition = null;

        if (candidate.Path.Length < 2)
            return true;

        var nullableSegments = new List<string>();
        var currentExpression = "source";

        for (var i = 0; i < candidate.Path.Length - 1; i++)
        {
            var segment = candidate.Path[i];
            currentExpression = $"{currentExpression}.{segment.Name}";
            if (segment.NullableAnnotation == NullableAnnotation.Annotated)
                nullableSegments.Add($"{currentExpression} == null");
        }

        if (nullableSegments.Count == 0)
            return true;

        if (targetType.IsValueType && targetType.NullableAnnotation != NullableAnnotation.Annotated)
            return false;

        nullGuardCondition = string.Join(" || ", nullableSegments);
        return true;
    }
}
