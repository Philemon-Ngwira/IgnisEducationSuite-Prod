using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgnisEducationSuite.Migrations
{
    /// <inheritdoc />
    public partial class FirstLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          
            migrationBuilder.AddColumn<bool>(
                name: "isFirstLogin",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.DropColumn(
                name: "isFirstLogin",
                table: "AspNetUsers");

        }
    }
}
