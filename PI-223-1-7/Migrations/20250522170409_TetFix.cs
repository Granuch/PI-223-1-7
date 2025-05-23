using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PI_223_1_7.Migrations
{
    /// <inheritdoc />
    public partial class TetFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Books_BookId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BookId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BookId1",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookId1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BookId1",
                table: "Orders",
                column: "BookId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Books_BookId1",
                table: "Orders",
                column: "BookId1",
                principalTable: "Books",
                principalColumn: "Id");
        }
    }
}
