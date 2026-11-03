namespace Rene.Utils.Db.UnitTest.Logging
{
    using MediatR;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Rene.Utils.Db.Logging;
    using static LoggingTestSupport;

    public class CommandLoggingBehaviorTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Handle_OrdersStagesAndCorrelatesRuntimeMetadata_WithMonotonicElapsed(bool rollbackUtc)
        {
            var clock = new ManualClock();
            var start = clock.UtcNow;
            var stages = new List<string>();
            CommandLoggingContext before = null;
            CommandCompletedContext completed = null;
            CancellationToken beforeToken = default, completedToken = default;
            using var cts = new CancellationTokenSource();
            using var provider = Provider(o =>
            {
                o.Before = (_, context, ct) =>
                {
                    stages.Add("Before"); before = context; beforeToken = ct;
                    return Task.CompletedTask;
                };
                o.Completed = (_, context, ct) =>
                {
                    stages.Add("Completed"); completed = context; completedToken = ct;
                    return Task.CompletedTask;
                };
            }, clock);
            using var scope = provider.CreateScope();
            var expected = new Response("original response");
            var result = await Behavior<Request, Response>(scope.ServiceProvider).Handle(new DerivedRequest(), () =>
            {
                stages.Add("Handler");
                clock.Timestamp += 125;
                clock.UtcNow = start.AddSeconds(rollbackUtc ? -60 : 5);
                return Task.FromResult(expected);
            }, cts.Token);

            result.Should().BeSameAs(expected);
            stages.Should().Equal("Before", "Handler", "Completed");
            before.ExecutionId.Should().NotBe(Guid.Empty);
            before.CommandType.Should().Be(typeof(DerivedRequest));
            before.StartedAt.Should().Be(start);
            before.StartedAt.Offset.Should().Be(TimeSpan.Zero);
            completed.Execution.Should().BeSameAs(before);
            completed.ExecutionId.Should().Be(before.ExecutionId);
            completed.CommandType.Should().Be(typeof(DerivedRequest));
            completed.StartedAt.Should().Be(start);
            completed.CompletedAt.Should().Be(start.AddSeconds(rollbackUtc ? -60 : 5));
            completed.CompletedAt.Offset.Should().Be(TimeSpan.Zero);
            completed.Elapsed.Should().Be(TimeSpan.FromMilliseconds(125));
            completed.Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            completed.Exception.Should().BeNull();
            beforeToken.Should().Be(cts.Token);
            completedToken.Should().Be(CancellationToken.None);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_OptionalCallback_ExecutesOnlyConfiguredStage(bool beforeOnly)
        {
            var stages = new List<string>();
            CommandCompletedContext completed = null;
            using var provider = Provider(o =>
            {
                if (beforeOnly) o.Before = (_, _, _) => { stages.Add("Before"); return Task.CompletedTask; };
                else o.Completed = (_, context, _) =>
                {
                    stages.Add("Completed"); completed = context; return Task.CompletedTask;
                };
            }, new ManualClock());
            using var scope = provider.CreateScope();
            var response = new Response("only callback");
            var result = await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); return Task.FromResult(response);
            }, default);

            result.Should().BeSameAs(response);
            stages.Should().Equal(beforeOnly ? new[] { "Before", "Handler" } : new[] { "Handler", "Completed" });
            if (!beforeOnly)
            {
                completed.Elapsed.Should().Be(TimeSpan.Zero);
                completed.Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
                completed.Exception.Should().BeNull();
            }
        }

        [Fact]
        public async Task Handle_AsyncStages_AwaitsBeforeHandlerAndCompletion()
        {
            var stages = new List<string>();
            var beforeRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handlerRelease = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var completedRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var provider = Provider(o =>
            {
                o.Before = async (_, _, _) =>
                {
                    stages.Add("Before"); await beforeRelease.Task;
                };
                o.Completed = async (_, _, _) =>
                {
                    stages.Add("Completed"); completedEntered.SetResult(); await completedRelease.Task;
                };
            });
            using var scope = provider.CreateScope();
            var expected = new Response("async response");
            var pending = Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
            {
                stages.Add("Handler"); handlerEntered.SetResult(); return handlerRelease.Task;
            }, default);
            // Capture each observable before releasing its gate; assert outside the swallowed callbacks.
            var beforeStages = stages.ToArray();
            var finishedBefore = pending.IsCompleted;
            beforeRelease.SetResult();
            await Task.WhenAny(handlerEntered.Task, completedEntered.Task, pending);
            var handlerStages = stages.ToArray();
            var finishedHandler = pending.IsCompleted;
            handlerRelease.SetResult(expected);
            await Task.WhenAny(completedEntered.Task, pending);
            var completedStages = stages.ToArray();
            var finishedCompleted = pending.IsCompleted;
            completedRelease.SetResult();
            var result = await pending;

            beforeStages.Should().Equal("Before");
            handlerStages.Should().Equal("Before", "Handler");
            completedStages.Should().Equal("Before", "Handler", "Completed");
            finishedBefore.Should().BeFalse();
            finishedHandler.Should().BeFalse();
            finishedCompleted.Should().BeFalse();
            result.Should().BeSameAs(expected);
        }

        [Theory]
        [InlineData("Failure")]
        [InlineData("Cancellation")]
        [InlineData("TaskCancellation")]
        public async Task Handle_HandlerFailure_PreservesExactExceptionAndOutcome(string failureMode)
        {
            using var cts = new CancellationTokenSource();
            var canceled = failureMode != "Failure";
            Exception failure = failureMode switch
            {
                "Cancellation" => new OperationCanceledException("handler canceled", cts.Token),
                "TaskCancellation" => new TaskCanceledException("handler task canceled", null, cts.Token),
                _ => new InvalidOperationException("handler failed")
            };
            var stages = new List<string>();
            CommandCompletedContext completed = null;
            CancellationToken observed = cts.Token;
            using var provider = Provider(o =>
            {
                o.Before = (_, _, _) => { stages.Add("Before"); return Task.CompletedTask; };
                o.Completed = (_, context, ct) =>
                {
                    stages.Add("Completed"); completed = context; observed = ct; return Task.CompletedTask;
                };
            });
            using var scope = provider.CreateScope();
            var caught = await Record.ExceptionAsync(() => Behavior<Request, Response>(scope.ServiceProvider)
                .Handle(new Request(), () =>
                {
                    stages.Add("Handler");
                    if (canceled) cts.Cancel();
                    return Task.FromException<Response>(failure);
                }, cts.Token));

            caught.Should().BeSameAs(failure);
            completed.Exception.Should().BeSameAs(failure);
            completed.Outcome.Should().Be(canceled ? CommandExecutionOutcome.Canceled : CommandExecutionOutcome.Failed);
            stages.Should().Equal("Before", "Handler", "Completed");
            observed.Should().Be(CancellationToken.None);
        }

        [Fact]
        public async Task Handle_PreCanceledToken_SkipsHandlerAndCompletesCanceled()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken beforeToken = default, completedToken = cts.Token;
            CommandCompletedContext completed = null;
            var stages = new List<string>();
            using var provider = Provider(o =>
            {
                o.Before = (_, _, ct) =>
                {
                    beforeToken = ct; stages.Add("Before"); return Task.CompletedTask;
                };
                o.Completed = (_, context, ct) =>
                {
                    completed = context; completedToken = ct; stages.Add("Completed"); return Task.CompletedTask;
                };
            });
            using var scope = provider.CreateScope();
            var caught = await Record.ExceptionAsync(() => Behavior<Request, Response>(scope.ServiceProvider)
                .Handle(new Request(), () =>
                {
                    stages.Add("Handler"); return Task.FromResult(new Response("must not execute"));
                }, cts.Token));

            caught.Should().BeOfType<OperationCanceledException>().Which.CancellationToken.Should().Be(cts.Token);
            completed.Exception.Should().BeSameAs(caught);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Canceled);
            stages.Should().Equal("Before", "Completed");
            beforeToken.Should().Be(cts.Token);
            completedToken.Should().Be(CancellationToken.None);
        }

        [Fact]
        public async Task Handle_BeforeCancelsAndThrows_LogsCallbackFailureAndSkipsHandler()
        {
            using var cts = new CancellationTokenSource();
            var callbackFailure = new OperationCanceledException("before canceled", cts.Token);
            var logs = new RecordingLogger();
            var stages = new List<string>();
            CommandCompletedContext completed = null;
            CancellationToken completedToken = cts.Token;
            using var provider = Provider(o =>
            {
                o.Before = (_, _, _) =>
                {
                    stages.Add("Before"); cts.Cancel(); throw callbackFailure;
                };
                o.Completed = (_, context, ct) =>
                {
                    stages.Add("Completed"); completed = context; completedToken = ct; return Task.CompletedTask;
                };
            }, logs: logs);
            using var scope = provider.CreateScope();
            var caught = await Record.ExceptionAsync(() => Behavior<Request, Response>(scope.ServiceProvider)
                .Handle(new Request(), () =>
                {
                    stages.Add("Handler"); return Task.FromResult(new Response("must not run"));
                }, cts.Token));

            stages.Should().Equal("Before", "Completed");
            caught.Should().BeOfType<OperationCanceledException>().Which.CancellationToken.Should().Be(cts.Token);
            caught.Should().NotBeSameAs(callbackFailure);
            completed.Exception.Should().BeSameAs(caught);
            completed.Outcome.Should().Be(CommandExecutionOutcome.Canceled);
            completedToken.Should().Be(CancellationToken.None);
            var log = logs.Entries.Should().ContainSingle().Subject;
            log.Level.Should().Be(LogLevel.Error);
            log.Exception.Should().BeSameAs(callbackFailure);
            log.Message.Should().Contain("Before");
        }

        public static IEnumerable<object[]> CallbackFailures()
        {
            foreach (var before in new[] { true, false })
            foreach (var mode in new[] { "Sync", "Async", "Canceled", "NullTask" })
            foreach (var outcome in new[] { "Success", "Failure", "Cancellation" })
                yield return new object[] { before, mode, outcome };
        }

        [Theory]
        [MemberData(nameof(CallbackFailures))]
        public async Task Handle_CallbackFailure_IsErrorLoggedWithoutReplacingHandlerResult(
            bool beforeFailure, string mode, string handlerOutcome)
        {
            var callbackFailure = mode == "Canceled" ? (Exception)new OperationCanceledException("callback canceled")
                : new InvalidOperationException("callback failed");
            var handlerFailure = handlerOutcome == "Cancellation" ? (Exception)new OperationCanceledException("original cancellation")
                : new ApplicationException("original handler failure");
            var stages = new List<string>();
            var logs = new RecordingLogger();
            CommandCompletedContext completed = null;
            Task Fail()
            {
                if (mode == "NullTask") return null;
                if (mode == "Sync") throw callbackFailure;
                return Task.FromException(callbackFailure);
            }
            using var provider = Provider(o =>
            {
                o.Before = (_, _, _) =>
                {
                    stages.Add("Before");
                    return beforeFailure ? Fail() : Task.CompletedTask;
                };
                o.Completed = (_, context, _) =>
                {
                    completed = context; stages.Add("Completed");
                    return beforeFailure ? Task.CompletedTask : Fail();
                };
            }, logs: logs);
            using var scope = provider.CreateScope();
            var expected = new Response("successful handler");
            Response response = null;
            var caught = await Record.ExceptionAsync(async () =>
                response = await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(), () =>
                {
                    stages.Add("Handler");
                    return handlerOutcome == "Success" ? Task.FromResult(expected) : Task.FromException<Response>(handlerFailure);
                }, default));

            stages.Should().Equal("Before", "Handler", "Completed");
            completed.Outcome.Should().Be(handlerOutcome switch
            {
                "Success" => CommandExecutionOutcome.Succeeded,
                "Failure" => CommandExecutionOutcome.Failed,
                _ => CommandExecutionOutcome.Canceled
            });
            if (handlerOutcome == "Success")
            {
                caught.Should().BeNull();
                response.Should().BeSameAs(expected);
                completed.Exception.Should().BeNull();
            }
            else
            {
                caught.Should().BeSameAs(handlerFailure);
                completed.Exception.Should().BeSameAs(handlerFailure);
            }
            var log = logs.Entries.Should().ContainSingle().Subject;
            log.Level.Should().Be(LogLevel.Error);
            log.Message.Should().Contain(beforeFailure ? "Before" : "Completed").And.Contain(typeof(Request).FullName);
            if (mode == "NullTask")
                log.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("null task");
            else log.Exception.Should().BeSameAs(callbackFailure);
        }

        [Fact]
        public async Task Handle_CallbacksResolveSameScopedProviderAndService_NotRootOrOtherScope()
        {
            var providers = new List<IServiceProvider>();
            var markers = new List<ScopedMarker>();
            using var provider = Provider(o =>
            {
                Task Observe(IServiceProvider services)
                {
                    providers.Add(services); markers.Add(services.GetRequiredService<ScopedMarker>());
                    return Task.CompletedTask;
                }
                o.Before = (services, _, _) => Observe(services);
                o.Completed = (services, _, _) => Observe(services);
            });
            using var first = provider.CreateScope();
            using var second = provider.CreateScope();
            foreach (var scope in new[] { first, second })
                await Behavior<Request, Response>(scope.ServiceProvider).Handle(new Request(),
                    () => Task.FromResult(new Response("scoped")), default);

            providers.Should().Equal(first.ServiceProvider, first.ServiceProvider, second.ServiceProvider, second.ServiceProvider);
            markers[0].Should().BeSameAs(first.ServiceProvider.GetRequiredService<ScopedMarker>());
            markers[1].Should().BeSameAs(markers[0]);
            markers[2].Should().BeSameAs(second.ServiceProvider.GetRequiredService<ScopedMarker>()).And.NotBeSameAs(markers[0]);
            markers[3].Should().BeSameAs(markers[2]);
        }

        [Fact]
        public async Task Handle_NestedExecutions_HaveIndependentCorrelatedContexts()
        {
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            using var provider = Provider(o =>
            {
                o.Before = (_, context, _) => { starts.Add(context); return Task.CompletedTask; };
                o.Completed = (_, context, _) => { ends.Add(context); return Task.CompletedTask; };
            });
            using var scope = provider.CreateScope();
            var behavior = Behavior<Request, Response>(scope.ServiceProvider);
            var innerFailure = new InvalidOperationException("inner");
            var outerResponse = new Response("outer");
            Exception innerCaught = null;
            var result = await behavior.Handle(new Request(), async () =>
            {
                innerCaught = await Record.ExceptionAsync(() => behavior.Handle(new DerivedRequest(),
                    () => Task.FromException<Response>(innerFailure), default));
                return outerResponse;
            }, default);

            result.Should().BeSameAs(outerResponse);
            innerCaught.Should().BeSameAs(innerFailure);
            starts.Should().HaveCount(2);
            ends.Should().HaveCount(2);
            starts.Select(c => c.ExecutionId).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
            ends[0].Execution.Should().BeSameAs(starts[1]);
            ends[0].CommandType.Should().Be(typeof(DerivedRequest));
            ends[0].Exception.Should().BeSameAs(innerFailure);
            ends[0].Outcome.Should().Be(CommandExecutionOutcome.Failed);
            ends[1].Execution.Should().BeSameAs(starts[0]);
            ends[1].Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            ends[1].Exception.Should().BeNull();
        }

        [Fact]
        public async Task Handle_OverlappingExecutions_DoNotShareExecutionState()
        {
            var starts = new List<CommandLoggingContext>();
            var ends = new List<CommandCompletedContext>();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var provider = Provider(o =>
            {
                o.Before = (_, context, _) => { starts.Add(context); return Task.CompletedTask; };
                o.Completed = (_, context, _) => { ends.Add(context); return Task.CompletedTask; };
            });
            using var scope = provider.CreateScope();
            var behavior = Behavior<Request, Response>(scope.ServiceProvider);
            var firstResponse = new Response("first");
            var secondFailure = new ApplicationException("overlapping failure");
            var firstTask = behavior.Handle(new Request(), async () =>
            {
                entered.SetResult(); await release.Task; return firstResponse;
            }, default);
            await entered.Task;
            Exception secondCaught;
            try
            {
                secondCaught = await Record.ExceptionAsync(() => behavior.Handle(new DerivedRequest(),
                    () => Task.FromException<Response>(secondFailure), default));
            }
            finally { release.SetResult(); }
            var firstResponseObserved = await firstTask;

            firstResponseObserved.Should().BeSameAs(firstResponse);
            secondCaught.Should().BeSameAs(secondFailure);
            starts.Should().HaveCount(2);
            ends.Should().HaveCount(2);
            starts.Select(c => c.ExecutionId).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
            ends[0].Execution.Should().BeSameAs(starts[1]);
            ends[0].CommandType.Should().Be(typeof(DerivedRequest));
            ends[0].Outcome.Should().Be(CommandExecutionOutcome.Failed);
            ends[0].Exception.Should().BeSameAs(secondFailure);
            ends[1].Execution.Should().BeSameAs(starts[0]);
            ends[1].Outcome.Should().Be(CommandExecutionOutcome.Succeeded);
            ends[1].Exception.Should().BeNull();
        }
    }
}
