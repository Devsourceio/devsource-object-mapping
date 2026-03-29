using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevSource.ObjectMapping.SourceGenerator;

public partial class MappingGenerator
{
    private static MappingInfo? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var typeDeclaration = GetTypeDeclaration(context.Node);
        if (typeDeclaration is null)
            return null;

        return !TryGetMappingSymbols(context, typeDeclaration, cancellationToken, out var sourceType, out var targetType, out var compilation) 
            ? null 
            : CreateMappingInfo(sourceType, targetType, typeDeclaration, compilation);
    }

    private static TypeDeclarationSyntax? GetTypeDeclaration(SyntaxNode node)
    {
        return node switch
        {
            ClassDeclarationSyntax c => c,
            RecordDeclarationSyntax r => r,
            _ => null
        };
    }

    private static bool TryGetMappingSymbols(
        GeneratorSyntaxContext context,
        TypeDeclarationSyntax typeDeclaration,
        CancellationToken cancellationToken,
        out INamedTypeSymbol sourceType,
        out INamedTypeSymbol targetType,
        out Compilation compilation)
    {
        sourceType = null!;
        targetType = null!;
        compilation = context.SemanticModel.Compilation;

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol namedTypeSymbol)
            return false;

        var interfaceSymbol = compilation.GetTypeByMetadataName("DevSource.ObjectMapping.IMapTo`1");
        if (interfaceSymbol is null)
            return false;

        foreach (var iface in namedTypeSymbol.AllInterfaces)
        {
            if (!iface.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default) ||
                iface.TypeArguments is not [INamedTypeSymbol mappedTargetType])
            {
                continue;
            }

            sourceType = namedTypeSymbol;
            targetType = mappedTargetType;
            return true;
        }

        return false;
    }

    private static MappingInfo CreateMappingInfo(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        TypeDeclarationSyntax typeDeclaration,
        Compilation compilation)
    {
        var hasOnBeforeMap = HasPartialMethod(sourceType, "OnBeforeMap", targetType);
        var hasOnAfterMap = HasPartialMethod(sourceType, "OnAfterMap", targetType);
        return new MappingInfo(sourceType, targetType, typeDeclaration, compilation, hasOnBeforeMap, hasOnAfterMap);
    }

    private static bool HasIMapToCandidate(TypeDeclarationSyntax typeDeclaration)
    {
        if (typeDeclaration.BaseList is null)
            return false;

        foreach (var baseType in typeDeclaration.BaseList.Types)
        {
            if (baseType.Type is not GenericNameSyntax genericName)
                continue;

            if (genericName.Identifier.ValueText == "IMapTo" && genericName.TypeArgumentList.Arguments.Count == 1)
                return true;
        }

        return false;
    }

    private static bool HasPartialMethod(INamedTypeSymbol typeSymbol, string methodName, INamedTypeSymbol targetType)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method ||
                method.Name != methodName ||
                method.PartialDefinitionPart is null)
            {
                continue;
            }

            var parameters = method.Parameters;
            switch (parameters.Length)
            {
                case 1 when SymbolEqualityComparer.Default.Equals(parameters[0].Type, typeSymbol):
                case 2 when SymbolEqualityComparer.Default.Equals(parameters[0].Type, typeSymbol) &&
                            SymbolEqualityComparer.Default.Equals(parameters[1].Type, targetType):
                    return true;
            }
        }

        return false;
    }
}
