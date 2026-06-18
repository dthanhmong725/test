using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DOAN_LAPTRINHWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Follows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FollowerId = table.Column<int>(type: "int", nullable: false),
                    FollowingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Follows_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Follows_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2195));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2199));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2201));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2202));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2203));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2205));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2206));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2207));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2209));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2210));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(1990));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2018));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2020));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2022));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2024));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2026));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 18, 16, 42, 24, 940, DateTimeKind.Utc).AddTicks(2028));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(2890), "$2a$11$srlguoMyimELKrP60I4Q9.PDxHHJOP3g1JXdnewUWPUw2Znq28.Ii", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(2892) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 101,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(3020), "$2a$11$srlguoMyimELKrP60I4Q9.PDxHHJOP3g1JXdnewUWPUw2Znq28.Ii", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(3020) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(3985), "$2a$11$srlguoMyimELKrP60I4Q9.PDxHHJOP3g1JXdnewUWPUw2Znq28.Ii", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(3986) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 103,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4012), "$2a$11$srlguoMyimELKrP60I4Q9.PDxHHJOP3g1JXdnewUWPUw2Znq28.Ii", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4013) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4028), "$2a$11$srlguoMyimELKrP60I4Q9.PDxHHJOP3g1JXdnewUWPUw2Znq28.Ii", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4028) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 201,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4046), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4046) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 202,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4057), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4057) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 203,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4064), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4065) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 204,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4072), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4072) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 205,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4171), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4171) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 206,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4179), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4179) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 207,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4224), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4225) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 208,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4232), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4233) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 209,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4240), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4241) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 210,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4248), "$2a$11$koSQ06.rb4TA4Wq0BEoQMONRS3jnVoKiKYHNbsDt5GgWVkPPYijju", new DateTime(2026, 6, 18, 16, 42, 25, 287, DateTimeKind.Utc).AddTicks(4253) });

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowerId_FollowingId",
                table: "Follows",
                columns: new[] { "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Follows_FollowingId",
                table: "Follows",
                column: "FollowingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Follows");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1538));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1541));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1543));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1544));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1545));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1546));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1547));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1550));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1551));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1365));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1370));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1373));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1374));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1375));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1377));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 3, 43, 35, 610, DateTimeKind.Utc).AddTicks(1378));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 846, DateTimeKind.Utc).AddTicks(9778), "$2a$11$pdjde9PtgzqcGb0lSDAYiOuM7Pshj5NWyjNcLNsRVWTSXfGqZWc36", new DateTime(2026, 6, 17, 3, 43, 35, 846, DateTimeKind.Utc).AddTicks(9782) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 101,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 846, DateTimeKind.Utc).AddTicks(9954), "$2a$11$pdjde9PtgzqcGb0lSDAYiOuM7Pshj5NWyjNcLNsRVWTSXfGqZWc36", new DateTime(2026, 6, 17, 3, 43, 35, 846, DateTimeKind.Utc).AddTicks(9954) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(696), "$2a$11$pdjde9PtgzqcGb0lSDAYiOuM7Pshj5NWyjNcLNsRVWTSXfGqZWc36", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(696) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 103,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(720), "$2a$11$pdjde9PtgzqcGb0lSDAYiOuM7Pshj5NWyjNcLNsRVWTSXfGqZWc36", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(720) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(855), "$2a$11$pdjde9PtgzqcGb0lSDAYiOuM7Pshj5NWyjNcLNsRVWTSXfGqZWc36", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(855) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 201,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(901), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(901) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 202,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(915), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(915) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 203,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(921), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(922) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 204,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(927), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(927) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 205,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(943), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(943) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 206,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(950), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(950) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 207,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(995), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(995) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 208,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1001), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1002) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 209,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1040), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1040) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 210,
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1047), "$2a$11$rD1zN2/JlxsqOTTn0qp5vu4Bllhcqy/.dBO094j7h5DKuZTjRDChO", new DateTime(2026, 6, 17, 3, 43, 35, 847, DateTimeKind.Utc).AddTicks(1057) });
        }
    }
}
