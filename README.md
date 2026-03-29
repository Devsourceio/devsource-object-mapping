# DevSource.ObjectMapping

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Source Generator](https://img.shields.io/badge/Roslyn-Source%20Generator-007ACC)
![Benchmarks](https://img.shields.io/badge/Benchmarks-Near%20Manual-brightgreen)

`DevSource.ObjectMapping` is a .NET object mapping library built around Roslyn Source Generators.

It aims to deliver mapping performance close to handwritten code while keeping mappings deterministic, debuggable, and compile-time safe.

## Overview

If you want the ergonomics of a mapper without the runtime black box, this library is built for that trade-off.

`DevSource.ObjectMapping` generates mapping code at compile time, keeps the API minimal, and favors explicit conventions over hidden runtime behavior.

## Why

Traditional mappers often trade performance and clarity for convenience. `DevSource.ObjectMapping` takes a different path:

- no runtime reflection for mapping execution
- generated extension methods instead of runtime mapping engines
- compile-time validation for incompatible mappings
- generated code that is easy to inspect and debug

## Installation

The repository currently uses the runtime package plus the source generator as an analyzer reference.

### Project reference setup

```xml
<ItemGroup>
  <ProjectReference Include="src/DevSource.ObjectMapping/DevSource.ObjectMapping.csproj" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="src/DevSource.ObjectMapping.SourceGenerator/DevSource.ObjectMapping.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Basic usage flow

1. Implement `IMapTo<TDestination>` on the source type.
2. Build the project.
3. Call the generated `To{Destination}()` extension method.

## Core idea

Define the mapping contract in the source type:

```csharp
public class User : IMapTo<UserDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

Then use the generated extension method:

```csharp
var dto = user.ToUserDto();
```

## Design Goals

- predictable conventions instead of runtime guessing
- compile-time diagnostics instead of runtime surprises
- generated code that is close to handwritten mapping
- performance that tracks manual implementations as closely as possible

## Features

- one-way mapping via `IMapTo<TDestination>`
- exact-match property mapping
- nested object mapping
- collection mapping
- `Dictionary<TKey, TValue>` / `IDictionary<TKey, TValue>` support
- flattening by convention, such as `Customer.Name -> CustomerName`
- compile-time diagnostics for invalid scenarios
- nullability-aware mapping behavior
- partial-hook customization support in the generator pipeline

## Quick Example

```csharp
public class User : IMapTo<UserDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

public class Address : IMapTo<AddressDto>
{
    public string City { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    public string City { get; set; } = string.Empty;
}

var dto = user.ToUserDto();
```

## Example: Nested Mapping

```csharp
public class Address : IMapTo<AddressDto>
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class UserWithAddress : IMapTo<UserWithAddressDto>
{
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

public class UserWithAddressDto
{
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

var dto = user.ToUserWithAddressDto();
```

## Example: Dictionary Mapping

```csharp
public class Product : IMapTo<ProductDto>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class Catalog : IMapTo<CatalogDto>
{
    public Dictionary<string, Product> Products { get; set; } = [];
}

public class CatalogDto
{
    public Dictionary<string, ProductDto> Products { get; set; } = [];
}
```

## Example: Flattening Convention

```csharp
public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OrderSummary : IMapTo<OrderSummaryDto>
{
    public int Id { get; set; }
    public CustomerInfo Customer { get; set; } = new();
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
```

Generated behavior includes:

- `Customer.Name -> CustomerName`
- `Customer.Email -> CustomerEmail`
- direct property matches still take precedence over flattening

## What You Get

- generated extension methods for a clean call site
- compile-time validation when mappings drift
- nullability-aware behavior aligned with C# nullable annotations
- support for common real-world graphs such as nested objects, lists, and dictionaries

## Limitations

- mappings are intentionally one-way and convention-driven
- property matching remains strict and case-sensitive
- unsupported or ambiguous scenarios are rejected instead of guessed
- strict nullability rules may require explicit handling in some mappings
- partial-hook customization exists in the generator pipeline, but should be treated carefully until its end-to-end behavior is fully validated in production-style scenarios

## Benchmarks

The project includes benchmark comparisons against handwritten mapping, `Mapster`, and `AutoMapper`.

High-level takeaway:

- `DevSource.ObjectMapping` stays very close to handwritten code
- it outperforms `Mapster` in all measured scenarios
- it outperforms `AutoMapper` by a wide margin in all measured scenarios
- allocations are usually identical to handwritten mapping

In practice, the benchmark profile supports the main claim of the project: generated mappings behave much closer to manual code than traditional runtime mappers.

Benchmark source:

- `benchmark/DevSource.ObjectMapping.Benchmarks/BenchmarkDotNet.Artifacts/results/DevSource.ObjectMapping.Benchmarks.MappingBenchmark-report-github.md`

### Results
```
| Method                            | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Manual_Simple                     |   4.077 ns | 0.0576 ns | 0.0539 ns |  1.00 |    0.02 | 0.0032 |      - |      40 B |        1.00 |
| DevSourceObjectMapping_Simple     |   4.330 ns | 0.0301 ns | 0.0282 ns |  1.06 |    0.02 | 0.0032 |      - |      40 B |        1.00 |
| Mapster_Simple                    |  10.017 ns | 0.0748 ns | 0.0700 ns |  2.46 |    0.04 | 0.0032 |      - |      40 B |        1.00 |
| AutoMapper_Simple                 |  30.729 ns | 0.0956 ns | 0.0847 ns |  7.54 |    0.10 | 0.0032 |      - |      40 B |        1.00 |
| Manual_Nested                     |   9.511 ns | 0.1628 ns | 0.1443 ns |  2.33 |    0.05 | 0.0064 |      - |      80 B |        2.00 |
| DevSourceObjectMapping_Nested     |   8.331 ns | 0.1708 ns | 0.1597 ns |  2.04 |    0.05 | 0.0064 |      - |      80 B |        2.00 |
| Mapster_Nested                    |  15.435 ns | 0.1231 ns | 0.1028 ns |  3.79 |    0.05 | 0.0063 |      - |      80 B |        2.00 |
| AutoMapper_Nested                 |  40.796 ns | 0.3250 ns | 0.3040 ns | 10.01 |    0.15 | 0.0063 |      - |      80 B |        2.00 |
| Manual_Collection                 |  30.353 ns | 0.4451 ns | 0.4164 ns |  7.45 |    0.14 | 0.0235 |      - |     296 B |        7.40 |
| DevSourceObjectMapping_Collection |  31.936 ns | 0.4784 ns | 0.4475 ns |  7.83 |    0.15 | 0.0235 |      - |     296 B |        7.40 |
| Mapster_Collection                |  35.279 ns | 0.2501 ns | 0.2217 ns |  8.65 |    0.12 | 0.0235 |      - |     296 B |        7.40 |
| AutoMapper_Collection             |  66.338 ns | 0.2214 ns | 0.1963 ns | 16.27 |    0.21 | 0.0242 |      - |     304 B |        7.60 |
| Manual_Dictionary                 |  48.725 ns | 0.2943 ns | 0.2753 ns | 11.95 |    0.17 | 0.0357 |      - |     448 B |       11.20 |
| DevSourceObjectMapping_Dictionary |  52.384 ns | 0.5931 ns | 0.5548 ns | 12.85 |    0.21 | 0.0357 | 0.0001 |     448 B |       11.20 |
| Mapster_Dictionary                |  99.640 ns | 0.6074 ns | 0.5682 ns | 24.44 |    0.34 | 0.0401 |      - |     504 B |       12.60 |
| AutoMapper_Dictionary             |  97.860 ns | 0.4737 ns | 0.4431 ns | 24.01 |    0.32 | 0.0356 |      - |     448 B |       11.20 |
| Manual_Flattening                 |   4.054 ns | 0.0600 ns | 0.0562 ns |  0.99 |    0.02 | 0.0032 |      - |      40 B |        1.00 |
| DevSourceObjectMapping_Flattening |   4.643 ns | 0.0838 ns | 0.0784 ns |  1.14 |    0.02 | 0.0032 |      - |      40 B |        1.00 |
| Mapster_Flattening                |  11.662 ns | 0.1038 ns | 0.0971 ns |  2.86 |    0.04 | 0.0032 |      - |      40 B |        1.00 |
| AutoMapper_Flattening             |  32.762 ns | 0.1921 ns | 0.1703 ns |  8.04 |    0.11 | 0.0032 |      - |      40 B |        1.00 |
| Manual_Combined                   |  72.556 ns | 0.4032 ns | 0.3771 ns | 17.80 |    0.24 | 0.0535 | 0.0001 |     672 B |       16.80 |
| DevSourceObjectMapping_Combined   |  74.406 ns | 0.7985 ns | 0.7079 ns | 18.25 |    0.29 | 0.0535 |      - |     672 B |       16.80 |
| Mapster_Combined                  | 112.088 ns | 0.7334 ns | 0.6501 ns | 27.50 |    0.38 | 0.0579 |      - |     728 B |       18.20 |
| AutoMapper_Combined               | 126.580 ns | 0.4094 ns | 0.3830 ns | 31.05 |    0.41 | 0.0548 |      - |     688 B |       17.20 |
```

## Samples

Runnable samples are available under `samples/` for:

- basic mapping
- nested objects, collections, and dictionaries
- flattening conventions
- customization scenarios

## Repository Layout

- `src/DevSource.ObjectMapping` - runtime contract surface
- `src/DevSource.ObjectMapping.SourceGenerator` - Roslyn generator implementation
- `tests/DevSource.ObjectMapping.Tests` - unit and diagnostic coverage
- `samples/` - runnable usage examples
- `benchmark/` - BenchmarkDotNet comparison suite

## Status

The library already covers the main mapping scenarios expected from a compile-time mapper and shows benchmark results consistent with its design goals.

It is a strong fit if you want mapping that is fast, explicit, generator-driven, and easy to reason about in code review and debugging sessions.
