using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed record LogEntry(LogLevel Level, string Category, string Message, Exception? Exception);

public sealed class LogRecorder : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public void Reset() => _entries.Clear();

    public ILogger CreateLogger(string categoryName) => new Recording(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Recording(LogRecorder owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                owner._entries.Enqueue(new LogEntry(logLevel, category, formatter(state, exception), exception));
            }
        }
    }
}
