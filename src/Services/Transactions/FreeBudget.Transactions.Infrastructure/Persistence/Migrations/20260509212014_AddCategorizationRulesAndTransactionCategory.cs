using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorizationRulesAndTransactionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorization_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    match_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorization_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_category",
                table: "transactions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_categorization_rules_created_by_user_id",
                table: "categorization_rules",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorization_rules");

            migrationBuilder.DropIndex(
                name: "IX_transactions_category",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "category",
                table: "transactions");
        }
    }
}
