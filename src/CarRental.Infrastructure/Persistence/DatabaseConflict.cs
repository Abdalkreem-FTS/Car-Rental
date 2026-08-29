using CarRental.Domain.Common;

namespace CarRental.Infrastructure.Persistence;

internal readonly record struct DatabaseConflict(string Constraint, string SqlState, Error Error);
