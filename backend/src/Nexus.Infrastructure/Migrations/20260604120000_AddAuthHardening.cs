using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <summary>
    /// Auth hardening pass: hash refresh tokens at rest, add rotation/audit columns,
    /// and introduce the User.Role column for RBAC. Combines what would otherwise
    /// be three back-to-back migrations because all three landed in the same commit
    /// and the existing refresh_tokens table contains no rows yet (added 1 day ago).
    /// </summary>
    public partial class AddAuthHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- refresh_tokens: rename Token -> TokenHash and add rotation/audit cols ---

            // Drop the old unique index first; we'll rebuild it on the renamed column.
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_Token",
                schema: "nexus",
                table: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "Token",
                schema: "nexus",
                table: "refresh_tokens",
                newName: "TokenHash");

            // Hex HMAC-SHA256 = 64 chars; 128 leaves headroom.
            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                schema: "nexus",
                table: "refresh_tokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedByTokenId",
                schema: "nexus",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByIp",
                schema: "nexus",
                table: "refresh_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "nexus",
                table: "refresh_tokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "nexus",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            // --- users: add Role column (default = Member = 0) ---

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "nexus",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "nexus",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "nexus",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "nexus",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "CreatedByIp",
                schema: "nexus",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "ReplacedByTokenId",
                schema: "nexus",
                table: "refresh_tokens");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                schema: "nexus",
                table: "refresh_tokens",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                schema: "nexus",
                table: "refresh_tokens",
                newName: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                schema: "nexus",
                table: "refresh_tokens",
                column: "Token",
                unique: true);
        }
    }
}
