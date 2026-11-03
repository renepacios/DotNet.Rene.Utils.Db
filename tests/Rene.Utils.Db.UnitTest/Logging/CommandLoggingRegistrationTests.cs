namespace Rene.Utils.Db.UnitTest.Logging
{
    using System.Reflection;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Rene.Utils.Db.Logging;
    using static LoggingTestSupport;

    public class CommandLoggingRegistrationTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task UseCommandLogging_CallbackModes_AreFluentAndUsedByAddDbUtils(int mode)
        {
            var options = new DbUtilsOptions();
            var stages = new List<string>();
            options.UseCommandLogging(o => Configure(o, mode, stages)).Should().BeSameAs(options);
            var services = new ServiceCollection();
            services.AddDbUtils<LoggingDbContext>(o =>
                o.UseCommandLogging(logging => Configure(logging, mode, stages)), typeof(CommandLoggingRegistrationTests).Assembly);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(new Response("configured"));
            }, default);
            stages.Should().Equal(ExpectedStages(mode));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task AddDbUtilsCommandLogging_CallbackModes_ReturnServicesAndExecuteCallbacks(int mode)
        {
            var services = new ServiceCollection();
            var stages = new List<string>();
            services.AddDbUtilsCommandLogging(o => Configure(o, mode, stages)).Should().BeSameAs(services);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var response = new Response("standalone");
            var result = await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(response);
            }, default);
            stages.Should().Equal(ExpectedStages(mode));
            result.Should().BeSameAs(response);
        }

        private static void Configure(CommandLoggingOptions options, int mode, List<string> stages)
        {
            if ((mode & 1) != 0) options.Before = (_, _, _) => { stages.Add("Before"); return Task.CompletedTask; };
            if ((mode & 2) != 0) options.Completed = (_, _, _) => { stages.Add("Completed"); return Task.CompletedTask; };
        }

        private static string[] ExpectedStages(int mode) => mode switch
        {
            1 => new[] { "Before", "Handler" },
            2 => new[] { "Handler", "Completed" },
            _ => new[] { "Before", "Handler", "Completed" }
        };

        [Fact]
        public void LoggingConfiguration_InvalidArguments_AreRejectedWithParameterNames()
        {
            var options = new DbUtilsOptions();
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => options.UseCommandLogging(null)).ParamName.Should().Be("configure");
            Assert.Throws<ArgumentException>(() => options.UseCommandLogging(_ => { })).ParamName.Should().Be("configure");
            Assert.Throws<ArgumentNullException>(() =>
                CommandLoggingServiceCollectionExtensions.AddDbUtilsCommandLogging(null, _ => { })).ParamName.Should().Be("services");
            Assert.Throws<ArgumentNullException>(() => services.AddDbUtilsCommandLogging(null)).ParamName.Should().Be("configure");
            Assert.Throws<ArgumentException>(() => services.AddDbUtilsCommandLogging(_ => { })).ParamName.Should().Be("configure");
            services.Should().BeEmpty();
        }

        [Fact]
        public void Contexts_AreImmutableAndRejectMissingCorrelationInputs()
        {
            var id = Guid.Parse("6f569148-552a-4276-bf28-056b35b98c62");
            var started = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
            var failure = new ApplicationException("recorded failure");
            var execution = new CommandLoggingContext(id, typeof(DerivedRequest), started);
            var completed = new CommandCompletedContext(execution, started.AddSeconds(2),
                TimeSpan.FromMilliseconds(50), CommandExecutionOutcome.Failed, failure);

            typeof(CommandLoggingContext).GetProperties().Should().OnlyContain(p => p.SetMethod == null);
            typeof(CommandCompletedContext).GetProperties().Should().OnlyContain(p => p.SetMethod == null);
            completed.Execution.Should().BeSameAs(execution);
            completed.ExecutionId.Should().Be(id);
            completed.CommandType.Should().Be(typeof(DerivedRequest));
            completed.StartedAt.Should().Be(started);
            completed.CompletedAt.Should().Be(started.AddSeconds(2));
            completed.Elapsed.Should().Be(TimeSpan.FromMilliseconds(50));
            completed.Outcome.Should().Be(CommandExecutionOutcome.Failed);
            completed.Exception.Should().BeSameAs(failure);
            Assert.Throws<ArgumentNullException>(() => new CommandLoggingContext(id, null, started))
                .ParamName.Should().Be("commandType");
            Assert.Throws<ArgumentNullException>(() => new CommandCompletedContext(null, started, default,
                CommandExecutionOutcome.Succeeded, null)).ParamName.Should().Be("execution");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Registration_RetainedConfigurationMutation_DoesNotChangeSnapshot(bool viaOptions)
        {
            CommandLoggingOptions retained = null;
            DbUtilsOptions retainedOptions = null;
            var stages = new List<string>();
            var services = new ServiceCollection();
            void Setup(CommandLoggingOptions o)
            {
                retained = o;
                Configure(o, 3, stages);
            }
            if (viaOptions)
                services.AddDbUtils<LoggingDbContext>(o =>
                {
                    retainedOptions = o;
                    o.UseCommandLogging(Setup);
                }, typeof(CommandLoggingRegistrationTests).Assembly);
            else services.AddDbUtilsCommandLogging(Setup);
            retained.Before = (_, _, _) => { stages.Add("MutatedBefore"); return Task.CompletedTask; };
            retained.Completed = null;
            retainedOptions?.UseCommandLogging(o =>
                o.Completed = (_, _, _) => { stages.Add("MutatedCompleted"); return Task.CompletedTask; });
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(new Response("snapshot"));
            }, default);
            stages.Should().Equal("Before", "Handler", "Completed");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task Registration_DuplicateOptIn_HasOneBehaviorAndLastCallbacksWin(int lastMode)
        {
            var stages = new List<string>();
            var services = new ServiceCollection();
            services.AddDbUtilsCommandLogging(o =>
            {
                o.Before = (_, _, _) => { stages.Add("OldBefore"); return Task.CompletedTask; };
                o.Completed = (_, _, _) => { stages.Add("OldCompleted"); return Task.CompletedTask; };
            });
            services.AddDbUtils<LoggingDbContext>(o => o.UseCommandLogging(logging => Configure(logging, lastMode, stages)),
                typeof(CommandLoggingRegistrationTests).Assembly);
            services.AddDbUtilsCommandLogging(o => Configure(o, lastMode, stages));
            services.Count(d => d.ServiceType == typeof(CommandLoggingOptions)).Should().Be(1);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetServices<IPipelineBehavior<Request, Response>>().Should().ContainSingle();
            await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(new Response("last"));
            }, default);
            stages.Should().Equal(ExpectedStages(lastMode));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task UseCommandLogging_RepeatedConfiguration_LastCallbacksWin(int lastMode)
        {
            var stages = new List<string>();
            var services = new ServiceCollection();
            services.AddDbUtils<LoggingDbContext>(options => options
                .UseCommandLogging(o =>
                {
                    o.Before = (_, _, _) => { stages.Add("OldBefore"); return Task.CompletedTask; };
                    o.Completed = (_, _, _) => { stages.Add("OldCompleted"); return Task.CompletedTask; };
                })
                .UseCommandLogging(o => Configure(o, lastMode, stages)),
                typeof(CommandLoggingRegistrationTests).Assembly);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(new Response("last options"));
            }, default);
            stages.Should().Equal(ExpectedStages(lastMode));
        }

        [Fact]
        public void Registration_TimeProvider_RespectsExistingRegistrationAndDefaultsToSystem()
        {
            var clock = new ManualClock();
            using var existing = Provider(o => o.Before = (_, _, _) => Task.CompletedTask, clock);
            existing.GetRequiredService<TimeProvider>().Should().BeSameAs(clock);
            using var fallback = Provider(o => o.Completed = (_, _, _) => Task.CompletedTask);
            fallback.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        }

        public static IEnumerable<object[]> Routes()
        {
            for (var route = 0; route < 8; route++)
            foreach (var enabled in new[] { false, true })
            for (var assembliesMode = 0; assembliesMode < 3; assembliesMode++)
                yield return new object[] { route, enabled, assembliesMode };
        }

        [Theory]
        [MemberData(nameof(Routes))]
        public async Task AddDbUtils_AllServiceAndHostOverloads_RespectOptIn(int route, bool enabled, int assembliesMode)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            IServiceCollection services = route is 2 or 3 or 6 or 7 ? builder.Services : new ServiceCollection();
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CommandLoggingRegistrationTests>());
            var stages = new List<string>();
            Action<DbUtilsOptions> setup = options =>
            {
                if (enabled) options.UseCommandLogging(o => Configure(o, 3, stages));
            };
            Assembly[] assemblies = assembliesMode switch
            {
                0 => new[] { typeof(CommandLoggingRegistrationTests).Assembly },
                1 => Array.Empty<Assembly>(),
                _ => null
            };
            // Routes without setup cannot opt in themselves; standalone registration must survive them.
            if (enabled && route >= 4) services.AddDbUtilsCommandLogging(o => Configure(o, 3, stages));
            object returned = route switch
            {
                0 => services.AddDbUtils<LoggingDbContext>(setup, assemblies),
                1 => services.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(setup, assemblies),
                2 => builder.AddDbUtils<LoggingDbContext>(setup, assemblies),
                3 => builder.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(setup, assemblies),
                4 => services.AddDbUtils<LoggingDbContext>(assemblies),
                5 => services.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(assemblies),
                6 => builder.AddDbUtils<LoggingDbContext>(assemblies),
                _ => builder.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(assemblies)
            };
            returned.Should().BeSameAs(route is 2 or 3 or 6 or 7 ? builder : services);
            var expected = new Response("mediator result");
            var handler = new Mock<IRequestHandler<Request, Response>>();
            handler.Setup(h => h.Handle(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
                .Returns(() => { stages.Add("Handler"); return Task.FromResult(expected); });
            services.AddTransient(_ => handler.Object);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Request());

            result.Should().BeSameAs(expected);
            stages.Should().Equal(enabled ? ExpectedStages(3) : new[] { "Handler" });
            scope.ServiceProvider.GetServices<IPipelineBehavior<Request, Response>>().Count().Should().Be(enabled ? 1 : 0);
            handler.Verify(h => h.Handle(It.IsAny<Request>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void AddDbUtils_ConfiguredRoutes_RejectNullAndEmptyLogging(int route)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            var services = new ServiceCollection();
            void Register(Action<DbUtilsOptions> setup)
            {
                switch (route)
                {
                    case 0: services.AddDbUtils<LoggingDbContext>(setup); break;
                    case 1: services.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(setup); break;
                    case 2: builder.AddDbUtils<LoggingDbContext>(setup); break;
                    default: builder.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(setup); break;
                }
            }
            Assert.Throws<ArgumentNullException>(() => Register(null)).ParamName.Should().Be("setup");
            Assert.Throws<ArgumentNullException>(() => Register(o => o.UseCommandLogging(null)))
                .ParamName.Should().Be("configure");
            Assert.Throws<ArgumentException>(() => Register(o => o.UseCommandLogging(_ => { })))
                .ParamName.Should().Be("configure");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void AddDbUtils_AllRoutes_RejectNullRegistrationTarget(int route)
        {
            IServiceCollection services = null;
            IHostApplicationBuilder builder = null;
            var caught = Assert.Throws<ArgumentNullException>(() =>
            {
                switch (route)
                {
                    case 0: services.AddDbUtils<LoggingDbContext>(_ => { }); break;
                    case 1: services.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(_ => { }); break;
                    case 2: builder.AddDbUtils<LoggingDbContext>(_ => { }); break;
                    case 3: builder.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(_ => { }); break;
                    case 4: services.AddDbUtils<LoggingDbContext>(); break;
                    case 5: services.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(); break;
                    case 6: builder.AddDbUtils<LoggingDbContext>(); break;
                    default: builder.AddDbUtils<LoggingDbContext, LoggingUnitOfWork>(); break;
                }
            });
            caught.ParamName.Should().Be(route is 2 or 3 or 6 or 7 ? "builder" : "services");
        }

        [Fact]
        public async Task Mediator_VoidRequest_IsLoggedWithOriginalCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            var stages = new List<string>();
            CommandLoggingContext before = null;
            CommandCompletedContext completed = null;
            CancellationToken observedBefore = default, observedHandler = default;
            var services = new ServiceCollection();
            services.AddDbUtilsCommandLogging(o =>
            {
                o.Before = (_, c, ct) =>
                {
                    before = c; observedBefore = ct; stages.Add("Before"); return Task.CompletedTask;
                };
                o.Completed = (_, c, _) =>
                {
                    completed = c; stages.Add("Completed"); return Task.CompletedTask;
                };
            });
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CommandLoggingRegistrationTests>());
            var handler = new Mock<IRequestHandler<VoidRequest>>();
            handler.Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()))
                .Returns((VoidRequest _, CancellationToken ct) =>
                {
                    observedHandler = ct; stages.Add("Handler"); return Task.CompletedTask;
                });
            services.AddTransient(_ => handler.Object);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new VoidRequest(), cts.Token);

            stages.Should().Equal("Before", "Handler", "Completed");
            before.CommandType.Should().Be(typeof(VoidRequest));
            completed.Execution.Should().BeSameAs(before);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            completed.Exception.Should().BeNull();
            observedBefore.Should().Be(cts.Token);
            observedHandler.Should().Be(cts.Token);
        }

        public sealed class VoidRequest : IRequest;
        internal sealed class LoggingDbContext(DbContextOptions<LoggingDbContext> options) : DbContext(options);
        internal sealed class LoggingUnitOfWork : IDbUtilsUnitOfWork
        {
            public int SaveChanges() => 0;
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
                => Task.FromResult(0);
        }
    }
}
