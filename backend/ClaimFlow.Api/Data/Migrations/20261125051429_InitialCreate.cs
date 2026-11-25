using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedOfficerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Market = table.Column<string>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IncidentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    VehicleRegistration = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PropertyAddress = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ClaimedAmountMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    AssessedAmountMinor = table.Column<long>(type: "INTEGER", nullable: true),
                    SettlementAmountMinor = table.Column<long>(type: "INTEGER", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.CheckConstraint("CK_Claims_AssessedAmount", "AssessedAmountMinor IS NULL OR AssessedAmountMinor > 0");
                    table.CheckConstraint("CK_Claims_Assignment", "(AssignedOfficerId IS NULL) = (Status = 'Submitted')");
                    table.CheckConstraint("CK_Claims_ClaimedAmount", "ClaimedAmountMinor > 0");
                    table.CheckConstraint("CK_Claims_Decided", "(DecidedAtUtc IS NOT NULL) = (Status IN ('Settled', 'Rejected'))");
                    table.CheckConstraint("CK_Claims_Property", "(PropertyAddress IS NOT NULL) = (Type = 'Property')");
                    table.CheckConstraint("CK_Claims_Rejection", "(RejectionReason IS NOT NULL) = (Status = 'Rejected')");
                    table.CheckConstraint("CK_Claims_Settlement", "(SettlementAmountMinor IS NOT NULL) = (Status = 'Settled')");
                    table.CheckConstraint("CK_Claims_SettlementAmount", "SettlementAmountMinor IS NULL OR SettlementAmountMinor > 0");
                    table.CheckConstraint("CK_Claims_Vehicle", "(VehicleRegistration IS NOT NULL) = (Type = 'Motor')");
                    table.ForeignKey(
                        name: "FK_Claims_Users_AssignedOfficerId",
                        column: x => x.AssignedOfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Users_ClaimantId",
                        column: x => x.ClaimantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InformationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedById = table.Column<int>(type: "INTEGER", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Response = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformationRequests", x => x.Id);
                    table.CheckConstraint("CK_InformationRequests_Answered", "(Response IS NOT NULL) = (Status = 'Answered') AND (RespondedAtUtc IS NOT NULL) = (Status = 'Answered')");
                    table.ForeignKey(
                        name: "FK_InformationRequests_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InformationRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorRole = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AmountMinor = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    InformationRequestId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimHistory_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimHistory_InformationRequests_InformationRequestId",
                        column: x => x.InformationRequestId,
                        principalTable: "InformationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimHistory_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistory_ActorId",
                table: "ClaimHistory",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistory_ClaimId_Id",
                table: "ClaimHistory",
                columns: new[] { "ClaimId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistory_InformationRequestId",
                table: "ClaimHistory",
                column: "InformationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_AssignedOfficerId_Status",
                table: "Claims",
                columns: new[] { "AssignedOfficerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimantId",
                table: "Claims",
                column: "ClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_ClaimId",
                table: "InformationRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_RequestedById",
                table: "InformationRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "UX_InformationRequests_OneOpen",
                table: "InformationRequests",
                column: "ClaimId",
                unique: true,
                filter: "Status = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimHistory");

            migrationBuilder.DropTable(
                name: "InformationRequests");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
