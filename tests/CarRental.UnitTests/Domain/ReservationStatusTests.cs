using CarRental.Domain.Enums;
using Shouldly;

namespace CarRental.UnitTests.Domain;

public sealed class ReservationStatusTests
{
    [Fact]
    public void ReservationStatus_ListsOnlyStatesSomethingCanActuallyPutAReservationIn()
    {
        Enum.GetNames<ReservationStatus>().ShouldBe(
            ["Confirmed", "Cancelled"],
            ignoreOrder: true,
            "adding a state means wiring the transition that reaches it, or the enum starts describing a lifecycle the system does not have");
    }
}
