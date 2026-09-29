using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentResidenceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegistrationStatus",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Các xe đã tồn tại trước khi có quy trình duyệt được xem là đã duyệt.
            migrationBuilder.Sql("UPDATE Vehicles SET RegistrationStatus = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "Vehicles");
        }
    }
}
