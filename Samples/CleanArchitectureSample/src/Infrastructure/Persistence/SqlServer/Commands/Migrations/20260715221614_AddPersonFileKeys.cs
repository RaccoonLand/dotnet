using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitectureSample.Persistence.SqlServer.Commands.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonFileKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoFileKey",
                table: "People",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFileKey",
                table: "People",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoFileKey",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ResumeFileKey",
                table: "People");
        }
    }
}
