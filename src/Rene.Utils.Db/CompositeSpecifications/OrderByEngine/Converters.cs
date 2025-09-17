using System.Linq.Expressions;
using System.Reflection;

namespace Rene.Utils.Db.CompositeSpecifications.OrderByEngine
{
    internal static class Converters
    {

        /// <summary>
        /// Método para traducir los orderby que nos vienen en el querystring a expresiones linq
        /// <param name="sortQueryString">
        /// pares separados por ",", done la primera posición es nombre del campo y la segunda es 'asc' o 'desc'
        /// </param>
        /// </summary>
        public static ICollection<KeyValuePair<bool, Expression<Func<T, object>>>> ToSortSequenceExpression<T>(
         this ICollection<string> sortQueryString)
        {

            /*
            * TODO:
            *      Implementar parseo para atributos de navegación del tipo curso.Nombre
            */



            var result = new List<KeyValuePair<bool, Expression<Func<T, object>>>>();

            if (sortQueryString == null) return result;



            foreach (var sort in sortQueryString)
            {
                if (string.IsNullOrEmpty(sort))
                    continue;

                var slices = sort.Split(',');

                //TODO contemplar caso de asc implícito
                if (slices.Length > 2)
                    throw new InvalidOperationException($"Order parameter invalid '{sort}'");

                var order = (slices.Length == 2 ? slices[1].ToLower() : string.Empty).Replace(" ", string.Empty);

                if (string.IsNullOrWhiteSpace(order))
                    order = "asc";

                var field = slices[0];

                if (order != "desc" && order != "asc") throw new InvalidOperationException($"Invalid order: '{order}'");

                if (string.IsNullOrWhiteSpace(field)) throw new InvalidOperationException($"Empty field with order '{order}'");

                result.Add(new KeyValuePair<bool, Expression<Func<T, object>>>(order == "asc", CreateExpression<T>(field)));
            }


            return result;
        }

        private static Expression<Func<T, object>> CreateExpression<T>(string field)
        {
            string[] props = field.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                var convention = prop.Substring(0, 1).ToUpper() + prop.Substring(1);
                PropertyInfo pi = type.GetProperty(convention);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            //var Expression.Lambda<Func<T, object>>(expr, arg)
            Expression conversion = Expression.Convert(expr, typeof(object));
            var func = Expression.Lambda<Func<T, object>>(conversion, arg);

            return func;
        }
    }
}
