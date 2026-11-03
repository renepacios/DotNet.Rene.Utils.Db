namespace Rene.Utils.Db.UnitTest;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Models;
using Models.Specifications;
using Rene.Utils.Db.Builder;
using Rene.Utils.Db.Commands;
using Rene.Utils.Db.ExtensionHelpers;
using Rene.Utils.Db.Events;

public sealed class CrudEventsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Add_Command_PublishesSavedEntityAndDistinctInputAndResponse_ToBothSubscribers(bool useExtension)
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        var input = SampleDetailsViewModel.Create(0, "Added", "input");

        var response = useExtension
            ? await mediator.Add(input)
            : await mediator.Send(new AddCommand<SampleDetailsViewModel>(input));

        var observations = probe.Calls
            .Where(call => call.Notification is EntityAdded<Sample, SampleDetailsViewModel>)
            .ToArray();
        observations.Should().HaveCount(2);
        observations.Select(call => call.Subscriber).Should().BeEquivalentTo("added-one", "added-two");
        observations.Select(call => call.Notification).Distinct(ReferenceEqualityComparer.Instance).Should().ContainSingle();
        scope.ServiceProvider.GetServices<INotificationHandler<EntityAdded<Sample, SampleDetailsViewModel>>>()
            .Should().HaveCount(2);
        scope.ServiceProvider.GetRequiredService<EventProbe>().Should().BeSameAs(probe);

        var notification = (EntityAdded<Sample, SampleDetailsViewModel>)observations[0].Notification;
        notification.Entity.Should().BeSameAs(
            ((EntityAdded<Sample, SampleDetailsViewModel>)observations[1].Notification).Entity);
        notification.ViewModel.Should().BeSameAs(input);
        notification.Response.Should().BeSameAs(response);
        response.Should().NotBeSameAs(input);
        response.Id.Should().BeGreaterThan(0);
        notification.Entity.Id.Should().Be(response.Id);
        notification.Entity.Name.Should().Be("Added");
        notification.Entity.Description.Should().Be("input");
        response.Description.Should().Be("mapped:input");
        observations.Should().OnlyContain(call =>
            call.PersistedSample != null &&
            call.PersistedSample.Id == response.Id &&
            call.PersistedSample.Name == "Added" &&
            call.PersistedSample.Description == "input");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_Command_PublishesRequestIdAndUpdatedEntity_ToBothSubscribers(bool useExtension)
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var storedEntity = Sample.Create(41, "Before", "before");
        context.Samples.Add(storedEntity);
        await context.SaveChangesAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        object requestId = 41;
        var input = SampleDetailsViewModel.Create(999, "Updated", "new input");
        var response = useExtension
            ? await mediator.Update(input, requestId)
            : await mediator.Send(new UpdateCommand<SampleDetailsViewModel>(input, requestId));

        var observations = probe.Calls
            .Where(call => call.Notification is EntityUpdated<Sample, SampleDetailsViewModel>)
            .ToArray();
        observations.Should().HaveCount(2);
        observations.Select(call => call.Subscriber).Should().BeEquivalentTo("updated-one", "updated-two");
        observations.Select(call => call.Notification).Distinct(ReferenceEqualityComparer.Instance).Should().ContainSingle();

        var notification = (EntityUpdated<Sample, SampleDetailsViewModel>)observations[0].Notification;
        notification.Entity.Should().BeSameAs(storedEntity);
        notification.ViewModel.Should().BeSameAs(input);
        notification.Id.Should().BeSameAs(requestId);
        notification.Response.Should().BeSameAs(response);
        response.Should().NotBeSameAs(input);
        response.Id.Should().Be(41);
        response.Name.Should().Be("Updated");
        response.Description.Should().Be("mapped:new input");
        notification.Entity.Id.Should().Be(41);
        observations.Should().OnlyContain(call =>
            call.PersistedSample != null &&
            call.PersistedSample.Id == 41 &&
            call.PersistedSample.Name == "Updated" &&
            call.PersistedSample.Description == "new input");
        (await context.Samples.AsNoTracking().ToListAsync()).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = 41, Name = "Updated", Description = "new input" });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Command_PublishesRemovedEntityWithoutMapping_ToBothSubscribers(bool useExtension)
    {
        var mapperCalls = new List<string>();
        using var provider = CreateProvider(mapperCalls: mapperCalls);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var storedEntity = Sample.Create(73, "Delete", "removed");
        context.Samples.Add(storedEntity);
        await context.SaveChangesAsync();
        mapperCalls.Clear();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        object requestId = 73;
        var result = useExtension
            ? await mediator.Delete<SampleDetailsViewModel>(requestId)
            : await mediator.Send(new DeleteCommand<SampleDetailsViewModel>(requestId));

        result.Should().BeTrue();
        mapperCalls.Should().BeEmpty();
        var observations = probe.Calls
            .Where(call => call.Notification is EntityDeleted<Sample, SampleDetailsViewModel>)
            .ToArray();
        observations.Should().HaveCount(2);
        observations.Select(call => call.Subscriber).Should().BeEquivalentTo("deleted-one", "deleted-two");
        observations.Select(call => call.Notification).Distinct(ReferenceEqualityComparer.Instance).Should().ContainSingle();
        var notification = (EntityDeleted<Sample, SampleDetailsViewModel>)observations[0].Notification;
        notification.Entity.Should().BeSameAs(storedEntity);
        notification.Id.Should().BeSameAs(requestId);
        observations.Should().OnlyContain(call => call.PersistedSample == null);
        (await context.Samples.AsNoTracking().ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Read_CommandsAndAllExtensionOverloads_DoNotPublishEvents()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        var saved = await mediator.Add(SampleDetailsViewModel.Create(0, "Alpha", "first"));
        probe.Calls.Clear();
        var specification = new NameStartsWithSpec("A");

        (await mediator.Send(new GetCommand<SampleDetailsViewModel>(saved.Id))).Name.Should().Be("Alpha");
        (await mediator.Get<SampleDetailsViewModel>(saved.Id)).Name.Should().Be("Alpha");
        (await mediator.Send(new GetAllCommand<SampleDetailsViewModel>())).Should().ContainSingle();
        (await mediator.GetAll<SampleDetailsViewModel>()).Should().ContainSingle();
        (await mediator.Send(new GetBySpecCommand<SampleDetailsViewModel, Sample>(specification))).Should().ContainSingle();
        (await mediator.GetBySpec<SampleDetailsViewModel, Sample>(specification)).Should().ContainSingle();

        (await mediator.Send(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(1))).Content.Should().ContainSingle();
        (await mediator.GetPaginated<SampleDetailsViewModel, Sample>(1)).Content.Should().ContainSingle();
        (await mediator.Send(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(specification, 1))).Content.Should().ContainSingle();
        (await mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, 1)).Content.Should().ContainSingle();
        var options = new PaginationOptions { Size = 1, Page = 0 };
        (await mediator.Send(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(specification, options))).Content.Should().ContainSingle();
        (await mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, options)).Content.Should().ContainSingle();

        probe.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_IsAwaitedBeforeMutationReturns()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        var subscriberEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSubscriber = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        probe.OnCall = async (subscriber, _, _) =>
        {
            if (subscriber != "added-one") return;
            subscriberEntered.TrySetResult();
            await releaseSubscriber.Task;
        };

        var sendTask = mediator.Send(new AddCommand<SampleDetailsViewModel>(
            SampleDetailsViewModel.Create(0, "Await", "subscriber")));
        await subscriberEntered.Task;

        sendTask.IsCompleted.Should().BeFalse();
        releaseSubscriber.SetResult();
        (await sendTask).Name.Should().Be("Await");
    }

    [Fact]
    public async Task PublishFailure_PropagatesOriginalException_AfterSave()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        var expected = new InvalidOperationException("subscriber failed");
        probe.OnCall = (_, _, _) => throw expected;

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Send(new AddCommand<SampleDetailsViewModel>(
                SampleDetailsViewModel.Create(0, "Saved", "subscriber failure"))));

        actual.Should().BeSameAs(expected);
        (await context.Samples.AsNoTracking().SingleAsync())
            .Should().BeEquivalentTo(new { Name = "Saved", Description = "subscriber failure" });
    }

    [Fact]
    public async Task PublishCancellation_UsesRequestTokenAndLeavesSavedRow()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        using var cancellationSource = new CancellationTokenSource();
        probe.OnCall = (_, _, cancellationToken) =>
        {
            cancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        };

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.Send(
                new AddCommand<SampleDetailsViewModel>(SampleDetailsViewModel.Create(0, "Canceled", "saved")),
                cancellationSource.Token));

        exception.CancellationToken.Should().Be(cancellationSource.Token);
        (await context.Samples.AsNoTracking().SingleAsync()).Name.Should().Be("Canceled");
        probe.Calls.Should().Contain(call => call.CancellationToken == cancellationSource.Token);
    }

    [Fact]
    public async Task NoNotificationSubscribers_DoesNotPreventCrud()
    {
        using var provider = CreateProvider(registerSubscribers: false);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();

        var result = await mediator.Add(SampleDetailsViewModel.Create(0, "No subscribers", "saved"));

        result.Id.Should().BeGreaterThan(0);
        (await context.Samples.AsNoTracking().SingleAsync()).Name.Should().Be("No subscribers");
    }

    [Fact]
    public void Notifications_RejectNullEntities_AndExposeGetOnlyReferences()
    {
        var input = SampleDetailsViewModel.Create(1, "Input", "input");
        var response = SampleDetailsViewModel.Create(1, "Response", "response");
        Action add = () => new EntityAdded<Sample, SampleDetailsViewModel>(null, input, response);
        Action update = () => new EntityUpdated<Sample, SampleDetailsViewModel>(null, input, 1, response);
        Action delete = () => new EntityDeleted<Sample, SampleDetailsViewModel>(null, 1);
        add.Should().Throw<ArgumentNullException>().WithParameterName("entity");
        update.Should().Throw<ArgumentNullException>().WithParameterName("entity");
        delete.Should().Throw<ArgumentNullException>().WithParameterName("entity");

        typeof(EntityAdded<Sample, SampleDetailsViewModel>).GetProperties().Should()
            .OnlyContain(property => property.SetMethod == null);
        typeof(EntityUpdated<Sample, SampleDetailsViewModel>).GetProperties().Should()
            .OnlyContain(property => property.SetMethod == null);
        typeof(EntityDeleted<Sample, SampleDetailsViewModel>).GetProperties().Should()
            .OnlyContain(property => property.SetMethod == null);
        typeof(INotification).IsAssignableFrom(typeof(EntityAdded<Sample, SampleDetailsViewModel>)).Should().BeTrue();
        typeof(INotification).IsAssignableFrom(typeof(EntityUpdated<Sample, SampleDetailsViewModel>)).Should().BeTrue();
        typeof(INotification).IsAssignableFrom(typeof(EntityDeleted<Sample, SampleDetailsViewModel>)).Should().BeTrue();
    }

    [Fact]
    public async Task AddMappingFailure_DoesNotSaveOrPublish()
    {
        await using var context = CreateContext();
        var input = SampleDetailsViewModel.Create(0, "Mapping", "failure");
        var mapper = new Mock<IDbUtilsMapper>(MockBehavior.Strict);
        var expected = new InvalidOperationException("input mapping failed");
        mapper.Setup(item => item.Map<Sample>(input)).Throws(expected);
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, null, publisher.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddCommand<SampleDetailsViewModel>(input), CancellationToken.None));

        actual.Should().BeSameAs(expected);
        (await context.Samples.AsNoTracking().ToListAsync()).Should().BeEmpty();
        VerifyNoPublish(publisher);
    }

    [Fact]
    public async Task AddSaveFailure_DoesNotPublish()
    {
        await using var context = CreateContext();
        var input = SampleDetailsViewModel.Create(0, "Save", "failure");
        var mapper = MapperForAdd(input);
        var expected = new InvalidOperationException("save failed");
        var unitOfWork = new Mock<IDbUtilsUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(expected);
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, unitOfWork.Object, publisher.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddCommand<SampleDetailsViewModel>(input), CancellationToken.None));

        actual.Should().BeSameAs(expected);
        (await context.Samples.AsNoTracking().ToListAsync()).Should().BeEmpty();
        VerifyNoPublish(publisher);
    }

    [Fact]
    public async Task UpdateMappingFailure_DoesNotSaveOrPublish()
    {
        await using var context = CreateContext();
        context.Samples.Add(Sample.Create(88, "Original", "original"));
        await context.SaveChangesAsync();
        var input = SampleDetailsViewModel.Create(999, "Not persisted", "mapping failed");
        var mapper = new Mock<IDbUtilsMapper>(MockBehavior.Strict);
        var expected = new InvalidOperationException("update mapping failed");
        mapper.Setup(item => item.Map(input, It.IsAny<Sample>())).Throws(expected);
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, null, publisher.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UpdateCommand<SampleDetailsViewModel>(input, 88), CancellationToken.None));

        actual.Should().BeSameAs(expected);
        (await context.Samples.AsNoTracking().SingleAsync())
            .Should().BeEquivalentTo(new { Id = 88, Name = "Original", Description = "original" });
        VerifyNoPublish(publisher);
    }

    [Fact]
    public async Task CanceledSave_DoesNotPublish()
    {
        await using var context = CreateContext();
        var input = SampleDetailsViewModel.Create(0, "Save", "canceled");
        var mapper = MapperForAdd(input);
        using var source = new CancellationTokenSource();
        CancellationToken observedToken = default;
        var unitOfWork = new Mock<IDbUtilsUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(token =>
            {
                observedToken = token;
                source.Cancel();
                return Task.FromCanceled<int>(token);
            });
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, unitOfWork.Object, publisher.Object);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new AddCommand<SampleDetailsViewModel>(input), source.Token));

        exception.CancellationToken.Should().Be(source.Token);
        observedToken.Should().Be(source.Token);
        (await context.Samples.AsNoTracking().ToListAsync()).Should().BeEmpty();
        VerifyNoPublish(publisher);
    }

    [Fact]
    public async Task AddResponseMappingFailure_DoesNotPublishAfterSuccessfulSave()
    {
        await using var context = CreateContext();
        var input = SampleDetailsViewModel.Create(0, "Persisted", "response map failure");
        var mapper = new Mock<IDbUtilsMapper>(MockBehavior.Strict);
        mapper.Setup(item => item.Map<Sample>(input))
            .Returns(new Sample { Name = input.Name, Description = input.Description });
        var expected = new InvalidOperationException("response mapping failed");
        mapper.Setup(item => item.Map<SampleDetailsViewModel>(It.IsAny<object>())).Throws(expected);
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, null, publisher.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddCommand<SampleDetailsViewModel>(input), CancellationToken.None));

        actual.Should().BeSameAs(expected);
        var persisted = await context.Samples.AsNoTracking().SingleAsync();
        persisted.Name.Should().Be("Persisted");
        persisted.Description.Should().Be("response map failure");
        VerifyNoPublish(publisher);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingRecord_DoesNotPublish(bool delete)
    {
        await using var context = CreateContext();
        var mapper = new Mock<IDbUtilsMapper>(MockBehavior.Strict);
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        var handler = CreateHandler(mapper.Object, context, null, publisher.Object);
        var commandId = 602;

        if (delete)
        {
            var actual = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new DeleteCommand<SampleDetailsViewModel>(commandId), CancellationToken.None));
            actual.Message.Should().Be($"Entidad no encontrada {commandId}");
        }
        else
        {
            var input = SampleDetailsViewModel.Create(commandId, "Missing", "missing");
            var actual = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new UpdateCommand<SampleDetailsViewModel>(input, commandId), CancellationToken.None));
            actual.Message.Should().Be($"Entidad no encontrada {commandId}");
        }

        (await context.Samples.AsNoTracking().ToListAsync()).Should().BeEmpty();
        VerifyNoPublish(publisher);
    }

    [Fact]
    public async Task CustomUnitOfWork_IsUsedBeforeAddNotification()
    {
        using var provider = CreateProvider(useCustomUnitOfWork: true);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<EventProbe>();
        var unitOfWorkProbe = scope.ServiceProvider.GetRequiredService<UnitOfWorkProbe>();
        using var cancellationSource = new CancellationTokenSource();

        var result = await mediator.Send(
            new AddCommand<SampleDetailsViewModel>(SampleDetailsViewModel.Create(0, "Custom UoW", "committed")),
            cancellationSource.Token);

        unitOfWorkProbe.SaveCount.Should().Be(1);
        unitOfWorkProbe.CancellationToken.Should().Be(cancellationSource.Token);
        probe.Calls.Should().HaveCount(2);
        probe.Calls.Should().OnlyContain(call => call.PersistedSample != null && call.PersistedSample.Id == result.Id);
    }

    private static ServiceProvider CreateProvider(
        bool registerSubscribers = true,
        List<string> mapperCalls = null,
        bool useCustomUnitOfWork = false)
    {
        var services = new ServiceCollection();
        services.AddDbContext<EventDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<EventProbe>();
        services.AddScoped<UnitOfWorkProbe>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CrudEventsTests).Assembly));

        void ConfigureMapper(DbUtilsOptions options) => options.UseMapper(m => m
            .Map<SampleDetailsViewModel, Sample>((_, viewModel) =>
            {
                mapperCalls?.Add("input-to-entity");
                return new Sample
                {
                    Id = viewModel.Id,
                    Name = viewModel.Name,
                    Description = viewModel.Description
                };
            })
            .Map<Sample, SampleDetailsViewModel>((_, entity) =>
            {
                mapperCalls?.Add("entity-to-response");
                return SampleDetailsViewModel.Create(entity.Id, entity.Name, $"mapped:{entity.Description}");
            })
            .MapInto<SampleDetailsViewModel, Sample>((viewModel, entity) =>
            {
                mapperCalls?.Add("map-into");
                entity.Id = viewModel.Id;
                entity.Name = viewModel.Name;
                entity.Description = viewModel.Description;
            }));

        if (useCustomUnitOfWork)
        {
            services.AddScoped<EventTestUnitOfWork>();
            services.AddDbUtils<EventDbContext, EventTestUnitOfWork>(
                ConfigureMapper, typeof(CrudEventsTests).Assembly);
        }
        else
        {
            services.AddDbUtils<EventDbContext>(ConfigureMapper, typeof(CrudEventsTests).Assembly);
        }

        if (!registerSubscribers)
        {
            services.RemoveAll<INotificationHandler<EntityAdded<Sample, SampleDetailsViewModel>>>();
            services.RemoveAll<INotificationHandler<EntityUpdated<Sample, SampleDetailsViewModel>>>();
            services.RemoveAll<INotificationHandler<EntityDeleted<Sample, SampleDetailsViewModel>>>();
        }

        return services.BuildServiceProvider();
    }

    private static EventDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EventDbContext(options);
    }

    private static GenericCommandHandler<SampleDetailsViewModel, Sample, EventDbContext, IDbUtilsUnitOfWork>
        CreateHandler(IDbUtilsMapper mapper, EventDbContext context, IDbUtilsUnitOfWork unitOfWork, IPublisher publisher)
        => new(mapper, context, unitOfWork, publisher);

    private static Mock<IDbUtilsMapper> MapperForAdd(SampleDetailsViewModel input)
    {
        var mapper = new Mock<IDbUtilsMapper>(MockBehavior.Strict);
        mapper.Setup(item => item.Map<Sample>(input))
            .Returns(new Sample { Name = input.Name, Description = input.Description });
        mapper.Setup(item => item.Map<SampleDetailsViewModel>(It.IsAny<object>()))
            .Returns((object entity) => SampleDetailsViewModel.Create((Sample)entity));
        return mapper;
    }

    private static void VerifyNoPublish(Mock<IPublisher> publisher)
    {
        publisher.Verify(item => item.Publish(
            It.IsAny<EntityAdded<Sample, SampleDetailsViewModel>>(), It.IsAny<CancellationToken>()), Times.Never());
        publisher.Verify(item => item.Publish(
            It.IsAny<EntityUpdated<Sample, SampleDetailsViewModel>>(), It.IsAny<CancellationToken>()), Times.Never());
        publisher.Verify(item => item.Publish(
            It.IsAny<EntityDeleted<Sample, SampleDetailsViewModel>>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    public sealed class EventDbContext(DbContextOptions<EventDbContext> options) : DbContext(options)
    {
        public DbSet<Sample> Samples => Set<Sample>();
    }

    public sealed class EventProbe
    {
        public List<EventCall> Calls { get; } = new();
        public Func<string, INotification, CancellationToken, Task> OnCall { get; set; }

        public async Task RecordAsync(
            string subscriber,
            INotification notification,
            Sample persistedSample,
            CancellationToken cancellationToken)
        {
            Calls.Add(new EventCall(subscriber, notification, persistedSample, cancellationToken));
            if (OnCall is not null)
                await OnCall(subscriber, notification, cancellationToken);
        }
    }

    public sealed record EventCall(
        string Subscriber,
        INotification Notification,
        Sample PersistedSample,
        CancellationToken CancellationToken);

    public sealed class AddedSubscriberOne(IServiceProvider services)
        : INotificationHandler<EntityAdded<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityAdded<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("added-one", notification, saved, cancellationToken);
        }
    }

    public sealed class AddedSubscriberTwo(IServiceProvider services)
        : INotificationHandler<EntityAdded<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityAdded<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("added-two", notification, saved, cancellationToken);
        }
    }

    public sealed class UpdatedSubscriberOne(IServiceProvider services)
        : INotificationHandler<EntityUpdated<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityUpdated<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("updated-one", notification, saved, cancellationToken);
        }
    }

    public sealed class UpdatedSubscriberTwo(IServiceProvider services)
        : INotificationHandler<EntityUpdated<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityUpdated<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("updated-two", notification, saved, cancellationToken);
        }
    }

    public sealed class DeletedSubscriberOne(IServiceProvider services)
        : INotificationHandler<EntityDeleted<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityDeleted<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("deleted-one", notification, saved, cancellationToken);
        }
    }

    public sealed class DeletedSubscriberTwo(IServiceProvider services)
        : INotificationHandler<EntityDeleted<Sample, SampleDetailsViewModel>>
    {
        public async Task Handle(EntityDeleted<Sample, SampleDetailsViewModel> notification, CancellationToken cancellationToken)
        {
            var probe = services.GetService<EventProbe>();
            var context = services.GetService<EventDbContext>();
            if (probe == null || context == null) return;
            var saved = await context.Samples.AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == notification.Entity.Id, cancellationToken);
            await probe.RecordAsync("deleted-two", notification, saved, cancellationToken);
        }
    }

    public sealed class UnitOfWorkProbe
    {
        public int SaveCount { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    public sealed class EventTestUnitOfWork(EventDbContext context, UnitOfWorkProbe probe) : IDbUtilsUnitOfWork
    {
        public int SaveChanges() => context.SaveChanges();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            probe.SaveCount++;
            probe.CancellationToken = cancellationToken;
            return context.SaveChangesAsync(cancellationToken);
        }

        public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
