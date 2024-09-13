namespace Rene.Utils.Db.UnitTest.Models
{
    using Magic.AutoMapper;

    public class SampleViewModel
        : IWithGenericHandler<Sample>,
            IMapFrom<Sample>, IEqualityComparer<SampleViewModel>
    {
        public int Id { get; set; }
        public string Name { get; set; }


        public bool Equals(SampleViewModel x, SampleViewModel y)
        {
            //complete body of this method
            return x != null && y != null && x.Id == y.Id && x.Name == y.Name;

        }

        public int GetHashCode(SampleViewModel obj)
        {
            return 0;
        }
    }
}
