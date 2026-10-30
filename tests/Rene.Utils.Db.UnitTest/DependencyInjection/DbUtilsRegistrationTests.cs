namespace Rene.Utils.Db.UnitTest.DependencyInjection
{
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Models;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.DbInternal;

    public class DbUtilsRegistrationTests
    {
        private static readonly Type GetAllHandlerType = typeof(IRequestHandler<GetAllCommand<SampleViewModel>, IEnumerable<SampleViewModel>>);

        private static Type HandlerUnitOfWorkType(IServiceCollection services)
            => services.First(d => d.ServiceType == GetAllHandlerType).ImplementationType!.GetGenericArguments()[3];

        private static ServiceCollection ServicesWithContext()
        {
            var services = new ServiceCollection();
            services.AddDbContext<RegistrationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            return services;
        }

        [Fact]
        public void HostBuilder_Without_Setup_Should_Register_Resolvable_Handlers_And_Fallback_UnitOfWork()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDbContext<RegistrationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            builder.Services.AddScoped(_ => Mock.Of<IDbUtilsMapper>());

            builder.AddDbUtils<RegistrationDbContext>(typeof(DbUtilsRegistrationTests).Assembly);

            HandlerUnitOfWorkType(builder.Services).Should().Be(typeof(FakeUnitOfWork<RegistrationDbContext>));

            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetService(GetAllHandlerType).Should().NotBeNull();
            scope.ServiceProvider.GetService<IDbUtilsUnitOfWork>().Should().BeOfType<FakeUnitOfWork<RegistrationDbContext>>();
        }

        [Fact]
        public void HostBuilder_And_ServiceCollection_Should_Register_The_Same_Services()
        {
            var builder = Host.CreateApplicationBuilder();
            var before = builder.Services.Count;
            builder.AddDbUtils<RegistrationDbContext>(o => o.UseMapper(_ => { }), typeof(DbUtilsRegistrationTests).Assembly);
            var fromBuilder = builder.Services.Skip(before).Select(d => (d.ServiceType, d.ImplementationType, d.Lifetime)).ToList();

            var services = new ServiceCollection();
            services.AddDbUtils<RegistrationDbContext>(o => o.UseMapper(_ => { }), typeof(DbUtilsRegistrationTests).Assembly);
            var fromServices = services.Select(d => (d.ServiceType, d.ImplementationType, d.Lifetime)).ToList();

            fromBuilder.Should().BeEquivalentTo(fromServices);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UseOwnUnitOfWork_Should_Not_Be_Overwritten_By_Fallback(bool useHostBuilder)
        {
            IServiceCollection services;
            if (useHostBuilder)
            {
                var builder = Host.CreateApplicationBuilder();
                builder.AddDbUtils<RegistrationDbContext>(o => o.UseOwnUnitOfWork<CustomUnitOfWork>(), typeof(DbUtilsRegistrationTests).Assembly);
                services = builder.Services;
            }
            else
            {
                services = ServicesWithContext();
                services.AddDbUtils<RegistrationDbContext>(o => o.UseOwnUnitOfWork<CustomUnitOfWork>(), typeof(DbUtilsRegistrationTests).Assembly);
            }

            HandlerUnitOfWorkType(services).Should().Be(typeof(CustomUnitOfWork));
            services.Should().NotContain(d => d.ServiceType == typeof(FakeUnitOfWork<RegistrationDbContext>));
        }

        [Fact]
        public void Generic_UnitOfWork_Overload_Should_Use_The_Given_UnitOfWork()
        {
            var services = ServicesWithContext();
            services.AddScoped<CustomUnitOfWork>();
            services.AddScoped(_ => Mock.Of<IDbUtilsMapper>());

            services.AddDbUtils<RegistrationDbContext, CustomUnitOfWork>(typeof(DbUtilsRegistrationTests).Assembly);

            HandlerUnitOfWorkType(services).Should().Be(typeof(CustomUnitOfWork));
            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetService(GetAllHandlerType).Should().NotBeNull();
        }

        [Fact]
        public void Fallback_Should_Not_Replace_A_Registered_UnitOfWork()
        {
            var services = ServicesWithContext();
            services.AddScoped<IDbUtilsUnitOfWork, CustomUnitOfWork>();

            services.AddDbUtils<RegistrationDbContext>(typeof(DbUtilsRegistrationTests).Assembly);

            services.Single(d => d.ServiceType == typeof(IDbUtilsUnitOfWork)).ImplementationType.Should().Be(typeof(CustomUnitOfWork));
        }

        public class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
        {
            public DbSet<Sample> Samples { get; set; }
        }

        public class CustomUnitOfWork : IDbUtilsUnitOfWork
        {
            public int SaveChanges() => 0;
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => Task.FromResult(0);
        }
    }
}
