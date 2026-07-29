using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCanonicalCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "IsActive", "Latitude", "Longitude", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), true, 31.520400m, 74.358700m, "Lahore", "LAHORE" },
                    { new Guid("10000000-0000-4000-8000-000000000002"), true, 24.860700m, 67.001100m, "Karachi", "KARACHI" },
                    { new Guid("10000000-0000-4000-8000-000000000003"), true, 33.684400m, 73.047900m, "Islamabad", "ISLAMABAD" },
                    { new Guid("10000000-0000-4000-8000-000000000004"), true, 33.565100m, 73.016900m, "Rawalpindi", "RAWALPINDI" },
                    { new Guid("10000000-0000-4000-8000-000000000005"), true, 31.450400m, 73.135000m, "Faisalabad", "FAISALABAD" },
                    { new Guid("10000000-0000-4000-8000-000000000006"), true, 30.157500m, 71.524900m, "Multan", "MULTAN" },
                    { new Guid("10000000-0000-4000-8000-000000000007"), true, 34.015100m, 71.524900m, "Peshawar", "PESHAWAR" },
                    { new Guid("10000000-0000-4000-8000-000000000008"), true, 30.179800m, 66.975000m, "Quetta", "QUETTA" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000008"));
        }
    }
}
