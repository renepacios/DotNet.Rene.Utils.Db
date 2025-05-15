// ReSharper disable once CheckNamespace

namespace Rene.Utils.Db.Tests.Common
{
    public class ProductDescriptionDto
    {
        public int ProductDescriptionId { get; set; }
        public string Description { get; set; } = null!;
        public Guid Rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}