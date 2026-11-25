using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimFlow.Api.Data.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.Property(c => c.CurrencyCode).HasMaxLength(3);
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.VehicleRegistration).HasMaxLength(20);
        builder.Property(c => c.PropertyAddress).HasMaxLength(300);
        builder.Property(c => c.RejectionReason).HasMaxLength(2000);

        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasOne(c => c.Claimant)
            .WithMany()
            .HasForeignKey(c => c.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AssignedOfficer)
            .WithMany()
            .HasForeignKey(c => c.AssignedOfficerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ClaimantId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => new { c.AssignedOfficerId, c.Status });

        // Enum values are stored as text, so these rules depend on the enum names.
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Claims_Assignment",
                "(AssignedOfficerId IS NULL) = (Status = 'Submitted')");
            table.HasCheckConstraint("CK_Claims_ClaimedAmount",
                "ClaimedAmountMinor > 0");
            table.HasCheckConstraint("CK_Claims_AssessedAmount",
                "AssessedAmountMinor IS NULL OR AssessedAmountMinor > 0");
            table.HasCheckConstraint("CK_Claims_SettlementAmount",
                "SettlementAmountMinor IS NULL OR SettlementAmountMinor > 0");
            table.HasCheckConstraint("CK_Claims_Settlement",
                "(SettlementAmountMinor IS NOT NULL) = (Status = 'Settled')");
            table.HasCheckConstraint("CK_Claims_Rejection",
                "(RejectionReason IS NOT NULL) = (Status = 'Rejected')");
            table.HasCheckConstraint("CK_Claims_Decided",
                "(DecidedAtUtc IS NOT NULL) = (Status IN ('Settled', 'Rejected'))");
            table.HasCheckConstraint("CK_Claims_Vehicle",
                "(VehicleRegistration IS NOT NULL) = (Type = 'Motor')");
            table.HasCheckConstraint("CK_Claims_Property",
                "(PropertyAddress IS NOT NULL) = (Type = 'Property')");
        });
    }
}
