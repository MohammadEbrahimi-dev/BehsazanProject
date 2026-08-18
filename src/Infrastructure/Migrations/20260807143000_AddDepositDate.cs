using Behsazan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Behsazan.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260807143000_AddDepositDate")]
    public class AddDepositDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DepositDate",
                table: "Deposits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE Deposits
                SET DepositDate = CreatedAt
                WHERE DepositDate = '2000-01-01'
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepositDate",
                table: "Deposits",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_DepositDate",
                table: "Deposits",
                column: "DepositDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Deposits_DepositDate",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "DepositDate",
                table: "Deposits");
        }
    }
}
