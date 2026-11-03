namespace Rene.Utils.Db.UnitTest.Authorization;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rene.Utils.Db.Authorization;
using Rene.Utils.Db.Commands;
using Rene.Utils.Db.ExtensionHelpers;
using Rene.Utils.Db.UnitTest.Models;
using System.Reflection;

public sealed class AuthorizationRegistrationTests
{
    [Fact]
    public void AuthorizationAliases_AreScannedThroughInheritedCommandAuthorization()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthorizationTestTypes.AuthorizationProbe>();
        services.AddDbUtilsAuthorization(typeof(AuthorizationTestTypes).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sample = new SampleDetailsViewModel();

        var cases = new (Type ServiceType, Func<object, IEnumerable<IAuthorizationRequirement>> GetRequirements, string Name)[]
        {
            (typeof(ICommandAuthorization<AddCommand<SampleDetailsViewModel>>),
                instance => ((IAuthorizationAdd<SampleDetailsViewModel>)instance)
                    .GetRequirements(new AddCommand<SampleDetailsViewModel>(sample)), "alias-add"),
            (typeof(ICommandAuthorization<UpdateCommand<SampleDetailsViewModel>>),
                instance => ((IAuthorizationUpdate<SampleDetailsViewModel>)instance)
                    .GetRequirements(new UpdateCommand<SampleDetailsViewModel>(sample, 7)), "alias-update"),
            (typeof(ICommandAuthorization<DeleteCommand<SampleDetailsViewModel>>),
                instance => ((IAuthorizationDelete<SampleDetailsViewModel>)instance)
                    .GetRequirements(new DeleteCommand<SampleDetailsViewModel>(7)), "alias-delete"),
            (typeof(ICommandAuthorization<GetCommand<SampleDetailsViewModel>>),
                instance => ((IAuthorizationGet<SampleDetailsViewModel>)instance)
                    .GetRequirements(new GetCommand<SampleDetailsViewModel>(7)), "alias-get"),
            (typeof(ICommandAuthorization<GetAllCommand<SampleDetailsViewModel>>),
                instance => ((IAuthorizationGetAll<SampleDetailsViewModel>)instance)
                    .GetRequirements(new GetAllCommand<SampleDetailsViewModel>()), "alias-get-all"),
            (typeof(ICommandAuthorization<GetBySpecCommand<SampleDetailsViewModel, Sample>>),
                instance => ((IAuthorizationGetBySpec<SampleDetailsViewModel, Sample>)instance)
                    .GetRequirements(new GetBySpecCommand<SampleDetailsViewModel, Sample>(null)), "alias-get-by-spec"),
            (typeof(ICommandAuthorization<GetPaginatedCommand<SampleDetailsViewModel, Sample>>),
                instance => ((IAuthorizationGetPaginated<SampleDetailsViewModel, Sample>)instance)
                    .GetRequirements(new GetPaginatedCommand<SampleDetailsViewModel, Sample>(10)), "alias-get-paginated")
        };

        foreach (var testCase in cases)
        {
            var descriptor = services.Single(service =>
                service.ServiceType == testCase.ServiceType &&
                service.ImplementationType == typeof(AuthorizationTestTypes.AllOperationsAuthorization));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

            var definition = scope.ServiceProvider.GetServices(testCase.ServiceType)
                .Single(instance => instance.GetType() == typeof(AuthorizationTestTypes.AllOperationsAuthorization));
            definition.Should().BeOfType<AuthorizationTestTypes.AllOperationsAuthorization>();
            var requirements = testCase.GetRequirements(definition).ToArray();
            requirements.Should().ContainSingle();
            requirements[0].Should().BeOfType<AuthorizationTestTypes.AuthorizationRequirement>();
            ((AuthorizationTestTypes.AuthorizationRequirement)requirements[0]).Name.Should().Be(testCase.Name);
        }
    }

    [Fact]
    public void AuthorizationRegistration_RepeatedCallsAndDuplicateAssemblies_AreIdempotent()
    {
        var assembly = typeof(AuthorizationTestTypes).Assembly;
        var services = new ServiceCollection();

        services.AddDbUtilsAuthorization(assembly, assembly).Should().BeSameAs(services);
        services.AddDbUtilsAuthorization(assembly).Should().BeSameAs(services);

        services.Count(service =>
            service.ServiceType == typeof(IPipelineBehavior<,>) &&
            service.ImplementationType?.IsGenericType == true &&
            service.ImplementationType.GetGenericTypeDefinition().Name == "CommandAuthorizationBehavior`2")
            .Should().Be(1);
        services.Count(service =>
            service.ServiceType == typeof(ICommandAuthorization<AddCommand<SampleDetailsViewModel>>) &&
            service.ImplementationType == typeof(AuthorizationTestTypes.AllOperationsAuthorization))
            .Should().Be(1);
        services.Count(service =>
            service.ServiceType == typeof(IRequirementHandler<AuthorizationTestTypes.AuthorizationRequirement>) &&
            service.ImplementationType == typeof(AuthorizationTestTypes.AuthorizationRequirementHandler))
            .Should().Be(1);

        // Two distinct definitions for this request are intentional and must both survive TryAddEnumerable.
        services.Count(service =>
            service.ServiceType == typeof(ICommandAuthorization<AuthorizationTestTypes.MultipleRequirementsCommand>))
            .Should().Be(2);
    }

