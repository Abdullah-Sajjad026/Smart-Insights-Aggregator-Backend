using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInsights.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTopicUpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                table: "TopicUpdates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAdminId",
                table: "TopicUpdates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
