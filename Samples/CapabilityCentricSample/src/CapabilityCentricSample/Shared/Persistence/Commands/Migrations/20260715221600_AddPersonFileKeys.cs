using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapabilityCentricSample.Shared.Persistence.Commands.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonFileKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoFileKey",
                table: "Persons",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFileKey",
                table: "Persons",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoFileKey",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "ResumeFileKey",
                table: "Persons");
        }
    }
}
