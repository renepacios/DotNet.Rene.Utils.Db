namespace Rene.Utils.Db.UnitTest.ExtensionHelpers
{
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Models;
    using Models.Specifications;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.ExtensionHelpers;

    public class MediatorCrudTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExtensionsAndGenericCommands_UseTheSameHandlersAndPipeline(bool useExtensions)
        {
            var services = new ServiceCollection();
            services.AddDbContext<SampleDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<List<Type>>();
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblyContaining<MediatorCrudTests>();
                configuration.AddOpenBehavior(typeof(RecordingBehavior<,>));
            });
            services.AddDbUtils<SampleDbContext>(options => options.UseMapper(mapper => mapper
                .Map<Sample, SampleDetailsViewModel>(entity => new SampleDetailsViewModel
                {
                    Id = entity.Id, Name = entity.Name, Description = entity.Description
                })
                .Map<SampleDetailsViewModel, Sample>(viewModel => new Sample
                {
                    Id = viewModel.Id, Name = viewModel.Name, Description = viewModel.Description
                })
                .MapInto<SampleDetailsViewModel, Sample>((viewModel, entity) =>
                {
                    entity.Name = viewModel.Name;
                    entity.Description = viewModel.Description;
                })), typeof(MediatorCrudTests).Assembly);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var viewModel = SampleDetailsViewModel.Create(1, "Alpha", "First");

            var added = useExtensions
                ? await mediator.Add(viewModel)
                : await mediator.Send(new AddCommand<SampleDetailsViewModel>(viewModel));
            added.Should().BeEquivalentTo(viewModel);

            var found = useExtensions
                ? await mediator.Get<SampleDetailsViewModel>(1)
                : await mediator.Send(new GetCommand<SampleDetailsViewModel>(1));
            found.Should().BeEquivalentTo(viewModel);

            var all = useExtensions
                ? await mediator.GetAll<SampleDetailsViewModel>()
                : await mediator.Send(new GetAllCommand<SampleDetailsViewModel>());
            all.Should().ContainSingle().Which.Should().BeEquivalentTo(viewModel);

            var specification = new NameStartsWithSpec("A");
            var filtered = useExtensions
                ? await mediator.GetBySpec<SampleDetailsViewModel, Sample>(specification)
                : await mediator.Send(new GetBySpecCommand<SampleDetailsViewModel, Sample>(specification));
            filtered.Should().ContainSingle().Which.Should().BeEquivalentTo(viewModel);

            var page = useExtensions
                ? await mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, 10)
                : await mediator.Send(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(specification, 10));
            page.TotalElements.Should().Be(1);
            page.Content.Should().ContainSingle().Which.Should().BeEquivalentTo(viewModel);

            var updatedViewModel = SampleDetailsViewModel.Create(1, "Beta", "Updated");
            var updated = useExtensions
                ? await mediator.Update(updatedViewModel, 1)
                : await mediator.Send(new UpdateCommand<SampleDetailsViewModel>(updatedViewModel, 1));
            updated.Should().BeEquivalentTo(updatedViewModel);
            var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            (await context.Samples.SingleAsync()).Name.Should().Be("Beta");

            var deleted = useExtensions
                ? await mediator.Delete<SampleDetailsViewModel>(1)
                : await mediator.Send(new DeleteCommand<SampleDetailsViewModel>(1));
            deleted.Should().BeTrue();
            (await context.Samples.AnyAsync()).Should().BeFalse();

            provider.GetRequiredService<List<Type>>().Should().Equal(
                typeof(AddCommand<SampleDetailsViewModel>),
                typeof(GetCommand<SampleDetailsViewModel>),
                typeof(GetAllCommand<SampleDetailsViewModel>),
                typeof(GetBySpecCommand<SampleDetailsViewModel, Sample>),
                typeof(GetPaginatedCommand<SampleDetailsViewModel, Sample>),
                typeof(UpdateCommand<SampleDetailsViewModel>),
                typeof(DeleteCommand<SampleDetailsViewModel>));
        }

        private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
        {
            public DbSet<Sample> Samples => Set<Sample>();
        }

        private sealed class RecordingBehavior<TRequest, TResponse>(List<Type> requests)
            : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
        {
            public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
                CancellationToken cancellationToken)
            {
                requests.Add(typeof(TRequest));
                return next();
            }
        }
    }
}
