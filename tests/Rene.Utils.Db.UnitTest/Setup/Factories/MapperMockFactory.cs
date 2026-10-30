namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using Models;
    using Models.AsyncEnumerable;

    internal static class MapperMockFactory
    {
        /// <summary>
        /// Creates a Mock of <c>IDbUtilsMapper</c> for the <c>Sample</c> entity scenario,
        /// with basic two-way mapping for <c>Sample</c> and <c>SampleDetailsViewModel</c>.
        /// </summary>
        public static Mock<IDbUtilsMapper> CreateSampleMapper()
        {
            var mapperMock = new Mock<IDbUtilsMapper>();

            mapperMock
                .Setup(m => m.Map<Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((object s) => Sample.Create((SampleDetailsViewModel)s));

            mapperMock
                .Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((object s) => SampleDetailsViewModel.Create((Sample)s));

            mapperMock
                .Setup(m => m.Map(It.IsAny<SampleDetailsViewModel>(), It.IsAny<Sample>()))
                .Returns((SampleDetailsViewModel s, Sample d) =>
                {
                    var a = Sample.Create(s);
                    d.Id = a.Id;
                    d.Description = a.Description;
                    d.Name = a.Name;
                    return d;
                });

            mapperMock
                .Setup(m => m.CanProject(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(true);

            mapperMock
                .Setup(m => m.ProjectTo<SampleDetailsViewModel>(It.IsAny<IQueryable>()))
                .Returns((IQueryable source) =>
                {
                    var result = new List<SampleDetailsViewModel>();
                    foreach (var item in source.Cast<Sample>())
                    {
                        result.Add(SampleDetailsViewModel.Create(item));
                    }

                    return new TestAsyncEnumerable<SampleDetailsViewModel>(result);
                });

            return mapperMock;
        }
    }

}
