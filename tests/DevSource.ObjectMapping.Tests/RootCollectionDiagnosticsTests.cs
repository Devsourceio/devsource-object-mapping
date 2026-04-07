using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.Tests;

public class RootCollectionDiagnosticsTests
{
    [Fact]
    public void RootCollectionWithoutSourceCollection_EmitsDSM011()
    {
        const string source = """
#nullable enable
using System.Collections.Generic;
using DevSource.ObjectMapping;

public class User : IMapTo<UserDto>
{
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
}

public record GetUser : IMapTo<List<UserDto>>
{
    public int Id { get; init; }
}
""";

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM011"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("GetUser", diagnostic.GetMessage());
        Assert.Contains("List<UserDto>", diagnostic.GetMessage());
    }

    [Fact]
    public void RootCollectionWithMultipleCompatibleSources_EmitsDSM012()
    {
        const string source = """
#nullable enable
using System.Collections.Generic;
using DevSource.ObjectMapping;

public class User : IMapTo<UserDto>
{
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
}

public record GetUser : IMapTo<List<UserDto>>
{
    public List<User> Users { get; init; } = [];
    public List<User> ArchivedUsers { get; init; } = [];
}
""";

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM012"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("GetUser", diagnostic.GetMessage());
    }

    [Fact]
    public void RootCollectionWithoutElementMapping_EmitsDSM013()
    {
        const string source = """
#nullable enable
using System.Collections.Generic;
using DevSource.ObjectMapping;

public class Account
{
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
}

public record GetUser : IMapTo<List<UserDto>>
{
    public List<Account> Users { get; init; } = [];
}
""";

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM013"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Account", diagnostic.GetMessage());
        Assert.Contains("UserDto", diagnostic.GetMessage());
    }

    [Fact]
    public void RootEnumerableWithoutSourceCollection_EmitsDSM011()
    {
        const string source = """
#nullable enable
using System.Collections.Generic;
using DevSource.ObjectMapping;

public class User : IMapTo<UserDto>
{
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
}

public record GetUser : IMapTo<IEnumerable<UserDto>>
{
    public int Id { get; init; }
}
""";

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM011"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("IEnumerable<UserDto>", diagnostic.GetMessage());
    }

    [Fact]
    public void RootCollectionInterfaceWithMultipleCompatibleSources_EmitsDSM012()
    {
        const string source = """
#nullable enable
using System.Collections.Generic;
using DevSource.ObjectMapping;

public class User : IMapTo<UserDto>
{
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
}

public record GetUser : IMapTo<ICollection<UserDto>>
{
    public ICollection<User> Users { get; init; } = [];
    public List<User> ArchivedUsers { get; init; } = [];
}
""";

        var diagnostics = GeneratorTestDriver.GetGeneratorDiagnostics(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "DSM012"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("ICollection<UserDto>", diagnostic.GetMessage());
    }
}
