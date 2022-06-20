using Abp.Dependency;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.QuickSearch
{
    public class QuickSearcher: IQuickSearcher, ITransientDependency
    {
        /// <summary>
        /// Get quick search expression for the specified entity type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="quickSearch">Quick search text</param>
        /// <returns></returns>
        public Expression<Func<T, bool>> GetQuickSearchExpression<T>(string quickSearch, ParameterExpression parameter) 
        {
            if (string.IsNullOrWhiteSpace(quickSearch))
                return null;

            var subExpressions = new List<Expression<Func<T, bool>>>();
            /*
            // get list of properties existing in the table configuration
            var props = GetPropertiesForSqlQuickSearch(rowType, columns, cacheKey);

            if (!props.Any())
                return Expression.Lambda<Func<T, bool>>(Expression.IsTrue(Expression.Constant(false)), parameter);

            var addSubQuery = new Action<string, object>((q, v) =>
            {
                var queryParamName = "p" + filterCriteria.FilterParameters.Count.ToString();
                var criteria = string.Format((string)q, ":" + queryParamName);
                subQueries.Add(criteria);

                filterCriteria.FilterParameters.Add(queryParamName, v);
            });

            foreach (var prop in props)
            {
                switch (prop.DataType)
                {
                    case GeneralDataType.Text:
                        {
                            if (!prop.Name.Contains('.'))
                            {
                                addSubQuery($"ent.{prop.Name} like {{0}}", "%" + sSearch + "%");
                            }
                            else
                            {
                                // use `exists` for nested entities because NH uses inner joins
                                var nestedEntity = prop.Name.LeftPart('.', ProcessDirection.RightToLeft);
                                var nestedProp = prop.Name.RightPart('.', ProcessDirection.RightToLeft);

                                addSubQuery($@"exists (from ent.{nestedEntity} where {nestedProp} like {{0}})", "%" + sSearch + "%");
                            }
                            break;
                        }
                    case GeneralDataType.EntityReference:
                        {
                            var nestedProperty = GetNestedProperty(rowType, prop.Name);
                            if (nestedProperty != null)
                            {
                                var nestedEntityConfig = _entityConfigurationStore.Get(nestedProperty.PropertyType);
                                if (nestedEntityConfig.DisplayNamePropertyInfo != null)
                                {
                                    var nestedPropertyDisplayName = nestedEntityConfig.DisplayNamePropertyInfo.Name;

                                    addSubQuery($@"exists (from ent.{prop.Name} where {nestedPropertyDisplayName} like {{0}})", "%" + sSearch + "%");
                                }
                            }

                            break;
                        }
                    case GeneralDataType.ReferenceList:
                        {
                            if (!string.IsNullOrWhiteSpace(prop.ReferenceListNamespace) && !string.IsNullOrWhiteSpace(prop.ReferenceListName))
                            {
                                addSubQuery($@"exists (select 1 from {nameof(ReferenceListItem)} item where item.{nameof(ReferenceListItem.ItemValue)} = ent.{prop.Name} and item.{nameof(ReferenceListItem.Item)} like {{0}} and item.ReferenceList.Namespace = '{prop.ReferenceListNamespace}' and item.ReferenceList.Name = '{prop.ReferenceListName}')", "%" + sSearch + "%");
                            }
                            break;
                        }
                    case GeneralDataType.MultiValueReferenceList:
                        {
                            if (!string.IsNullOrWhiteSpace(prop.ReferenceListNamespace) && !string.IsNullOrWhiteSpace(prop.ReferenceListName))
                            {
                                addSubQuery($@"exists (select 1 from {nameof(ReferenceListItem)} item where (item.{nameof(ReferenceListItem.ItemValue)} & ent.{prop.Name}) > 0 and item.{nameof(ReferenceListItem.Item)} like {{0}} and item.ReferenceList.Namespace = '{prop.ReferenceListNamespace}' and item.ReferenceList.Name = '{prop.ReferenceListName}')", "%" + sSearch + "%");
                            }
                            break;
                        }
                }
            }
            */
            if (!subExpressions.Any())
                return null;

            return CombineExpressions(subExpressions);
        }

        private Expression<Func<T, bool>> CombineExpressions<T>(List<Expression<Func<T, bool>>> expressions) 
        {
            Expression<Func<T, bool>> acc = null;

            var a = Expression<Func<T, bool>>.OrElse(acc, acc);

            Expression<Func<T, bool>> bind(Expression<Func<T, bool>> acc, Expression<Func<T, bool>> right) => acc == null ? right : acc.OrElse(right);

            foreach (var expression in expressions) 
            {
                acc = bind(acc, expression);
            }
            return acc;
        }

        //public Expression AppendQuickSearchCriteria<TEntity>(string quickSearch)
        public IQueryable<TEntity> ApplyQuickSearch<TEntity>(IQueryable<TEntity> query, string sorting) 
        {
            throw new NotImplementedException();
        }

        /*
        /// <summary>
        /// Returns a list of properties for the SQL quick search
        /// </summary>
        private List<QuickSearchPropertyInfo> GetPropertiesForSqlQuickSearch<TEntity>(List<DataTableColumn> columns, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                return DoGetPropertiesForSqlQuickSearch(rowType, columns);

            var cacheManager = StaticContext.IocManager.Resolve<ICacheManager>();

            return cacheManager
                .GetCache<string, List<QuickSearchPropertyInfo>>("SqlQuickSearchCache")
                .Get(cacheKey, (s) => DoGetPropertiesForSqlQuickSearch(rowType, columns));
        }
        */
    }
}
