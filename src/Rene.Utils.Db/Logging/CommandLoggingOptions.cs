namespace Rene.Utils.Db.Logging
{
    /// <summary>Application-owned diagnostic callbacks. No logging provider is required by the callbacks.</summary>
    public sealed class CommandLoggingOptions
    {
        /// <summary>Runs before the remaining pipeline, with the request cancellation token.</summary>
        public Func<IServiceProvider, CommandLoggingContext, CancellationToken, Task> Before { get; set; }

        /// <summary>
        /// Runs after success, failure or cancellation. Receives CancellationToken.None so a canceled request
        /// does not suppress its completion event. Callback failures are reported through standard logging.
        /// </summary>
        public Func<IServiceProvider, CommandCompletedContext, CancellationToken, Task> Completed { get; set; }
    }
}
