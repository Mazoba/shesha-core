using Abp.Dependency;
using Abp.Domain.Entities;
using Newtonsoft.Json.Linq;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shesha.JsonLogic
{
    /// <summary>
    /// Json Logic to Linq converter
    /// </summary>
    public class JsonLogic2LinqConverter : IJsonLogic2LinqConverter, ITransientDependency
    {
        public void Convert(JToken rule, JsonLogic2LinqConverterContext context)
        {
            throw new NotImplementedException();
        }

        private const string StringStr = "string";

        private readonly string BooleanStr = nameof(Boolean).ToLower();
        private readonly string Number = nameof(Number).ToLower();
        private readonly string In = nameof(In).ToLower();
        private readonly string And = nameof(And).ToLower();

        private readonly MethodInfo MethodContains = typeof(Enumerable).GetMethods(
                        BindingFlags.Static | BindingFlags.Public)
                        .Single(m => m.Name == nameof(Enumerable.Contains)
                            && m.GetParameters().Length == 2);

        private delegate Expression Binder(Expression left, Expression right);

        private Expression CombineExpressions<T>(JToken[] tokens, Binder binder, ParameterExpression param) 
        {
            Expression acc = null;

            Expression bind(Expression acc, Expression right) => acc == null ? right : binder(acc, right);

            foreach (var argument in tokens)
            {
                var parsedArgument = ParseTree<T>(argument, param);
                acc = bind(acc, parsedArgument);
            }

            return acc;
        }

        private Expression ParseTree<T>(
            JToken rule,
            ParameterExpression param)
        {
            if (rule is JValue value)
                return Expression.Constant(value.Value);

            if (rule is JArray array)
                throw new NotImplementedException();

            if (rule is JObject ruleObj)
            {
                if (!ruleObj.HasValues)
                    return Expression.Empty();

                if (!TryGetOperator(rule, out var @operator))
                    throw new Exception("Failed to parse expression");

                switch (@operator.Name) 
                {
                    case JsOperators.And:
                        return CombineExpressions<T>(@operator.Arguments, Expression.AndAlso, param);

                    case JsOperators.Or:
                        return CombineExpressions<T>(@operator.Arguments, Expression.OrElse, param);

                    case JsOperators.Equal:
                    case JsOperators.StrictEqual:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            if (arg1.Type == typeof(Guid) && arg2.Type == typeof(string)) 
                            {
                                var toGuidMethod = typeof(StringHelper).GetMethod(nameof(StringHelper.ToGuid), new Type[] { typeof(string) });

                                arg2 = Expression.Call(
                                    null,
                                    toGuidMethod,
                                    arg2);
                            }

                            return Expression.Equal(arg1, arg2);
                        }

                    case JsOperators.NotEqual:
                    case JsOperators.StrictNotEqual:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.NotEqual(arg1, arg2);
                        }

                    case JsOperators.Greater:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            if (ExpressionExtensions.IsNullableExpression(arg1) && !ExpressionExtensions.IsNullableExpression(arg2))
                                arg2 = Expression.Convert(arg2, arg1.Type);
                            else if (!ExpressionExtensions.IsNullableExpression(arg1) && ExpressionExtensions.IsNullableExpression(arg2))
                                arg1 = Expression.Convert(arg1, arg2.Type);

                            // 1. split arguments to pair
                            // 2. convert to nullable if required

                            /*
                            if (ExpressionExtensions.IsNullableExpression(arg1)) {
                                // https://stackoverflow.com/questions/2088231/expression-greaterthan-fails-if-one-operand-is-nullable-type-other-is-non-nulla

                                var arg1NotNullable = Expression.Convert(arg1, ReflectionHelper.GetUnderlyingTypeIfNullable(arg1.Type));

                                var memberIsNotNull = Expression.NotEqual(arg1, Expression.Constant(null));
                                return Expression.AndAlso(memberIsNotNull, Expression.GreaterThan(arg1NotNullable, arg2));
                            }
                            */
                                
                            return Expression.GreaterThan(arg1, arg2);
                        }

                    case JsOperators.GreaterOrEqual:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.GreaterThanOrEqual(arg1, arg2);
                        }

                    case JsOperators.Less:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.LessThan(arg1, arg2);
                        }

                    case JsOperators.LessOrEqual:
                        {
                            // todo: add support of more than two agruments
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{@operator.Name} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.LessThanOrEqual(arg1, arg2);
                        }

                    case JsOperators.Negotiation:
                    case JsOperators.Not:
                        {
                            if (@operator.Arguments.Count() != 1)
                                throw new Exception($"{JsOperators.Not} operator must contain exactly one argument");

                            var arg = ParseTree<T>(@operator.Arguments[0], param);

                            if (arg is MemberExpression memberExpr)
                                return Expression.Equal(memberExpr, Expression.Constant(null));

                            return Expression.Not(arg);
                        }
                    case JsOperators.DoubleNegotiation:
                        {
                            if (@operator.Arguments.Count() != 1)
                                throw new Exception($"{JsOperators.Not} operator must contain exactly one argument");

                            var arg = ParseTree<T>(@operator.Arguments[0], param);

                            if (arg is MemberExpression memberExpr)
                                return Expression.NotEqual(memberExpr, Expression.Constant(null));

                            return Expression.Not(Expression.Not(arg));
                        }

                    case JsOperators.Var:
                        {
                            if (@operator.Arguments.Count() != 1)
                                throw new Exception($"{JsOperators.Var} operator must contain exactly one argument");

                            var name = GetStringValue(@operator.Arguments.First());

                            var expr = ExpressionExtensions.GetMemberExpression(param, name);

                            if (expr.Type.IsEntityType()) 
                            {
                                name = $"{name}.{nameof(IEntity.Id)}";
                                expr = ExpressionExtensions.GetMemberExpression(param, name);
                            }

                            return expr;
                        }

                    case JsOperators.In:
                        {
                            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new Type[] { typeof(string) });

                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{JsOperators.In} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            // note: `in` arguments are reversed
                            return Expression.Call(
                                    arg2,
                                    containsMethod,
                                    arg1);
                        }

                    case JsOperators.EndsWith:
                        {
                            var endsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new Type[] { typeof(string) });

                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{JsOperators.EndsWith} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.Call(
                                    arg1,
                                    endsWithMethod,
                                    arg2);
                        }
                    
                    case JsOperators.StartsWith:
                        {
                            // note: now it supports only strings
                            // todo: add check
                            var startsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new Type[] { typeof(string) });

                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{JsOperators.StartsWith} operator require two arguments");

                            var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                            var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                            return Expression.Call(
                                    arg1,
                                    startsWithMethod,
                                    arg2);
                        }
                }
            }
            
            return null;
        }

        private IEnumerable<Expression> ConvertArguments<T>(JToken[] args, ParameterExpression param) 
        {
            var convertedArgs = args.Select(arg => ParseTree<T>(arg, param));
            return convertedArgs;
        }

        private IEnumerable<Expression> FixExpressionsNullability(IEnumerable<Expression> expressions) 
        {
            if (expressions.Any(e => ExpressionExtensions.IsNullableExpression(e)) && expressions.Any(e => !ExpressionExtensions.IsNullableExpression(e)))
            {
                var convertedExpressions = expressions.Select(e =>
                {
                    if (!ExpressionExtensions.IsNullableExpression(e))
                    {
                        return Expression.Convert(e, typeof(Nullable<>).MakeGenericType(e.Type));
                    }
                    else
                        return e;
                });
                return convertedExpressions;
            }
            return expressions;
        }

        private IEnumerable<ExpressionPair> SplitArgumentsIntoPair(Expression[] arguments) 
        {
            if (arguments.Count() < 2)
                throw new NotSupportedException("Number of arguments must be 2 or greater");

            var pairs = new List<ExpressionPair>();
            for (int i = 0; i < arguments.Count() - 1; i++)
            {
                pairs.Add(new ExpressionPair(arguments[i], arguments[i + 1]));
            }

            return pairs;
        }

        /*
         if (ExpressionExtensions.IsNullableExpression(arg1) && !ExpressionExtensions.IsNullableExpression(arg2))
                                arg2 = Expression.Convert(arg2, arg1.Type);
                            else if (!ExpressionExtensions.IsNullableExpression(arg1) && ExpressionExtensions.IsNullableExpression(arg2))
                                arg1 = Expression.Convert(arg1, arg2.Type);
         */

        private string GetStringValue(JToken arg)
        {
            if (arg == null)
                return null;

            if (arg is JValue value && value.Value is string stringValue)
                return stringValue;

            throw new NotSupportedException("Not supported value type");
        }

        /// <summary>
        /// Get operation props
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public OperationProps GetOperation(JToken rule)
        {
            if (rule is JObject ruleObj)
            {
                var p = ruleObj.Properties().First();
                var operationName = p.Name;
                var operationArguments = (p.Value is JArray jArrayArgs)
                    ? jArrayArgs.ToArray()
                    : new JToken[] { p.Value };
                return new OperationProps
                {
                    Name = operationName,
                    Arguments = operationArguments,
                };
            }

            return null;
        }

        public bool IsOperator(JToken rule) 
        {
            return TryGetOperator(rule, out var _);
        }

        public bool TryGetOperator(JToken rule, out OperationProps @operator) 
        {
            if (!(rule is JObject ruleObj) || ruleObj.Properties().Count() != 1) 
            {
                @operator = null;
                return false;
            }

            var p = ruleObj.Properties().First();
            var operationName = p.Name;
            var operationArguments = (p.Value is JArray jArrayArgs)
                ? jArrayArgs.ToArray()
                : new JToken[] { p.Value };

            @operator = new OperationProps { 
                Name = operationName,
                Arguments = operationArguments
            };
            return true;
        }

        private List<string> KnownOperators = new List<string> {

            JsOperators.Equal,
            JsOperators.StrictEqual,
            JsOperators.NotEqual,
            JsOperators.StrictNotEqual,
            JsOperators.Less,
            JsOperators.LessOrEqual,
            JsOperators.Greater,
            JsOperators.GreaterOrEqual,
            JsOperators.Var,
            JsOperators.And,
            JsOperators.Or,
            JsOperators.DoubleNegotiation,
            JsOperators.Negotiation,
            JsOperators.Not,
            JsOperators.In,
            JsOperators.StartsWith,
            JsOperators.EndsWith
        };

        public Expression<Func<T, bool>> ParseExpressionOf<T>(JObject rule)
        {
            if (rule.IsNullOrEmpty())
                return null;

            var itemExpression = Expression.Parameter(typeof(T), "ent");
            var conditions = ParseTree<T>(rule, itemExpression);
            if (conditions.CanReduce)
            {
                conditions = conditions.ReduceAndCheck();
            }

            var query = Expression.Lambda<Func<T, bool>>(conditions, itemExpression);
            return query;
        }

        public Func<T, bool> ParsePredicateOf<T>(JObject rule)
        {
            if (rule.IsNullOrEmpty())
                return null;

            var query = ParseExpressionOf<T>(rule);
            return query.Compile();
        }
    }

    public static class JsOperators
    {
        public const string Equal = "==";
        public const string StrictEqual = "===";
        public const string NotEqual = "!=";
        public const string StrictNotEqual = "!==";
        public const string Less = "<";
        public const string LessOrEqual = "<=";
        public const string Greater = ">";
        public const string GreaterOrEqual = ">=";
        public const string Var = "var";
        public const string And = "and";
        public const string Or = "or";
        public const string DoubleNegotiation = "!!";
        public const string Negotiation = "!";
        public const string Not = "not";
        public const string In = "in";
        public const string StartsWith = "startsWith";
        public const string EndsWith = "endsWith";
    }

    public class ExpressionPair 
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public ExpressionPair(Expression left, Expression right)
        {
            Left = left;
            Right = right;
        }
    }
}
