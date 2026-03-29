namespace DevSource.ObjectMapping.Sample.Conventions;

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

public class Program
{
    public static void Main()
    {
        Console.WriteLine("== Conventions / flattening ==");

        var source = new OrderSummary
        {
            Id = 7,
            CustomerName = "Valor direto",
            Customer = new CustomerInfo
            {
                Name = "Valor flattening",
                Email = "cliente@example.com"
            }
        };

        var dto = source.ToOrderSummaryDto()!;

        Console.WriteLine($"Id: {dto.Id}");
        Console.WriteLine($"CustomerName (match direto tem precedencia): {dto.CustomerName}");
        Console.WriteLine($"CustomerEmail (via flattening): {dto.CustomerEmail}");
    }
}
