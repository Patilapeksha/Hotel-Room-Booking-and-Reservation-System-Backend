using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelsStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Hotels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Hotels");
        }
    }
}
