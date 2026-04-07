using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace DevSource.ObjectMapping.SourceGenerator;

public partial class MappingGenerator
{
    private static string GeneratePropertyAssignment(PropertyMapping mapping)
    {
        if (mapping.IsCircularReference)
            return string.Empty;

        var sourceAccessExpression = BuildSourceAccessExpression(mapping);
        var nullGuardCondition = BuildNullGuardCondition(mapping, sourceAccessExpression);

        if (mapping.IsDictionary)
            return GenerateDictionaryAssignment(mapping, sourceAccessExpression, nullGuardCondition);

        if (mapping is { IsCollection: true, ElementMappingMethod: not null })
            return GenerateCollectionAssignment(mapping, sourceAccessExpression, nullGuardCondition);

        return mapping is not { IsNested: true, MappingMethod: not null } 
            ? GenerateDirectAssignment(mapping, sourceAccessExpression, nullGuardCondition) 
            : GenerateNestedAssignment(mapping, sourceAccessExpression, nullGuardCondition);
    }

    private static string GenerateDictionaryAssignment(PropertyMapping mapping, string sourceAccessExpression, string nullGuardCondition)
    {
        return RequiresNullGuard(mapping)
            ? $"""
               {mapping.TargetProperty.Name} = {nullGuardCondition}
                   ? null
                   : MapDictionary{mapping.TargetProperty.Name}({sourceAccessExpression}),
               """
            : $"{mapping.TargetProperty.Name} = MapDictionary{mapping.TargetProperty.Name}({sourceAccessExpression}),";
    }

    private static string GenerateCollectionAssignment(PropertyMapping mapping, string sourceAccessExpression, string nullGuardCondition)
    {
        return RequiresNullGuard(mapping)
            ? $"""
               {mapping.TargetProperty.Name} = {nullGuardCondition}
                   ? null
                   : MapCollection{mapping.TargetProperty.Name}({sourceAccessExpression}),
               """
            : $"{mapping.TargetProperty.Name} = MapCollection{mapping.TargetProperty.Name}({sourceAccessExpression}),";
    }

    private static string GenerateDirectAssignment(PropertyMapping mapping, string sourceAccessExpression, string nullGuardCondition)
    {
        return mapping.SourceNullGuardCondition is null
            ? $"{mapping.TargetProperty.Name} = {sourceAccessExpression},"
            : $"{mapping.TargetProperty.Name} = {nullGuardCondition} ? {BuildFlattenedNullValueLiteral(mapping.TargetProperty.Type)} : {sourceAccessExpression},";
    }

    private static string GenerateNestedAssignment(PropertyMapping mapping, string sourceAccessExpression, string nullGuardCondition)
    {
        return RequiresNullGuard(mapping)
            ? $"{mapping.TargetProperty.Name} = {nullGuardCondition} ? null : {sourceAccessExpression}.{mapping.MappingMethod},"
            : $"{mapping.TargetProperty.Name} = {sourceAccessExpression}.{mapping.MappingMethod},";
    }

    private static bool RequiresNullGuard(PropertyMapping mapping)
    {
        return mapping.SourceProperty.Type.NullableAnnotation == NullableAnnotation.Annotated ||
               mapping.SourceNullGuardCondition is not null;
    }

    private static string BuildSourceAccessExpression(PropertyMapping mapping)
    {
        return string.IsNullOrEmpty(mapping.SourceAccessExpression)
            ? $"source.{mapping.SourceProperty.Name}"
            : mapping.SourceAccessExpression;
    }

    private static string BuildNullGuardCondition(PropertyMapping mapping, string sourceAccessExpression)
    {
        return mapping.SourceNullGuardCondition ?? $"{sourceAccessExpression} == null";
    }

    private static string BuildFlattenedNullValueLiteral(ITypeSymbol targetType)
    {
        if (targetType.IsValueType)
            return $"({targetType.ToDisplayString()})null";

        return targetType.NullableAnnotation == NullableAnnotation.Annotated ? "null" : "null!";
    }

    private static string GenerateMappingCode(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        ImmutableArray<PropertyMapping> propertyMappings,
        bool hasOnBeforeMap = false,
        bool hasOnAfterMap = false)
    {
        var template = CreateMappingTemplate(sourceType, targetType, propertyMappings, hasOnBeforeMap, hasOnAfterMap);
        return template.HasEmptyBody 
            ? GenerateEmptyMappingCode(template) 
            : GenerateFullMappingCode(template);
    }

