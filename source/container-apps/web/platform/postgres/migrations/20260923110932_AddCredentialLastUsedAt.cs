using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialLastUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsedAt",
                schema: "identity",
                table: "credentials",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                schema: "identity",
                table: "credentials");
        }
    }
}
