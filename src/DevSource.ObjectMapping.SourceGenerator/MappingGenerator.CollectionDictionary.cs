using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevSource.ObjectMapping.SourceGenerator;

public partial class MappingGenerator
{
    private static CollectionMappingResult TryGetCollectionMapping(
        Compilation compilation,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty)
    {
        if (!TryGetCollectionTypes(sourceProperty, targetProperty, out var sourceNamedType, out var targetNamedType))
            return CollectionMappingResult.None;

        var sourceElementType = GetCollectionElementType(sourceNamedType);
        var targetElementType = GetCollectionElementType(targetNamedType);
        if (sourceElementType is null || targetElementType is null)
            return CollectionMappingResult.None;

        return TryResolveCollectionInterfaceMapping(compilation, sourceElementType, targetElementType)
               ?? TryResolveCollectionConventionMapping(compilation, sourceElementType, targetElementType)
               ?? CollectionMappingResult.None;
    }

    private static DictionaryMappingResult TryGetDictionaryMapping(
        Compilation compilation,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty)
    {
        if (!TryGetDictionaryTypes(sourceProperty, targetProperty, out var sourceNamedType, out var targetNamedType))
            return DictionaryMappingResult.None;

        var sourceArguments = GetDictionaryTypeArguments(sourceNamedType);
        var targetArguments = GetDictionaryTypeArguments(targetNamedType);
        if (sourceArguments is null || targetArguments is null)
            return DictionaryMappingResult.None;

        var keyMapping = ResolveDictionaryMemberMapping(compilation, sourceArguments.Value.KeyType, targetArguments.Value.KeyType);
        var valueMapping = ResolveDictionaryMemberMapping(compilation, sourceArguments.Value.ValueType, targetArguments.Value.ValueType);

        return BuildDictionaryMappingResult(sourceArguments.Value, keyMapping, valueMapping, compilation);
    }

    private static bool TryGetCollectionTypes(
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
        out INamedTypeSymbol sourceNamedType,
        out INamedTypeSymbol targetNamedType)
    {
        sourceNamedType = null!;
        targetNamedType = null!;

        if (!IsCollectionType(sourceProperty.Type) ||
            sourceProperty.Type is not INamedTypeSymbol sourceNamed ||
            targetProperty.Type is not INamedTypeSymbol targetNamed)
        {
            return false;
        }

        sourceNamedType = sourceNamed;
        targetNamedType = targetNamed;
        return true;
    }

    private static CollectionMappingResult? TryResolveCollectionInterfaceMapping(
        Compilation compilation,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType)
    {
        var interfaceSymbol = compilation.GetTypeByMetadataName("DevSource.ObjectMapping.IMapTo`1");
        if (sourceElementType is not INamedTypeSymbol sourceElementNamed || interfaceSymbol is null)
            return null;

        foreach (var iface in sourceElementNamed.Interfaces.Concat(sourceElementNamed.AllInterfaces))
        {
            if (iface is not INamedTypeSymbol ifaceNamed ||
                !ifaceNamed.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default) ||
                ifaceNamed.TypeArguments is not [INamedTypeSymbol targetElement] ||
                !SymbolEqualityComparer.Default.Equals(targetElement, targetElementType))
            {
                continue;
            }

            return CreateCollectionMappingResult(compilation, sourceElementType, targetElementType, $"To{targetElement.Name}()", sourceElementNamed, targetElement);
        }

