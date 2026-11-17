using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Rene.Utils.Db.Commands
{
    /// <summary>
    /// Runs an asynchronous delay or application-supplied task through the MediatR pipeline.
    /// Intended for diagnostics, not database operations.
    /// </summary>
    public sealed class HeavyCommandTest : IRequest
    {
        public long Milliseconds { get; }
        public Func<CancellationToken, Task> Work { get; }

        /// <summary>
        /// Creates a cancellable, non-blocking delay. Zero completes immediately; negative values are invalid.
        /// </summary>
        public HeavyCommandTest(long milliseconds)
        {
            if (milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
            Milliseconds = milliseconds;
        }

        /// <summary>
        /// Creates a command that awaits the supplied work with the request cancellation token.
        /// </summary>
        public HeavyCommandTest(Func<CancellationToken, Task> work)
        {
            Work = work ?? throw new ArgumentNullException(nameof(work));
        }
    }
}
