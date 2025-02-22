namespace ExampleAuth.Api.Users.Entities;

public record User
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Login { get; init; }
    public required string Password { get; init; }
}