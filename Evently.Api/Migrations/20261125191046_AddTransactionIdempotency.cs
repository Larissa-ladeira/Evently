using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evently.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Provider_ProviderTransactionId",
                table: "Transactions",
                columns: new[] { "Provider", "ProviderTransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Provider_ProviderTransactionId",
                table: "Transactions");
        }
    }
}
