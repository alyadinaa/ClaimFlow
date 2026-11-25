using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Data;

public class ClaimFlowDbContext(DbContextOptions<ClaimFlowDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimFlowDbContext).Assembly);
    }
}
