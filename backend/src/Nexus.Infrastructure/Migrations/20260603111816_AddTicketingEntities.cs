using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssigneeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tickets_users_AssigneeUserId",
                        column: x => x.AssigneeUserId,
                        principalSchema: "nexus",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tickets_users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalSchema: "nexus",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalSchema: "nexus",
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_comments",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_comments_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "nexus",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ticket_comments_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "nexus",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_status_changes",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_status_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_status_changes_tickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "nexus",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ticket_status_changes_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalSchema: "nexus",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_comments_TicketId",
                schema: "nexus",
                table: "ticket_comments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_comments_UserId",
                schema: "nexus",
                table: "ticket_comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_status_changes_ChangedByUserId",
                schema: "nexus",
                table: "ticket_status_changes",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_status_changes_TicketId",
                schema: "nexus",
                table: "ticket_status_changes",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_AssigneeUserId",
                schema: "nexus",
                table: "tickets",
                column: "AssigneeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CreatorUserId",
                schema: "nexus",
                table: "tickets",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_WorkspaceId_Number",
                schema: "nexus",
                table: "tickets",
                columns: new[] { "WorkspaceId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_comments",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "ticket_status_changes",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "nexus");
        }
    }
}
