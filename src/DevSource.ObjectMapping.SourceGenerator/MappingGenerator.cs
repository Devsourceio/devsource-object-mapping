using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace DevSource.ObjectMapping.SourceGenerator;

/// <summary>
/// Represents a source generator that generates object mappings between types implementing a specific mapping interface.
/// </summary>
/// <remarks>
/// This generator identifies types that implement the `DevSource.ObjectMapping.IMapTo<T>` interface,
/// inspects their properties, and automatically generates mapping methods to convert objects of the source type
/// to the target type specified in the interface implementation.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public partial class MappingGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the source generator by setting up the syntax and implementation contexts required for generating code.
    /// </summary>
    /// <param name="context">
    /// The <see cref="IncrementalGeneratorInitializationContext"/> that enables the configuration of syntax providers,
    /// semantic transformations, and source outputs for the generator.
    /// </param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                Predicate,
                Transform)
            .Where(static mapping => mapping is not null);

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is TypeDeclarationSyntax typeDeclaration && HasIMapToCandidate(typeDeclaration);
    }

    private static void Execute(SourceProductionContext context, MappingInfo? mappingInfo)
    {
        if (mappingInfo is null)
            return;

        var (sourceType, targetType, syntaxNode, compilation, hasOnBeforeMap, hasOnAfterMap) = mappingInfo;
        var sourceProperties = GetPublicProperties(sourceType);
        var targetProperties = GetPublicProperties(targetType);
        var propertyMap = MatchProperties(context, compilation, sourceType, targetType, sourceProperties, targetProperties, syntaxNode);
        var sourceCode = GenerateMappingCode(sourceType, targetType, propertyMap, hasOnBeforeMap, hasOnAfterMap);
        var fileName = $"{sourceType.Name}To{targetType.Name}.g.cs";
        context.AddSource(fileName, sourceCode);
    }

    private static ImmutableArray<IPropertySymbol> GetPublicProperties(INamedTypeSymbol typeSymbol)
    {
        return [
            ..typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Where(p => p.GetMethod is not null)
        ];
    }
}
