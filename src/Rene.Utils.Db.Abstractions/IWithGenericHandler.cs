
using System;

namespace Rene.Utils.Db
{
    public interface IWithGenericHandler<TModel>
    {
        public Type MapFromType => typeof(TModel);
    }
}
