using System.Collections.Immutable;
using DevSource.ObjectMapping.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DevSource.ObjectMapping.Tests;

internal static class GeneratorTestDriver
{
    internal static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorDiagnosticsTests",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new MappingGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        var runResult = driver.GetRunResult();

        return outputCompilation.GetDiagnostics().AddRange(runResult.Diagnostics);
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var references = trustedAssemblies
            .Where(path =>
                path.EndsWith("System.Runtime.dll", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("mscorlib.dll", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("netstandard.dll", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("System.Collections.dll", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("System.Linq.dll", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(IMapTo<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(MappingGenerator).Assembly.Location));

        return [.. references];
    }
}
