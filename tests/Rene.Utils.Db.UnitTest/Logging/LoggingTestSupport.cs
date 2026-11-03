namespace Rene.Utils.Db.UnitTest.Logging
{
    using System.Collections.Concurrent;
    using MediatR;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Rene.Utils.Db.Logging;

    public static class LoggingTestSupport
    {
        internal static ServiceProvider Provider(Action<CommandLoggingOptions> configure,
            TimeProvider clock = null, RecordingLogger logs = null)
        {
            var services = new ServiceCollection();
            services.AddScoped<ScopedMarker>();
            if (clock != null) services.AddSingleton(clock);
            if (logs != null) services.AddLogging(builder => builder.AddProvider(logs));
            services.AddDbUtilsCommandLogging(configure);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        internal static IPipelineBehavior<TRequest, TResponse> Behavior<TRequest, TResponse>(IServiceProvider services)
            where TRequest : notnull
            => services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Single();

        internal sealed class ScopedMarker;
        public class Request : IRequest<Response>;
        public sealed class DerivedRequest : Request;
        public sealed record Response(string Value);

        internal sealed class ManualClock : TimeProvider
        {
            internal DateTimeOffset UtcNow = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
            internal long Timestamp = 1000;
            public override long TimestampFrequency => 1000;
            public override DateTimeOffset GetUtcNow() => UtcNow;
            public override long GetTimestamp() => Timestamp;
        }

        internal sealed record LogEntry(LogLevel Level, Exception Exception, string Message);

        internal sealed class RecordingLogger : ILoggerProvider
        {
            internal ConcurrentQueue<LogEntry> Entries { get; } = new();
            public ILogger CreateLogger(string categoryName) => new Sink(Entries);
            public void Dispose() { }

            private sealed class Sink(ConcurrentQueue<LogEntry> entries) : ILogger
            {
                public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                    Func<TState, Exception, string> formatter)
                    => entries.Enqueue(new LogEntry(logLevel, exception, formatter(state, exception)));
            }
        }
    }
}
