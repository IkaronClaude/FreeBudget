using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "paid_by_user_id",
                table: "ledger_entries",
                newName: "paid_by_member_id");

            migrationBuilder.RenameColumn(
                name: "owed_by_user_id",
                table: "ledger_entries",
                newName: "owed_by_member_id");

            migrationBuilder.RenameIndex(
                name: "IX_ledger_entries_paid_by_user_id",
                table: "ledger_entries",
                newName: "IX_ledger_entries_paid_by_member_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_at",
                table: "ledger_entries",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "entry_date",
                table: "ledger_entries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ledger_entries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "paid_by_member_id",
                table: "ledger_entries",
                newName: "paid_by_user_id");

            migrationBuilder.RenameColumn(
                name: "owed_by_member_id",
                table: "ledger_entries",
                newName: "owed_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_ledger_entries_paid_by_member_id",
                table: "ledger_entries",
                newName: "IX_ledger_entries_paid_by_user_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_at",
                table: "ledger_entries",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "entry_date",
                table: "ledger_entries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ledger_entries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
