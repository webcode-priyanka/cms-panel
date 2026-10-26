using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSPanel.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogTagRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogBlogTag",
                columns: table => new
                {
                    BlogTagsId = table.Column<int>(type: "int", nullable: false),
                    BlogsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogBlogTag", x => new { x.BlogTagsId, x.BlogsId });
                    table.ForeignKey(
                        name: "FK_BlogBlogTag_BlogTags_BlogTagsId",
                        column: x => x.BlogTagsId,
                        principalTable: "BlogTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogBlogTag_Blogs_BlogsId",
                        column: x => x.BlogsId,
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogBlogTag_BlogsId",
                table: "BlogBlogTag",
                column: "BlogsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogBlogTag");
        }
    }
}
