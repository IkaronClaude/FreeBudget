using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountParentAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "nickname",
                table: "bank_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "bank_accounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_bank_account_id",
                table: "bank_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_parent_bank_account_id",
                table: "bank_accounts",
                column: "parent_bank_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bank_accounts_parent_bank_account_id",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "parent_bank_account_id",
                table: "bank_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "nickname",
                table: "bank_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
