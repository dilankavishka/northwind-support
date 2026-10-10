using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportDesk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Escalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EscalatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escalations_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_TicketId",
                table: "Escalations",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Escalations");
        }
    }
}
