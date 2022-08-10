﻿using Abp.Dependency;
using Abp.Domain.Entities;
using Newtonsoft.Json.Linq;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        private Expression CombineExpressions<T>(Expression[] expressions, Binder binder, ParameterExpression param)
        {
            Expression acc = null;

            Expression bind(Expression acc, Expression right) => acc == null ? right : binder(acc, right);

            foreach (var expression in expressions)
            {
                acc = bind(acc, expression);
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
                            return ParseEqualExpression<T>(param, @operator);
                        }

                    case JsOperators.NotEqual:
                    case JsOperators.StrictNotEqual:
                        {
                            var equalExpression= ParseEqualExpression<T>(param, @operator);
                            return Expression.Not(equalExpression);
                        }

                    case JsOperators.Greater:
                        {
                            return Compare<T>(param, @operator.Arguments, pair => pair, 
                                pair => 
                                {
                                    Expression expr = null;

                                    #region datetime
                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDateTime(pair.Left, pair.Right,
                                        dt => dt.EndOfTheMinute(), // compare with end of the minute to exclude current value
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDateTime(pair.Right, pair.Left,
                                        dt => dt.StartOfTheMinute(),
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;
                                    #endregion

                                    #region date

                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDate(pair.Left, pair.Right,
                                        dt => dt.EndOfTheDay(), // compare with end of the day to exclude current value
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDate(pair.Right, pair.Left,
                                        dt => dt.StartOfTheDay(),
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    #region time

                                    // try to compare var and time const (normal order)
                                    if (TryCompareMemberAndTime(pair.Left, pair.Right,
                                        t => t.EndOfTheMinute(), // compare with end of the minute
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare time const and var (reverse order)
                                    if (TryCompareMemberAndTime(pair.Right, pair.Left,
                                        t => t.StartOfTheMinute(),
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    var convertedPair = PrepareExpressionPair(pair);
                                    return Expression.GreaterThan(convertedPair.Left, convertedPair.Right);
                                });
                        }

                    case JsOperators.GreaterOrEqual:
                        {
                            return Compare<T>(param, @operator.Arguments, pair => pair, 
                                pair => 
                                {
                                    Expression expr = null;

                                    #region datetime
                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDateTime(pair.Left, pair.Right,
                                        dt => dt.StartOfTheMinute(), // compare with start of the minute to include current value
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDateTime(pair.Right, pair.Left,
                                        dt => dt.EndOfTheMinute(),
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;
                                    #endregion

                                    #region date

                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDate(pair.Left, pair.Right,
                                        dt => dt.StartOfTheDay(), // compare with start of the day to include current value
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDate(pair.Right, pair.Left,
                                        dt => dt.EndOfTheDay(),
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    #region time

                                    // try to compare var and time const (normal order)
                                    if (TryCompareMemberAndTime(pair.Left, pair.Right,
                                        t => t.StartOfTheMinute(), // compare with start of the minute
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare time const and var (reverse order)
                                    if (TryCompareMemberAndTime(pair.Right, pair.Left,
                                        t => t.EndOfTheMinute(),
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    var convertedPair = PrepareExpressionPair(pair);
                                    return Expression.GreaterThanOrEqual(convertedPair.Left, convertedPair.Right);
                                });
                        }

                    case JsOperators.Less:
                        {
                            return Compare<T>(param, @operator.Arguments, pair => pair,
                                pair =>
                                {
                                    Expression expr = null;

                                    #region datetime
                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDateTime(pair.Left, pair.Right,
                                        dt => dt.StartOfTheMinute(), // compare with start of the minute to exclude current value
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDateTime(pair.Right, pair.Left,
                                        dt => dt.EndOfTheMinute(),
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;
                                    #endregion

                                    #region date

                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDate(pair.Left, pair.Right,
                                        dt => dt.StartOfTheDay(), // compare with start of the day to exclude current value
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDate(pair.Right, pair.Left,
                                        dt => dt.EndOfTheDay(),
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    #region time

                                    // try to compare var and time const (normal order)
                                    if (TryCompareMemberAndTime(pair.Left, pair.Right,
                                        t => t.StartOfTheMinute(), // compare with start of the minute to exclude current value
                                        Expression.LessThan,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare time const and var (reverse order)
                                    if (TryCompareMemberAndTime(pair.Right, pair.Left,
                                        t => t.EndOfTheMinute(),
                                        Expression.GreaterThan,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    var convertedPair = PrepareExpressionPair(pair);
                                    return Expression.LessThan(convertedPair.Left, convertedPair.Right);
                                });
                        }

                    case JsOperators.LessOrEqual:
                        {
                            return Compare<T>(param, @operator.Arguments, pair => pair,
                                pair =>
                                {
                                    Expression expr = null;

                                    #region datetime
                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDateTime(pair.Left, pair.Right,
                                        dt => dt.EndOfTheMinute(), // compare with end of the minute to include current value
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDateTime(pair.Right, pair.Left,
                                        dt => dt.StartOfTheMinute(),
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;
                                    #endregion
                                    
                                    #region date
                                    
                                    // try to compare var and datetime const (normal order)
                                    if (TryCompareMemberAndDate(pair.Left, pair.Right,
                                        dt => dt.EndOfTheDay(), // compare with end of the day to include current value
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare datetime const and var (reverse order)
                                    if (TryCompareMemberAndDate(pair.Right, pair.Left,
                                        dt => dt.StartOfTheDay(),
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    #region time

                                    // try to compare var and time const (normal order)
                                    if (TryCompareMemberAndTime(pair.Left, pair.Right,
                                        t => t.EndOfTheMinute(), // compare with end of the minute to include current value
                                        Expression.LessThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    // try to compare time const and var (reverse order)
                                    if (TryCompareMemberAndTime(pair.Right, pair.Left,
                                        t => t.StartOfTheMinute(),
                                        Expression.GreaterThanOrEqual,
                                        out expr
                                    ))
                                        return expr;

                                    #endregion

                                    var convertedPair = PrepareExpressionPair(pair);
                                    return Expression.LessThanOrEqual(convertedPair.Left, convertedPair.Right);
                                });
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
                            if (@operator.Arguments.Count() != 2)
                                throw new Exception($"{JsOperators.In} operator require two arguments");


                            if (@operator.Arguments[1] is JArray arrayArg)
                            {
                                var parsedArray = arrayArg.Select(i => ParseTree<T>(i, param)).ToArray();
                                var arg = ParseTree<T>(@operator.Arguments[0], param);
                                if (arg is MemberExpression memberExpr && memberExpr.Member.IsReferenceListProperty()) 
                                {
                                    arg = Expression.Convert(arg, typeof(Int64?));
                                }

                                var arrExpressions = parsedArray.Select(item => Expression.Equal(arg, Expression.Convert(item, typeof(Int64?)))).ToArray();
                                return CombineExpressions<T>(arrExpressions, Expression.OrElse, param);
                            }
                            else {
                                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new Type[] { typeof(string) });

                                var arg1 = ParseTree<T>(@operator.Arguments[0], param);
                                var arg2 = ParseTree<T>(@operator.Arguments[1], param);

                                // note: `in` arguments are reversed
                                return Expression.Call(
                                        arg2,
                                        containsMethod,
                                        arg1);
                            }
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

        private bool TryCompareMemberAndDateTime(Expression left, Expression right, Func<DateTime, DateTime> dateConverter, Binder binder, out Expression expression)
        {
            expression = null;
            if (IsDateTimeMember(left) &&
                right is ConstantExpression constExpr && constExpr.Type == typeof(DateTime))
            {
                var dateExpr = Expression.Constant(dateConverter.Invoke((DateTime)constExpr.Value));
                expression = SafeNullable(left, dateExpr, binder);
                return true;
            }

            return false;
        }

        private bool TryCompareMemberAndDate(Expression left, Expression right, Func<DateTime, DateTime> dateConverter, Binder binder, out Expression expression)
        {
            expression = null;
            if (IsDateMember(left) &&
                right is ConstantExpression constExpr && constExpr.Type == typeof(DateTime))
            {
                var dateExpr = Expression.Constant(dateConverter.Invoke((DateTime)constExpr.Value));
                expression = SafeNullable(left, dateExpr, binder);
                return true;
            }

            return false;
        }

        private bool TryCompareMemberAndTime(Expression left, Expression right, Func<TimeSpan, TimeSpan> timeConverter, Binder binder, out Expression expression)
        {
            expression = null;
            if (IsTimeMember(left) && right.NodeType == ExpressionType.Constant)
            {
                right = ConvertTimeSpanConst(right, timeConverter);
                expression = SafeNullable(left, right, binder);
                return true;
            }
            
            return false;
        }

        private Expression ParseEqualExpression<T>(ParameterExpression param, OperationProps @operator)
        {
            return Compare<T>(param, @operator.Arguments, pair => pair, 
                pair =>
            {
                if (IsDateTimeMember(pair.Left) && pair.Right.NodeType == ExpressionType.Constant)
                {
                    // for datetime we strip seconds and compare as a date range: HH:mm:00 <= member <= HH:mm:59
                    var from = ConvertDateTimeConst(pair.Right, d => d.StartOfTheMinute());
                    var to = ConvertDateTimeConst(pair.Right, d => d.EndOfTheMinute());

                    return CombineExpressions<T>(new Expression[]
                        {
                                            SafeNullable(from, pair.Left, Expression.LessThanOrEqual),
                                            SafeNullable(pair.Left, to, Expression.LessThanOrEqual),
                        },
                        Expression.AndAlso,
                        param);
                }
                if (IsDateMember(pair.Left) && pair.Right.NodeType == ExpressionType.Constant)
                {
                    // date member should be compared as range: StartOfTheDay <= member <= EndOfTheDay
                    var from = ConvertDateTimeConst(pair.Right, d => d.StartOfTheDay());
                    var to = ConvertDateTimeConst(pair.Right, d => d.EndOfTheDay());

                    return CombineExpressions<T>(new Expression[]
                        {
                                            SafeNullable(from, pair.Left, Expression.LessThanOrEqual),
                                            SafeNullable(pair.Left, to, Expression.LessThanOrEqual),
                        },
                        Expression.AndAlso,
                        param);
                }
                if (IsTimeMember(pair.Left) && pair.Right.NodeType == ExpressionType.Constant)
                {
                    // time member should be compared as range (from member:00 to member:59 seconds)
                    var from = ConvertTimeSpanConst(pair.Right, t => t.StartOfTheMinute());
                    var to = ConvertTimeSpanConst(pair.Right, d => d.EndOfTheMinute());

                    return CombineExpressions<T>(new Expression[]
                        {
                                            SafeNullable(from, pair.Left, Expression.LessThanOrEqual),
                                            SafeNullable(pair.Left, to, Expression.LessThanOrEqual),
                        },
                        Expression.AndAlso,
                        param);
                }
                var convertedPair = PrepareExpressionPair(pair);
                return Expression.Equal(convertedPair.Left, convertedPair.Right);
            });
        }

        private Expression SafeNullable(Expression left, Expression right, Binder binder) 
        {
            ConvertNullable(ref left, ref right);
            ConvertNullable(ref right, ref left);

            return binder(left, right);
        }

        private bool IsDateMember(Expression expression)
        {
            if (!(expression is MemberExpression memberExpr) || expression.Type.GetUnderlyingTypeIfNullable() != typeof(DateTime))
                return false;

            var dataTypeAttribute = memberExpr.Member.GetAttribute<DataTypeAttribute>();
            return dataTypeAttribute?.DataType == DataType.Date;
        }

        private Expression ConvertDateTimeConst(Expression expression, Func<DateTime, DateTime> convertor)
        {
            if (expression is ConstantExpression constExpr && constExpr.Type == typeof(DateTime))
            {
                return Expression.Constant(convertor.Invoke((DateTime)constExpr.Value));
            }
            return expression;
        }

        private Expression ConvertTimeSpanConst(Expression expression, Func<TimeSpan, TimeSpan> convertor)
        {
            if (!(expression is ConstantExpression constExpr))
                return expression;

            TimeSpan? value = constExpr.Type == typeof(TimeSpan)
                ? (TimeSpan)constExpr.Value
                : constExpr.Type == typeof(Int64)
                    ? TimeSpan.FromSeconds((Int64)constExpr.Value)
                    : null;
            
            return value.HasValue
                ? Expression.Constant(convertor.Invoke(value.Value))
                : expression;
        }

        private bool IsDateTimeMember(Expression expression)
        {
            if (!(expression is MemberExpression memberExpr) || expression.Type.GetUnderlyingTypeIfNullable() != typeof(DateTime))
                return false;

            var dataTypeAttribute = memberExpr.Member.GetAttribute<DataTypeAttribute>();
            return dataTypeAttribute == null || dataTypeAttribute?.DataType == DataType.DateTime;
        }

        private bool IsTimeMember(Expression expression)
        {
            return expression is MemberExpression && expression.Type.GetUnderlyingTypeIfNullable() == typeof(TimeSpan);
        }
        
        private void ConvertGuids(ref Expression a, ref Expression b)
        {
            if (a.Type == typeof(Guid) && b.Type == typeof(string))
            {
                var toGuidMethod = typeof(StringHelper).GetMethod(nameof(StringHelper.ToGuid), new Type[] { typeof(string) });

                b = Expression.Call(
                    null,
                    toGuidMethod,
                    b);
            }
        }

        private void ConvertNumericConsts(ref Expression a, ref Expression b)
        {
            if (!(a is MemberExpression memberExpr && b is ConstantExpression constExpr))
                return;

            if (memberExpr.Type.GetUnderlyingTypeIfNullable() == typeof(int) && constExpr.Type == typeof(Int64)) 
            {
                var constValue = (Int64)constExpr.Value;
                if (constValue <= int.MaxValue)
                    b = Expression.Constant(Convert.ToInt32(constValue));
                else
                    throw new OverflowException($"Constant value must be not grester than {int.MaxValue} (max int size) to compare with {memberExpr.Member.Name}, currtent value is {constValue}");
            }
            if (memberExpr.Type.GetUnderlyingTypeIfNullable() == typeof(Int64) && constExpr.Type == typeof(int))
            {
                b = Expression.Constant((Int64)constExpr.Value);
            }
        }

        private void ConvertTicksTimeSpan(ref Expression a, ref Expression b)
        {
            if (a.Type == typeof(TimeSpan) && b.Type == typeof(Int64))
            {
                var fromSecondsMethod = typeof(TimeSpan).GetMethod(nameof(TimeSpan.FromSeconds), new Type[] { typeof(double) });
                
                b = Expression.Call(
                    null,
                    fromSecondsMethod,
                    Expression.Convert(b, typeof(double)));
            }
        }

        private void ConvertNullable(ref Expression a, ref Expression b) 
        {
            if (ExpressionExtensions.IsNullableExpression(a) && !ExpressionExtensions.IsNullableExpression(b))
                b = Expression.Convert(b, a.Type);
        }

        private Expression Compare<T>(ParameterExpression param, JToken[] tokens, Func<ExpressionPair, ExpressionPair> preparePair, Func<ExpressionPair, Expression> comparator) 
        {
            var expressions = tokens.Select(t => ParseTree<T>(t, param)).ToArray();

            var pairs = SplitArgumentsIntoPair(expressions).Select(pair => preparePair(pair));

            var pairExpressions = pairs.Select(pair => comparator.Invoke(pair)).ToArray();

            return CombineExpressions<T>(pairExpressions, Expression.AndAlso, param);
        }

        private ExpressionPair PrepareExpressionPair(ExpressionPair pair) 
        {
            var left = pair.Left;
            var right = pair.Right;

            // if one of arguments is a Guid and another one is a string - convert string to Guid
            ConvertGuids(ref left, ref right);
            ConvertGuids(ref right, ref left);

            ConvertNumericConsts(ref left, ref right);
            ConvertNumericConsts(ref right, ref left);

            // check nullability in pairs and convert not nullable argument to nullable if required
            ConvertNullable(ref left, ref right);
            ConvertNullable(ref right, ref left);

            return new ExpressionPair(left, right);
        }

        private Expression Compare<T>(ParameterExpression param, JToken[] tokens, Func<ExpressionPair, Expression> comparator) 
        {
            return Compare<T>(param, tokens, pair => PrepareExpressionPair(pair), comparator);
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
