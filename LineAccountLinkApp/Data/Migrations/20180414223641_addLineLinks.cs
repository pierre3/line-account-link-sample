using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace LineAccountLinkApp.Data.Migrations
{
    public partial class addLineLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineLinkNonce",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "LineLinks",
                columns: table => new
                {
                    Nonce = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineLinks", x => x.Nonce);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineLinks");

            migrationBuilder.AddColumn<string>(
                name: "LineLinkNonce",
                table: "AspNetUsers",
                nullable: true);
        }
    }
}
