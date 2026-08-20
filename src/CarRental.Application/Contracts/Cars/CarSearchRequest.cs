using CarRental.Domain.Enums;

namespace CarRental.Application.Contracts.Cars;

public sealed record CarSearchRequest(
    string? Query = null,
    string? Location = null,
    DateOnly? PickupDate = null,
    DateOnly? ReturnDate = null,
    CarCategory? Category = null,
    TransmissionType? Transmission = null,
    FuelType? Fuel = null,
    int? MinSeats = null,
    decimal? MinDailyRate = null,
    decimal? MaxDailyRate = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 12);