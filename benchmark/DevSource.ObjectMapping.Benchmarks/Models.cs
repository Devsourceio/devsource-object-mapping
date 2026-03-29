using System.Collections.Generic;
using DevSource.ObjectMapping;

namespace DevSource.ObjectMapping.Benchmarks;

public class User : IMapTo<UserDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class Address : IMapTo<AddressDto>
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class UserWithNested : IMapTo<UserWithNestedDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

public class UserWithNestedDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

public class Order : IMapTo<OrderDto>
{
    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class UserWithCollection : IMapTo<UserWithCollectionDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = [];
}

public class UserWithCollectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<OrderDto> Orders { get; set; } = [];
}

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

public class UserWithDictionary : IMapTo<UserWithDictionaryDto>
{
    public int Id { get; set; }
    public Dictionary<string, Product> FavoriteProducts { get; set; } = [];
}

public class UserWithDictionaryDto
{
    public int Id { get; set; }
    public Dictionary<string, ProductDto> FavoriteProducts { get; set; } = [];
}

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OrderSummary : IMapTo<OrderSummaryDto>
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomerInfo Customer { get; set; } = new();
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}

public class UserAggregate : IMapTo<UserAggregateDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public List<Order> Orders { get; set; } = [];
    public Dictionary<string, Product> FavoriteProducts { get; set; } = [];
}

public class UserAggregateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public List<OrderDto> Orders { get; set; } = [];
    public Dictionary<string, ProductDto> FavoriteProducts { get; set; } = [];
}
