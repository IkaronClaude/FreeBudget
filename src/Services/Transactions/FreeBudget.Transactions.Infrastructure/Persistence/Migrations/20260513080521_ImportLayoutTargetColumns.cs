using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeBudget.Transactions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportLayoutTargetColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "target_amount_column",
                table: "import_layouts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_currency_column",
                table: "import_layouts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "target_amount_column",
                table: "import_layouts");

            migrationBuilder.DropColumn(
                name: "target_currency_column",
                table: "import_layouts");
        }
    }
}
