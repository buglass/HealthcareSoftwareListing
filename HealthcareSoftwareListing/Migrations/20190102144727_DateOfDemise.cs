using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthcareSoftwareListing.Migrations
{
    public partial class DateOfDemise : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateOfDemise",
                table: "Companies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfDemise",
                table: "Companies");
        }
    }
}
