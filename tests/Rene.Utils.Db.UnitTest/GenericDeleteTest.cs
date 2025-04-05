namespace Rene.Utils.Db.UnitTest
{
    using Builder;
    using Commands;
    using Helpers;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Moq;
    using Setup.Scenarios;

    public class GenericDeleteTest
    {
        [Fact]
        public async Task Delete_With_FakeUow_Command_Work_As_Expected()
        {
            // 1) Create a new scenario with some sample data
            var scenario = MockScenarioFactory.CreateSampleScenario(howMany: 5);

            // 2) Build the DeleteCommand and handler
            var entityId = 2;
            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);

            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.FakeUowMock.Object // pass our fake UoW
            );

            // 3) Execute
            var result = await handler.Handle(command, CancellationToken.None);

            // 4) Verify
            scenario.DbSetMock.VerifyRemoveCalledWithId(entityId);
            scenario.DbContextMock.VerifySaveChangesAsyncCalled();
            scenario.FakeUowMock.VerifySaveChangesAsyncCalled();

            // 5) Assert the result
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_With_Uow_Command_Work_As_Expected()
        {
            // 1) Create a new scenario with some sample data
            var scenario = MockScenarioFactory.CreateSampleScenario(howMany: 5);

            // 2) Build the DeleteCommand and handler
            var entityId = 2;
            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);

            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.UowMock.Object // pass our fake UoW
            );

            // 3) Execute
            var result = await handler.Handle(command, CancellationToken.None);

            // 4) Verify
            scenario.DbSetMock.VerifyRemoveCalledWithId(entityId);
            scenario.DbContextMock.VerifySaveChangesAsyncCalled();
            scenario.UowMock.VerifySaveChangesAsyncCalled();

            // 5) Assert the result
            result.Should().BeTrue();
        }


        [Fact]
        public async Task Delete_With_NonExistent_Entry_Command_Work_As_Expected()
        {
            // 1) Create a new scenario with some sample data
            var scenario = MockScenarioFactory.CreateSampleScenario(howMany: 5);

            // 2) Build the DeleteCommand and handler
            var entityId = 6;
            var command = new DeleteCommand<SampleDetailsViewModel>(entityId);

            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.UowMock.Object // pass our fake UoW
            );

            // 3) Execute
            var action = async () => await handler.Handle(command, new CancellationToken());

            // 4) Assert the result
            await action.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Entidad no encontrada {entityId}");

            // 5) Verify
            scenario.DbSetMock.VerifyRemoveCalledWithId(entityId, Times.Never());
            scenario.DbContextMock.VerifySaveChangesAsyncCalled(Times.Never());
            scenario.UowMock.VerifySaveChangesAsyncCalled(Times.Never());
        }

        //TODO:
        /*
         * - Test validation commands
         * - Test events
         *
         */
    }

}