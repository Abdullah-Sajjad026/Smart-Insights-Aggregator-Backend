using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInsights.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicStatusAndUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Topics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusUpdatedAt",
                table: "Topics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TopicUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicUpdates_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicUpdates_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicUpdates_CreatedById",
                table: "TopicUpdates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TopicUpdates_TopicId",
                table: "TopicUpdates",
                column: "TopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicUpdates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAt",
                table: "Topics");
        }
    }
}
