namespace DevSource.ObjectMapping.Sample.RootCollections;

public class User : IMapTo<UserDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
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

public class Program
{
    public static void Main()
    {
        Console.WriteLine("== Root collections ==");

        var users = new List<User>
        {
            new() { Id = 1, Name = "Maria", Email = "maria@example.com" },
            new() { Id = 2, Name = "Joao", Email = "joao@example.com" }
        };

        var listQuery = new GetUsersList { Users = users };
        var enumerableQuery = new GetUsersEnumerable { Users = users };
        var collectionQuery = new GetUsersCollection { Users = users };

        var listResult = listQuery.ToListOfUserDto()!;
        var enumerableResult = enumerableQuery.ToEnumerableOfUserDto()!.ToList();
        var collectionResult = collectionQuery.ToCollectionOfUserDto()!;

        Console.WriteLine($"List count: {listResult.Count}");
        Console.WriteLine($"IEnumerable first: {enumerableResult[0].Name}");
        Console.WriteLine($"ICollection last email: {collectionResult.Last().Email}");
    }
}
