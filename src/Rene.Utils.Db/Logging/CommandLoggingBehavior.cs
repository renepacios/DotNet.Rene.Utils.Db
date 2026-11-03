using MediatR;
using Microsoft.Extensions.Logging;

namespace Rene.Utils.Db.Logging
{
    internal sealed class CommandLoggingBehavior<TRequest, TResponse>(
        IServiceProvider services, CommandLoggingOptions options, TimeProvider timeProvider,
        ILogger<CommandLoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var startedAt = timeProvider.GetUtcNow();
            var timestamp = timeProvider.GetTimestamp();
            var execution = new CommandLoggingContext(Guid.NewGuid(), request.GetType(), startedAt);
            Exception failure = null;
            var outcome = CommandExecutionOutcome.Succeeded;

            try
            {
                if (options.Before != null)
                    await InvokeCallback(() => options.Before(services, execution, cancellationToken), "Before")
                        .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                return await next().ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                failure = exception;
                outcome = CommandExecutionOutcome.Canceled;
                throw;
            }
            catch (Exception exception)
            {
                failure = exception;
                outcome = CommandExecutionOutcome.Failed;
                throw;
            }
            finally
            {
                if (options.Completed != null)
                {
                    var completed = new CommandCompletedContext(execution, timeProvider.GetUtcNow(),
                        timeProvider.GetElapsedTime(timestamp), outcome, failure);
                    await InvokeCallback(() => options.Completed(services, completed, CancellationToken.None), "Completed")
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task InvokeCallback(Func<Task> callback, string stage)
        {
            try
            {
                await (callback() ?? throw new InvalidOperationException("A command logging callback returned a null task."))
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Command logging callback {Stage} failed for {CommandType}.",
                    stage, typeof(TRequest).FullName);
            }
        }
    }
}
