using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyImageDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "PropertyImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "PropertyImages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "PropertyImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "PropertyImages");
        }
    }
}
