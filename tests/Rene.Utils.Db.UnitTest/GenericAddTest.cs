namespace Rene.Utils.Db.UnitTest
{
    using Builder;
    using Commands;
    using FluentAssertions;
    using Helpers;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Moq;
    using Setup.Scenarios;

    public class GenericAddTest
    {
        [Fact]
        public async Task Add_Command_ShouldCallUnitOfWork_AndInsertEntity()
        {
            // 1) We reuse the scenario from the fixture
            var scenario = MockScenarioFactory.CreateSampleScenario(10);

            // 2) Create the handler
            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                scenario.UowMock.Object
            );

            // 3) Input command
            var vm = SampleDetailsViewModel.DefaultSampleDetails;
            var addCommand = new AddCommand<SampleDetailsViewModel>(vm);

            // 4) Execute
            var result = await handler.Handle(addCommand, CancellationToken.None);

            // 5) Verify using helper methods
            scenario.MapperMock.VerifyMappedToEntity(vm);
            scenario.DbSetMock.VerifyAddAsyncCalledWith(vm);

            scenario.DbContextMock.VerifySaveChangesAsyncCalled();
            scenario.UowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            scenario.MapperMock.VerifyMappedBackToViewModel(vm);

            // 6) Assert 
            result.Should().BeOfType<SampleDetailsViewModel>();
            result.Id.Should().Be(99);
            result.Name.Should().Be("NoBodySample");
            result.Description.Should().Be("This is a fake description");
        }

        [Fact]
        public async Task Add_Command_WithoutUow_WorksAsExpected()
        {
            // 1) Reuse the same scenario
            var scenario = MockScenarioFactory.CreateSampleScenario(10);

            // 2) Handler without UoW
            var handler = new GenericCommandHandler<SampleDetailsViewModel, Sample, DbContext, IDbUtilsUnitOfWork>(
                scenario.MapperMock.Object,
                scenario.DbContextMock.Object,
                null // no UoW
            );

            var vm = SampleDetailsViewModel.DefaultSampleDetails;
            var addCommand = new AddCommand<SampleDetailsViewModel>(vm);

            // 3) Execute
            var result = await handler.Handle(addCommand, CancellationToken.None);

            // 4) Verifications
            scenario.MapperMock.VerifyMappedToEntity(vm);
            scenario.DbSetMock.VerifyAddAsyncCalledWith(vm);
            scenario.DbContextMock.VerifySaveChangesAsyncCalled();
            scenario.MapperMock.VerifyMappedBackToViewModel(vm);

            // 5) Check result
            result.Should().BeOfType<SampleDetailsViewModel>();
            result.Id.Should().Be(99);
            result.Name.Should().Be("NoBodySample");
            result.Description.Should().Be("This is a fake description");
        }

        //TODO:
        /*
         * - Test validation commands
         * - Test events
         *
         */
    }
}