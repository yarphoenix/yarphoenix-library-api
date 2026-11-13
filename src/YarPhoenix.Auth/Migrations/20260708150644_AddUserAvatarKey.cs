using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YarPhoenix.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarKey",
                schema: "auth",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarKey",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
