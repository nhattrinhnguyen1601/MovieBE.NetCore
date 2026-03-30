namespace MovieApi.Domain.Entities;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public List<UserRole> UserRoles { get; set; } = new();
}