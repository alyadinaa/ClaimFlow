using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class ClaimHistoryConfiguration : IEntityTypeConfiguration<ClaimHistory>
{
    public void Configure(EntityTypeBuilder<ClaimHistory> builder)
    {
        builder.Property(h => h.Note).HasMaxLength(2000);
        builder.Property(h => h.CurrencyCode).HasMaxLength(3);

        builder.HasOne(h => h.Claim)
            .WithMany(c => c.History)
            .HasForeignKey(h => h.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many history rows may reference the same request.
        builder.HasOne(h => h.InformationRequest)
            .WithMany()
            .HasForeignKey(h => h.InformationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.ClaimId, h.Id });
    }
}
