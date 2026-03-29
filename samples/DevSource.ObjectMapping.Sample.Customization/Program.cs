namespace DevSource.ObjectMapping.Sample.Customization;

public partial class Invoice : IMapTo<InvoiceDto>
{
    public int Number { get; set; }
    public decimal Total { get; set; }

    partial void OnBeforeMap(Invoice source);
    partial void OnAfterMap(Invoice source, InvoiceDto target);
}

public class InvoiceDto
{
    public int Number { get; set; }
    public decimal Total { get; set; }
    public string Display { get; set; } = string.Empty;
}

public partial class Invoice
{
    partial void OnBeforeMap(Invoice source)
    {
        Console.WriteLine($"Preparando mapeamento da invoice {source.Number}");
    }

    partial void OnAfterMap(Invoice source, InvoiceDto target)
    {
        target.Display = $"Invoice #{source.Number} - Total {source.Total:C}";
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("== Customization hooks ==");

        var invoice = new Invoice
        {
            Number = 42,
            Total = 199.90m
        };

        var dto = invoice.ToInvoiceDto()!;

        Console.WriteLine($"Number: {dto.Number}");
        Console.WriteLine($"Total: {dto.Total}");
        Console.WriteLine($"Display: {dto.Display}");

        if (string.IsNullOrWhiteSpace(dto.Display))
        {
            Console.WriteLine("Observacao: os hooks foram declarados no sample, mas nao foram executados pela versao atual do generator.");
        }
    }
}
