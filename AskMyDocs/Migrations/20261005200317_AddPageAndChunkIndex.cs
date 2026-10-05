using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskMyDocs.Migrations
{
    /// <inheritdoc />
    public partial class AddPageAndChunkIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "DocumentChunks");
        }
    }
}
