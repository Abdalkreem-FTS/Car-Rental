using CarRental.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarRental.IntegrationTests.Database;

public sealed class SchemaTests(CarRentalApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Migrate_WhenApplied_CreatesExactlyTheTablesTheModelNeeds()
    {
        var tables = await Factory.Database.TablesAsync();

        tables.ShouldBe([
            "AspNetRoleClaims",
            "AspNetRoles",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserRoles",
            "AspNetUserTokens",
            "AspNetUsers",
            "Cars",
            "IdempotentRequests",
            "OutboxEmails",
            "RefreshTokens",
            "Reservations",
            "__EFMigrationsHistory",
        ]);
    }

    [Fact]
    public async Task Migrate_WhenApplied_CreatesTheAspNetIdentitySchema()
    {
        var identityTables = (await Factory.Database.TablesAsync()).Where(name => name.StartsWith("AspNet", StringComparison.Ordinal)).ToList();

        identityTables.ShouldContain("AspNetUsers");
        identityTables.ShouldContain("AspNetRoles");
        identityTables.ShouldContain("AspNetUserRoles");
    }

    [Fact]
    public async Task Model_WhenComparedToTheLastMigration_ReportsNoPendingChanges()
    {
        var hasPendingChanges = await Factory.WithDbAsync(db =>
            Task.FromResult(db.Database.HasPendingModelChanges()));

        hasPendingChanges.ShouldBeFalse("run 'dotnet ef migrations add' — the model has drifted from the last migration");
    }

    [Theory]
    [InlineData("AspNetUsers", "NormalizedEmail")]
    [InlineData("AspNetUsers", "DriverLicenseNumber")]
    [InlineData("Cars", "PlateNumber")]
    [InlineData("RefreshTokens", "TokenHash")]
    public async Task Migrate_ForAnIdentifyingColumn_CreatesAUniqueIndex(string table, string column)
    {
        var uniqueIndexes = await Factory.Database.UniqueIndexesAsync(table);

        uniqueIndexes.ShouldContain(definition => definition.Contains($"\"{column}\""),
            $"{table}.{column} must be unique at the database level, not only in application code");
    }

    [Fact]
    public async Task Migrate_ForDatesAndInstants_UsesDateAndTimestamptzRespectively()
    {
        var reservationStart = await Factory.Database.ColumnTypeAsync("Reservations", "StartDate");
        var dateOfBirth = await Factory.Database.ColumnTypeAsync("AspNetUsers", "DateOfBirth");
        var createdAt = await Factory.Database.ColumnTypeAsync("AspNetUsers", "CreatedAtUtc");

        reservationStart.ShouldBe("date");
        dateOfBirth.ShouldBe("date");
        createdAt.ShouldBe("timestamp with time zone");
    }

    [Fact]
    public async Task Migrate_ForAMoneyColumn_UsesNumericWithAnExactScale()
    {
        var rate = await Factory.Database.QuerySingleAsync<string>(
            """
            SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')'
            FROM information_schema.columns
            WHERE table_name = 'Cars' AND column_name = 'DailyRate'
            """);

        rate.ShouldBe("numeric(10,2)");
    }

    [Fact]
    public async Task Migrate_ForAnEnumColumn_UsesTextRatherThanOrdinals()
    {
        (await Factory.Database.ColumnTypeAsync("Cars", "Category")).ShouldBe("character varying");
        (await Factory.Database.ColumnTypeAsync("Reservations", "Status")).ShouldBe("character varying");

        var categories = await Factory.Database.QueryAsync<string>("SELECT DISTINCT \"Category\" FROM \"Cars\" ORDER BY 1");
        categories.ShouldContain("SUV");
        categories.ShouldContain("Luxury");
    }

    [Fact]
    public async Task Migrate_ForTheUsersTable_MakesTheProfileFieldsWeRequireNotNullable()
    {
        var nullable = await Factory.Database.NullableColumnsAsync("AspNetUsers");

        nullable.ShouldContain("AddressLine2");
        nullable.ShouldContain("DateOfBirth");
        nullable.ShouldContain("DriverLicenseNumber", "an administrator is not a renter and has no licence");

        foreach (var required in new[] { "FirstName", "LastName", "AddressLine1", "City", "Country" })
        {
            nullable.ShouldNotContain(required);
        }
    }

    [Fact]
    public async Task Migrate_ForForeignKeys_CascadesCredentialsButHoldsOntoRentalHistory()
    {
        var deleteRules = await Factory.Database.ForeignKeyDeleteRulesAsync();

        deleteRules.ShouldContain("RefreshTokens.UserId -> CASCADE");
        deleteRules.ShouldContain("AspNetUserRoles.UserId -> CASCADE");
        deleteRules.ShouldContain(
            "Reservations.UserId -> RESTRICT",
            "a closed account must not take the record of who held which car with it");

        deleteRules.ShouldContain("Reservations.CarId -> RESTRICT");
    }

    [Fact]
    public async Task Migrate_ForTheUserRolesTable_KeysItOnUserAndRoleTogether()
    {
        var primaryKey = await Factory.Database.PrimaryKeyAsync("AspNetUserRoles");

        primaryKey.ShouldBe("UserId,RoleId");
    }

    [Fact]
    public async Task Migrate_WhenApplied_EnablesTheExtensionsTheSchemaDependsOn()
    {
        var extensions = await Factory.Database.ExtensionsAsync();

        extensions.ShouldContain("btree_gist");
        extensions.ShouldContain("pg_trgm");
    }

    [Fact]
    public async Task Migrate_WhenApplied_IndexesEverySearchedColumnForTrigramMatching()
    {
        var definitions = await Factory.Database.IndexDefinitionsAsync("Cars");

        foreach (var column in new[] { "Make", "Model", "Location" })
        {
            definitions.ShouldContain(
                definition => definition.Contains($"""USING gin ("{column}" gin_trgm_ops)"""),
                $"ILIKE '%term%' on {column} cannot use a btree index, so it needs a trigram one.");
        }
    }

    [Fact]
    public async Task Migrate_ForTheLicenceIndex_LeavesRoomForPrincipalsWhoNeverRent()
    {
        var definitions = await Factory.Database.IndexDefinitionsAsync("AspNetUsers");

        definitions.ShouldContain(
            definition => definition.Contains("\"DriverLicenseNumber\"") && definition.Contains("IS NOT NULL"),
            "without the filter every licenceless account competes for one slot in the unique index");
    }
}
