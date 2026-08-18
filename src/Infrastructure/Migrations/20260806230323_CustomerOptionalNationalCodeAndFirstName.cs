using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Behsazan.Infrastructure.Migrations
{
    public partial class CustomerOptionalNationalCodeAndFirstName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_NationalCode",
                table: "Customers");

            migrationBuilder.Sql(
                "UPDATE [Customers] SET [NationalCode] = NULL WHERE [NationalCode] = N'' OR LTRIM(RTRIM([NationalCode])) = N''");

            migrationBuilder.AlterColumn<string>(
                name: "NationalCode",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NationalCode",
                table: "Customers",
                column: "NationalCode",
                unique: true,
                filter: "[NationalCode] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_NationalCode",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "NationalCode",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NationalCode",
                table: "Customers",
                column: "NationalCode",
                unique: true);
        }
    }
}
