using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class InformationRequestConfiguration : IEntityTypeConfiguration<InformationRequest>
{
    public void Configure(EntityTypeBuilder<InformationRequest> builder)
    {
        builder.Property(r => r.Question).HasMaxLength(2000);
        builder.Property(r => r.Response).HasMaxLength(2000);

        builder.HasOne(r => r.Claim)
            .WithMany(c => c.InformationRequests)
            .HasForeignKey(r => r.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedBy)
            .WithMany()
            .HasForeignKey(r => r.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Both indexes use ClaimId, so each needs its own name; otherwise one overwrites the other.
        builder.HasIndex(r => r.ClaimId, "IX_InformationRequests_ClaimId");
        builder.HasIndex(r => r.ClaimId, "UX_InformationRequests_OneOpen")
            .IsUnique()
            .HasFilter("Status = 'Open'");

        builder.ToTable(table => table.HasCheckConstraint("CK_InformationRequests_Answered",
            "(Response IS NOT NULL) = (Status = 'Answered') AND (RespondedAtUtc IS NOT NULL) = (Status = 'Answered')"));
    }
}
