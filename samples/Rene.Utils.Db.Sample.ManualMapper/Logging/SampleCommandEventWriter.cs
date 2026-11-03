namespace Rene.Utils.Db.Sample.ManualMapper.Logging
{
    using Rene.Utils.Db.Logging;

    public interface ISampleCommandEventWriter
    {
        Task Starting(CommandLoggingContext context, CancellationToken cancellationToken);
        Task Completed(CommandCompletedContext context, CancellationToken cancellationToken);
    }

    public sealed class SampleCommandEventWriter(ILogger<SampleCommandEventWriter> logger) : ISampleCommandEventWriter
    {
        public Task Starting(CommandLoggingContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Starting {CommandType} at {StartedAt}, execution {ExecutionId}",
                context.CommandType, context.StartedAt, context.ExecutionId);
            return Task.CompletedTask;
        }

        public Task Completed(CommandCompletedContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var level = context.Outcome == CommandExecutionOutcome.Succeeded ? LogLevel.Information : LogLevel.Warning;
            logger.Log(level,
                "Completed {CommandType}: {Outcome} in {ElapsedMs} ms at {CompletedAt}, execution {ExecutionId}",
                context.CommandType, context.Outcome, context.Elapsed.TotalMilliseconds,
                context.CompletedAt, context.ExecutionId);
            return Task.CompletedTask;
        }
    }
}