    [Fact]
    public void AuthorizationRegistration_RejectsInvalidServiceAndAssemblyArguments()
    {
        var assembly = typeof(AuthorizationTestTypes).Assembly;
        var services = new ServiceCollection();
        IServiceCollection nullServiceCollection = null;

        Action nullServices = () => nullServiceCollection.AddDbUtilsAuthorization(assembly);
        Action nullAssemblies = () => services.AddDbUtilsAuthorization((Assembly[])null);
        Action noAssemblies = () => services.AddDbUtilsAuthorization();
        Action nullAssembly = () => services.AddDbUtilsAuthorization((Assembly)null);
        Action nullManualHandlerServices = () =>
            nullServiceCollection.AddAuthorizationRequirementHandler<
                AuthorizationTestTypes.ClosedRequirement<AuthorizationTestTypes.ManualTag>,
                AuthorizationTestTypes.ClosedRequirementHandler<AuthorizationTestTypes.ManualTag>>();

        nullServices.Should().Throw<ArgumentNullException>().WithParameterName("services");
        nullAssemblies.Should().Throw<ArgumentNullException>().WithParameterName("assemblies");
        noAssemblies.Should().Throw<ArgumentException>().WithParameterName("assemblies");
        nullAssembly.Should().Throw<ArgumentException>().WithParameterName("assemblies");
        nullManualHandlerServices.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AuthorizationServices_AreScopedPerRequestScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthorizationTestTypes.AuthorizationProbe>();
        services.AddDbUtilsAuthorization(typeof(AuthorizationTestTypes).Assembly);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstDefinition = firstScope.ServiceProvider
            .GetRequiredService<ICommandAuthorization<AuthorizationTestTypes.ProtectedCommand>>();
        var firstHandler = firstScope.ServiceProvider
            .GetRequiredService<IRequirementHandler<AuthorizationTestTypes.AuthorizationRequirement>>();
        var secondDefinition = secondScope.ServiceProvider
            .GetRequiredService<ICommandAuthorization<AuthorizationTestTypes.ProtectedCommand>>();
        var secondHandler = secondScope.ServiceProvider
            .GetRequiredService<IRequirementHandler<AuthorizationTestTypes.AuthorizationRequirement>>();

        firstScope.ServiceProvider
            .GetRequiredService<ICommandAuthorization<AuthorizationTestTypes.ProtectedCommand>>()
            .Should().BeSameAs(firstDefinition);
        firstScope.ServiceProvider
            .GetRequiredService<IRequirementHandler<AuthorizationTestTypes.AuthorizationRequirement>>()
            .Should().BeSameAs(firstHandler);
        secondDefinition.Should().NotBeSameAs(firstDefinition);
        secondHandler.Should().NotBeSameAs(firstHandler);
    }

    [Fact]
    public async Task AuthorizationRegistration_FindsConcreteCustomCommandDefinitions()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new AuthorizationTestTypes.ProtectedCommand());

        result.Should().Be(42);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .EvaluatedRequirements.Should().Equal("protected-command");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().Equal(nameof(AuthorizationTestTypes.ProtectedCommand));
    }

    [Fact]
    public async Task RequirementHandler_IsReusableAcrossDifferentCommands()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        (await mediator.Send(new AuthorizationTestTypes.ProtectedCommand())).Should().Be(42);
        (await mediator.Send(new AuthorizationTestTypes.ReusableRequirementCommand())).Should().Be(43);

        var probe = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>();
        probe.EvaluatedRequirements.Should().Equal("protected-command", "reusable-command");
        probe.ExecutedCommands.Should().Equal(
            nameof(AuthorizationTestTypes.ProtectedCommand),
            nameof(AuthorizationTestTypes.ReusableRequirementCommand));
    }

    [Fact]
    public async Task ClosedGenericRequirementHandler_RequiresAndSupportsManualRegistration()
    {
        var services = AuthorizationTestServices.CreateMediatorServices();
        var closedRequirement = typeof(AuthorizationTestTypes.ClosedRequirement<AuthorizationTestTypes.ManualTag>);
        var closedHandler = typeof(IRequirementHandler<>).MakeGenericType(closedRequirement);
        services.Any(service => service.ServiceType == closedHandler).Should().BeFalse();

        services.AddAuthorizationRequirementHandler<
            AuthorizationTestTypes.ClosedRequirement<AuthorizationTestTypes.ManualTag>,
            AuthorizationTestTypes.ClosedRequirementHandler<AuthorizationTestTypes.ManualTag>>()
            .Should().BeSameAs(services);
        services.AddAuthorizationRequirementHandler<
            AuthorizationTestTypes.ClosedRequirement<AuthorizationTestTypes.ManualTag>,
            AuthorizationTestTypes.ClosedRequirementHandler<AuthorizationTestTypes.ManualTag>>();
        services.Count(service =>
            service.ServiceType == closedHandler &&
            service.ImplementationType == typeof(AuthorizationTestTypes.ClosedRequirementHandler<AuthorizationTestTypes.ManualTag>))
            .Should().Be(1);
        services.Single(service => service.ServiceType == closedHandler)
            .Lifetime.Should().Be(ServiceLifetime.Scoped);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.ManualClosedHandlerCommand());

        result.Should().Be(31);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be(nameof(AuthorizationTestTypes.ManualClosedHandlerCommand));
    }

    [Fact]
    public async Task MultipleDefinitionsAndRequirements_AllMustAllow()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.MultipleRequirementsCommand());

        result.Should().Be(44);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .EvaluatedRequirements.Should().BeEquivalentTo(new[]
            {
                "first-definition-a", "first-definition-b", "second-definition-a", "second-definition-b"
            });
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be(nameof(AuthorizationTestTypes.MultipleRequirementsCommand));
    }

    [Fact]
    public async Task MultipleDefinitions_AnyDeniedRequirementBlocksTheCommand()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider(probe =>
            probe.DeniedRequirement = "second-definition-b");
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.MultipleRequirementsCommand());

        var exception = await act.Should().ThrowAsync<CommandAuthorizationException>();

        exception.Which.RequirementType.Should().Be(typeof(AuthorizationTestTypes.AuthorizationRequirement));
        exception.Which.FailureReason.Should().Be("denied-by-test-policy");
        var probe = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>();
        probe.EvaluatedRequirements.Should().Contain("second-definition-b");
        probe.ExecutedCommands.Should().BeEmpty();
    }
}

