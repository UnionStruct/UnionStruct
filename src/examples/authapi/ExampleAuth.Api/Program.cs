using ExampleAuth.Api.Infrastructure.Persistence;
using ExampleAuth.Api.Services;
using ExampleAuth.Api.Services.Abstraction;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<ExampleAuthContext>(opt => { opt.UseSqlite("Database=example.auth.db"); });
builder.Services.AddTransient<ISecurityService, SecurityService>();
builder.Services.AddHostedService<MigrationHostedService>();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); });

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();

await app.RunAsync();