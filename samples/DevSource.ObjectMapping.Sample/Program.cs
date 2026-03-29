namespace DevSource.ObjectMapping.Sample;

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

public class Program
{
    public static void Main()
    {
        Console.WriteLine("== Basic mapping ==");

        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        var dto = user.ToUserDto()!;
        Console.WriteLine($"Id: {dto.Id},\nName: {dto.Name},\nEmail: {dto.Email},\nAge: {dto.Age}");
    }
}
