# CRUD events

Generic write handlers publish typed MediatR notifications by default. No additional
package or opt-in setting is required. Continue registering MediatR and `AddDbUtils`
as usual. Without subscribers, commands return normally.

| Notification | Contents |
|--------------|----------|
| `EntityAdded<TModel, TViewModel>` | Saved `Entity`, input `ViewModel`, mapped `Response` |
| `EntityUpdated<TModel, TViewModel>` | Saved `Entity`, input `ViewModel`, request `Id`, mapped `Response` |
| `EntityDeleted<TModel, TViewModel>` | Removed `Entity` and request `Id`; no view model is generated |

All notifications implement `INotification` and live in `Rene.Utils.Db.Events`
in the abstractions package. Their generic arguments identify the entity/DTO pair
registered through `IWithGenericHandler<TEntity>`. A subscription for one DTO
does not automatically receive events for a different DTO mapped to the same entity.

`ViewModel` is the original command input, not the persisted response. `Response` is
the mapped object the Add/Update handler returns, including generated values when
the mapping exposes them. `Id` is the identifier passed to Update/Delete, not a
value inferred from the input DTO. Add exposes generated identifiers through `Entity`
and `Response`, rather than assuming every entity has a property called `Id`.

## Subscribe

Using the `Product` and `ProductDto` from [getting started](getting-started.md):

```csharp
using MediatR;
using Rene.Utils.Db.Events;

public sealed class ProductAddedHandler : INotificationHandler<EntityAdded<Product, ProductDto>>
{
    public Task Handle(EntityAdded<Product, ProductDto> notification, CancellationToken token)
    {
        // Resolve your application services through constructor injection.
        // notification.Entity contains the saved entity.
        // notification.ViewModel is the input; notification.Response is the mapped result.
        return Task.CompletedTask;
    }
}
```

Register the subscriber assembly through MediatR, in addition to the assemblies
passed to `AddDbUtils` for generic CRUD registration:

```csharp
services.AddMediatR(options =>
    options.RegisterServicesFromAssemblyContaining<ProductAddedHandler>());
```

Use `INotificationHandler<EntityUpdated<Product, ProductDto>>` and
`INotificationHandler<EntityDeleted<Product, ProductDto>>` for the other operations.
One class can implement all three interfaces. Multiple subscribers are supported;
their ordering/concurrency follows the notification publisher configured in MediatR.
The default MediatR publisher awaits subscribers sequentially; do not rely on a
particular subscription order.

## When events run

For Add/Update the sequence is:

1. Apply the existing mapping and write the entity.
2. Await the existing context/UoW `SaveChangesAsync`.
3. Map the response.
4. Await `IPublisher.Publish` with the request cancellation token.
5. Return the same mapped response.

For Delete, the handler finds/removes the entity, saves changes, publishes and
returns `true`. It does not map a deletion DTO.

`Send(new AddCommand<ProductDto>(dto))` and `mediator.Add(dto)` publish the same event.
Publication belongs to the generic handler itself, not a pipeline behavior.
Get, GetAll, GetBySpec and every paginated query publish no CRUD events.
Custom command handlers do not automatically publish these notifications; publish
application-specific events explicitly where appropriate.

No notification is emitted when entity lookup, input mapping or saving fails.
If response mapping fails after saving, publication is also skipped even though
the write may already be persisted. There is no automatic retry.

## Failure and transaction boundaries

> [!WARNING]
> Publish is awaited **after saving**. A subscriber exception or cancellation propagates
> to the caller, but does not undo changes already saved. Retrying the whole command
> can repeat a write or deliver an event more than once.

The supplied entity and DTOs are live object references, not immutable snapshots.
Subscribers should treat them as read-only. Do not modify tracked entities, retain
them beyond their scope, or pass them to a background task without making an
appropriate snapshot. A subscriber can mutate the response object the caller receives.

The subscriber runs through the same scoped MediatR provider. It can resolve scoped
services, but should not recursively issue the same write without a termination condition.
Subscriber side effects are not automatically saved or included in a transaction.

An external transaction can still be active after `SaveChangesAsync`: publication
does **not** guarantee that transaction has committed. A custom unit of work must
implement actual persistence; the event follows its save method returning normally.

These are **in-process CRUD notifications**, not event sourcing, a durable event store,
an outbox or a message broker. A process crash between saving and publishing can lose
notifications. Use a transactional outbox for reliable integration events or
transactional audit requirements.

## Examples

All three sample projects include `WeatherForecastEventHandler`, subscribed to Add,
Update and Delete for `WeatherForecastViewModelDetails`. Their existing MediatR
assembly registration discovers it automatically. Both controller styles remain
available; POST, PUT and DELETE display the corresponding event in the application logs.
