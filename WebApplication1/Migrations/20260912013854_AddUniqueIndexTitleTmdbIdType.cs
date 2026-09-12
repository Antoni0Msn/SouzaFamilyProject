using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexTitleTmdbIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Titles_TmdbId",
                table: "Titles");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_TmdbId_Type",
                table: "Titles",
                columns: new[] { "TmdbId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Titles_TmdbId_Type",
                table: "Titles");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_TmdbId",
                table: "Titles",
                column: "TmdbId");
        }
    }
}
