using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClaimFlow.Api.Data;

public class ClaimFlowDbContext(DbContextOptions<ClaimFlowDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<ClaimHistory> ClaimHistory => Set<ClaimHistory>();

    public DbSet<InformationRequest> InformationRequests => Set<InformationRequest>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimFlowDbContext).Assembly);
    }

    // SQLite does not keep DateTime.Kind, so values are marked as UTC when read.
    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
