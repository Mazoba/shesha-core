using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonLogic.Net;
using Newtonsoft.Json.Linq;
using Shesha.Configuration.Runtime;
using Shesha.Domain.Attributes;
using Shesha.Metadata;
using Shesha.Utilities;

namespace Shesha.JsonLogic
{
    /// <summary>
    /// JsonLogic operators
    /// </summary>
    public class LinqOperators
    {
        private Dictionary<string, Action<IJsonLogic2LinqConverter, JToken[], JsonLogic2LinqConverterContext>> registry;

        public static LinqOperators Default { get; } = new LinqOperators();

        public LinqOperators()
        {
            registry = new Dictionary<string, Action<IJsonLogic2LinqConverter, JToken[], JsonLogic2LinqConverterContext>>();
            AddDefaultOperations();
        }

        public void AddOperator(string name, Action<IJsonLogic2LinqConverter, JToken[], JsonLogic2LinqConverterContext> operation)
        {
            registry[name] = operation;
        }

        public void DeleteOperator(string name)
        {
            registry.Remove(name);
        }

        public Action<IJsonLogic2LinqConverter, JToken[], JsonLogic2LinqConverterContext> GetOperator(string name)
        {
            return registry[name];
        }

        public static bool IsAny<T>(params object[] subjects)
        {
            return subjects.Any(x => x != null && x is T);
        }

        private void AddDefaultOperations()
        {
            AddOperator("==", (p, args, data) =>
            {
                // convert arguments and find enity property
                //data.Filter.By()
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" = ");
            });

            AddOperator("===", GetOperator("=="));

            AddOperator("!=", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" <> ");
            });

            AddOperator("!==", GetOperator("!="));

            AddOperator("<", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" < ");
            });

            AddOperator("<=", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" <= ");
            });

            AddOperator(">", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" > ");
            });

            AddOperator(">=", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data).Delimited(" >= ");
            });

            AddOperator("var", (p, args, data) =>
            {
                throw new NotImplementedException();

                //const string prefix = "ent";

                //if (!args.Any()) return
                //    prefix;

                //var name = GetStringValue(args.First());
                //name = p.ResolveVariable(name, data);

                //if (name == null)
                //    return prefix;

                //// handle entity references
                //// todo: implement support of nested entities
                //var dataType = data.FieldsMetadata.TryGetValue(name, out var meta) ? meta.DataType : null;
                //if (dataType == DataTypes.EntityReference)
                //{
                //    name = $"{name}.Id";
                //}

                //return $"{prefix}.{name}";

                /*
                try
                {
                    var result = GetValueByName(data, names.ToString());
                    // This will return JValue or null if missing. Actual value of null will be wrapped in JToken with value null
                    if (result is JValue)
                    {
                        // permit correct type wrangling to occur (AdjustType) without duplicating code
                        result = p.Apply((JValue)result, null);

                    }
                    else if (result == null && args.Count() == 2)
                    {
                        object defaultValue = p.Apply(args.Last(), data);
                        result = defaultValue;
                    }

                    return result;
                }
                catch
                {
                    object defaultValue = (args.Count() == 2) ? p.Apply(args.Last(), data) : null;
                    return defaultValue;
                }
                */
            });

            AddOperator("and", (p, args, data) => {
                throw new NotImplementedException(); 
                //return ConvertArguments(p, args, data, true).Delimited(" and ");
            });

            AddOperator("or", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return ConvertArguments(p, args, data, true).Delimited(" or ");
            });

            AddOperator("!!", (p, args, data) =>
            {
                throw new NotImplementedException();
                //return $"{p.Convert(args.First(), data)} is not null";
            });

            AddOperator("!", (p, args, data) =>
            {
                throw new NotImplementedException();
                

                // special handling of single string argument
                //var argument = args.First();
                //var convertedArgument = p.Convert(argument, data);

                //var nestedOperation = p.GetOperation(argument);
                //if (nestedOperation.Operation == "var")
                //{
                //    // check datatype of the argument and add " or trim({convertedArgument}) = ''" for strings
                //    return $"{convertedArgument} is null";
                //}

                //return $"not ({convertedArgument})";
            });

            AddOperator("not", GetOperator("!"));

            AddOperator("in", (p, args, data) => {
                throw new NotImplementedException();
                
                //var first = p.Convert(args[0], data);
                //var second = p.Convert(args[1], data);

                //var isArray = args[1] is JArray;
                //if (isArray)
                //{
                //    return $"{first} in ({second})";
                //}
                //else
                //{
                //    // process as string
                //    return $"{second} like '%' + {first.Trim('\'')} + '%'";
                //}
            });

            AddOperator("startsWith", (p, args, data) => {
                throw new NotImplementedException();
                
                //var first = p.Convert(args[0], data);
                //var second = p.Convert(args[1], data);

                //return $"{first} like {second.Trim('\'')} + '%'";
            });

            AddOperator("endsWith", (p, args, data) => {
                throw new NotImplementedException();
                
                //var first = p.Convert(args[0], data);
                //var second = p.Convert(args[1], data);

                //return $"{first} like '%' + {second.Trim('\'')}";
            });

        }

        private object GetValueByName(object data, string namePath)
        {
            if (string.IsNullOrEmpty(namePath)) return data;

            if (data == null) throw new ArgumentNullException(nameof(data));

            string[] names = namePath.Split('.');
            object d = data;
            foreach (string name in names)
            {
                if (d == null) return null;
                if (d.GetType().IsArray)
                {
                    d = (d as Array).GetValue(int.Parse(name));
                }
                else if (DictionaryType(d) != null)
                {
                    var type = DictionaryType(d);
                    var prop = type.GetTypeInfo().DeclaredProperties.FirstOrDefault(p => p.Name == "Item");
                    d = prop.GetValue(d, new object[] { name });
                }
                else if (d is IEnumerable<object>)
                {
                    d = (d as IEnumerable<object>).Skip(int.Parse(name)).First();
                }
                else
                {
                    var property = d.GetType().GetTypeInfo().GetDeclaredProperty(name);
                    if (property == null) throw new Exception();
                    d = property.GetValue(d);
                }
            }
            return d;
        }

        private Type DictionaryType(object d)
        {
            return d.GetType().GetTypeInfo().ImplementedInterfaces.FirstOrDefault(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        private string GetStringValue(JToken arg)
        {
            if (arg == null)
                return null;

            if (arg is JValue value && value.Value is string stringValue)
                return stringValue;

            throw new NotSupportedException("Not supported value type");
        }

        private List<string> ConvertArguments(IJsonLogic2LinqConverter converter, JToken[] args, JsonLogic2LinqConverterContext context, bool wrap = false)
        {
            /*
            return args.Select(a =>
            {
                var arg = converter.Convert(a, context);
                return wrap
                    ? $"({arg})"
                    : arg;
            })
                .ToList();
            */
            throw new NotImplementedException();
        }
    }
}
