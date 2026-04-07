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

## AI Integration
- See `/mcp` for AI usage instructions.

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
3. Call the generated extension method for the target shape.

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

For root collection targets, the generator emits collection-specific method names:

```csharp
var list = query.ToListOfUserDto();
var enumerable = query.ToEnumerableOfUserDto();
var collection = query.ToCollectionOfUserDto();
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
- root collection mapping via `IMapTo<List<T>>`, `IMapTo<IEnumerable<T>>`, and `IMapTo<ICollection<T>>`
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

## Example: Root Collection Mapping

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

public record GetUsersList : IMapTo<List<UserDto>>
{
    public List<User> Users { get; init; } = [];
}

public record GetUsersEnumerable : IMapTo<IEnumerable<UserDto>>
{
    public IEnumerable<User> Users { get; init; } = [];
}

public record GetUsersCollection : IMapTo<ICollection<UserDto>>
{
    public ICollection<User> Users { get; init; } = [];
}

var list = new GetUsersList { Users = users }.ToListOfUserDto();
var enumerable = new GetUsersEnumerable { Users = users }.ToEnumerableOfUserDto();
var collection = new GetUsersCollection { Users = users }.ToCollectionOfUserDto();
```

Rules for root collection mapping:

- the source type must expose exactly one compatible public collection property
- the source collection element must map to the target element via `IMapTo<T>` or the existing DTO naming convention
- if zero compatible collections are found, or more than one is found, generation fails with diagnostics

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
- support for root collection projections when the source is a query/command object that wraps a single collection

## Limitations

