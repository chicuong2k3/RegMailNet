using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RegMailNet.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Browser = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefaultSmsService = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CapsolverKey = table.Column<string>(type: "text", nullable: false),
                    NopechaKey = table.Column<string>(type: "text", nullable: false),
                    SmsPoolToken = table.Column<string>(type: "text", nullable: false),
                    FiveSimToken = table.Column<string>(type: "text", nullable: false),
                    GetsmsCodeUser = table.Column<string>(type: "text", nullable: false),
                    GetsmsCodeToken = table.Column<string>(type: "text", nullable: false),
                    Proxies = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountHistory_CreatedAt",
                table: "AccountHistory",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountHistory");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
