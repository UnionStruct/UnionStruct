using Microsoft.EntityFrameworkCore;

namespace ExampleAuth.Api.Infrastructure.Persistence;

public class ExampleAuthContext : DbContext
{
    public ExampleAuthContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExampleAuthContext).Assembly);
    }
}