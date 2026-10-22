using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PergamonLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Artist = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReleaseYear = table.Column<int>(type: "int", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CatalogueNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    VinylSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VinylSpeed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MediaCondition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SleeveCondition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscogsReleaseId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    MusicReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_Artist_Title",
                table: "MusicReleases",
                columns: new[] { "Artist", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_Barcode",
                table: "MusicReleases",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_CatalogueNumber",
                table: "MusicReleases",
                column: "CatalogueNumber");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_Format",
                table: "MusicReleases",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_Status",
                table: "MusicReleases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_MusicReleaseId_Position",
                table: "Tracks",
                columns: new[] { "MusicReleaseId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "MusicReleases");
        }
    }
}
