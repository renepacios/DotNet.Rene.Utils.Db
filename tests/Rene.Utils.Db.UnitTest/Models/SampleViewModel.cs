namespace Rene.Utils.Db.UnitTest.Models
{
    using Magic.AutoMapper;

    public class SampleViewModel
        : IWithGenericHandler<Sample>,
            IMapFrom<Sample>
    {
        public int Id { get; set; }
        public string Name { get; set; }

    }
}
