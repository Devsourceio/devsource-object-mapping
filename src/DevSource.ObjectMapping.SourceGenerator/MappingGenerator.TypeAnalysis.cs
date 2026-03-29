using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

public partial class MappingGenerator
{
    private static ITypeSymbol? GetCollectionElementType(INamedTypeSymbol collectionType)
    {
        var elementTypes = collectionType.TypeArguments;
        if (elementTypes.Length > 0)
            return elementTypes[0];

        return (from iface in collectionType.AllInterfaces
                where iface.TypeArguments.Length > 0
                select iface.TypeArguments[0])
            .FirstOrDefault();
    }

    private static (ITypeSymbol KeyType, ITypeSymbol ValueType)? GetDictionaryTypeArguments(INamedTypeSymbol dictionaryType)
    {
        if (dictionaryType.TypeArguments.Length == 2)
            return (dictionaryType.TypeArguments[0], dictionaryType.TypeArguments[1]);

        var dictionaryInterface = dictionaryType.AllInterfaces
            .FirstOrDefault(i => i.Name == "IDictionary" && i.TypeArguments.Length == 2);

        return dictionaryInterface is null
            ? null
            : (dictionaryInterface.TypeArguments[0], dictionaryInterface.TypeArguments[1]);
    }

    private static bool IsNullableMismatch(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var sourceNullable = sourceType.NullableAnnotation == NullableAnnotation.Annotated;
        var targetNullable = targetType.NullableAnnotation == NullableAnnotation.Annotated;
        return sourceNullable && !targetNullable;
    }

    private static DictionaryMemberMapping ResolveDictionaryMemberMapping(
        Compilation compilation,
        ITypeSymbol sourceType,
        ITypeSymbol targetType)
    {
        if (TryResolveMappedTypePair(compilation, sourceType, targetType, out var nestedSource, out var nestedTarget))
            return new DictionaryMemberMapping($"To{nestedTarget.Name}()", nestedSource, nestedTarget);

        if (AreTypesCompatible(sourceType, targetType))
            return DictionaryMemberMapping.None;

        return DictionaryMemberMapping.None;
    }

    private static NestedMappingResult TryGetNestedMapping(
        Compilation compilation,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty)
    {
        var sourceType = sourceProperty.Type;
        var targetType = targetProperty.Type;
        var sourceNamedType = sourceType as INamedTypeSymbol ?? sourceType.OriginalDefinition as INamedTypeSymbol;
        if (sourceNamedType is null || IsPrimitiveType(sourceType) || IsCollectionType(sourceType))
            return NestedMappingResult.None;

        var interfaceSymbol = compilation.GetTypeByMetadataName("DevSource.ObjectMapping.IMapTo`1");
        if (interfaceSymbol is null)
            return NestedMappingResult.None;

        foreach (var iface in sourceNamedType.AllInterfaces)
        {
            if (!iface.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default) ||
                iface.TypeArguments is not [INamedTypeSymbol targetNestedType] ||
                !SymbolEqualityComparer.Default.Equals(targetNestedType, targetType))
            {
                continue;
            }

            var methodName = $"To{targetNestedType.Name}()";
            var hasCircularReference = HasCircularReference(compilation, sourceNamedType, targetNestedType);
            return new NestedMappingResult(true, methodName, hasCircularReference, sourceNamedType, targetNestedType);
        }

