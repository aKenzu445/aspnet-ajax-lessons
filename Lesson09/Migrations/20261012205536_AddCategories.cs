using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lesson1.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Работа" },
                    { 2, "Учёба" },
                    { 3, "Личное" }
                });

            // === это я дописал руками: переношу старые данные (зан8 зад3) ===
            // раньше категория была строкой в Tasks.Category (дз зан5-6)
            // EF сгенерил DropColumn("Category") самым первым - все строки бы пропали
            // поэтому сначала переношу в новую таблицу и связь, а удаляю столбец в самом конце

            // 1) категории которых нет среди стартовых (типа "Спорт") закидываю в таблицу
            migrationBuilder.Sql(@"
                INSERT INTO Categories (Name)
                SELECT DISTINCT Category FROM Tasks
                WHERE Category IS NOT NULL
                  AND Category NOT IN (SELECT Name FROM Categories);");

            // 2) каждой задаче ставлю номер категории по старому названию
            migrationBuilder.Sql(@"
                UPDATE Tasks
                SET CategoryId = (SELECT Id FROM Categories WHERE Categories.Name = Tasks.Category)
                WHERE Category IS NOT NULL;");

            // 3) и тока теперь сношу старый текстовый столбец
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CategoryId",
                table: "Tasks",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Categories_CategoryId",
                table: "Tasks",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Categories_CategoryId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_CategoryId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tasks");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }
    }
}
