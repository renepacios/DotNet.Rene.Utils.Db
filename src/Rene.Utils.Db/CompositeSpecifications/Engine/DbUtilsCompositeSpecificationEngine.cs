using System.Linq.Expressions;

namespace Rene.Utils.Db.CompositeSpecifications.Engine
{
    /*
     *  https://gist.github.com/janvanderhaegen/1620fee98ea8578862aa
     *  https://stackoverflow.com/questions/457316/combining-two-expressions-expressionfunct-bool
     *  https://www.codeproject.com/Articles/895951/Combining-expressions-to-dynamically-append-criter
     *
     */
    internal abstract class DbUtilsCompositeSpecificationEngine<T>
    {
        public IDbUtilsSpecification<T> Left { get; }
        public IDbUtilsSpecification<T> Right { get; }


        private Expression<Func<T, bool>> _leftCriteria = null;
        protected Expression<Func<T, bool>> LeftCriteria => _leftCriteria ??= UnifyExpressions(Left);


        private Expression<Func<T, bool>> _rightCriteria = null;
        protected Expression<Func<T, bool>> RightCriteria => _rightCriteria ??= UnifyExpressions(Right);

        //   protected ParameterVisitor Visitor { get; }

        public abstract IDbUtilsCompositeSpecification<T> Build(); //Todo: implementar helpers con extensores y sobrecarga de operadores lógicos

        protected internal DbUtilsCompositeSpecificationEngine(IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
        {
            _ = left ?? throw new ArgumentNullException(nameof(left));
            _ = right ?? throw new ArgumentNullException(nameof(right));

            if (left.Criteria == null && !left.Criterias.AnyNotNull()) throw new ArgumentNullException($" {nameof(left)} has not criterias");
            if (right.Criteria == null && !right.Criterias.AnyNotNull()) throw new ArgumentNullException($" {nameof(right)} has not criterias");

            Left = left;
            Right = right;

        }

        //List<Expression<Func<T, object>>> Includes { get; }
        //List<string> IncludeStrings { get; }
        //List<KeyValuePair<bool, Expression<Func<T, object>>>> OrderBy { get; }
        //Expression<Func<T, object>> GroupBy { get; }


    //    protected 




        /// <summary>
        /// Dentro de una especificación podemos tener varias expresiones de filtrado. Para combinar las especificaciones, debemos montar una unica expresión.
        /// </summary>
        /// <param name="spe"></param>
        /// <returns></returns>
        private Expression<Func<T, bool>> UnifyExpressions(IDbUtilsSpecification<T> spe)
        {
            if (!spe.Criterias.AnyNotNull())
            {
                return spe.Criteria;
            }

            //necesitamos una expresion base para ir componiendo la expresión común cogemos la suelta o la primera del array
            // en la implementación de las especificaciones hay un workarround de Criteria=_ => true, cuando se montan las expresiones sobre el array
            // que implicaría que spe.Criteria nunca será nulo. Obviamente esto podría cambiar a futuro y se contemplará este cambio
            Expression<Func<T, bool>> currentExp = spe.Criteria ?? spe.Criterias.First();

            int skip = spe.Criteria == null ? 1 : 0;
            var parameter = Expression.Parameter(typeof(T));


            foreach (Expression<Func<T, bool>> itemExpression in spe.Criterias.Skip(skip))
            {
                var currentVisitor = new ParameterVisitor(currentExp.Parameters[0], parameter);
                var current = currentVisitor.Visit(currentExp.Body);
                var expVisitor = new ParameterVisitor(itemExpression.Parameters[0], parameter);
                var exp = expVisitor.Visit(itemExpression.Body);

                currentExp = Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(current, exp), parameter);
            }

            return currentExp;
        }


        /// <summary>
        /// Para expresiones unarias de tipo ToInvert
        /// </summary>
        /// <param name="spe"></param>
        protected internal DbUtilsCompositeSpecificationEngine(IDbUtilsSpecification<T> spe)
        {
            _ = spe ?? throw new ArgumentNullException(nameof(spe));
            if (spe.Criteria == null && !spe.Criterias.AnyNotNull()) throw new ArgumentNullException($" {nameof(spe)} has not criterias");
          

            Left = spe;
            //LeftCriteria = spe.Criteria;

        }


    }

    internal class AndSpecificationEngine<T> : DbUtilsCompositeSpecificationEngine<T>
    {
        public AndSpecificationEngine(IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
            : base(left, right)
        {

        }

        public override IDbUtilsCompositeSpecification<T> Build()
        {
            // RightCriteria = Visitor.Visit(RightCriteria) as Expression<Func<T, bool>>;
            
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ParameterVisitor(LeftCriteria.Parameters[0], parameter);
            var left = leftVisitor.Visit(LeftCriteria.Body);
            var rightVisitor = new ParameterVisitor(RightCriteria.Parameters[0], parameter);
            var right = rightVisitor.Visit(RightCriteria.Body);

            var newExpresion = Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right), parameter);


            return new CompositeSpecification<T>
            {
                Criteria = newExpresion
            };
        }


    }

    internal class ORSpecificationEngine<T> : DbUtilsCompositeSpecificationEngine<T>
    {
        public ORSpecificationEngine(IDbUtilsSpecification<T> left, IDbUtilsSpecification<T> right)
            : base(left, right)
        {
        }

        public override IDbUtilsCompositeSpecification<T> Build()
        {
            // RightCriteria = Visitor.Visit(RightCriteria) as Expression<Func<T, bool>>;
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ParameterVisitor(LeftCriteria.Parameters[0], parameter);
            var left = leftVisitor.Visit(LeftCriteria.Body);
            var rightVisitor = new ParameterVisitor(RightCriteria.Parameters[0], parameter);
            var right = rightVisitor.Visit(RightCriteria.Body);

            var newExpresion = Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(left, right), parameter);

            return new CompositeSpecification<T>
            {
                Criteria = newExpresion
            };
        }

    }


    internal class NotSpecificationEngine<T> : DbUtilsCompositeSpecificationEngine<T>
    {
        public NotSpecificationEngine(IDbUtilsSpecification<T> spe)
            : base(spe)
        {
        }

        public override IDbUtilsCompositeSpecification<T> Build()
        {
            // RightCriteria = Visitor.Visit(RightCriteria) as Expression<Func<T, bool>>;
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ParameterVisitor(LeftCriteria.Parameters[0], parameter);
            var left = leftVisitor.Visit(LeftCriteria.Body);

            var newExpresion = Expression.Lambda<Func<T, bool>>(
                Expression.Not(left), parameter);


            return new CompositeSpecification<T>
            {
                Criteria = newExpresion
            };

        }

    }

}