    private static MappingCodeTemplate CreateMappingTemplate(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        ImmutableArray<PropertyMapping> propertyMappings,
        bool hasOnBeforeMap,
        bool hasOnAfterMap)
    {
        var collectionProperties = propertyMappings
            .Where(p => p is { IsCollection: true, ElementMappingMethod: not null, IsCircularReference: false })
            .ToList();
        var dictionaryProperties = propertyMappings
            .Where(p => p is { IsDictionary: true, IsCircularReference: false })
            .ToList();

        return new MappingCodeTemplate(
            sourceType.ContainingNamespace.ToDisplayString(),
            sourceType.Name,
            targetType.Name,
            BuildPropertyAssignments(propertyMappings),
            GenerateCollectionHelpers(collectionProperties),
            GenerateDictionaryHelpers(dictionaryProperties),
            hasOnBeforeMap ? "\n        OnBeforeMap(source);" : string.Empty,
            hasOnAfterMap ? "\n        OnAfterMap(source, target);" : string.Empty,
            propertyMappings.IsEmpty && collectionProperties.Count == 0 && dictionaryProperties.Count == 0);
    }

    private static string BuildPropertyAssignments(ImmutableArray<PropertyMapping> propertyMappings)
    {
        return string.Join("\n        ", propertyMappings
            .Select(GeneratePropertyAssignment)
            .Where(static assignment => !string.IsNullOrWhiteSpace(assignment)));
    }

    private static string GenerateEmptyMappingCode(MappingCodeTemplate template)
    {
        return $$"""
        // <auto-generated/>
        #nullable enable
        namespace {{template.NamespaceName}};

        public static class {{template.SourceName}}MappingExtensions
        {
            public static {{template.TargetName}}? To{{template.TargetName}}(this {{template.SourceName}}? source)
            {
                if (source is null)
                    return null!;

                return new {{template.TargetName}}();
            }
        }
        """;
    }

    private static string GenerateRootCollectionMappingCode(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        RootCollectionMappingInfo mapping)
    {
        var namespaceName = sourceType.ContainingNamespace.ToDisplayString();
        var targetElementTypeName = mapping.TargetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sourceCollectionTypeName = mapping.SourceProperty.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetListTypeName = $"global::System.Collections.Generic.List<{targetElementTypeName}>";
        var targetCollectionTypeName = mapping.TargetCollectionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = GetRootCollectionMethodName(mapping.TargetCollectionType, mapping.TargetElementType);
        var helperMethod = GenerateRootCollectionHelper(mapping, targetListTypeName, sourceCollectionTypeName);

        return $$"""
        // <auto-generated/>
        #nullable enable
        namespace {{namespaceName}};

        public static class {{sourceType.Name}}MappingExtensions
        {
            public static {{targetCollectionTypeName}}? {{methodName}}(this {{sourceType.Name}}? source)
            {
                if (source is null)
                    return null!;

                return MapRootCollection(source.{{mapping.SourceProperty.Name}});
            }

        {{helperMethod}}
        }
        """;
    }

    private static string GenerateFullMappingCode(MappingCodeTemplate template)
    {
        return $$"""
        // <auto-generated/>
        #nullable enable
        namespace {{template.NamespaceName}};

        public static class {{template.SourceName}}MappingExtensions
        {
            public static {{template.TargetName}}? To{{template.TargetName}}(this {{template.SourceName}}? source)
            {
                if (source is null)
                    return null!;
        {{template.OnBeforeMapCall}}
                var target = new {{template.TargetName}}
                {
        {{template.PropertyAssignments}}
                };
        {{template.OnAfterMapCall}}
                return target;
            }
        {{template.CollectionHelpers}}
        {{template.DictionaryHelpers}}
        }
        """;
    }

