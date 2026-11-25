using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VetPlatform.Infrastructure.Data;

#nullable disable

namespace VetPlatform.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261124193000_AddAppointmentReasonAndIndexes")]
public partial class AddAppointmentReasonAndIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Reason",
            table: "Appointments",
            type: "character varying(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "Consulta veterinaria");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_ClinicId",
            table: "Appointments",
            column: "ClinicId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_OwnerId",
            table: "Appointments",
            column: "OwnerId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_VetUserId",
            table: "Appointments",
            column: "VetUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Appointments_ClinicId", table: "Appointments");
        migrationBuilder.DropIndex(name: "IX_Appointments_OwnerId", table: "Appointments");
        migrationBuilder.DropIndex(name: "IX_Appointments_VetUserId", table: "Appointments");
        migrationBuilder.DropColumn(name: "Reason", table: "Appointments");
    }
}
