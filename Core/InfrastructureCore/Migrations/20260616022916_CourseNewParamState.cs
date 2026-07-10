using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureCore.Migrations
{
    /// <inheritdoc />
    public partial class CourseNewParamState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "State",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Courses");
        }
    }
}
