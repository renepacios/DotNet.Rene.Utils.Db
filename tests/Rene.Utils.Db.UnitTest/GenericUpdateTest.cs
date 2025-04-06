namespace Rene.Utils.Db.UnitTest
{
    using Builder;
    using Commands;
    using FluentAssertions;
    using Helpers;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Setup.Scenarios;

    public class GenericUpdateTest
    {
        [Fact]
        public async Task Update_With_FakeUow_Command_Work_As_Expected()
        {
            // 1) We reuse the scenario from the fixture
            var scenario = MockScenarioFactory.CreateSampleScenario(howMany: 10, strategy: UowStrategy.Fake);

            var entityId = 2;
            var newName = "New Name";
            var newDescription = "New Description";

            // 2) Create the handler
            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.UowMock.Object
            );

            // 3) Input command
            var vm = SampleDetailsViewModel
                .DefaultSampleDetails
                .WithDescription(newDescription)
                .WithName(newName)
                .WithId(entityId);

            var updateCommand = new UpdateCommand<SampleDetailsViewModel>(vm, entityId);

            // 4) Execute
            var result = await handler.Handle(updateCommand, CancellationToken.None);

            scenario.MapperMock.VerifyMappedToEntity(vm, entityId);

            scenario.DbSetMock.VerifyUpdateCalledWithId(vm, entityId);

            scenario.UowMock.VerifySaveChangesAsyncCalled();

            scenario.MapperMock.VerifyMappedBackToViewModel(vm);

            // 5) Assert
            result.Should().BeOfType<SampleDetailsViewModel>();

            result.Id.Should().Be(entityId);
            result.Name.Should().Be(newName);
            result.Description.Should().Be(newDescription);
        }


        //TODO:
        /*
         * - Test validation commands
         * - Test events
         *
         */
    }

}