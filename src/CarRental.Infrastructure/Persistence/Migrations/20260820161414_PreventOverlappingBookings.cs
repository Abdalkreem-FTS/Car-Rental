using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Persistence.Migrations
{
    public partial class PreventOverlappingBookings : Migration
    {
        private const string ConstraintName = "ck_reservations_no_overlapping_confirmed_bookings";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql($"""
                ALTER TABLE "Reservations"
                ADD CONSTRAINT "{ConstraintName}"
                EXCLUDE USING gist (
                    "CarId" WITH =,
                    -- '[]' includes the return day, so a car is occupied until the end of the day it
                    -- comes back and cannot be re-let the same day. That is a turnaround policy, not
                    -- a technical detail: '[)' would allow same-day changeover.
                    daterange("StartDate", "EndDate", '[]') WITH &&
                -- Two things this depends on, neither of which the database can enforce:
                --   1. Status is stored as text. Change ReservationConfiguration to store the enum
                --      as an ordinal and this predicate matches no row, forever, without an error.
                --   2. It has to agree with Reservation.BlocksTheWindow, which is the one place the
                --      application expresses this same rule.
                -- OverlapConstraintTests covers both by writing straight to the table.
                ) WHERE ("Status" = 'Confirmed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""ALTER TABLE "Reservations" DROP CONSTRAINT "{ConstraintName}";""");
        }
    }
}