        return NestedMappingResult.None;
    }

    private static bool ShouldSuppressCircularBranch(
        INamedTypeSymbol currentSourceType,
        INamedTypeSymbol currentTargetType,
        INamedTypeSymbol? nextSourceType,
        INamedTypeSymbol? nextTargetType)
    {
        if (nextSourceType is null || nextTargetType is null)
            return false;

        var currentKey = GetMappingKey(currentSourceType, currentTargetType);
        var nextKey = GetMappingKey(nextSourceType, nextTargetType);
        return string.CompareOrdinal(nextKey, currentKey) >= 0;
    }

    private static string GetMappingKey(INamedTypeSymbol sourceType, INamedTypeSymbol targetType)
    {
        return $"{sourceType.ToDisplayString()}->{targetType.ToDisplayString()}";
    }

    private static bool HasCircularReference(
        Compilation compilation,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType)
    {
        return HasCircularReference(
            compilation,
            sourceType,
            targetType,
            new HashSet<MappingPair>(MappingPairComparer.Instance));
    }

    private static bool HasCircularReference(
        Compilation compilation,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        HashSet<MappingPair> path)
    {
        var currentPair = new MappingPair(sourceType, targetType);
        if (!path.Add(currentPair))
            return true;

        foreach (var sourceProperty in GetPublicProperties(sourceType))
        {
            var targetProperty = GetPublicProperties(targetType)
                .FirstOrDefault(p => p.Name == sourceProperty.Name);

            if (targetProperty is null)
                continue;

            if ((!TryResolveMappedTypePair(compilation, sourceProperty.Type, targetProperty.Type, out var nestedSource, out var nestedTarget) ||
                 !HasCircularReference(compilation, nestedSource, nestedTarget, path)) &&
                (!TryResolveCollectionMappedTypePair(compilation, sourceProperty.Type, targetProperty.Type, out var collectionSource, out var collectionTarget) ||
                 !HasCircularReference(compilation, collectionSource, collectionTarget, path)) &&
                (!TryResolveDictionaryMappedTypePair(compilation, sourceProperty.Type, targetProperty.Type, out var dictionarySource, out var dictionaryTarget) ||
                 !HasCircularReference(compilation, dictionarySource, dictionaryTarget, path)))
            {
                continue;
            }

            path.Remove(currentPair);
            return true;
        }

        path.Remove(currentPair);
        return false;
    }

    private static bool TryResolveMappedTypePair(
        Compilation compilation,
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        out INamedTypeSymbol sourceNamedType,
        out INamedTypeSymbol targetNamedType)
    {
        sourceNamedType = null!;
        targetNamedType = null!;

        if (IsPrimitiveType(sourceType) || IsCollectionType(sourceType) || sourceType is not INamedTypeSymbol sourceNamed)
            return false;

        if (targetType is not INamedTypeSymbol targetNamed)
            return false;

        var interfaceSymbol = compilation.GetTypeByMetadataName("DevSource.ObjectMapping.IMapTo`1");
        if (interfaceSymbol is null)
            return false;

        foreach (var iface in sourceNamed.AllInterfaces)
        {
            if (!iface.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default) ||
                iface.TypeArguments is not [INamedTypeSymbol mappedTarget] ||
                !SymbolEqualityComparer.Default.Equals(mappedTarget, targetNamed))
            {
                continue;
            }

            sourceNamedType = sourceNamed;
            targetNamedType = mappedTarget;
            return true;
        }

        return false;
    }

    private static bool TryResolveCollectionMappedTypePair(
        Compilation compilation,
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        out INamedTypeSymbol sourceElementType,
        out INamedTypeSymbol targetElementType)
    {
        sourceElementType = null!;
        targetElementType = null!;

        if (!IsCollectionType(sourceType) || sourceType is not INamedTypeSymbol sourceNamed || targetType is not INamedTypeSymbol targetNamed)
            return false;

        var sourceElement = GetCollectionElementType(sourceNamed) as INamedTypeSymbol;
        var targetElement = GetCollectionElementType(targetNamed) as INamedTypeSymbol;
        if (sourceElement is null || targetElement is null)
            return false;

        if (!TryResolveMappedTypePair(compilation, sourceElement, targetElement, out var resolvedSource, out var resolvedTarget))
            return false;

        sourceElementType = resolvedSource;
        targetElementType = resolvedTarget;
        return true;
    }

    private static bool TryResolveDictionaryMappedTypePair(
        Compilation compilation,
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        out INamedTypeSymbol sourceValueType,
        out INamedTypeSymbol targetValueType)
    {
        sourceValueType = null!;
        targetValueType = null!;

        if (!IsDictionaryType(sourceType) || sourceType is not INamedTypeSymbol sourceNamed || targetType is not INamedTypeSymbol targetNamed)
            return false;

        var sourceArguments = GetDictionaryTypeArguments(sourceNamed);
        var targetArguments = GetDictionaryTypeArguments(targetNamed);
        if (sourceArguments is null || targetArguments is null)
            return false;

        return TryResolveMappedTypePair(
            compilation,
            sourceArguments.Value.ValueType,
            targetArguments.Value.ValueType,
            out sourceValueType,
            out targetValueType);
    }

    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        return type.SpecialType is >= SpecialType.System_Int32 and <= SpecialType.System_String ||
               type.TypeKind == TypeKind.Enum ||
               type is { IsValueType: true, IsDefinition: false };
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        if (IsDictionaryType(type))
            return false;

        return namedType.TypeArguments.Length > 0 &&
               namedType.Name is "List" or "IEnumerable" or "IList" or "ICollection";
    }

    private static bool IsDictionaryType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        if (namedType.Name is "Dictionary" or "IDictionary" && namedType.TypeArguments.Length == 2)
            return true;

        return namedType.AllInterfaces.Any(i => i.Name == "IDictionary" && i.TypeArguments.Length == 2);
    }

    private static bool CanUseDtoConvention(string sourceTypeName, string targetTypeName)
    {
        return targetTypeName.StartsWith(sourceTypeName, StringComparison.Ordinal) &&
               targetTypeName.Length > sourceTypeName.Length;
    }

    private static bool AreTypesCompatible(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
            return true;
        if (sourceType.IsValueType && targetType.IsValueType)
            return true;
        if (sourceType.IsReferenceType && targetType.IsReferenceType)
            return true;
        return sourceType.TypeKind == TypeKind.Enum && targetType.TypeKind == TypeKind.Enum;
    }
}