    private static string GenerateCollectionHelpers(List<PropertyMapping> collectionProperties)
    {
        if (collectionProperties.Count == 0)
            return string.Empty;

        var helpers = (from prop in collectionProperties
                       let sourceType = prop.SourceProperty.Type
                       let targetType = prop.TargetProperty.Type
                       let elementMethod = prop.ElementMappingMethod ?? string.Empty
                       let targetElementType = GetCollectionElementType((INamedTypeSymbol)targetType)
                       let targetElementFullName = targetElementType?.ToDisplayString() ?? "object"
                       let sourceTypeFullName = sourceType.ToDisplayString()
                       let targetTypeName = $"global::System.Collections.Generic.List<{targetElementFullName}>"
                       let methodCall = string.IsNullOrEmpty(elementMethod) ? string.Empty : $".{elementMethod}"
                       select string.Format("""
                                            private static {0} MapCollection{1}({2} source)
                                            {{
                                                if (source == null)
                                                    return null!;

                                                var result = new {0}(source.Count);
                                                foreach (var item in source)
                                                {{
                                                    var mapped = item{3};
                                                    if (mapped != null)
                                                        result.Add(mapped);
                                                }}
                                                return result;
                                            }}
                                            """, targetTypeName, prop.TargetProperty.Name, sourceTypeFullName, methodCall))
            .ToList();

        return string.Join("\n\n", helpers);
    }

    private static string GenerateDictionaryHelpers(List<PropertyMapping> dictionaryProperties)
    {
        if (dictionaryProperties.Count == 0)
            return string.Empty;

        var helpers = dictionaryProperties.Select(prop =>
        {
            var sourceType = (INamedTypeSymbol)prop.SourceProperty.Type;
            var targetArguments = GetDictionaryTypeArguments((INamedTypeSymbol)prop.TargetProperty.Type);
            var sourceTypeFullName = sourceType.ToDisplayString();
            var keyTypeName = targetArguments?.KeyType.ToDisplayString() ?? "object";
            var valueTypeName = targetArguments?.ValueType.ToDisplayString() ?? "object";
            var targetTypeName = $"global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>";
            var keyMapExpression = BuildDictionaryMapExpression("entry.Key", prop.DictionaryKeyType, prop.DictionaryKeyMappingMethod);
            var valueMapExpression = BuildDictionaryMapExpression("entry.Value", prop.DictionaryValueType, prop.DictionaryValueMappingMethod);

            return string.Format("""
                                 private static {0} MapDictionary{1}({2} source)
                                 {{
                                     if (source == null)
                                         return null!;

                                     var result = new {0}(source.Count);
                                     foreach (var entry in source)
                                     {{
                                         result.Add({3}, {4});
                                     }}
                                     return result;
                                 }}
                                 """, targetTypeName, prop.TargetProperty.Name, sourceTypeFullName, keyMapExpression, valueMapExpression);
        }).ToList();

        return string.Join("\n\n", helpers);
    }

    private static string BuildDictionaryMapExpression(string inputExpression, ITypeSymbol? sourceType, string? mappingMethod)
    {
        if (string.IsNullOrEmpty(mappingMethod))
            return inputExpression;

        var isNullable = sourceType?.NullableAnnotation == NullableAnnotation.Annotated;
        return isNullable
            ? $"{inputExpression} == null ? null! : {inputExpression}.{mappingMethod}!"
            : $"{inputExpression}.{mappingMethod}!";
    }

    private static string GenerateRootCollectionHelper(
        RootCollectionMappingInfo mapping,
        string targetListTypeName,
        string sourceCollectionTypeName)
    {
        var mapExpression = string.IsNullOrEmpty(mapping.ElementMappingMethod)
            ? "item"
            : $"item.{mapping.ElementMappingMethod}";

        var addStatement = ShouldGuardCollectionElementAdd(mapping.TargetElementType)
            ? "if (mapped != null)\n                result.Add(mapped);"
            : "result.Add(mapped);";

        return $$"""
            private static {{targetListTypeName}}? MapRootCollection({{sourceCollectionTypeName}} source)
            {
                if (source == null)
                    return null!;

                var result = new {{targetListTypeName}}();
                foreach (var item in source)
                {
                    var mapped = {{mapExpression}};
                    {{addStatement}}
                }

                return result;
            }
        """;
    }

    private static bool ShouldGuardCollectionElementAdd(ITypeSymbol elementType)
    {
        return !elementType.IsValueType || elementType.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private static string GetRootCollectionMethodName(INamedTypeSymbol targetCollectionType, INamedTypeSymbol targetElementType)
    {
        return targetCollectionType.Name switch
        {
            "IEnumerable" => $"ToEnumerableOf{targetElementType.Name}",
            "ICollection" => $"ToCollectionOf{targetElementType.Name}",
            _ => $"ToListOf{targetElementType.Name}"
        };
    }
}
