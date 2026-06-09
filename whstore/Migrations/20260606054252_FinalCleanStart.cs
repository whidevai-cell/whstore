using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace whstore.Migrations
{
    /// <inheritdoc />
    public partial class FinalCleanStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    originalprice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageurl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    affiliatelink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    producturl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commissionrate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shippingcost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    storename = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reviewcount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    reviewrate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ishotproduct = table.Column<bool>(type: "bit", nullable: false),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
