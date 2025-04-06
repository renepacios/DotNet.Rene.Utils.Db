namespace Rene.Utils.Db.UnitTest
{
    using Helpers;
    using Microsoft.EntityFrameworkCore;
    using Rene.Utils.Db.Builder;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.UnitTest.Models;
    using Rene.Utils.Db.UnitTest.Setup.Scenarios;

    public class GenericAllTest
    {

        [Fact]
        public async Task GetAll_With_Uow_Command_Should_Work_As_Expected()
        {

            // 1) We reuse the scenario from the fixture
            var scenario = MockScenarioFactory.CreateSampleScenario(howMany: 10);

            // 2) Create the handler
            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.UowMock.Object
            );

            // 3) Input command
            var getCommand = new GetAllCommand<SampleDetailsViewModel>();

            // 4) Execute
            var result = await handler.Handle(getCommand, CancellationToken.None);

            scenario.MapperMock.VerifyProjectedToViewModel(result);

            // 5) Assert
            result
                .Should()
                .AllBeOfType<SampleDetailsViewModel>();

            result
                .Count()
                .Should()
                .Be(scenario.Data.Count);

        }
    }

}