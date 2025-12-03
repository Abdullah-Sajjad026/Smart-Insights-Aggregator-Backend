using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInsights.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToTopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Topics",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Topics");
        }
    }
}
