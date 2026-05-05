using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFoundTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedClaimantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedClaimantId",
                table: "Items",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedClaimantId",
                table: "Items");
        }
    }
}
