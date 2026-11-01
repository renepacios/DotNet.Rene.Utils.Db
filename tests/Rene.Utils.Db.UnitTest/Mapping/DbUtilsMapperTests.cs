namespace Rene.Utils.Db.UnitTest.Mapping
{
    using global::AutoMapper;
    using global::Mapster;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Models;
    using Rene.Utils.Db.Mappers;
    using Rene.Utils.Db.Builder;
    using Rene.Utils.Db.Commands;


    public class DbUtilsMapperTests
    {
        private static IServiceProvider BuildProvider(Action<DbUtilsOptions> setup, Action<IServiceCollection> configureServices = null)
        {
            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            services.AddDbUtils<DbContext>(setup, typeof(DbUtilsMapperTests).Assembly);
            return services.BuildServiceProvider();
        }

        private static List<Sample> Samples() => new()
        {
            Sample.Create(1, "One", "First"),
            Sample.Create(2, "Two", "Second")
        };

        [Fact]
        public void Manual_Expression_Mapping_Should_Map_And_Project()
        {
            var provider = BuildProvider(o => o.UseMapper(m => m
                .Map<Sample, SampleDetailsViewModel>(s => new SampleDetailsViewModel { Id = s.Id, Name = s.Name, Description = s.Description })));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();

            mapper.Map<SampleDetailsViewModel>(Sample.Create(5, "Five", "Fifth"))
                .Should().BeEquivalentTo(new { Id = 5, Name = "Five", Description = "Fifth" });

            mapper.CanProject(typeof(Sample), typeof(SampleDetailsViewModel)).Should().BeTrue();

            mapper.ProjectTo<SampleDetailsViewModel>(Samples().AsQueryable())
                .Select(x => x.Name)
                .Should().Equal("One", "Two");
        }

        [Fact]
        public void Manual_Function_Mapping_Should_Resolve_Services_And_Not_Project()
        {
            var provider = BuildProvider(
                o => o.UseMapper(m => m
                    .Map<Sample, SampleDetailsViewModel>((sp, s) => new SampleDetailsViewModel { Id = s.Id, Name = sp.GetRequiredService<string>() })),
                services => services.AddSingleton("FromDI"));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();

            mapper.Map<SampleDetailsViewModel>(Sample.Create()).Name.Should().Be("FromDI");
            mapper.CanProject(typeof(Sample), typeof(SampleDetailsViewModel)).Should().BeFalse();
        }

        [Fact]
        public void Manual_MapInto_Should_Update_Existing_Instance()
        {
            var provider = BuildProvider(o => o.UseMapper(m => m
                .MapInto<SampleDetailsViewModel, Sample>((vm, entity) =>
                {
                    entity.Name = vm.Name;
                    entity.Description = vm.Description;
                })));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();
            var entity = Sample.Create(7, "Old", "Old");

            var result = mapper.Map(SampleDetailsViewModel.Create(0, "New", "NewDesc"), entity);

            result.Should().BeSameAs(entity);
            entity.Should().BeEquivalentTo(new { Id = 7, Name = "New", Description = "NewDesc" });
        }

        [Fact]
        public void Manual_Mapping_Not_Registered_Should_Throw()
        {
            var provider = BuildProvider(o => o.UseMapper(_ => { }));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();

            var act = () => mapper.Map<SampleDetailsViewModel>(Sample.Create());

            act.Should().Throw<InvalidOperationException>().WithMessage("*No mapping registered*");
        }

        [Fact]
        public void UseMapper_Generic_Should_Register_Custom_Implementation()
        {
            var provider = BuildProvider(o => o.UseMapper<CustomMapper>());

            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>().Should().BeOfType<CustomMapper>();
        }

        [Fact]
        public void UseAutoMapper_Should_Map_And_Project()
        {
            var provider = BuildProvider(
                o => o.UseAutoMapper(),
                services => services.AddSingleton<IMapper>(new MapperConfiguration(c => c.CreateMap<Sample, SampleDetailsViewModel>()).CreateMapper()));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();

            mapper.Should().BeOfType<AutoMapperDbUtilsMapper>();
            mapper.Map<SampleDetailsViewModel>(Sample.Create(3, "Three", "Third")).Name.Should().Be("Three");
            mapper.ProjectTo<SampleDetailsViewModel>(Samples().AsQueryable()).Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void UseMapster_Should_Map_And_Project()
        {
            var provider = BuildProvider(o => o.UseMapster(new TypeAdapterConfig()));

            using var scope = provider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>();

            mapper.Should().BeOfType<MapsterDbUtilsMapper>();
            mapper.Map<SampleDetailsViewModel>(Sample.Create(3, "Three", "Third")).Name.Should().Be("Three");
            mapper.ProjectTo<SampleDetailsViewModel>(Samples().AsQueryable()).Select(x => x.Id).Should().Equal(1, 2);

            var entity = Sample.Create(9, "Old", "Old");
            mapper.Map(SampleDetailsViewModel.Create(9, "New", "NewDesc"), entity);
            entity.Name.Should().Be("New");
        }

        [Fact]
        public async Task Handler_Should_Map_In_Memory_When_Mapper_Cannot_Project()
        {
            await using var context = new SampleDbContext(new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            context.Samples.AddRange(Samples());
            await context.SaveChangesAsync();

            var mapper = new Mock<IDbUtilsMapper>();
            mapper.Setup(m => m.CanProject(It.IsAny<Type>(), It.IsAny<Type>())).Returns(false);
            mapper.Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((object s) => SampleDetailsViewModel.Create((Sample)s));

            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, SampleDbContext, IDbUtilsUnitOfWork>(
                mapper.Object, context, null);

            var result = await handler.Handle(new GetAllCommand<SampleDetailsViewModel>(), CancellationToken.None);

            result.Select(x => x.Name).Should().BeEquivalentTo("One", "Two");
            mapper.Verify(m => m.ProjectTo<SampleDetailsViewModel>(It.IsAny<IQueryable>()), Times.Never());
            mapper.Verify(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handler_Should_Project_With_Manual_Expression_Mapping_Against_EF()
        {
            await using var context = new SampleDbContext(new DbContextOptionsBuilder<SampleDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            context.Samples.AddRange(Samples());
            await context.SaveChangesAsync();

            var provider = BuildProvider(o => o.UseMapper(m => m
                .Map<Sample, SampleDetailsViewModel>(s => new SampleDetailsViewModel { Id = s.Id, Name = s.Name, Description = s.Description })));
            using var scope = provider.CreateScope();

            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, SampleDbContext, IDbUtilsUnitOfWork>(
                scope.ServiceProvider.GetRequiredService<IDbUtilsMapper>(), context, null);

            var result = await handler.Handle(new GetAllCommand<SampleDetailsViewModel>(), CancellationToken.None);

            result.Select(x => x.Description).Should().BeEquivalentTo("First", "Second");
        }

        private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
        {
            public DbSet<Sample> Samples => Set<Sample>();
        }

        private sealed class CustomMapper : IDbUtilsMapper
        {
            public TDestination Map<TDestination>(object source) => default;

            public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) => destination;

            public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source) => Enumerable.Empty<TDestination>().AsQueryable();
        }
    }
}
