using Microsoft.CodeAnalysis;

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

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
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

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "DSM004");
    }
}
