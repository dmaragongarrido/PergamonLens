using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PergamonLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenreAndOverviewToMediaRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "MediaReleases",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overview",
                table: "MediaReleases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genre",
                table: "MediaReleases");

            migrationBuilder.DropColumn(
                name: "Overview",
                table: "MediaReleases");
        }
    }
}
