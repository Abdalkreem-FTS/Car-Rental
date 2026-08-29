using Npgsql;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed class DatabaseProbe(string connectionString)
{
    public async Task<List<T>> QueryAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var results = new List<T>();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetFieldValue<T>(0));
        }

        return results;
    }

    public async Task<T> QuerySingleAsync<T>(string sql)
    {
        var rows = await QueryAsync<T>(sql);

        return rows.Count == 1
            ? rows[0]
            : throw new InvalidOperationException($"Expected exactly one row, got {rows.Count}, from: {sql}");
    }

    public async Task<object?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        return await command.ExecuteScalarAsync();
    }

    public Task<string> ColumnTypeAsync(string table, string column) => QuerySingleAsync<string>(
        $"""
        SELECT data_type FROM information_schema.columns
        WHERE table_name = '{table}' AND column_name = '{column}'
        """);

    public Task<List<string>> NullableColumnsAsync(string table) => QueryAsync<string>(
        $"""
        SELECT column_name FROM information_schema.columns
        WHERE table_name = '{table}' AND is_nullable = 'YES'
        ORDER BY column_name
        """);

    public Task<List<string>> UniqueIndexesAsync(string table) => QueryAsync<string>(
        $"""
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = '{table}' AND indexdef LIKE '%UNIQUE%'
        """);

    public Task<List<string>> TablesAsync() => QueryAsync<string>(
        """
        SELECT table_name FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        ORDER BY table_name
        """);

    public Task<List<string>> ExtensionsAsync() => QueryAsync<string>(
        "SELECT extname FROM pg_extension ORDER BY extname");

    public Task<List<string>> IndexDefinitionsAsync(string table) => QueryAsync<string>(
        $"""
        SELECT indexdef FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = '{table}'
        ORDER BY indexname
        """);

    public Task<List<string>> ForeignKeyDeleteRulesAsync() => QueryAsync<string>(
        """
        SELECT tc.table_name || '.' || kcu.column_name || ' -> ' || rc.delete_rule
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
        JOIN information_schema.referential_constraints rc ON tc.constraint_name = rc.constraint_name
        WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_schema = 'public'
        ORDER BY 1
        """);

    public Task<string> PrimaryKeyAsync(string table) => QuerySingleAsync<string>(
        $"""
        SELECT string_agg(kcu.column_name, ',' ORDER BY kcu.ordinal_position)
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
        WHERE tc.table_name = '{table}' AND tc.constraint_type = 'PRIMARY KEY'
        """);
}
