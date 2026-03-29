using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevSource.ObjectMapping.SourceGenerator;

internal record MappingInfo(
    INamedTypeSymbol SourceType, 
    INamedTypeSymbol TargetType, 
    TypeDeclarationSyntax SyntaxNode, 
    Compilation Compilation,
    bool HasOnBeforeMap = false,
    bool HasOnAfterMap = false);