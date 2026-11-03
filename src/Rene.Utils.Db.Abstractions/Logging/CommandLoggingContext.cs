using System;

namespace Rene.Utils.Db.Logging
{
    /// <summary>Identifies one execution of a MediatR request without serializing its payload.</summary>
    public sealed class CommandLoggingContext
    {
        public CommandLoggingContext(Guid executionId, Type commandType, DateTimeOffset startedAt)
        {
            ExecutionId = executionId;
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            StartedAt = startedAt;
        }

        public Guid ExecutionId { get; }
        public Type CommandType { get; }
        public DateTimeOffset StartedAt { get; }
    }

    /// <summary>The outcome of the wrapped request pipeline, not necessarily a database commit.</summary>
    public enum CommandExecutionOutcome
    {
        Succeeded,
        Failed,
        Canceled
    }

    /// <summary>Immutable completion metadata correlated with the corresponding start event.</summary>
    public sealed class CommandCompletedContext
    {
        public CommandCompletedContext(CommandLoggingContext execution, DateTimeOffset completedAt,
            TimeSpan elapsed, CommandExecutionOutcome outcome, Exception exception)
        {
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            CompletedAt = completedAt;
            Elapsed = elapsed;
            Outcome = outcome;
            Exception = exception;
        }

        public CommandLoggingContext Execution { get; }
        public Guid ExecutionId => Execution.ExecutionId;
        public Type CommandType => Execution.CommandType;
        public DateTimeOffset StartedAt => Execution.StartedAt;
        public DateTimeOffset CompletedAt { get; }
        public TimeSpan Elapsed { get; }
        public CommandExecutionOutcome Outcome { get; }
        public Exception Exception { get; }
    }
}