public sealed class CommandAuthorizationBehaviorTests
{
    [Fact]
    public async Task RequirementEvaluation_IsSequentialAndStopsAtFirstDenial()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider(probe =>
            probe.DeniedRequirement = "denied-in-sequence");
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await GetAuthorizationException(() =>
            mediator.Send(new AuthorizationTestTypes.SequentialRequirementsCommand()));

        exception.CommandType.Should().Be(typeof(AuthorizationTestTypes.SequentialRequirementsCommand));
        exception.RequirementType.Should().Be(typeof(AuthorizationTestTypes.AuthorizationRequirement));
        exception.FailureReason.Should().Be("denied-by-test-policy");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .EvaluatedRequirements.Should().Equal("first-in-sequence", "denied-in-sequence");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task DeniedAddViaSendAndConvenienceExtension_DoesNotPersist()
    {
        using var provider = AuthorizationTestServices.CreateDatabaseMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationDbContext>();
        var viewModel = SampleDetailsViewModel.Create(13, "Denied", "Must not be stored");

        var sendException = await GetAuthorizationException(() =>
            mediator.Send(new AddCommand<SampleDetailsViewModel>(viewModel)));
        var extensionException = await GetAuthorizationException(() => mediator.Add(viewModel));

        sendException.CommandType.Should().Be(typeof(AddCommand<SampleDetailsViewModel>));
        extensionException.CommandType.Should().Be(typeof(AddCommand<SampleDetailsViewModel>));
        sendException.RequirementType.Should().Be(typeof(AuthorizationTestTypes.PersistenceRequirement));
        extensionException.RequirementType.Should().Be(typeof(AuthorizationTestTypes.PersistenceRequirement));
        sendException.FailureReason.Should().Be("write-policy-denied");
        extensionException.FailureReason.Should().Be("write-policy-denied");
        (await context.Samples.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeniedGetViaSendAndConvenienceExtension_UsesAuthorization()
    {
        using var provider = AuthorizationTestServices.CreateDatabaseMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationDbContext>();
        context.Samples.Add(Sample.Create(9, "Protected", "Read must be denied"));
        await context.SaveChangesAsync();

        var sendException = await GetAuthorizationException(() =>
            mediator.Send(new GetCommand<SampleDetailsViewModel>(9)));
        var extensionException = await GetAuthorizationException(() => mediator.Get<SampleDetailsViewModel>(9));

        sendException.CommandType.Should().Be(typeof(GetCommand<SampleDetailsViewModel>));
        extensionException.CommandType.Should().Be(typeof(GetCommand<SampleDetailsViewModel>));
        sendException.RequirementType.Should().Be(typeof(AuthorizationTestTypes.GetDenialRequirement));
        extensionException.RequirementType.Should().Be(typeof(AuthorizationTestTypes.GetDenialRequirement));
        sendException.FailureReason.Should().Be("read-policy-denied");
        extensionException.FailureReason.Should().Be("read-policy-denied");
        (await context.Samples.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CommandsWithoutDefinitions_RemainAllowedWithAndWithoutOptIn()
    {
        using var providerWithoutFeature = AuthorizationTestServices.CreateMediatorProvider(
            registerAuthorization: false);
        using var providerWithFeature = AuthorizationTestServices.CreateMediatorProvider();
        using var scopeWithoutFeature = providerWithoutFeature.CreateScope();
        using var scopeWithFeature = providerWithFeature.CreateScope();

        var unregisteredResult = await scopeWithoutFeature.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.UnprotectedCommand());
        var optedInResult = await scopeWithFeature.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.UnprotectedCommand());

        unregisteredResult.Should().Be(73);
        optedInResult.Should().Be(73);
        scopeWithoutFeature.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be(nameof(AuthorizationTestTypes.UnprotectedCommand));
        scopeWithFeature.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be(nameof(AuthorizationTestTypes.UnprotectedCommand));
        scopeWithFeature.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .EvaluatedRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistingGenericCrudCommandsWithoutDefinitions_AreUnchangedWithAndWithoutOptIn()
    {
        foreach (var registerAuthorization in new[] { false, true })
        {
            using var provider = AuthorizationTestServices.CreateDatabaseMediatorProvider(registerAuthorization);
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var viewModel = new AuthorizationTestTypes.UnprotectedViewModel
            {
                Id = 22,
                Name = "Unprotected",
                Description = "Existing generic CRUD path"
            };

            var added = await mediator.Add(viewModel);
            var found = await mediator.Get<AuthorizationTestTypes.UnprotectedViewModel>(22);
            var persisted = await scope.ServiceProvider
                .GetRequiredService<AuthorizationTestTypes.AuthorizationDbContext>().Samples.SingleAsync();

            added.Should().BeEquivalentTo(viewModel);
            found.Should().BeEquivalentTo(viewModel);
            persisted.Should().BeEquivalentTo(new Sample
            {
                Id = 22,
                Name = "Unprotected",
                Description = "Existing generic CRUD path"
            });
        }
    }

    [Fact]
    public async Task AuthorizationRequiresExactlyOneRequirementHandler_WhenNoneIsRegistered()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.MissingHandlerCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("exactly one registered handler; found 0");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationRequiresExactlyOneRequirementHandler_WhenTwoAreRegistered()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.DuplicateHandlerCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("exactly one registered handler; found 2");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationRejectsNullHandlerResult()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.NullResultCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("returned a null result");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationRejectsNullRequirementCollection()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.NullCollectionCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("returned a null requirement collection");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationRejectsNullRequirement()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.NullItemCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("returned a null requirement");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationRejectsEmptyRequirementCollection()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.EmptyCollectionCommand());

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Message.Should().Contain("returned no requirements");
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task RequirementHandlerException_IsPropagatedUnchanged()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.ThrowingHandlerCommand());

        var exception = await act.Should().ThrowAsync<AuthorizationTestTypes.RequirementFailureException>();

        exception.Which.Should().BeSameAs(AuthorizationTestTypes.RequirementFailureException.Instance);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task RequirementHandlerReceivesCancellationToken()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();

        var result = await mediator.Send(new AuthorizationTestTypes.CancellationCommand(), cancellation.Token);

        result.Should().Be(52);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .LastCancellationToken.Should().Be(cancellation.Token);
        scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>()
            .ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be(nameof(AuthorizationTestTypes.CancellationCommand));
    }

