﻿// <auto-generated/>
using Microsoft.EntityFrameworkCore.Migrations;

namespace GraphQL.AspNet.JMeterAPI.Migrations
{
    public partial class BakeryDbCreation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    Address1 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    State = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bakery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    Address1 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bakery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bakery_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PastryRecipe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipeText = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastryRecipe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastryRecipe_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    BakeryId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoice_Bakery_BakeryId",
                        column: x => x.BakeryId,
                        principalTable: "Bakery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Invoice_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "PastryStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BakeryId = table.Column<int>(type: "int", nullable: false),
                    PastryRecipeId = table.Column<int>(type: "int", nullable: false),
                    NumberForSale = table.Column<int>(type: "int", nullable: false),
                    SalePriceEach = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastryStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PastryStock_Bakery_BakeryId",
                        column: x => x.BakeryId,
                        principalTable: "Bakery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_PastryStock_PastryRecipe_PastryRecipeId",
                        column: x => x.PastryRecipeId,
                        principalTable: "PastryRecipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumberSold = table.Column<int>(type: "int", nullable: false),
                    SalePriceEach = table.Column<decimal>(type: "decimal(15,2)", precision: 15, scale: 2, nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PastryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItem_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItem_PastryRecipe_PastryId",
                        column: x => x.PastryId,
                        principalTable: "PastryRecipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bakery_OrganizationId",
                table: "Bakery",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItem_InvoiceId",
                table: "InvoiceLineItem",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItem_PastryId",
                table: "InvoiceLineItem",
                column: "PastryId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_BakeryId",
                table: "Invoice",
                column: "BakeryId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrganizationId",
                table: "Invoice",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PastryRecipe_OrganizationId",
                table: "PastryRecipe",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PastryStock_BakeryId",
                table: "PastryStock",
                column: "BakeryId");

            migrationBuilder.CreateIndex(
                name: "IX_PastryStock_PastryRecipeId",
                table: "PastryStock",
                column: "PastryRecipeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLineItem");

            migrationBuilder.DropTable(
                name: "PastryStock");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "PastryRecipe");

            migrationBuilder.DropTable(
                name: "Bakery");

            migrationBuilder.DropTable(
                name: "Organization");
        }
    }
}
