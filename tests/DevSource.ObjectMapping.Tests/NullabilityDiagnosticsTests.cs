using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DevSource.ObjectMapping.SourceGenerator;

namespace DevSource.ObjectMapping.Tests;

public class NullabilityDiagnosticsTests
{
    [Fact]
    public void NullableToNonNullable_EmitsActionableDSM004()
    {
        const string source = """
#nullable enable
using DevSource.ObjectMapping;

public class NullableSource : IMapTo<NonNullableTarget>
{
    public string? Name { get; set; }
}

public class NonNullableTarget
{
    public string Name { get; set; } = string.Empty;
}
""";

        var diagnostics = GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM004"));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("may be null", diagnostic.GetMessage());
        Assert.Contains("Make the target nullable or handle the assignment explicitly.", diagnostic.GetMessage());
        Assert.Contains("Name", diagnostic.GetMessage());
    }

    [Fact]
    public void SafeNullableMapping_DoesNotEmitDSM004()
    {
        const string source = """
#nullable enable
using DevSource.ObjectMapping;

public class NullableSource : IMapTo<NullableTarget>
{
    public string? Name { get; set; }
}

public class NullableTarget
{
    public string? Name { get; set; }
}
""";

        var diagnostics = GetGeneratorDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "DSM004");
    }

    private static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName: "NullabilityDiagnosticsTests",
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
