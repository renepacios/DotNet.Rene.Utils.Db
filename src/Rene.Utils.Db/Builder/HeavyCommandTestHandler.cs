using Rene.Utils.Db.Commands;

namespace Rene.Utils.Db.Builder
{
    internal sealed class HeavyCommandTestHandler(TimeProvider timeProvider) : IRequestHandler<HeavyCommandTest>
    {
        public async Task Handle(HeavyCommandTest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.Work != null)
            {
                await (request.Work(cancellationToken)
                    ?? throw new InvalidOperationException("The heavy command delegate returned a null task."))
                    .ConfigureAwait(false);
                return;
            }

            var remaining = request.Milliseconds;
            while (remaining > 0)
            {
                var milliseconds = Math.Min(remaining, int.MaxValue);
                await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), timeProvider, cancellationToken)
                    .ConfigureAwait(false);
                remaining -= milliseconds;
            }
        }
    }
}