    [Fact]
    public async Task CancellationAfterRequirementEvaluation_PreventsCommandHandler()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>();
        using var cancellation = new CancellationTokenSource();
        probe.CancelAfterRequirement = cancellation;

        var act = () => mediator.Send(new AuthorizationTestTypes.CancellationCommand(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        probe.LastCancellationToken.Should().Be(cancellation.Token);
        probe.ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task CancellationBetweenRequirements_PreventsLaterRequirementAndCommandHandler()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var probe = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>();
        using var cancellation = new CancellationTokenSource();
        probe.CancelAfterRequirement = cancellation;

        var act = () => mediator.Send(
            new AuthorizationTestTypes.CancellationBetweenRequirementsCommand(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        probe.LastCancellationToken.Should().Be(cancellation.Token);
        probe.EvaluatedRequirements.Should().Equal("cancel-after-first");
        probe.ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task CanceledRequest_DoesNotReachRequirementOrCommandHandler()
    {
        using var provider = AuthorizationTestServices.CreateMediatorProvider();
        using var scope = provider.CreateScope();
        var probe = scope.ServiceProvider.GetRequiredService<AuthorizationTestTypes.AuthorizationProbe>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new AuthorizationTestTypes.CancellationCommand(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        probe.EvaluatedRequirements.Should().BeEmpty();
        probe.ExecutedCommands.Should().BeEmpty();
    }

    private static async Task<CommandAuthorizationException> GetAuthorizationException(Func<Task> action)
        => (await action.Should().ThrowAsync<CommandAuthorizationException>()).Which;
}

public sealed class AuthorizationAbstractionTests
{
    [Fact]
    public void AuthorizationResultAllow_ReportsSuccessWithoutFailureReason()
    {
        var result = AuthorizationResult.Allow();

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public void AuthorizationResultDeny_PreservesReasonAndRejectsWhitespace()
    {
        var result = AuthorizationResult.Deny("policy rejected");

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("policy rejected");
        foreach (var reason in new[] { null, "", "   ", "\t" })
        {
            Action act = () => AuthorizationResult.Deny(reason);
            act.Should().Throw<ArgumentException>().WithParameterName("reason");
        }
    }

    [Fact]
    public void CommandAuthorizationException_ExposesMetadataWithoutLeakingReason()
    {
        const string failureReason = "private authorization detail";
        var exception = new CommandAuthorizationException(
            typeof(AuthorizationTestTypes.ProtectedCommand),
            typeof(AuthorizationTestTypes.AuthorizationRequirement),
            failureReason);

        exception.CommandType.Should().Be(typeof(AuthorizationTestTypes.ProtectedCommand));
        exception.RequirementType.Should().Be(typeof(AuthorizationTestTypes.AuthorizationRequirement));
        exception.FailureReason.Should().Be(failureReason);
        exception.Message.Should().Contain(nameof(AuthorizationTestTypes.ProtectedCommand));
        exception.Message.Should().NotContain(failureReason);
    }

    [Fact]
    public void CommandAuthorizationException_RejectsNullMetadata()
    {
        Action nullCommand = () => new CommandAuthorizationException(
            null, typeof(AuthorizationTestTypes.AuthorizationRequirement), "denied");
        Action nullRequirement = () => new CommandAuthorizationException(
            typeof(AuthorizationTestTypes.ProtectedCommand), null, "denied");
        Action nullReason = () => new CommandAuthorizationException(
            typeof(AuthorizationTestTypes.ProtectedCommand),
            typeof(AuthorizationTestTypes.AuthorizationRequirement), null);

        nullCommand.Should().Throw<ArgumentNullException>().WithParameterName("commandType");
        nullRequirement.Should().Throw<ArgumentNullException>().WithParameterName("requirementType");
        nullReason.Should().Throw<ArgumentNullException>().WithParameterName("failureReason");
    }
}

public static class AuthorizationTestServices
{
    public static ServiceCollection CreateMediatorServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<AuthorizationTestTypes.AuthorizationProbe>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(AuthorizationTestTypes).Assembly));
        services.AddDbUtilsAuthorization(typeof(AuthorizationTestTypes).Assembly);
        return services;
    }

    public static ServiceProvider CreateMediatorProvider(
        Action<AuthorizationTestTypes.AuthorizationProbe> configureProbe = null,
        bool registerAuthorization = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<AuthorizationTestTypes.AuthorizationProbe>(_ =>
        {
            var probe = new AuthorizationTestTypes.AuthorizationProbe();
            configureProbe?.Invoke(probe);
            return probe;
        });
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(AuthorizationTestTypes).Assembly));
        if (registerAuthorization)
            services.AddDbUtilsAuthorization(typeof(AuthorizationTestTypes).Assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static ServiceProvider CreateDatabaseMediatorProvider(bool registerAuthorization = true)
    {
        var services = new ServiceCollection();
        var assembly = typeof(AuthorizationTestTypes).Assembly;
        services.AddLogging();
        services.AddSingleton<AuthorizationTestTypes.AuthorizationProbe>();
        services.AddDbContext<AuthorizationTestTypes.AuthorizationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddDbUtils<AuthorizationTestTypes.AuthorizationDbContext>(
            options => options.UseMapper(mapper => mapper
                .Map<Sample, SampleDetailsViewModel>(entity => new SampleDetailsViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description
                })
                .Map<SampleDetailsViewModel, Sample>(viewModel => new Sample
                {
                    Id = viewModel.Id,
                    Name = viewModel.Name,
                    Description = viewModel.Description
                })
                .MapInto<SampleDetailsViewModel, Sample>((viewModel, entity) =>
                {
                    entity.Name = viewModel.Name;
                    entity.Description = viewModel.Description;
                })
                .Map<Sample, AuthorizationTestTypes.UnprotectedViewModel>(entity =>
                    new AuthorizationTestTypes.UnprotectedViewModel
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Description = entity.Description
                    })
                .Map<AuthorizationTestTypes.UnprotectedViewModel, Sample>(viewModel =>
                    new Sample
                    {
                        Id = viewModel.Id,
                        Name = viewModel.Name,
                        Description = viewModel.Description
                    })
                .MapInto<AuthorizationTestTypes.UnprotectedViewModel, Sample>((viewModel, entity) =>
                {
                    entity.Name = viewModel.Name;
                    entity.Description = viewModel.Description;
                })), assembly);
        if (registerAuthorization)
            services.AddDbUtilsAuthorization(assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}

public static class AuthorizationTestTypes
{
    public sealed class AuthorizationProbe
    {
        public List<string> EvaluatedRequirements { get; } = [];
        public List<string> ExecutedCommands { get; } = [];
        public string DeniedRequirement { get; set; }
        public CancellationToken LastCancellationToken { get; set; }
        public CancellationTokenSource CancelAfterRequirement { get; set; }
    }

    public sealed class AuthorizationRequirement(string name) : IAuthorizationRequirement
    {
        public string Name { get; } = name;
    }

    public sealed class AuthorizationRequirementHandler(AuthorizationProbe probe)
        : IRequirementHandler<AuthorizationRequirement>
    {
        public Task<AuthorizationResult> Handle(
            AuthorizationRequirement requirement, CancellationToken cancellationToken)
        {
            probe.EvaluatedRequirements.Add(requirement.Name);
            probe.LastCancellationToken = cancellationToken;
            probe.CancelAfterRequirement?.Cancel();
            return Task.FromResult(requirement.Name == probe.DeniedRequirement
                ? AuthorizationResult.Deny("denied-by-test-policy")
                : AuthorizationResult.Allow());
        }
    }

    public sealed class AllOperationsAuthorization :
        IAuthorizationAdd<SampleDetailsViewModel>,
        IAuthorizationUpdate<SampleDetailsViewModel>,
        IAuthorizationDelete<SampleDetailsViewModel>,
        IAuthorizationGet<SampleDetailsViewModel>,
        IAuthorizationGetAll<SampleDetailsViewModel>,
        IAuthorizationGetBySpec<SampleDetailsViewModel, Sample>,
        IAuthorizationGetPaginated<SampleDetailsViewModel, Sample>
    {
        private static IEnumerable<IAuthorizationRequirement> For(string name)
            => [new AuthorizationRequirement(name)];

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<AddCommand<SampleDetailsViewModel>>.GetRequirements(
                AddCommand<SampleDetailsViewModel> command) => For("alias-add");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<UpdateCommand<SampleDetailsViewModel>>.GetRequirements(
                UpdateCommand<SampleDetailsViewModel> command) => For("alias-update");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<DeleteCommand<SampleDetailsViewModel>>.GetRequirements(
                DeleteCommand<SampleDetailsViewModel> command) => For("alias-delete");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<GetCommand<SampleDetailsViewModel>>.GetRequirements(
                GetCommand<SampleDetailsViewModel> command) => For("alias-get");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<GetAllCommand<SampleDetailsViewModel>>.GetRequirements(
                GetAllCommand<SampleDetailsViewModel> command) => For("alias-get-all");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<GetBySpecCommand<SampleDetailsViewModel, Sample>>.GetRequirements(
                GetBySpecCommand<SampleDetailsViewModel, Sample> command) => For("alias-get-by-spec");

        IEnumerable<IAuthorizationRequirement>
            ICommandAuthorization<GetPaginatedCommand<SampleDetailsViewModel, Sample>>.GetRequirements(
                GetPaginatedCommand<SampleDetailsViewModel, Sample> command) => For("alias-get-paginated");
    }

    public sealed class ProtectedCommand : IRequest<int> { }
    public sealed class ReusableRequirementCommand : IRequest<int> { }
    public sealed class MultipleRequirementsCommand : IRequest<int> { }
    public sealed class SequentialRequirementsCommand : IRequest<int> { }
    public sealed class ManualClosedHandlerCommand : IRequest<int> { }
    public sealed class MissingHandlerCommand : IRequest<int> { }
    public sealed class DuplicateHandlerCommand : IRequest<int> { }
    public sealed class NullResultCommand : IRequest<int> { }
    public sealed class NullCollectionCommand : IRequest<int> { }
    public sealed class NullItemCommand : IRequest<int> { }
    public sealed class EmptyCollectionCommand : IRequest<int> { }
    public sealed class ThrowingHandlerCommand : IRequest<int> { }
    public sealed class CancellationCommand : IRequest<int> { }
    public sealed class CancellationBetweenRequirementsCommand : IRequest<int> { }
    public sealed class UnprotectedCommand : IRequest<int> { }

    public sealed class UnprotectedViewModel : Rene.Utils.Db.IWithGenericHandler<Sample>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class TestCommandHandler(AuthorizationProbe probe) :
        IRequestHandler<ProtectedCommand, int>,
        IRequestHandler<ReusableRequirementCommand, int>,
        IRequestHandler<MultipleRequirementsCommand, int>,
        IRequestHandler<SequentialRequirementsCommand, int>,
        IRequestHandler<ManualClosedHandlerCommand, int>,
        IRequestHandler<MissingHandlerCommand, int>,
        IRequestHandler<DuplicateHandlerCommand, int>,
        IRequestHandler<NullResultCommand, int>,
        IRequestHandler<NullCollectionCommand, int>,
        IRequestHandler<NullItemCommand, int>,
        IRequestHandler<EmptyCollectionCommand, int>,
        IRequestHandler<ThrowingHandlerCommand, int>,
        IRequestHandler<CancellationCommand, int>,
        IRequestHandler<CancellationBetweenRequirementsCommand, int>,
        IRequestHandler<UnprotectedCommand, int>
    {
        private Task<int> Complete<TCommand>(int result)
        {
            probe.ExecutedCommands.Add(typeof(TCommand).Name);
            return Task.FromResult(result);
        }

        Task<int> IRequestHandler<ProtectedCommand, int>.Handle(ProtectedCommand request, CancellationToken token)
            => Complete<ProtectedCommand>(42);

        Task<int> IRequestHandler<ReusableRequirementCommand, int>.Handle(
            ReusableRequirementCommand request, CancellationToken token) => Complete<ReusableRequirementCommand>(43);

        Task<int> IRequestHandler<MultipleRequirementsCommand, int>.Handle(
            MultipleRequirementsCommand request, CancellationToken token) => Complete<MultipleRequirementsCommand>(44);

        Task<int> IRequestHandler<SequentialRequirementsCommand, int>.Handle(
            SequentialRequirementsCommand request, CancellationToken token) => Complete<SequentialRequirementsCommand>(45);

        Task<int> IRequestHandler<ManualClosedHandlerCommand, int>.Handle(
            ManualClosedHandlerCommand request, CancellationToken token) => Complete<ManualClosedHandlerCommand>(31);

        Task<int> IRequestHandler<MissingHandlerCommand, int>.Handle(
            MissingHandlerCommand request, CancellationToken token) => Complete<MissingHandlerCommand>(46);

        Task<int> IRequestHandler<DuplicateHandlerCommand, int>.Handle(
            DuplicateHandlerCommand request, CancellationToken token) => Complete<DuplicateHandlerCommand>(47);

        Task<int> IRequestHandler<NullResultCommand, int>.Handle(
            NullResultCommand request, CancellationToken token) => Complete<NullResultCommand>(48);

        Task<int> IRequestHandler<NullCollectionCommand, int>.Handle(
            NullCollectionCommand request, CancellationToken token) => Complete<NullCollectionCommand>(49);

        Task<int> IRequestHandler<NullItemCommand, int>.Handle(
            NullItemCommand request, CancellationToken token) => Complete<NullItemCommand>(50);

        Task<int> IRequestHandler<EmptyCollectionCommand, int>.Handle(
            EmptyCollectionCommand request, CancellationToken token) => Complete<EmptyCollectionCommand>(51);

        Task<int> IRequestHandler<ThrowingHandlerCommand, int>.Handle(
            ThrowingHandlerCommand request, CancellationToken token) => Complete<ThrowingHandlerCommand>(53);

        Task<int> IRequestHandler<CancellationCommand, int>.Handle(
            CancellationCommand request, CancellationToken token) => Complete<CancellationCommand>(52);

        Task<int> IRequestHandler<CancellationBetweenRequirementsCommand, int>.Handle(
            CancellationBetweenRequirementsCommand request, CancellationToken token)
            => Complete<CancellationBetweenRequirementsCommand>(54);

        Task<int> IRequestHandler<UnprotectedCommand, int>.Handle(
            UnprotectedCommand request, CancellationToken token) => Complete<UnprotectedCommand>(73);
    }

    public abstract class SingleRequirementAuthorization<TCommand>(string requirementName)
        : ICommandAuthorization<TCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(TCommand command)
            => [new AuthorizationRequirement(requirementName)];
    }

    public sealed class ProtectedCommandAuthorization()
        : SingleRequirementAuthorization<ProtectedCommand>("protected-command") { }

    public sealed class ReusableCommandAuthorization()
        : SingleRequirementAuthorization<ReusableRequirementCommand>("reusable-command") { }

    public sealed class MultipleRequirementsFirstAuthorization()
        : ICommandAuthorization<MultipleRequirementsCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(MultipleRequirementsCommand command)
            => [new AuthorizationRequirement("first-definition-a"), new AuthorizationRequirement("first-definition-b")];
    }

    public sealed class MultipleRequirementsSecondAuthorization()
        : ICommandAuthorization<MultipleRequirementsCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(MultipleRequirementsCommand command)
            => [new AuthorizationRequirement("second-definition-a"), new AuthorizationRequirement("second-definition-b")];
    }

