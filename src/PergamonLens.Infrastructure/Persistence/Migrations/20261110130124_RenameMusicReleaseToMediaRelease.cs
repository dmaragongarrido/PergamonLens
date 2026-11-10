using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PergamonLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMusicReleaseToMediaRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Eliminar foreign key anterior hacia MusicReleases
            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_MusicReleases_MusicReleaseId",
                table: "Tracks");

            // 2. Renombrar tabla MusicReleases -> MediaReleases (Preservando 100% de datos)
            migrationBuilder.RenameTable(
                name: "MusicReleases",
                newName: "MediaReleases");

            // 3. Renombrar constraint de clave primaria
            migrationBuilder.Sql("ALTER TABLE \"MediaReleases\" RENAME CONSTRAINT \"PK_MusicReleases\" TO \"PK_MediaReleases\";");

            // 4. Renombrar columna y su índice en la tabla dependiente Tracks
            migrationBuilder.RenameColumn(
                name: "MusicReleaseId",
                table: "Tracks",
                newName: "MediaReleaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Tracks_MusicReleaseId_Position",
                table: "Tracks",
                newName: "IX_Tracks_MediaReleaseId_Position");

            // 5. Renombrar los 9 índices existentes de la tabla renombrada
            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_Artist_Title",
                table: "MediaReleases",
                newName: "IX_MediaReleases_Artist_Title");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_Barcode",
                table: "MediaReleases",
                newName: "IX_MediaReleases_Barcode");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_CatalogueNumber",
                table: "MediaReleases",
                newName: "IX_MediaReleases_CatalogueNumber");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_Format",
                table: "MediaReleases",
                newName: "IX_MediaReleases_Format");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_Status",
                table: "MediaReleases",
                newName: "IX_MediaReleases_Status");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_UserId",
                table: "MediaReleases",
                newName: "IX_MediaReleases_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_UserId_Category_CreatedAtUtc",
                table: "MediaReleases",
                newName: "IX_MediaReleases_UserId_Category_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_UserId_Format",
                table: "MediaReleases",
                newName: "IX_MediaReleases_UserId_Format");

            migrationBuilder.RenameIndex(
                name: "IX_MusicReleases_UserId_Status",
                table: "MediaReleases",
                newName: "IX_MediaReleases_UserId_Status");

            // 6. Restablecer foreign key hacia MediaReleases
            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_MediaReleases_MediaReleaseId",
                table: "Tracks",
                column: "MediaReleaseId",
                principalTable: "MediaReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_MediaReleases_MediaReleaseId",
                table: "Tracks");

            migrationBuilder.RenameTable(
                name: "MediaReleases",
                newName: "MusicReleases");

            migrationBuilder.Sql("ALTER TABLE \"MusicReleases\" RENAME CONSTRAINT \"PK_MediaReleases\" TO \"PK_MusicReleases\";");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_Artist_Title",
                table: "MusicReleases",
                newName: "IX_MusicReleases_Artist_Title");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_Barcode",
                table: "MusicReleases",
                newName: "IX_MusicReleases_Barcode");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_CatalogueNumber",
                table: "MusicReleases",
                newName: "IX_MusicReleases_CatalogueNumber");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_Format",
                table: "MusicReleases",
                newName: "IX_MusicReleases_Format");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_Status",
                table: "MusicReleases",
                newName: "IX_MusicReleases_Status");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_UserId",
                table: "MusicReleases",
                newName: "IX_MusicReleases_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_UserId_Category_CreatedAtUtc",
                table: "MusicReleases",
                newName: "IX_MusicReleases_UserId_Category_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_UserId_Format",
                table: "MusicReleases",
                newName: "IX_MusicReleases_UserId_Format");

            migrationBuilder.RenameIndex(
                name: "IX_MediaReleases_UserId_Status",
                table: "MusicReleases",
                newName: "IX_MusicReleases_UserId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Tracks_MediaReleaseId_Position",
                table: "Tracks",
                newName: "IX_Tracks_MusicReleaseId_Position");

            migrationBuilder.RenameColumn(
                name: "MediaReleaseId",
                table: "Tracks",
                newName: "MusicReleaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_MusicReleases_MusicReleaseId",
                table: "Tracks",
                column: "MusicReleaseId",
                principalTable: "MusicReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
