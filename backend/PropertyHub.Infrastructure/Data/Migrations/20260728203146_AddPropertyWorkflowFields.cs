using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AreaSquareFeet",
                table: "Properties",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAtUtc",
                table: "Properties",
                type: "datetime2(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedByUserId",
                table: "Properties",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedAddress",
                table: "Properties",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "Properties",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Properties",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SellerProfileId_NormalizedTitle_NormalizedAddress_Purpose_PropertyType",
                table: "Properties",
                columns: new[] { "SellerProfileId", "NormalizedTitle", "NormalizedAddress", "Purpose", "PropertyType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_SellerProfileId_NormalizedTitle_NormalizedAddress_Purpose_PropertyType",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "AreaSquareFeet",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ModeratedAtUtc",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NormalizedAddress",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Properties");
        }
    }
}
