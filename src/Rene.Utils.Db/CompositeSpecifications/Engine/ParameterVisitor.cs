using System.Linq.Expressions;

namespace Rene.Utils.Db.CompositeSpecifications.Engine
{
    internal class ParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _leftParameter;
        private readonly ParameterExpression _rightParameter;

        public ParameterVisitor(ParameterExpression leftParameter, ParameterExpression rightParameter)
        {
            _leftParameter = leftParameter;
            _rightParameter = rightParameter;
        }

        //protected override Expression VisitParameter(ParameterExpression node)
        //{
        //    return object.ReferenceEquals(node, _leftParameter)
        //        ? _rightParameter
        //        : base.VisitParameter(node);
        //}

        public override Expression Visit(Expression node)
        {
            if (node == _leftParameter)
                return _rightParameter;
            return base.Visit(node);
        }
    }
}