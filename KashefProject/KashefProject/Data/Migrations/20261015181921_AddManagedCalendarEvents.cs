using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KashefProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedCalendarEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarCatalogStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarCatalogStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TitleFa = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    DescriptionFa = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    NoteFa = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    NoteEn = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Rule = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    HistoricalYear = table.Column<int>(type: "INTEGER", nullable: true),
                    OnlyImperialYear = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.CheckConstraint("CK_CalendarEvent_Day", "Day BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_CalendarEvent_Duration", "Duration BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_CalendarEvent_Month", "Month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_Key",
                table: "CalendarEvents",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarCatalogStates");

            migrationBuilder.DropTable(
                name: "CalendarEvents");
        }
    }
}
