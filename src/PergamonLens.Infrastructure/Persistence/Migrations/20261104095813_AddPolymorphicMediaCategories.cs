using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PergamonLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPolymorphicMediaCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MusicReleases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Music");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCatalogId",
                table: "MusicReleases",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PagesCount",
                table: "MusicReleases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "MusicReleases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId_Category_CreatedAtUtc",
                table: "MusicReleases",
                columns: new[] { "UserId", "Category", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId_Category_CreatedAtUtc",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "ExternalCatalogId",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "PagesCount",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "MusicReleases");
        }
    }
}
