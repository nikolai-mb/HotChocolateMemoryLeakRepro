using Microsoft.EntityFrameworkCore;

namespace HCMemoryLeakRepro;

public class ApplicationDbContext : DbContext
{
    public DbSet<CarBrand> CarBrands { get; private set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var carBrands = modelBuilder.Entity<CarBrand>();
        carBrands.HasData(
            new CarBrand(1, "Audi"),
            new CarBrand(2, "Bmw"),
            new CarBrand(3, "Mercedes"));
    }
}