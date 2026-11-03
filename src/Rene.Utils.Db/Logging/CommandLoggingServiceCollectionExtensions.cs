using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rene.Utils.Db.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CommandLoggingServiceCollectionExtensions
    {
        /// <summary>
        /// Enables diagnostic callbacks for MediatR requests. Call before registering other behaviors
        /// to include their execution. Repeated calls replace callbacks without duplicating the behavior.
        /// </summary>
        public static IServiceCollection AddDbUtilsCommandLogging(this IServiceCollection services,
            Action<CommandLoggingOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            var options = new CommandLoggingOptions();
            configure(options);
            if (options.Before == null && options.Completed == null)
                throw new ArgumentException("Configure at least one command logging callback.", nameof(configure));

            services.AddLogging();
            services.TryAddSingleton(TimeProvider.System);
            // Snapshot the delegates so retaining the configuration object cannot change a live pipeline.
            services.Replace(ServiceDescriptor.Singleton(new CommandLoggingOptions
            {
                Before = options.Before,
                Completed = options.Completed
            }));
            services.TryAddEnumerable(ServiceDescriptor.Transient(
                typeof(IPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>)));
            return services;
        }
    }
}
