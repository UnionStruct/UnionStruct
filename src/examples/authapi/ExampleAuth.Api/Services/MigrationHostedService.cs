using ExampleAuth.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExampleAuth.Api.Services;

public class MigrationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MigrationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExampleAuthContext>();

        await dbContext.Database.MigrateAsync(cancellationToken: stoppingToken);
    }
}