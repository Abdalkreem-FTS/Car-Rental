using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CarRental.IntegrationTests.Infrastructure;

public sealed class SmtpStub : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task<string> _conversation;

    public SmtpStub()
    {
        _listener = new TcpListener(IPAddress.Loopback, port: 0);
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _conversation = Task.Run(AcceptAsync);
    }

    public int Port { get; }

    public Task<string> MessageAsync() => _conversation;

    private async Task<string> AcceptAsync()
    {
        using var client = await _listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };

        var body = new StringBuilder();

        await writer.WriteLineAsync("220 stub ready");

        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250 stub");
            }
            else if (line.StartsWith("MAIL", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("RCPT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250 ok");
            }
            else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 send it");

                while (await reader.ReadLineAsync() is { } content && content != ".")
                {
                    body.AppendLine(content);
                }

                await writer.WriteLineAsync("250 queued");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");

                break;
            }
            else
            {
                await writer.WriteLineAsync("250 ok");
            }
        }

        return body.ToString();
    }

    public ValueTask DisposeAsync()
    {
        _listener.Stop();

        return ValueTask.CompletedTask;
    }
}
