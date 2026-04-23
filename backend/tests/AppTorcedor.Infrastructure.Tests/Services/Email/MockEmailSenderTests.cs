using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Tests.Services.Email;

public sealed class MockEmailSenderTests
{
    [Fact]
    public async Task SendAsync_logs_destination_and_subject()
    {
        var provider = new ListLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(provider));
        var sut = new MockEmailSender(factory.CreateLogger<MockEmailSender>());

        await sut.SendAsync(new EmailMessage("fan@club.test", "Olá", "<p>Corpo</p>"));

        Assert.Single(provider.Entries);
        var line = provider.Entries[0];
        Assert.Contains("fan@club.test", line, StringComparison.Ordinal);
        Assert.Contains("Olá", line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_respects_cancellation()
    {
        var sut = new MockEmailSender(Microsoft.Extensions.Logging.Abstractions.NullLogger<MockEmailSender>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.SendAsync(new EmailMessage("a@b", "s", "<p>x</p>"), cts.Token));
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public List<string> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class ListLogger(List<string> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                entries.Add(formatter(state, exception));

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose()
                {
                }
            }
        }
    }
}