    public sealed class SequentialRequirementsAuthorization()
        : ICommandAuthorization<SequentialRequirementsCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(SequentialRequirementsCommand command)
            =>
            [
                new AuthorizationRequirement("first-in-sequence"),
                new AuthorizationRequirement("denied-in-sequence"),
                new AuthorizationRequirement("never-evaluated")
            ];
    }

    public sealed class ClosedRequirement<TTag> : IAuthorizationRequirement { }
    public sealed class ManualTag { }

    public sealed class ManualRequirementAuthorization()
        : ICommandAuthorization<ManualClosedHandlerCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(ManualClosedHandlerCommand command)
            => [new ClosedRequirement<ManualTag>()];
    }

    public sealed class ClosedRequirementHandler<TTag>
        : IRequirementHandler<ClosedRequirement<TTag>>
    {
        public Task<AuthorizationResult> Handle(
            ClosedRequirement<TTag> requirement, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationResult.Allow());
    }

    public sealed class MissingHandlerRequirement : IAuthorizationRequirement { }

    public sealed class MissingHandlerCommandAuthorization()
        : ICommandAuthorization<MissingHandlerCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(MissingHandlerCommand command)
            => [new MissingHandlerRequirement()];
    }

    public sealed class DuplicateRequirement : IAuthorizationRequirement { }

    public sealed class DuplicateHandlerAuthorization()
        : ICommandAuthorization<DuplicateHandlerCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(DuplicateHandlerCommand command)
            => [new DuplicateRequirement()];
    }

    public sealed class FirstDuplicateRequirementHandler : IRequirementHandler<DuplicateRequirement>
    {
        public Task<AuthorizationResult> Handle(DuplicateRequirement requirement, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationResult.Allow());
    }

    public sealed class SecondDuplicateRequirementHandler : IRequirementHandler<DuplicateRequirement>
    {
        public Task<AuthorizationResult> Handle(DuplicateRequirement requirement, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationResult.Allow());
    }

    public sealed class NullResultRequirement : IAuthorizationRequirement { }

    public sealed class NullResultAuthorization()
        : ICommandAuthorization<NullResultCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(NullResultCommand command)
            => [new NullResultRequirement()];
    }

    public sealed class NullResultRequirementHandler : IRequirementHandler<NullResultRequirement>
    {
        public Task<AuthorizationResult> Handle(NullResultRequirement requirement, CancellationToken cancellationToken)
            => Task.FromResult<AuthorizationResult>(null);
    }

    public sealed class NullCollectionAuthorization() : ICommandAuthorization<NullCollectionCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(NullCollectionCommand command) => null;
    }

    public sealed class NullItemAuthorization() : ICommandAuthorization<NullItemCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(NullItemCommand command) => [null];
    }

    public sealed class EmptyCollectionAuthorization() : ICommandAuthorization<EmptyCollectionCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(EmptyCollectionCommand command)
            => Array.Empty<IAuthorizationRequirement>();
    }

    public sealed class ThrowingRequirement : IAuthorizationRequirement { }

    public sealed class ThrowingRequirementAuthorization()
        : ICommandAuthorization<ThrowingHandlerCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(ThrowingHandlerCommand command)
            => [new ThrowingRequirement()];
    }

    public sealed class RequirementFailureException : Exception
    {
        public static RequirementFailureException Instance { get; } = new("requirement handler failed");

        private RequirementFailureException(string message) : base(message) { }
    }

    public sealed class ThrowingRequirementHandler : IRequirementHandler<ThrowingRequirement>
    {
        public Task<AuthorizationResult> Handle(ThrowingRequirement requirement, CancellationToken cancellationToken)
            => Task.FromException<AuthorizationResult>(RequirementFailureException.Instance);
    }

    public sealed class CancellationRequirement : IAuthorizationRequirement { }

    public sealed class CancellationRequirementAuthorization()
        : ICommandAuthorization<CancellationCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(CancellationCommand command)
            => [new CancellationRequirement()];
    }

    public sealed class CancellationBetweenRequirementsAuthorization()
        : ICommandAuthorization<CancellationBetweenRequirementsCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(CancellationBetweenRequirementsCommand command)
            =>
            [
                new AuthorizationRequirement("cancel-after-first"),
                new AuthorizationRequirement("must-not-run-after-cancel")
            ];
    }

    public sealed class CancellationRequirementHandler(AuthorizationProbe probe)
        : IRequirementHandler<CancellationRequirement>
    {
        public Task<AuthorizationResult> Handle(
            CancellationRequirement requirement, CancellationToken cancellationToken)
        {
            probe.LastCancellationToken = cancellationToken;
            probe.CancelAfterRequirement?.Cancel();
            return Task.FromResult(AuthorizationResult.Allow());
        }
    }

    public sealed class PersistenceRequirement : IAuthorizationRequirement { }

    public sealed class DeniedAddAuthorization() : IAuthorizationAdd<SampleDetailsViewModel>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(AddCommand<SampleDetailsViewModel> command)
            => [new PersistenceRequirement()];
    }

    public sealed class PersistenceRequirementHandler : IRequirementHandler<PersistenceRequirement>
    {
        public Task<AuthorizationResult> Handle(
            PersistenceRequirement requirement, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationResult.Deny("write-policy-denied"));
    }

    public sealed class GetDenialRequirement : IAuthorizationRequirement { }

    public sealed class DeniedGetAuthorization() : IAuthorizationGet<SampleDetailsViewModel>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(GetCommand<SampleDetailsViewModel> command)
            => [new GetDenialRequirement()];
    }

    public sealed class GetDenialRequirementHandler : IRequirementHandler<GetDenialRequirement>
    {
        public Task<AuthorizationResult> Handle(
            GetDenialRequirement requirement, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizationResult.Deny("read-policy-denied"));
    }

    public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
    {
        public DbSet<Sample> Samples => Set<Sample>();
    }
}
