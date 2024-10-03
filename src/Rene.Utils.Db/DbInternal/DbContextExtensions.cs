using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rene.Utils.Db.DbInternal
{
    using Microsoft.EntityFrameworkCore;

    internal static class DbContextExtensions
    {
        public static string GetKeyNameFromEntityType<T>(this DbContext context)
        {
            //var entityType = context.Model.FindEntityType(typeof(T));
            //var key = entityType.FindPrimaryKey();
            //var keyName = key.Properties.Select(x => x.Name).FirstOrDefault();
            //return keyName;

            return GetKeyNameFromEntityType(context, typeof(T));
        }


        public static string GetKeyNameFromEntityType(this DbContext context, Type type)
        {
            var entityType = context.Model.FindEntityType(type);
            var key = entityType.FindPrimaryKey();
            var keyName = key.Properties.Select(x => x.Name).FirstOrDefault();
            return keyName;
        }
    }
}