- mappings are intentionally one-way and convention-driven
- property matching remains strict and case-sensitive
- unsupported or ambiguous scenarios are rejected instead of guessed
- strict nullability rules may require explicit handling in some mappings
- root collection mapping requires exactly one compatible public collection on the source type
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
| Method                                | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Manual_Simple                         |   4.502 ns | 0.0498 ns | 0.0466 ns |   4.508 ns |  1.00 |    0.01 | 0.0032 |      - |      40 B |        1.00 |
| DevSourceObjectMapping_Simple         |   4.614 ns | 0.1180 ns | 0.1104 ns |   4.641 ns |  1.03 |    0.03 | 0.0032 |      - |      40 B |        1.00 |
| Mapster_Simple                        |   9.091 ns | 0.2187 ns | 0.2994 ns |   8.935 ns |  2.02 |    0.07 | 0.0032 |      - |      40 B |        1.00 |
| AutoMapper_Simple                     |  30.842 ns | 0.1772 ns | 0.1658 ns |  30.831 ns |  6.85 |    0.08 | 0.0032 |      - |      40 B |        1.00 |
| Manual_RootList                       |  28.151 ns | 0.5950 ns | 0.5844 ns |  27.970 ns |  6.25 |    0.14 | 0.0159 |      - |     200 B |        5.00 |
| DevSourceObjectMapping_RootList       |  28.670 ns | 0.3904 ns | 0.3260 ns |  28.694 ns |  6.37 |    0.09 | 0.0166 |      - |     208 B |        5.20 |
| Mapster_RootList                      |  34.069 ns | 0.6583 ns | 0.6466 ns |  34.229 ns |  7.57 |    0.16 | 0.0159 |      - |     200 B |        5.00 |
| AutoMapper_RootList                   |  58.589 ns | 1.0317 ns | 0.9650 ns |  58.788 ns | 13.01 |    0.25 | 0.0166 |      - |     208 B |        5.20 |
| Manual_RootEnumerable                 |  41.192 ns | 0.8630 ns | 1.3689 ns |  40.439 ns |  9.15 |    0.31 | 0.0229 |      - |     288 B |        7.20 |
| DevSourceObjectMapping_RootEnumerable |  37.812 ns | 0.2067 ns | 0.1833 ns |  37.798 ns |  8.40 |    0.09 | 0.0229 |      - |     288 B |        7.20 |
| Mapster_RootEnumerable                |  30.942 ns | 0.2314 ns | 0.2165 ns |  30.918 ns |  6.87 |    0.08 | 0.0159 |      - |     200 B |        5.00 |
| AutoMapper_RootEnumerable             |  69.071 ns | 0.3486 ns | 0.3090 ns |  69.040 ns | 15.34 |    0.17 | 0.0229 |      - |     288 B |        7.20 |
| Manual_RootCollection                 |  26.116 ns | 0.1992 ns | 0.1863 ns |  26.114 ns |  5.80 |    0.07 | 0.0166 |      - |     208 B |        5.20 |
| DevSourceObjectMapping_RootCollection |  27.767 ns | 0.2045 ns | 0.1912 ns |  27.759 ns |  6.17 |    0.07 | 0.0166 |      - |     208 B |        5.20 |
| Mapster_RootCollection                |  31.261 ns | 0.2618 ns | 0.2449 ns |  31.275 ns |  6.94 |    0.09 | 0.0159 |      - |     200 B |        5.00 |
| AutoMapper_RootCollection             |  53.704 ns | 0.3027 ns | 0.2831 ns |  53.621 ns | 11.93 |    0.13 | 0.0166 |      - |     208 B |        5.20 |
| Manual_Nested                         |   8.795 ns | 0.0938 ns | 0.0878 ns |   8.783 ns |  1.95 |    0.03 | 0.0064 |      - |      80 B |        2.00 |
| DevSourceObjectMapping_Nested         |   8.167 ns | 0.0788 ns | 0.0737 ns |   8.187 ns |  1.81 |    0.02 | 0.0064 |      - |      80 B |        2.00 |
| Mapster_Nested                        |  15.529 ns | 0.1414 ns | 0.1322 ns |  15.505 ns |  3.45 |    0.04 | 0.0063 |      - |      80 B |        2.00 |
| AutoMapper_Nested                     |  38.567 ns | 0.1184 ns | 0.1107 ns |  38.566 ns |  8.57 |    0.09 | 0.0063 |      - |      80 B |        2.00 |
| Manual_Collection                     |  32.863 ns | 0.6650 ns | 0.6221 ns |  32.601 ns |  7.30 |    0.15 | 0.0235 |      - |     296 B |        7.40 |
| DevSourceObjectMapping_Collection     |  30.987 ns | 0.5799 ns | 0.5425 ns |  31.057 ns |  6.88 |    0.14 | 0.0235 |      - |     296 B |        7.40 |
| Mapster_Collection                    |  36.157 ns | 0.4102 ns | 0.3425 ns |  36.234 ns |  8.03 |    0.11 | 0.0235 |      - |     296 B |        7.40 |
| AutoMapper_Collection                 |  65.215 ns | 0.6144 ns | 0.5747 ns |  65.358 ns | 14.49 |    0.19 | 0.0242 |      - |     304 B |        7.60 |
| Manual_Dictionary                     |  48.476 ns | 0.3394 ns | 0.3175 ns |  48.533 ns | 10.77 |    0.13 | 0.0357 |      - |     448 B |       11.20 |
| DevSourceObjectMapping_Dictionary     |  51.005 ns | 0.6629 ns | 0.5536 ns |  50.864 ns | 11.33 |    0.16 | 0.0357 | 0.0001 |     448 B |       11.20 |
| Mapster_Dictionary                    |  93.286 ns | 1.6285 ns | 1.5233 ns |  93.658 ns | 20.72 |    0.39 | 0.0401 |      - |     504 B |       12.60 |
| AutoMapper_Dictionary                 |  97.592 ns | 1.2601 ns | 1.1787 ns |  97.722 ns | 21.68 |    0.33 | 0.0356 |      - |     448 B |       11.20 |
| Manual_Flattening                     |   3.886 ns | 0.0506 ns | 0.0473 ns |   3.884 ns |  0.86 |    0.01 | 0.0032 |      - |      40 B |        1.00 |
| DevSourceObjectMapping_Flattening     |   3.940 ns | 0.0343 ns | 0.0304 ns |   3.945 ns |  0.88 |    0.01 | 0.0032 |      - |      40 B |        1.00 |
| Mapster_Flattening                    |  10.614 ns | 0.1140 ns | 0.1067 ns |  10.650 ns |  2.36 |    0.03 | 0.0032 |      - |      40 B |        1.00 |
| AutoMapper_Flattening                 |  30.659 ns | 0.1746 ns | 0.1633 ns |  30.655 ns |  6.81 |    0.08 | 0.0032 |      - |      40 B |        1.00 |
| Manual_Combined                       |  70.233 ns | 0.5914 ns | 0.4938 ns |  70.283 ns | 15.60 |    0.19 | 0.0535 | 0.0001 |     672 B |       16.80 |
| DevSourceObjectMapping_Combined       |  73.812 ns | 0.6388 ns | 0.5975 ns |  73.935 ns | 16.40 |    0.21 | 0.0535 |      - |     672 B |       16.80 |
| Mapster_Combined                      | 110.019 ns | 1.1498 ns | 1.0192 ns | 110.000 ns | 24.44 |    0.33 | 0.0579 |      - |     728 B |       18.20 |
| AutoMapper_Combined                   | 123.211 ns | 0.6699 ns | 0.6266 ns | 123.376 ns | 27.37 |    0.31 | 0.0548 |      - |     688 B |       17.20 |
```

## Samples

Runnable samples are available under `samples/` for:

- basic mapping
- nested objects, collections, and dictionaries
- root collection mapping for `List<T>`, `IEnumerable<T>`, and `ICollection<T>`
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
