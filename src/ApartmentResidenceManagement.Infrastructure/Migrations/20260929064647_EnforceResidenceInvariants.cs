using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceResidenceInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveOwnerApartmentId",
                table: "ResidenceHistories",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN `IsActive` = 1 AND `RelationshipType` = 0 THEN `ApartmentId` ELSE NULL END",
                stored: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveResidentId",
                table: "ResidenceHistories",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN `IsActive` = 1 THEN `ResidentId` ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ActiveOwnerApartmentId",
                table: "ResidenceHistories",
                column: "ActiveOwnerApartmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_ActiveResidentId",
                table: "ResidenceHistories",
                column: "ActiveResidentId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResidenceHistories_ActiveEndDate",
                table: "ResidenceHistories",
                sql: "(`IsActive` = 1 AND `EndDate` IS NULL) OR (`IsActive` = 0 AND `EndDate` IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResidenceHistories_DateRange",
                table: "ResidenceHistories",
                sql: "`EndDate` IS NULL OR `EndDate` >= `StartDate`");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResidenceHistories_ActiveOwnerApartmentId",
                table: "ResidenceHistories");

            migrationBuilder.DropIndex(
                name: "IX_ResidenceHistories_ActiveResidentId",
                table: "ResidenceHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ResidenceHistories_ActiveEndDate",
                table: "ResidenceHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ResidenceHistories_DateRange",
                table: "ResidenceHistories");

            migrationBuilder.DropColumn(
                name: "ActiveOwnerApartmentId",
                table: "ResidenceHistories");

            migrationBuilder.DropColumn(
                name: "ActiveResidentId",
                table: "ResidenceHistories");
        }
    }
}
