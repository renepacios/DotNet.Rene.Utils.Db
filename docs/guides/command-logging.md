# Command logging

`UseCommandLogging` enables application-owned callbacks around MediatR requests.
You do not need to implement a pipeline behavior or install Serilog, NLog or another
logging framework. The callbacks choose the service that writes the events.
Logging is disabled unless explicitly configured.

## Configure callbacks

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.AddDbUtils<ShopDbContext>(options => options
    .UseMapper<MyMapper>()
    .UseCommandLogging(logging =>
    {
        logging.Before = (services, context, cancellationToken) =>
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting {CommandType} at {StartedAt}, execution {ExecutionId}",
                context.CommandType, context.StartedAt, context.ExecutionId);
            return Task.CompletedTask;
        };

        logging.Completed = (services, context, cancellationToken) =>
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Completed {CommandType}: {Outcome} in {ElapsedMs} ms",
                context.CommandType, context.Outcome, context.Elapsed.TotalMilliseconds);
            return Task.CompletedTask;
        };
    }), typeof(ProductDto).Assembly);
```

`MyMapper` is your application implementation of `IDbUtilsMapper`, as described in
[configuration](configuration.md). Keep your existing mapper configuration; logging composes with manual mappings,
`UseAutoMapper()` and `UseMapster()`. The sample above uses standard logging only
as an example. Any service registered in your application can be resolved instead:

```csharp
logging.Before = (services, context, token) =>
    services.GetRequiredService<IMyEventWriter>().Starting(context, token);

logging.Completed = (services, context, token) =>
    services.GetRequiredService<IMyEventWriter>().Completed(context, token);
```

`IMyEventWriter` is your own application interface, with methods returning `Task`.
An async lambda can also await more than one operation. Callbacks are awaited
sequentially, not run in the background.

Both callbacks are optional individually, but at least one must be configured.
Repeated `UseCommandLogging` calls replace the previously configured callbacks.
The library snapshots delegates during configuration: do not capture mutable
request-specific state in these shared lambdas. Resolve scoped services inside them.

## Context and lifetime

Each callback receives the **service provider of the current executing scope**.
It is not the root provider and no second scope is created. Resolve `IMediator`
from a scope if callbacks depend on scoped services, including in background workers.

`Before` receives `CommandLoggingContext`; `Completed` receives `CommandCompletedContext`.
Context properties are read-only:

| Property | Available | Meaning |
|----------|-----------|---------|
| `ExecutionId` | Both | Unique correlation ID per execution, including nested requests |
| `CommandType` | Both | Runtime request type, retaining generic type arguments |
| `StartedAt` | Both | UTC `DateTimeOffset` captured before the first callback |
| `CompletedAt` | Completed | UTC instant after the remaining pipeline terminates |
| `Elapsed` | Completed | Monotonic duration, including Before and the remaining pipeline, excluding Completed |
| `Outcome` | Completed | `Succeeded`, `Failed` or `Canceled` |
| `Exception` | Completed | Original pipeline exception; null on success |
| `Execution` | Completed | The same immutable context passed to Before |

The clock is `TimeProvider.System` by default. An existing DI registration of
`TimeProvider` is preserved. Timestamp differences are not used to measure duration,
so adjustments to the wall clock do not distort elapsed time.

No command payload or response is exposed or serialized automatically.
Exception objects can contain sensitive data; only log details intentionally.
An outcome of `Succeeded` means the wrapped pipeline returned normally, not that a
business-level result was successful or an enclosing transaction has committed.

## Failure and cancellation

`Completed` runs on success, exception and cancellation, including cancellation before
the business handler. `OperationCanceledException` from the remaining pipeline produces
`Canceled`; other exceptions produce `Failed`. The original exception is rethrown unchanged.

`Before` receives the request cancellation token. `Completed` receives
`CancellationToken.None` so canceling a request does not suppress its final diagnostic event.
If an event writer needs a time limit, implement a bounded timeout in that writer.
All callback time counts toward the caller's latency.

Failures from either logging callback, including synchronous exceptions, canceled tasks
and null tasks, are reported through the standard .NET logging infrastructure at Error level.
They do not prevent the business handler, replace its response or mask its exception.
The host must configure a standard logging provider to retain these fallback error reports.
The callbacks themselves are not required to use that logging infrastructure.

This is **diagnostic logging, not mandatory auditing**. A failed Before callback does
not block a write. A failed Completed callback cannot undo a committed write.
Do not use these callbacks as authorization checks or as transactional audit guarantees.

## Scope of the behavior and ordering

Both explicit `Send(new ...Command(...))` and the mediator convenience extensions
use the same behavior. Custom MediatR requests are included too, not just CRUD.
Direct calls to handlers bypass the pipeline. Notifications (`Publish`) and stream
requests are not covered.

The behavior wraps the rest of the pipeline in registration order. Register logging
before authorization, validation, caching and transaction behaviors if you want to
observe their denials, cache hits and full elapsed time.

For applications needing explicit ordering or only custom requests, register it directly:

```csharp
services.AddDbUtilsCommandLogging(logging =>
{
    logging.Completed = (services, context, token) =>
        services.GetRequiredService<IMyEventWriter>().Completed(context, token);
});
// Register remaining behaviors afterwards.
```

The standalone API and `UseCommandLogging` install the same behavior. Repeated
registrations do not duplicate it; the last callback configuration wins, without
changing its existing position in the pipeline. Settings are global for this
service collection, not scoped to a particular DbContext.

## Examples

All three sample projects demonstrate the feature without new package references:

| Sample | Demonstration |
|--------|---------------|
| AutoMapper | Before and Completed lambdas resolve `ILogger<Program>` |
| Mapster | Only Completed is configured |
| ManualMapper | Both callbacks resolve scoped `ISampleCommandEventWriter`, an application-owned service |

Each sample retains both the explicit-command and mediator-extension controllers.
Run any sample and invoke those endpoints to see correlated command events.
