using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sharing_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    match_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_by_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_member_ids = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sharing_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sharing_rules_created_by_user_id",
                table: "sharing_rules",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sharing_rules");
        }
    }
}
