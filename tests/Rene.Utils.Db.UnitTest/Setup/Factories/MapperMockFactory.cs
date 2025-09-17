namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using AutoMapper;
    using Models;
    using Models.AsyncEnumerable;
    using System.Linq.Expressions;

    internal static class MapperMockFactory
    {
        /// <summary>
        /// Creates a Mock of <c>IMapper</c> for the <c>Sample</c> entity scenario,
        /// with basic two-way mapping for <c>Sample</c> and <c>SampleDetailsViewModel</c>.
        /// </summary>
        public static Mock<IMapper> CreateSampleMapper()
        {
            var mapperMock = new Mock<IMapper>();

            mapperMock
                .Setup(m => m.Map<Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));

            mapperMock
                .Setup(m => m.Map<SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

            mapperMock
                .Setup(m => m.Map<Sample, SampleDetailsViewModel>(It.IsAny<Sample>()))
                .Returns((Sample s) => SampleDetailsViewModel.Create(s));

            mapperMock
                .Setup(m => m.Map<SampleDetailsViewModel, Sample>(It.IsAny<SampleDetailsViewModel>()))
                .Returns((SampleDetailsViewModel s) => Sample.Create(s));


            mapperMock
                .Setup(m => m.Map<SampleDetailsViewModel, Sample>(It.IsAny<SampleDetailsViewModel>(), It.IsAny<Sample>()))
                .Callback((SampleDetailsViewModel s, Sample d) =>
                {
                    var a = Sample.Create(s);
                    d.Id = a.Id;
                    d.Description = a.Description;
                    d.Name = a.Name;
                });

            mapperMock
                .Setup(m => m.Map<IEnumerable<SampleDetailsViewModel>>(It.IsAny<IEnumerable<Sample>>()))
                .Returns((IEnumerable<Sample> s) =>
                {
                    var result = new List<SampleDetailsViewModel>();
                    foreach (var item in s)
                    {
                        result.Add(SampleDetailsViewModel.Create(item));
                    }
                    return result;
                });

            mapperMock
                .Setup(m => m.ProjectTo<SampleDetailsViewModel>(
                    It.IsAny<IQueryable<Sample>>(),
                    It.IsAny<object?>(),
                    It.IsAny<Expression<Func<SampleDetailsViewModel, object>>[]>()
                ))
                .Returns((IQueryable<Sample> source, object? parameters, Expression<Func<SampleDetailsViewModel, object>>[] membersToExpand) =>
                {
                    var result = new List<SampleDetailsViewModel>();
                    foreach (var item in source)
                    {
                        result.Add(SampleDetailsViewModel.Create(item));
                    }

                    return new TestAsyncEnumerable<SampleDetailsViewModel>(result);
                });


            return mapperMock;
        }
    }

}
