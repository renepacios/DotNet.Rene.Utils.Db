namespace Rene.Utils.Db.UnitTest.ExtensionHelpers
{
    using MediatR;
    using Models;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.ExtensionHelpers;

    public class IMediatorExtensionsTests
    {
        [Fact]
        public void Get_ForwardsId()
        {
            var id = Guid.NewGuid();
            AssertSend<GetCommand<SampleDetailsViewModel>, SampleDetailsViewModel>(
                (mediator, token) => mediator.Get<SampleDetailsViewModel>(id, token),
                command => Equals(command.Id, id), new SampleDetailsViewModel());
        }

        [Fact]
        public void GetAll_ForwardsCommand()
            => AssertSend<GetAllCommand<SampleDetailsViewModel>, IEnumerable<SampleDetailsViewModel>>(
                (mediator, token) => mediator.GetAll<SampleDetailsViewModel>(token),
                _ => true, new[] { new SampleDetailsViewModel() });

        [Fact]
        public void GetBySpec_ForwardsSpecification()
        {
            var specification = new DbUtilsAllSpecification<Sample>();
            AssertSend<GetBySpecCommand<SampleDetailsViewModel, Sample>, IReadOnlyList<SampleDetailsViewModel>>(
                (mediator, token) => mediator.GetBySpec<SampleDetailsViewModel, Sample>(specification, token),
                command => ReferenceEquals(command.Specification, specification), new[] { new SampleDetailsViewModel() });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void GetPaginated_WithoutSpecification_ForwardsPage(int page)
            => AssertSend<GetPaginatedCommand<SampleDetailsViewModel, Sample>, IDbUtilsPaginatedData<SampleDetailsViewModel>>(
                (mediator, token) => mediator.GetPaginated<SampleDetailsViewModel, Sample>(20, page, token),
                command => command.Specification == null && command.Size == 20 && command.PageNumber == page,
                Mock.Of<IDbUtilsPaginatedData<SampleDetailsViewModel>>());

        [Fact]
        public void GetPaginated_WithSpecification_ForwardsArguments()
        {
            var specification = new DbUtilsAllSpecification<Sample>();
            AssertSend<GetPaginatedCommand<SampleDetailsViewModel, Sample>, IDbUtilsPaginatedData<SampleDetailsViewModel>>(
                (mediator, token) => mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, 15, 2, token),
                command => ReferenceEquals(command.Specification, specification) && command.Size == 15 && command.PageNumber == 2,
                Mock.Of<IDbUtilsPaginatedData<SampleDetailsViewModel>>());
        }

        [Fact]
        public void GetPaginated_WithOptions_ForwardsArguments()
        {
            var specification = new DbUtilsAllSpecification<Sample>();
            var options = new PaginationOptions { Size = 30, Page = 4 };
            AssertSend<GetPaginatedCommand<SampleDetailsViewModel, Sample>, IDbUtilsPaginatedData<SampleDetailsViewModel>>(
                (mediator, token) => mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, options, token),
                command => ReferenceEquals(command.Specification, specification) && command.Size == 30 && command.PageNumber == 4,
                Mock.Of<IDbUtilsPaginatedData<SampleDetailsViewModel>>());
        }

        [Fact]
        public void Add_ForwardsViewModel()
        {
            var viewModel = new SampleDetailsViewModel();
            AssertSend<AddCommand<SampleDetailsViewModel>, SampleDetailsViewModel>(
                (mediator, token) => mediator.Add(viewModel, token),
                command => ReferenceEquals(command.ViewModel, viewModel), viewModel);
        }

        [Fact]
        public void Update_ForwardsViewModelAndId()
        {
            var viewModel = new SampleDetailsViewModel();
            AssertSend<UpdateCommand<SampleDetailsViewModel>, SampleDetailsViewModel>(
                (mediator, token) => mediator.Update(viewModel, 42, token),
                command => ReferenceEquals(command.ViewModel, viewModel) && Equals(command.Id, 42), viewModel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Delete_ForwardsIdAndReturnsResult(bool deleted)
            => AssertSend<DeleteCommand<SampleDetailsViewModel>, bool>(
                (mediator, token) => mediator.Delete<SampleDetailsViewModel>("key", token),
                command => Equals(command.Id, "key"), deleted);

        [Fact]
        public async Task GetAll_WithoutToken_UsesDefaultCancellationToken()
        {
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            mediator.Setup(m => m.Send(It.IsAny<IRequest<IEnumerable<SampleDetailsViewModel>>>(), CancellationToken.None))
                .ReturnsAsync(Array.Empty<SampleDetailsViewModel>());

            var result = await mediator.Object.GetAll<SampleDetailsViewModel>();

            result.Should().BeEmpty();
            mediator.VerifyAll();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetPaginated_WithoutPage_UsesPageZero(bool withSpecification)
        {
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            mediator.Setup(m => m.Send(
                    It.Is<IRequest<IDbUtilsPaginatedData<SampleDetailsViewModel>>>(request =>
                        request is GetPaginatedCommand<SampleDetailsViewModel, Sample> &&
                        ((GetPaginatedCommand<SampleDetailsViewModel, Sample>)request).PageNumber == 0 &&
                        ((GetPaginatedCommand<SampleDetailsViewModel, Sample>)request).Size == 10),
                    CancellationToken.None))
                .ReturnsAsync(Mock.Of<IDbUtilsPaginatedData<SampleDetailsViewModel>>());

            if (withSpecification)
                await mediator.Object.GetPaginated<SampleDetailsViewModel, Sample>(new DbUtilsAllSpecification<Sample>(), 10);
            else
                await mediator.Object.GetPaginated<SampleDetailsViewModel, Sample>(10);

            mediator.VerifyAll();
        }

        [Fact]
        public async Task Get_PropagatesHandlerException()
        {
            var exception = new KeyNotFoundException("Missing entity");
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<IRequest<SampleDetailsViewModel>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task> act = () => mediator.Object.Get<SampleDetailsViewModel>(1);

            (await act.Should().ThrowAsync<KeyNotFoundException>()).Which.Should().BeSameAs(exception);
        }

        [Fact]
        public async Task Add_PropagatesCancellation()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<IRequest<SampleDetailsViewModel>>(), source.Token))
                .Returns(Task.FromCanceled<SampleDetailsViewModel>(source.Token));

            Func<Task> act = () => mediator.Object.Add(new SampleDetailsViewModel(), source.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Theory]
        [InlineData("Get")]
        [InlineData("GetAll")]
        [InlineData("GetBySpec")]
        [InlineData("GetPaginated")]
        [InlineData("GetPaginatedWithSpec")]
        [InlineData("GetPaginatedWithOptions")]
        [InlineData("Add")]
        [InlineData("Update")]
        [InlineData("Delete")]
        public void NullMediator_ThrowsArgumentNullException(string operation)
        {
            IMediator mediator = null;
            var specification = new DbUtilsAllSpecification<Sample>();
            Func<Task> act = () => operation switch
            {
                "Get" => mediator.Get<SampleDetailsViewModel>(1),
                "GetAll" => mediator.GetAll<SampleDetailsViewModel>(),
                "GetBySpec" => mediator.GetBySpec<SampleDetailsViewModel, Sample>(specification),
                "GetPaginated" => mediator.GetPaginated<SampleDetailsViewModel, Sample>(10),
                "GetPaginatedWithSpec" => mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, 10),
                "GetPaginatedWithOptions" => mediator.GetPaginated<SampleDetailsViewModel, Sample>(specification, PaginationOptions.AsDefault),
                "Add" => mediator.Add(new SampleDetailsViewModel()),
                "Update" => mediator.Update(new SampleDetailsViewModel(), 1),
                "Delete" => mediator.Delete<SampleDetailsViewModel>(1),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };

            Action invoke = () => act();
            invoke.Should().Throw<ArgumentNullException>().WithParameterName("mediator");
        }

        [Fact]
        public void GetPaginated_NullOptions_ThrowsBeforeSending()
        {
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            Action act = () => mediator.Object.GetPaginated<SampleDetailsViewModel, Sample>(
                new DbUtilsAllSpecification<Sample>(), null);

            act.Should().Throw<ArgumentNullException>().WithParameterName("paginationOptions");
            mediator.VerifyNoOtherCalls();
        }

        private static void AssertSend<TCommand, TResult>(
            Func<IMediator, CancellationToken, Task<TResult>> invoke, Func<TCommand, bool> matches, TResult result)
            where TCommand : class, IRequest<TResult>
        {
            using var source = new CancellationTokenSource();
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            var expectedTask = Task.FromResult(result);
            mediator.Setup(m => m.Send(
                    It.Is<IRequest<TResult>>(request => request is TCommand && matches((TCommand)request)), source.Token))
                .Returns(expectedTask);

            var actualTask = invoke(mediator.Object, source.Token);

            actualTask.Should().BeSameAs(expectedTask);
            mediator.Verify(m => m.Send(It.IsAny<IRequest<TResult>>(), source.Token), Times.Once);
            mediator.VerifyNoOtherCalls();
        }
    }
}
