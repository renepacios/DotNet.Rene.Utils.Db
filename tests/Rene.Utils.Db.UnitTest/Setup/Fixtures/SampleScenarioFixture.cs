namespace Rene.Utils.Db.UnitTest.Setup.Fixtures
{
    using Models;
    using Scenarios;
    using Xunit;

    [CollectionDefinition("SampleScenarioCollection")]
    public class SampleScenarioCollection : ICollectionFixture<SampleScenarioFixture>
    {
        // This class has no code and is never created directly.
        // It's just a marker for xUnit to link the fixture with the collection name.
    }

    public class SampleScenarioFixture
    {
        // Called once per test class (or once for the entire collection),
        // so we can seed 10 items or pass a custom amount.
        public MockScenario<Sample> Scenario { get; } = MockScenarioFactory.CreateSampleScenario(10);
    }

}
