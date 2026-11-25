using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Email).HasMaxLength(254);
        builder.Property(u => u.DisplayName).HasMaxLength(100);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
