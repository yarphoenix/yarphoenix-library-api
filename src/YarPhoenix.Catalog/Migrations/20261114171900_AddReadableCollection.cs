using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YarPhoenix.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddReadableCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<int>(
                name: "FallbackGutenbergId",
                schema: "catalog",
                table: "Books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Genres",
                schema: "catalog",
                table: "Books",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "InCollection",
                schema: "catalog",
                table: "Books",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                schema: "catalog",
                table: "Books",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReadableSource",
                schema: "catalog",
                table: "Books",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                // Имя значения enum, а не пустая строка: существующие строки должны читаться.
                defaultValue: "None");

            // Книги, загруженные пользователями до этой миграции: свой файл и нет каталожной записи.
            migrationBuilder.Sql(
                """
                UPDATE catalog."Books"
                SET "ReadableSource" = 'Upload'
                WHERE "FileId" IS NOT NULL AND "ExternalId" IS NULL;
                """);

            migrationBuilder.AddColumn<string>(
                name: "SourceRef",
                schema: "catalog",
                table: "Books",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Subjects",
                schema: "catalog",
                table: "Books",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "CollectionImportStates",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    NextPage = table.Column<int>(type: "integer", nullable: false),
                    LastCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionImportStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Author",
                schema: "catalog",
                table: "Books",
                column: "Author")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Genres",
                schema: "catalog",
                table: "Books",
                column: "Genres")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Popularity",
                schema: "catalog",
                table: "Books",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"InCollection\"");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                schema: "catalog",
                table: "Books",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionImportStates",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Books_Author",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Genres",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Popularity",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Title",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "FallbackGutenbergId",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Genres",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "InCollection",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Popularity",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ReadableSource",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "SourceRef",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Subjects",
                schema: "catalog",
                table: "Books");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
