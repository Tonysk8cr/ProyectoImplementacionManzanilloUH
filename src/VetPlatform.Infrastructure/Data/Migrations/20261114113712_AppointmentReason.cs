using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Consulta veterinaria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Appointments");
        }
    }
}
