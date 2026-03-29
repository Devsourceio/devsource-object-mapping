namespace DevSource.ObjectMapping.Tests;

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

public class BasicMappingTests
{
    [Fact]
    public void SimpleMapping_Works()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        var dto = user.ToUserDto()!;

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Name, dto.Name);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.Age, dto.Age);
    }

    [Fact]
    public void NullMapping_ReturnsNull()
    {
        User? user = null;
        var dto = user?.ToUserDto();
        Assert.Null(dto);
    }

    [Fact]
    public void MappingWithNullNestedObject_Works()
    {
        var user = new UserWithAddress
        {
            Id = 1,
            Name = "John",
            Address = null
        };

        var dto = user.ToUserWithAddressDto()!;
        
        Assert.Equal(1, dto.Id);
        Assert.Equal("John", dto.Name);
        Assert.Null(dto.Address);
    }

    [Fact]
    public void MappingWithNestedObject_Works()
    {
        var user = new UserWithAddress
        {
            Id = 1,
            Name = "John",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield"
            }
        };

        var dto = user.ToUserWithAddressDto()!;
        
        Assert.Equal(1, dto.Id);
        Assert.NotNull(dto.Address);
        Assert.Equal("123 Main St", dto.Address!.Street);
        Assert.Equal("Springfield", dto.Address.City);
    }

    [Fact]
    public void MappingWithCollection_Works()
    {
        var order = new Order
        {
            Id = 1,
            Items =
            [
                new OrderItem { ProductName = "Widget", Quantity = 2 },
                new OrderItem { ProductName = "Gadget", Quantity = 1 }
            ]
        };

        var dto = order.ToOrderDto()!;
        
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("Widget", dto.Items[0].ProductName);
        Assert.Equal(2, dto.Items[0].Quantity);
    }

    [Fact]
    public void MappingWithCircularReference_CutsRecursiveBranchSafely()
    {
        var parent = new CyclicParent
        {
            Name = "Parent"
        };

        var child = new CyclicChild
        {
            Name = "Child",
            Parent = parent
        };

        parent.Child = child;

        var dto = parent.ToCyclicParentDto()!;

        Assert.Equal("Parent", dto.Name);
        Assert.NotNull(dto.Child);
        Assert.Equal("Child", dto.Child!.Name);
        Assert.Null(dto.Child.Parent);
    }

    [Fact]
    public void MappingWithSelfCircularCollectionReference_SuppressesRecursiveCollectionSafely()
    {
        var root = new TreeNode
        {
            Name = "Root"
        };

        var child = new TreeNode
        {
            Name = "Child",
            Children = [root]
        };

        root.Children = [child];

        var dto = root.ToTreeNodeDto()!;

        Assert.Equal("Root", dto.Name);
        Assert.Empty(dto.Children);
    }

    [Fact]
    public void MappingWithDictionaryOfPrimitiveValues_Works()
    {
        var source = new Catalog
        {
            Quantities = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 2
            }
        };

        var dto = source.ToCatalogDto()!;

        Assert.Equal(2, dto.Quantities.Count);
        Assert.Equal(1, dto.Quantities["A"]);
        Assert.Equal(2, dto.Quantities["B"]);
    }

    [Fact]
    public void MappingWithDictionaryOfNestedValues_Works()
    {
        var source = new ProductDirectory
        {
            Products = new Dictionary<string, Product>
            {
                ["widget"] = new() { Name = "Widget", Price = 10 },
                ["gadget"] = new() { Name = "Gadget", Price = 20 }
            }
        };

        var dto = source.ToProductDirectoryDto()!;

        Assert.Equal(2, dto.Products.Count);
        Assert.Equal("Widget", dto.Products["widget"].Name);
        Assert.Equal(20, dto.Products["gadget"].Price);
    }

    [Fact]
    public void MappingWithFlatteningConvention_Works()
    {
        var source = new OrderSummary
        {
            Id = 7,
            Customer = new CustomerInfo
            {
                Name = "Maria",
                Email = "maria@example.com"
            }
        };

        var dto = source.ToOrderSummaryDto()!;

        Assert.Equal(7, dto.Id);
        Assert.Equal("Maria", dto.CustomerName);
        Assert.Equal("maria@example.com", dto.CustomerEmail);
    }

    [Fact]
    public void MappingWithExactMatch_PrefersDirectPropertyOverFlattening()
    {
        var source = new CustomerProjection
        {
            CustomerName = "Direct",
            Customer = new CustomerInfo
            {
                Name = "Flattened"
            }
        };

        var dto = source.ToCustomerProjectionDto()!;

        Assert.Equal("Direct", dto.CustomerName);
    }
}

public class UserWithAddress : IMapTo<UserWithAddressDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

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

public class UserWithAddressDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
}

public class Order : IMapTo<OrderDto>
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem : IMapTo<OrderItemDto>
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

#pragma warning disable DSM009

public class CyclicParent : IMapTo<CyclicParentDto>
{
    public string Name { get; set; } = string.Empty;
    public CyclicChild? Child { get; set; }
}

public class CyclicChild : IMapTo<CyclicChildDto>
{
    public string Name { get; set; } = string.Empty;
    public CyclicParent? Parent { get; set; }
}

public class CyclicParentDto
{
    public string Name { get; set; } = string.Empty;
    public CyclicChildDto? Child { get; set; }
}

public class CyclicChildDto
{
    public string Name { get; set; } = string.Empty;
    public CyclicParentDto? Parent { get; set; }
}

public class TreeNode : IMapTo<TreeNodeDto>
{
    public string Name { get; set; } = string.Empty;
    public List<TreeNode> Children { get; set; } = [];
}

public class TreeNodeDto
{
    public string Name { get; set; } = string.Empty;
    public List<TreeNodeDto> Children { get; set; } = [];
}

public class Catalog : IMapTo<CatalogDto>
{
    public Dictionary<string, int> Quantities { get; set; } = [];
}

public class CatalogDto
{
    public Dictionary<string, int> Quantities { get; set; } = [];
}

public class ProductDirectory : IMapTo<ProductDirectoryDto>
{
    public Dictionary<string, Product> Products { get; set; } = [];
}

public class ProductDirectoryDto
{
    public Dictionary<string, ProductDto> Products { get; set; } = [];
}

public class Product : IMapTo<ProductDto>
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
}

public class ProductDto
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
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

public class CustomerProjection : IMapTo<CustomerProjectionDto>
{
    public string CustomerName { get; set; } = string.Empty;
    public CustomerInfo Customer { get; set; } = new();
}

public class CustomerProjectionDto
{
    public string CustomerName { get; set; } = string.Empty;
}

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

#pragma warning restore DSM009
