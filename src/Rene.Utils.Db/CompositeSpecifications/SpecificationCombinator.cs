using Rene.Utils.Db.CompositeSpecifications.Engine;

namespace Rene.Utils.Db.CompositeSpecifications
{
    public static class SpecificationCombinator
    {


        public static IDbUtilsCompositeSpecification<T> And<T>(this IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
        {
            var specificationEngine = new AndSpecificationEngine<T>(left, right);
            var s = specificationEngine.Build();
            return s;

        }

        public static IDbUtilsCompositeSpecification<T> Or<T>(this IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
        {
            var specificationEngine = new ORSpecificationEngine<T>(left, right);
            var s = specificationEngine.Build();
            return s;
        }

        public static IDbUtilsCompositeSpecification<T> ToInvert<T>(this IDbUtilsSpecification<T> spe)
        {
            var specificationEngine = new NotSpecificationEngine<T>(spe);
            var s = specificationEngine.Build();
            return s;
        }
    }

}


