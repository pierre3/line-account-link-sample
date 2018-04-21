using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace LineAccountLinkApp.Data.Migrations
{
    public partial class modifyLineLinkObj : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LineLinks",
                table: "LineLinks");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LineLinks",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nonce",
                table: "LineLinks",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddPrimaryKey(
                name: "PK_LineLinks",
                table: "LineLinks",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LineLinks",
                table: "LineLinks");

            migrationBuilder.AlterColumn<string>(
                name: "Nonce",
                table: "LineLinks",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LineLinks",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddPrimaryKey(
                name: "PK_LineLinks",
                table: "LineLinks",
                column: "Nonce");
        }
    }
}
