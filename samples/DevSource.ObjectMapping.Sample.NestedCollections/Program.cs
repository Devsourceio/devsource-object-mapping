namespace DevSource.ObjectMapping.Sample.NestedCollections;

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

public class OrderItem : IMapTo<OrderItemDto>
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class OrderItemDto
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
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

public class Customer : IMapTo<CustomerDto>
{
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public Dictionary<string, Product> FavoriteProducts { get; set; } = [];
}

public class CustomerDto
{
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public Dictionary<string, ProductDto> FavoriteProducts { get; set; } = [];
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("== Nested + collections + dictionary ==");

        var customer = new Customer
        {
            Name = "Maria",
            Address = new Address
            {
                Street = "Rua Central, 100",
                City = "Sao Paulo"
            },
            Items =
            [
                new OrderItem { Sku = "A-10", Quantity = 2 },
                new OrderItem { Sku = "B-20", Quantity = 1 }
            ],
            FavoriteProducts = new Dictionary<string, Product>
            {
                ["featured"] = new() { Name = "Notebook", Price = 3500m },
                ["backup"] = new() { Name = "Mouse", Price = 150m }
            }
        };

        var dto = customer.ToCustomerDto()!;

        Console.WriteLine($"Customer: {dto.Name}");
        Console.WriteLine($"City: {dto.Address?.City}");
        Console.WriteLine($"Items: {dto.Items.Count}");
        Console.WriteLine($"First item: {dto.Items[0].Sku} x{dto.Items[0].Quantity}");
        Console.WriteLine($"Featured product: {dto.FavoriteProducts["featured"].Name} - {dto.FavoriteProducts["featured"].Price:C}");
    }
}
