using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchedTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "matched_transaction_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_matched_transaction_id",
                table: "transactions",
                column: "matched_transaction_id",
                filter: "matched_transaction_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_matched_transaction_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "matched_transaction_id",
                table: "transactions");
        }
    }
}
