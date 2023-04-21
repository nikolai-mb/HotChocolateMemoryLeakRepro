using HCMemoryLeakRepro;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
});

builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .RegisterDbContext<ApplicationDbContext>(DbContextKind.Pooled)
    .InitializeOnStartup();

var app = builder.Build();

// Create database
using var context = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();
context.Database.EnsureCreated();

app.MapGraphQL();
app.Run();