        return null;
    }

    private static CollectionMappingResult? TryResolveCollectionConventionMapping(
        Compilation compilation,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType)
    {
        if (!CanUseDtoConvention(sourceElementType.Name, targetElementType.Name))
            return null;

        var sourceElementSymbol = sourceElementType as INamedTypeSymbol;
        var targetElementNamed = targetElementType as INamedTypeSymbol;
        return CreateCollectionMappingResult(compilation, sourceElementType, targetElementType, $"To{targetElementType.Name}()", sourceElementSymbol, targetElementNamed);
    }

    private static CollectionMappingResult CreateCollectionMappingResult(
        Compilation compilation,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType,
        string mappingMethod,
        INamedTypeSymbol? mappedSourceType,
        INamedTypeSymbol? mappedTargetType)
    {
        var hasCircularReference = mappedSourceType is not null && mappedTargetType is not null &&
                                   HasCircularReference(compilation, mappedSourceType, mappedTargetType);

        return new CollectionMappingResult(true, sourceElementType, mappingMethod, hasCircularReference, mappedSourceType, mappedTargetType);
    }

    private static bool TryGetDictionaryTypes(
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
        out INamedTypeSymbol sourceNamedType,
        out INamedTypeSymbol targetNamedType)
    {
        sourceNamedType = null!;
        targetNamedType = null!;

        if (!IsDictionaryType(sourceProperty.Type) ||
            sourceProperty.Type is not INamedTypeSymbol sourceNamed ||
            targetProperty.Type is not INamedTypeSymbol targetNamed)
        {
            return false;
        }

        sourceNamedType = sourceNamed;
        targetNamedType = targetNamed;
        return true;
    }

    private static DictionaryMappingResult BuildDictionaryMappingResult(
        (ITypeSymbol KeyType, ITypeSymbol ValueType) sourceArguments,
        DictionaryMemberMapping keyMapping,
        DictionaryMemberMapping valueMapping,
        Compilation compilation)
    {
        var hasCircularReference = HasDictionaryCircularReference(keyMapping, compilation) || HasDictionaryCircularReference(valueMapping, compilation);

        return new DictionaryMappingResult(
            true,
            sourceArguments.KeyType,
            sourceArguments.ValueType,
            keyMapping.MappingMethod,
            valueMapping.MappingMethod,
            hasCircularReference,
            valueMapping.MappedSourceType ?? keyMapping.MappedSourceType,
            valueMapping.MappedTargetType ?? keyMapping.MappedTargetType);
    }

    private static bool HasDictionaryCircularReference(DictionaryMemberMapping mapping, Compilation compilation)
    {
        return mapping.MappedSourceType is not null &&
               mapping.MappedTargetType is not null &&
               HasCircularReference(compilation, mapping.MappedSourceType, mapping.MappedTargetType);
    }

    private static bool TryGetRootCollectionTarget(INamedTypeSymbol targetType, out INamedTypeSymbol targetElementType)
    {
        targetElementType = null!;

        if (targetType.TypeArguments.Length != 1 ||
            targetType.ContainingNamespace.ToDisplayString() != "System.Collections.Generic" ||
            targetType.TypeArguments[0] is not INamedTypeSymbol elementType)
        {
            return false;
        }

        if (targetType.Name is not ("List" or "IEnumerable" or "ICollection"))
            return false;

        targetElementType = elementType;
        return true;
    }

    private static bool TryGetRootCollectionMapping(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        TypeDeclarationSyntax syntaxNode,
        out RootCollectionMappingInfo mapping)
    {
        mapping = default;

        if (!TryGetRootCollectionTarget(targetType, out var targetElementType))
            return false;

        var sourceCollectionProperties = GetPublicProperties(sourceType)
            .Where(p => IsCollectionType(p.Type))
            .ToList();

        var compatibleCandidates = new List<RootCollectionMappingInfo>();
        var unmappedElementCandidates = new List<ITypeSymbol>();

        foreach (var sourceProperty in sourceCollectionProperties)
        {
            if (sourceProperty.Type is not INamedTypeSymbol sourceCollectionType)
                continue;

            var sourceElementType = GetCollectionElementType(sourceCollectionType);
            if (sourceElementType is null)
                continue;

            if (TryResolveRootCollectionElementMapping(compilation, sourceElementType, targetElementType, out var elementMappingMethod, out var hasCircularReference, out var mappedSourceType, out var mappedTargetType))
            {
                compatibleCandidates.Add(new RootCollectionMappingInfo(
                    sourceProperty,
                    sourceElementType,
                    targetType,
                    targetElementType,
                    elementMappingMethod,
                    hasCircularReference,
                    mappedSourceType,
                    mappedTargetType));
                continue;
            }

            unmappedElementCandidates.Add(sourceElementType);
        }

        if (compatibleCandidates.Count == 1)
        {
            mapping = compatibleCandidates[0];
            if (mapping.HasCircularReference)
                ReportDiagnostic(context, MappingDiagnostics.DSM009, mapping.SourceProperty, syntaxNode, mapping.SourceElementType.Name);

            return true;
        }

        if (compatibleCandidates.Count > 1)
        {
            ReportDiagnostic(context, MappingDiagnostics.DSM012, syntaxNode.GetLocation(), sourceType.Name, targetType.ToDisplayString());
            return false;
        }

        if (unmappedElementCandidates.Count > 0)
        {
            var sourceElementName = unmappedElementCandidates[0].ToDisplayString();
            ReportDiagnostic(context, MappingDiagnostics.DSM013, syntaxNode.GetLocation(), sourceElementName, targetElementType.ToDisplayString());
            return false;
        }

        ReportDiagnostic(context, MappingDiagnostics.DSM011, syntaxNode.GetLocation(), sourceType.Name, targetType.ToDisplayString());
        return false;
    }

    private static bool TryResolveRootCollectionElementMapping(
        Compilation compilation,
        ITypeSymbol sourceElementType,
        INamedTypeSymbol targetElementType,
        out string? elementMappingMethod,
        out bool hasCircularReference,
        out INamedTypeSymbol? mappedSourceType,
        out INamedTypeSymbol? mappedTargetType)
    {
        elementMappingMethod = null;
        hasCircularReference = false;
        mappedSourceType = null;
        mappedTargetType = null;

        if (SymbolEqualityComparer.Default.Equals(sourceElementType, targetElementType))
            return true;

        var interfaceMapping = TryResolveCollectionInterfaceMapping(compilation, sourceElementType, targetElementType);
        if (interfaceMapping is { IsCollection: true })
        {
            elementMappingMethod = interfaceMapping.Value.ElementMappingMethod;
            hasCircularReference = interfaceMapping.Value.HasCircularReference;
            mappedSourceType = interfaceMapping.Value.MappedSourceType;
            mappedTargetType = interfaceMapping.Value.MappedTargetType;
            return true;
        }

        var conventionMapping = TryResolveCollectionConventionMapping(compilation, sourceElementType, targetElementType);
        if (conventionMapping is { IsCollection: true })
        {
            elementMappingMethod = conventionMapping.Value.ElementMappingMethod;
            hasCircularReference = conventionMapping.Value.HasCircularReference;
            mappedSourceType = conventionMapping.Value.MappedSourceType;
            mappedTargetType = conventionMapping.Value.MappedTargetType;
            return true;
        }

        return false;
    }
}
