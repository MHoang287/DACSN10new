using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACSN10.Migrations
{
    /// <inheritdoc />
    public partial class InitSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SiteDescription = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    AllowNewRegistration = table.Column<bool>(type: "bit", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnableDarkMode = table.Column<bool>(type: "bit", nullable: false),
                    EnableAnimations = table.Column<bool>(type: "bit", nullable: false),
                    Require2FAForAdmins = table.Column<bool>(type: "bit", nullable: false),
                    EncryptSession = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutMinutes = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PasswordComplexityLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MaxLoginFailures = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AccountLockMinutes = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ForceHttps = table.Column<bool>(type: "bit", nullable: false),
                    NotifyNewRegistration = table.Column<bool>(type: "bit", nullable: false),
                    NotifyNewPayment = table.Column<bool>(type: "bit", nullable: false),
                    NotifyNewCourse = table.Column<bool>(type: "bit", nullable: false),
                    NotifySystemError = table.Column<bool>(type: "bit", nullable: false),
                    BrowserNotifications = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSound = table.Column<bool>(type: "bit", nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    NotificationFromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EnableMomo = table.Column<bool>(type: "bit", nullable: false),
                    EnableVnPay = table.Column<bool>(type: "bit", nullable: false),
                    EnableZaloPay = table.Column<bool>(type: "bit", nullable: false),
                    GoogleAnalyticsId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FacebookPixelId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HotjarSiteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnableS3 = table.Column<bool>(type: "bit", nullable: false),
                    EnableGcs = table.Column<bool>(type: "bit", nullable: false),
                    EnableSlack = table.Column<bool>(type: "bit", nullable: false),
                    EnableDiscord = table.Column<bool>(type: "bit", nullable: false),
                    MainApiKeyMasked = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WebhookApiKeyMasked = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupRecords");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
