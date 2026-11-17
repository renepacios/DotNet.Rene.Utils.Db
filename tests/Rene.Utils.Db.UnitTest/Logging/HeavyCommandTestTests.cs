namespace Rene.Utils.Db.UnitTest.Logging
{
    using System.Threading.Channels;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.Logging;

    public class HeavyCommandTestTests
    {
        [Theory]
        [InlineData(0L)]
        [InlineData(1500L)]
        [InlineData(2147483647L)]
        [InlineData(2147483648L)]
        public async Task Send_Delay_AwaitsRequestedMillisecondsAndLogsElapsed(long milliseconds)
        {
            var clock = new TimerClock();
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(clock, starts, ends);
            using var scope = provider.CreateScope();
            var pending = scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(milliseconds));

            var remaining = milliseconds;
            while (remaining > 0)
            {
                pending.IsCompleted.Should().BeFalse();
                ends.Should().BeEmpty();
                var timer = await clock.Timers.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                var expected = Math.Min(remaining, int.MaxValue);
                timer.DueTime.Should().Be(TimeSpan.FromMilliseconds(expected));
                clock.Milliseconds += expected;
                timer.Callback(timer.State);
                remaining -= expected;
            }
            await pending;

            var completed = ends.Should().ContainSingle().Subject;
            completed.Execution.Should().BeSameAs(starts.Should().ContainSingle().Subject);
            completed.CommandType.Should().Be(typeof(HeavyCommandTest));
            completed.Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            completed.Exception.Should().BeNull();
            completed.Elapsed.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
            clock.Timers.Reader.TryRead(out _).Should().BeFalse();
        }

        [Fact]
        public async Task Send_Delegate_AwaitsWorkAndForwardsCancellationToken()
        {
            var clock = new TimerClock();
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(clock, starts, ends);
            using var scope = provider.CreateScope();
            using var cts = new CancellationTokenSource();
            var release = new TaskCompletionSource();
            CancellationToken observed = default;
            var calls = 0;
            var pending = scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(token =>
                {
                    observed = token;
                    calls++;
                    return release.Task;
                }), cts.Token);
            var completedBeforeRelease = pending.IsCompleted;
            var endsBeforeRelease = ends.Count;
            clock.Milliseconds = 1750;
            release.SetResult();
            await pending;

            completedBeforeRelease.Should().BeFalse();
            endsBeforeRelease.Should().Be(0);
            calls.Should().Be(1);
            observed.Should().Be(cts.Token);
            var completed = ends.Should().ContainSingle().Subject;
            completed.Execution.Should().BeSameAs(starts.Should().ContainSingle().Subject);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            completed.Elapsed.Should().Be(TimeSpan.FromMilliseconds(1750));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Send_DelegateFailure_PreservesExceptionAndLogsOutcome(bool canceled)
        {
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(new TimerClock(), new(), ends);
            using var scope = provider.CreateScope();
            Exception failure = canceled ? new OperationCanceledException("work canceled")
                : new InvalidOperationException("work failed");
            var caught = await Record.ExceptionAsync(() => scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(_ => Task.FromException(failure))));

            caught.Should().BeSameAs(failure);
            var completed = ends.Should().ContainSingle().Subject;
            completed.Exception.Should().BeSameAs(failure);
            completed.Outcome.Should().Be(canceled ? CommandExecutionOutcome.Canceled : CommandExecutionOutcome.Failed);
        }

        [Fact]
        public async Task Send_LongMaxDelay_CanBeCanceledWithoutOverflow()
        {
            var clock = new TimerClock();
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(clock, new(), ends);
            using var scope = provider.CreateScope();
            using var cts = new CancellationTokenSource();
            var pending = scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(long.MaxValue), cts.Token);
            var timer = await clock.Timers.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            var finishedBeforeCancellation = pending.IsCompleted;
            cts.Cancel();
            var caught = await Record.ExceptionAsync(() => pending);

            finishedBeforeCancellation.Should().BeFalse();
            timer.DueTime.Should().Be(TimeSpan.FromMilliseconds(int.MaxValue));
            caught.Should().BeAssignableTo<OperationCanceledException>()
                .Which.CancellationToken.Should().Be(cts.Token);
            var completed = ends.Should().ContainSingle().Subject;
            completed.Outcome.Should().Be(CommandExecutionOutcome.Canceled);
            completed.Exception.Should().BeSameAs(caught);
        }

        [Fact]
        public async Task Send_NullTask_ThrowsAndLogsFailure()
        {
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(new TimerClock(), new(), ends);
            using var scope = provider.CreateScope();
            var caught = await Record.ExceptionAsync(() => scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(_ => null)));

            caught.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("null task");
            var completed = ends.Should().ContainSingle().Subject;
            completed.Exception.Should().BeSameAs(caught);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Failed);
        }

        [Fact]
        public void Constructor_InvalidArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeavyCommandTest(-1))
                .ParamName.Should().Be("milliseconds");
            Assert.Throws<ArgumentNullException>(() => new HeavyCommandTest(null))
                .ParamName.Should().Be("work");
        }

        [Fact]
        public async Task Send_PreCanceledToken_DoesNotInvokeWork()
        {
            var ends = new List<CommandCompletedContext>();
            using var provider = CreateProvider(new TimerClock(), new(), ends);
            using var scope = provider.CreateScope();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var calls = 0;
            var caught = await Record.ExceptionAsync(() => scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new HeavyCommandTest(_ =>
                {
                    calls++;
                    return Task.CompletedTask;
                }), cts.Token));

            calls.Should().Be(0);
            caught.Should().BeAssignableTo<OperationCanceledException>()
                .Which.CancellationToken.Should().Be(cts.Token);
            ends.Should().ContainSingle().Which.Outcome.Should().Be(CommandExecutionOutcome.Canceled);
        }

        [Fact]
        public async Task Send_WithoutLogging_StillExecutesWork()
        {
            var services = new ServiceCollection();
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<HeavyCommandTestTests>());
            services.AddDbUtils<TestDbContext>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var calls = 0;

            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new HeavyCommandTest(_ =>
            {
                calls++;
                return Task.CompletedTask;
            }));

            calls.Should().Be(1);
            scope.ServiceProvider.GetServices<IPipelineBehavior<HeavyCommandTest, Unit>>().Should().BeEmpty();
        }

        private static ServiceProvider CreateProvider(TimeProvider clock, List<CommandLoggingContext> starts,
            List<CommandCompletedContext> ends)
        {
            var services = new ServiceCollection();
            services.AddSingleton(clock);
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<HeavyCommandTestTests>());
            services.AddDbUtils<TestDbContext>(o => o.UseCommandLogging(logging =>
            {
                logging.Before = (_, context, _) => { starts.Add(context); return Task.CompletedTask; };
                logging.Completed = (_, context, _) => { ends.Add(context); return Task.CompletedTask; };
            }));
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        private sealed class TestDbContext : DbContext;

        private sealed class TimerClock : TimeProvider
        {
            public long Milliseconds { get; set; }
            public Channel<(TimerCallback Callback, object State, TimeSpan DueTime)> Timers { get; }
                = Channel.CreateUnbounded<(TimerCallback, object, TimeSpan)>();
            public override long TimestampFrequency => 1000;
            public override long GetTimestamp() => Milliseconds;
            public override DateTimeOffset GetUtcNow()
                => new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(Milliseconds);

            public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = Mock.Of<ITimer>();
                Timers.Writer.TryWrite((callback, state, dueTime));
                return timer;
            }
        }
    }
}
