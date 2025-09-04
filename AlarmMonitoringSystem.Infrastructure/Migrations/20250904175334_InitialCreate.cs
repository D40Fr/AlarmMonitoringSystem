using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlarmMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlarmId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    AlarmTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NumericValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RawData = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LogTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_AlarmId_ClientId_Unique",
                table: "Alarms",
                columns: new[] { "AlarmId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_AlarmTime",
                table: "Alarms",
                column: "AlarmTime");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_ClientId",
                table: "Alarms",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsAcknowledged",
                table: "Alarms",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsActive",
                table: "Alarms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_IsActive_IsAcknowledged",
                table: "Alarms",
                columns: new[] { "IsActive", "IsAcknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_Severity",
                table: "Alarms",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_Type",
                table: "Alarms",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId",
                table: "Clients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IpAddress_Port",
                table: "Clients",
                columns: new[] { "IpAddress", "Port" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IsActive",
                table: "Clients",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Status",
                table: "Clients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionLogs_ClientId",
                table: "ConnectionLogs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionLogs_ClientId_LogTime",
                table: "ConnectionLogs",
                columns: new[] { "ClientId", "LogTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionLogs_LogTime",
                table: "ConnectionLogs",
                column: "LogTime");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionLogs_Status",
                table: "ConnectionLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "ConnectionLogs");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
