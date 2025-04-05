namespace Rene.Utils.Db.UnitTest.Setup.Factories
{
    using AutoMapper;
    using Models;

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

            return mapperMock;
        }
    }

}
