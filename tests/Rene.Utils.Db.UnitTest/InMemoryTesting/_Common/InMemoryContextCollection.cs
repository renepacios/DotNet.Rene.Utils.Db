namespace Rene.Utils.Db.UnitTest.InMemoryTesting._Common
{
    // This class has no code, it's just a marker for xUnit
    [CollectionDefinition("InMemoryContextCollection")]
    public class InMemoryContextCollection : ICollectionFixture<InMemoryContextFixture>;
}