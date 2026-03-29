using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using DevSource.ObjectMapping;
using Mapster;

namespace DevSource.ObjectMapping.Benchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser]
public class MappingBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    private readonly User _simpleUser;
    private readonly UserWithNested _userWithNested;
    private readonly UserWithCollection _userWithCollection;
    private readonly UserWithDictionary _userWithDictionary;
    private readonly OrderSummary _orderSummary;
    private readonly UserAggregate _userAggregate;
    private readonly IMapper _autoMapper;

    public MappingBenchmark()
    {
        _simpleUser = CreateSimpleUser();
        _userWithNested = CreateUserWithNested();
        _userWithCollection = CreateUserWithCollection();
        _userWithDictionary = CreateUserWithDictionary();
        _orderSummary = CreateOrderSummary();
        _userAggregate = CreateUserAggregate();
        _autoMapper = CreateAutoMapper();
    }

    [Benchmark(Baseline = true)]
    public UserDto Manual_Simple() => ManualMapSimple(_simpleUser);

    [Benchmark]
    public UserDto DevSourceObjectMapping_Simple() => _simpleUser.ToUserDto()!;

    [Benchmark]
    public UserDto Mapster_Simple() => _simpleUser.Adapt<UserDto>();

    [Benchmark]
    public UserDto AutoMapper_Simple() => _autoMapper.Map<UserDto>(_simpleUser);

    [Benchmark]
    public UserWithNestedDto Manual_Nested() => ManualMapNested(_userWithNested);

    [Benchmark]
    public UserWithNestedDto DevSourceObjectMapping_Nested() => _userWithNested.ToUserWithNestedDto()!;

    [Benchmark]
    public UserWithNestedDto Mapster_Nested() => _userWithNested.Adapt<UserWithNestedDto>();

    [Benchmark]
    public UserWithNestedDto AutoMapper_Nested() => _autoMapper.Map<UserWithNestedDto>(_userWithNested);

    [Benchmark]
    public UserWithCollectionDto Manual_Collection() => ManualMapCollection(_userWithCollection);

    [Benchmark]
    public UserWithCollectionDto DevSourceObjectMapping_Collection() => _userWithCollection.ToUserWithCollectionDto()!;

    [Benchmark]
    public UserWithCollectionDto Mapster_Collection() => _userWithCollection.Adapt<UserWithCollectionDto>();

    [Benchmark]
    public UserWithCollectionDto AutoMapper_Collection() => _autoMapper.Map<UserWithCollectionDto>(_userWithCollection);

    [Benchmark]
    public UserWithDictionaryDto Manual_Dictionary() => ManualMapDictionary(_userWithDictionary);

    [Benchmark]
    public UserWithDictionaryDto DevSourceObjectMapping_Dictionary() => _userWithDictionary.ToUserWithDictionaryDto()!;

    [Benchmark]
    public UserWithDictionaryDto Mapster_Dictionary() => _userWithDictionary.Adapt<UserWithDictionaryDto>();

    [Benchmark]
    public UserWithDictionaryDto AutoMapper_Dictionary() => _autoMapper.Map<UserWithDictionaryDto>(_userWithDictionary);

    [Benchmark]
    public OrderSummaryDto Manual_Flattening() => ManualMapFlattening(_orderSummary);

    [Benchmark]
    public OrderSummaryDto DevSourceObjectMapping_Flattening() => _orderSummary.ToOrderSummaryDto()!;

    [Benchmark]
    public OrderSummaryDto Mapster_Flattening() => _orderSummary.Adapt<OrderSummaryDto>();

    [Benchmark]
    public OrderSummaryDto AutoMapper_Flattening() => _autoMapper.Map<OrderSummaryDto>(_orderSummary);

    [Benchmark]
    public UserAggregateDto Manual_Combined() => ManualMapCombined(_userAggregate);

    [Benchmark]
    public UserAggregateDto DevSourceObjectMapping_Combined() => _userAggregate.ToUserAggregateDto()!;

    [Benchmark]
    public UserAggregateDto Mapster_Combined() => _userAggregate.Adapt<UserAggregateDto>();

    [Benchmark]
    public UserAggregateDto AutoMapper_Combined() => _autoMapper.Map<UserAggregateDto>(_userAggregate);

    private static User CreateSimpleUser()
    {
        return new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };
    }

    private static UserWithNested CreateUserWithNested()
    {
        return new UserWithNested
        {
            Id = 1,
            Name = "John",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                ZipCode = "12345"
            }
        };
    }

    private static UserWithCollection CreateUserWithCollection()
    {
        return new UserWithCollection
        {
            Id = 1,
            Name = "John",
            Orders =
            [
                new() { Id = 1, Product = "Widget", Quantity = 2, Price = 10.00m },
                new() { Id = 2, Product = "Gadget", Quantity = 1, Price = 25.00m },
                new() { Id = 3, Product = "Gizmo", Quantity = 3, Price = 5.00m }
            ]
        };
    }

    private static UserWithDictionary CreateUserWithDictionary()
    {
        return new UserWithDictionary
        {
            Id = 2,
            FavoriteProducts = new Dictionary<string, Product>
            {
                ["featured"] = new() { Name = "Notebook", Price = 3500m },
                ["backup"] = new() { Name = "Mouse", Price = 150m },
                ["travel"] = new() { Name = "Tablet", Price = 2200m }
            }
        };
    }

    private static OrderSummary CreateOrderSummary()
    {
        return new OrderSummary
        {
            Id = 7,
            CustomerName = "Valor direto",
            Customer = new CustomerInfo
            {
                Name = "Valor flattening",
                Email = "cliente@example.com"
            }
        };
    }

    private static UserAggregate CreateUserAggregate()
    {
        return new UserAggregate
        {
            Id = 10,
            Name = "Aggregate User",
            Address = new Address
            {
                Street = "456 Side St",
                City = "Curitiba",
                ZipCode = "80000-000"
            },
            Orders =
            [
                new() { Id = 10, Product = "Monitor", Quantity = 1, Price = 900m },
                new() { Id = 11, Product = "Keyboard", Quantity = 2, Price = 200m }
            ],
            FavoriteProducts = new Dictionary<string, Product>
            {
                ["office"] = new() { Name = "Chair", Price = 1200m },
                ["audio"] = new() { Name = "Headset", Price = 400m }
            }
        };
    }

    private static IMapper CreateAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>();
            cfg.CreateMap<Address, AddressDto>();
            cfg.CreateMap<UserWithNested, UserWithNestedDto>();
            cfg.CreateMap<Order, OrderDto>();
            cfg.CreateMap<UserWithCollection, UserWithCollectionDto>();
            cfg.CreateMap<Product, ProductDto>();
            cfg.CreateMap<UserWithDictionary, UserWithDictionaryDto>();
            cfg.CreateMap<OrderSummary, OrderSummaryDto>()
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email));
            cfg.CreateMap<UserAggregate, UserAggregateDto>();
        });

        return config.CreateMapper();
    }

    private static UserDto ManualMapSimple(User source)
    {
        return new UserDto
        {
            Id = source.Id,
            Name = source.Name,
            Email = source.Email,
            Age = source.Age
        };
    }

    private static UserWithNestedDto ManualMapNested(UserWithNested source)
    {
        return new UserWithNestedDto
        {
            Id = source.Id,
            Name = source.Name,
            Address = source.Address is null
                ? null
                : new AddressDto
                {
                    Street = source.Address.Street,
                    City = source.Address.City,
                    ZipCode = source.Address.ZipCode
                }
        };
    }

    private static UserWithCollectionDto ManualMapCollection(UserWithCollection source)
    {
        var orders = new List<OrderDto>(source.Orders.Count);
        foreach (var order in source.Orders)
        {
            orders.Add(new OrderDto
            {
                Id = order.Id,
                Product = order.Product,
                Quantity = order.Quantity,
                Price = order.Price
            });
        }

        return new UserWithCollectionDto
        {
            Id = source.Id,
            Name = source.Name,
            Orders = orders
        };
    }

    private static UserWithDictionaryDto ManualMapDictionary(UserWithDictionary source)
    {
        var products = new Dictionary<string, ProductDto>(source.FavoriteProducts.Count);
        foreach (var entry in source.FavoriteProducts)
        {
            products.Add(entry.Key, new ProductDto
            {
                Name = entry.Value.Name,
                Price = entry.Value.Price
            });
        }

        return new UserWithDictionaryDto
        {
            Id = source.Id,
            FavoriteProducts = products
        };
    }

    private static OrderSummaryDto ManualMapFlattening(OrderSummary source)
    {
        return new OrderSummaryDto
        {
            Id = source.Id,
            CustomerName = source.CustomerName,
            CustomerEmail = source.Customer.Email
        };
    }

    private static UserAggregateDto ManualMapCombined(UserAggregate source)
    {
        var target = new UserAggregateDto
        {
            Id = source.Id,
            Name = source.Name,
            Address = source.Address is null
                ? null
                : new AddressDto
                {
                    Street = source.Address.Street,
                    City = source.Address.City,
                    ZipCode = source.Address.ZipCode
                },
            Orders = new List<OrderDto>(source.Orders.Count),
            FavoriteProducts = new Dictionary<string, ProductDto>(source.FavoriteProducts.Count)
        };

        foreach (var order in source.Orders)
        {
            target.Orders.Add(new OrderDto
            {
                Id = order.Id,
                Product = order.Product,
                Quantity = order.Quantity,
                Price = order.Price
            });
        }

        foreach (var entry in source.FavoriteProducts)
        {
            target.FavoriteProducts.Add(entry.Key, new ProductDto
            {
                Name = entry.Value.Name,
                Price = entry.Value.Price
            });
        }

        return target;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkRunner.Run<MappingBenchmark>();
    }
}
