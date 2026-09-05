using CarRental.Domain.Common;

namespace CarRental.Infrastructure.Persistence.Conflicts;

internal readonly record struct DatabaseConflict(string Constraint, string SqlState, Error Error);
