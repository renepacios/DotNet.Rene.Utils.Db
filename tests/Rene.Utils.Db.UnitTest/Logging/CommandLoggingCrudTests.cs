namespace Rene.Utils.Db.UnitTest.Logging
{
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Models;
    using Models.Specifications;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.ExtensionHelpers;
    using Rene.Utils.Db.Logging;
    using static LoggingTestSupport;

    public class CommandLoggingCrudTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Logging_GenericCrudSendAndConvenienceRoutes_UseSameCallbacksAndMapper(bool useExtensions, bool enabled)
        {
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            var providers = new List<IServiceProvider>();
            var tokens = new List<CancellationToken>();
            using var cts = new CancellationTokenSource();
            using var provider = CreateProvider(starts, ends, providers, tokens, enabled);
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var vm = SampleDetailsViewModel.Create(17, "Alpha", "first");

            var added = useExtensions ? await mediator.Add(vm, cts.Token)
                : await mediator.Send(new AddCommand<SampleDetailsViewModel>(vm), cts.Token);
            added.Id.Should().Be(17);
            added.Name.Should().Be("Alpha");
            added.Description.Should().Be("first");
            var found = useExtensions ? await mediator.Get<SampleDetailsViewModel>(17, cts.Token)
                : await mediator.Send(new GetCommand<SampleDetailsViewModel>(17), cts.Token);
            found.Id.Should().Be(17);
            found.Name.Should().Be("Alpha");
            var all = useExtensions ? await mediator.GetAll<SampleDetailsViewModel>(cts.Token)
                : await mediator.Send(new GetAllCommand<SampleDetailsViewModel>(), cts.Token);
            all.Should().ContainSingle().Which.Id.Should().Be(17);
            var spec = new NameStartsWithSpec("A");
            var filtered = useExtensions ? await mediator.GetBySpec<SampleDetailsViewModel, Sample>(spec, cts.Token)
                : await mediator.Send(new GetBySpecCommand<SampleDetailsViewModel, Sample>(spec), cts.Token);
            filtered.Should().ContainSingle().Which.Name.Should().Be("Alpha");
            var page = useExtensions
                ? await mediator.GetPaginated<SampleDetailsViewModel, Sample>(spec, 10, 0, cts.Token)
                : await mediator.Send(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(spec, 10), cts.Token);
            page.TotalElements.Should().Be(1);
            page.Content.Should().ContainSingle().Which.Id.Should().Be(17);

            var updatedVm = SampleDetailsViewModel.Create(17, "Beta", "changed");
            var updated = useExtensions ? await mediator.Update(updatedVm, 17, cts.Token)
                : await mediator.Send(new UpdateCommand<SampleDetailsViewModel>(updatedVm, 17), cts.Token);
            updated.Name.Should().Be("Beta");
            updated.Description.Should().Be("changed");
            var context = scope.ServiceProvider.GetRequiredService<CrudDbContext>();
            (await context.Samples.SingleAsync()).Name.Should().Be("Beta");
            var deleted = useExtensions ? await mediator.Delete<SampleDetailsViewModel>(17, cts.Token)
                : await mediator.Send(new DeleteCommand<SampleDetailsViewModel>(17), cts.Token);
            deleted.Should().BeTrue();
            (await context.Samples.CountAsync()).Should().Be(0);

            if (!enabled)
            {
                starts.Should().BeEmpty();
                ends.Should().BeEmpty();
                providers.Should().BeEmpty();
                tokens.Should().BeEmpty();
                scope.ServiceProvider.GetServices<IPipelineBehavior<AddCommand<SampleDetailsViewModel>, SampleDetailsViewModel>>()
                    .Should().BeEmpty();
                return;
            }
            starts.Select(c => c.CommandType).Should().Equal(
                typeof(AddCommand<SampleDetailsViewModel>),
                typeof(GetCommand<SampleDetailsViewModel>),
                typeof(GetAllCommand<SampleDetailsViewModel>),
                typeof(GetBySpecCommand<SampleDetailsViewModel, Sample>),
                typeof(GetPaginatedCommand<SampleDetailsViewModel, Sample>),
                typeof(UpdateCommand<SampleDetailsViewModel>),
                typeof(DeleteCommand<SampleDetailsViewModel>));
            AssertSuccessfulExecutions(starts, ends, providers, tokens, scope.ServiceProvider, cts.Token, 7);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Logging_PaginationConvenienceOverloads_LogOneCorrelatedExecution(int route)
        {
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            var providers = new List<IServiceProvider>();
            var tokens = new List<CancellationToken>();
            using var cts = new CancellationTokenSource();
            using var provider = CreateProvider(starts, ends, providers, tokens);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CrudDbContext>();
            context.Samples.AddRange(Sample.Create(1, "Alpha", "first"), Sample.Create(2, "Beta", "second"));
            await context.SaveChangesAsync();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var spec = new NameStartsWithSpec("A");
            var page = route switch
            {
                0 => await mediator.GetPaginated<SampleDetailsViewModel, Sample>(1, 1, cts.Token),
                1 => await mediator.GetPaginated<SampleDetailsViewModel, Sample>(spec, 1, 0, cts.Token),
                _ => await mediator.GetPaginated<SampleDetailsViewModel, Sample>(spec,
                    new PaginationOptions { Size = 1, Page = 0 }, cts.Token)
            };

            page.TotalElements.Should().Be(route == 0 ? 2 : 1);
            page.Content.Should().ContainSingle().Which.Id.Should().Be(route == 0 ? 2 : 1);
            starts.Select(c => c.CommandType).Should().Equal(typeof(GetPaginatedCommand<SampleDetailsViewModel, Sample>));
            AssertSuccessfulExecutions(starts, ends, providers, tokens, scope.ServiceProvider, cts.Token, 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Logging_GenericCrudFailure_PreservesThrownExceptionInCompletion(bool useExtensions)
        {
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(starts, ends, new(), new());
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var caught = await Record.ExceptionAsync(() => useExtensions
                ? mediator.Delete<SampleDetailsViewModel>(404)
                : mediator.Send(new DeleteCommand<SampleDetailsViewModel>(404)));

            caught.Should().BeOfType<KeyNotFoundException>().Which.Message.Should().Contain("404");
            starts.Should().ContainSingle().Which.CommandType.Should().Be(typeof(DeleteCommand<SampleDetailsViewModel>));
            var completed = ends.Should().ContainSingle().Subject;
            completed.Execution.Should().BeSameAs(starts[0]);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Failed);
            completed.Exception.Should().BeSameAs(caught);
            (await scope.ServiceProvider.GetRequiredService<CrudDbContext>().Samples.CountAsync()).Should().Be(0);
        }

        private static ServiceProvider CreateProvider(List<CommandLoggingContext> starts,
            List<CommandCompletedContext> ends, List<IServiceProvider> providers, List<CancellationToken> tokens, bool enabled = true)
        {
            var services = new ServiceCollection();
            services.AddDbContext<CrudDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<TimeProvider>(new ManualClock());
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CommandLoggingCrudTests>());
            services.AddDbUtils<CrudDbContext>(options =>
            {
                var configured = options.UseMapper(mapper => mapper
                    .Map<Sample, SampleDetailsViewModel>(entity => new SampleDetailsViewModel
                    {
                        Id = entity.Id, Name = entity.Name, Description = entity.Description
                    })
                    .Map<SampleDetailsViewModel, Sample>(vm => new Sample
                    {
                        Id = vm.Id, Name = vm.Name, Description = vm.Description
                    })
                    .MapInto<SampleDetailsViewModel, Sample>((vm, entity) =>
                    {
                        entity.Name = vm.Name; entity.Description = vm.Description;
                    }));
                if (enabled) configured.UseCommandLogging(o =>
                {
                    o.Before = (sp, context, ct) =>
                    {
                        starts.Add(context); providers.Add(sp); tokens.Add(ct);
                        return Task.CompletedTask;
                    };
                    o.Completed = (sp, context, ct) =>
                    {
                        ends.Add(context); providers.Add(sp); tokens.Add(ct);
                        return Task.CompletedTask;
                    };
                }).Should().BeSameAs(options);
            }, typeof(CommandLoggingCrudTests).Assembly);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        private static void AssertSuccessfulExecutions(List<CommandLoggingContext> starts,
            List<CommandCompletedContext> ends, List<IServiceProvider> providers, List<CancellationToken> tokens,
            IServiceProvider expectedProvider, CancellationToken expectedToken, int count)
        {
            starts.Should().HaveCount(count);
            ends.Should().HaveCount(count);
            starts.Select(c => c.ExecutionId).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
            for (var i = 0; i < count; i++)
            {
                ends[i].Execution.Should().BeSameAs(starts[i]);
                ends[i].Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
                ends[i].Exception.Should().BeNull();
                ends[i].Elapsed.Should().Be(TimeSpan.Zero);
                tokens[2 * i].Should().Be(expectedToken);
                tokens[2 * i + 1].Should().Be(CancellationToken.None);
            }
            providers.Should().HaveCount(2 * count).And.OnlyContain(sp => ReferenceEquals(sp, expectedProvider));
        }

        private sealed class CrudDbContext(DbContextOptions<CrudDbContext> options) : DbContext(options)
        {
            public DbSet<Sample> Samples => Set<Sample>();
        }
    }
}
