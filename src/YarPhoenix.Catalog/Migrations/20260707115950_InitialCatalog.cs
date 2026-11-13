using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YarPhoenix.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Books",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FileId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Author = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CoverId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    FirstPublishYear = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReaderProfiles",
                schema: "catalog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastActiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                schema: "catalog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Threshold = table.Column<int>(type: "integer", nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => new { x.UserId, x.Threshold });
                });

            migrationBuilder.CreateTable(
                name: "UserBooks",
                schema: "catalog",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CfiPosition = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Progress = table.Column<double>(type: "double precision", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBooks", x => new { x.UserId, x.BookId });
                    table.ForeignKey(
                        name: "FK_UserBooks_Books_BookId",
                        column: x => x.BookId,
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Provider_ExternalId",
                schema: "catalog",
                table: "Books",
                columns: new[] { "Provider", "ExternalId" },
                unique: true,
                filter: "\"Provider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserBooks_BookId",
                schema: "catalog",
                table: "UserBooks",
                column: "BookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReaderProfiles",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "UserAchievements",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "UserBooks",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Books",
                schema: "catalog");
        }
    }
}
