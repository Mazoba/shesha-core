using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NHibernate;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.JsonLogic;
using Shesha.Metadata;
using Shesha.Utilities;
using Shesha.Web.DataTable;

namespace Shesha.Web.Autocomplete
{
    [AbpAuthorize()]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AutocompleteController: ControllerBase, ITransientDependency
    {
        private readonly IEntityConfigurationStore _configurationStore;
        private readonly IIocResolver _iocResolver;
        private readonly IJsonLogic2HqlConverter _jsonLogic2HqlConverter;
        private readonly IMetadataProvider _metadataProvider;

        public AutocompleteController(IEntityConfigurationStore configurationStore, IIocResolver iocResolver, IJsonLogic2HqlConverter jsonLogic2HqlConverter, IMetadataProvider metadataProvider)
        {
            _configurationStore = configurationStore;
            _iocResolver = iocResolver;
            _jsonLogic2HqlConverter = jsonLogic2HqlConverter;
            _metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Get autocomplete list
        /// </summary>
        /// <param name="term"></param>
        /// <param name="typeShortAlias"></param>
        /// <param name="allowInherited"></param>
        /// <param name="selectedValue"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<AutocompleteItemDto>> ListAsync(string term, string typeShortAlias, bool allowInherited, Guid? selectedValue, string filter, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(typeShortAlias))
                    return new List<AutocompleteItemDto>();

                var entityConfig = _configurationStore.Get(typeShortAlias);

                if (entityConfig.DisplayNamePropertyInfo == null)
                    throw new Exception($"EntityDisplayName not found for entity of type `{entityConfig.EntityType.FullName}`, one of properties should be marked with the `EntityDisplayNameAttribute` to use generic autocomplete");

                var method = this.GetType().GetMethod(nameof(GetList));
                if (method == null)
                    throw new Exception($"{nameof(GetList)} not found");

                var idProp = entityConfig.EntityType.GetProperty("Id");
                if (idProp == null)
                    throw new Exception("Id property not found");

                var genericMethod = method.MakeGenericMethod(entityConfig.EntityType, idProp.PropertyType);

                var task = (Task)genericMethod.Invoke(this, new object[] { term, allowInherited, selectedValue, filter, cancellationToken });
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty == null)
                    throw new Exception("Result property not found");

                return resultProperty.GetValue(task) as List<AutocompleteItemDto>;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Get autocomplete list
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TPrimaryKey"></typeparam>
        /// <param name="term"></param>
        /// <param name="allowInherited"></param>
        /// <param name="selectedValue"></param>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<AutocompleteItemDto>> GetList<TEntity, TPrimaryKey>(string term, bool allowInherited, Guid? selectedValue, string filter, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var sessionFactory = _iocResolver.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();

            var entityConfig = _configurationStore.Get(typeof(TEntity));
            var displayProperty = entityConfig.DisplayNamePropertyInfo.Name;

            var termLength = (term ?? "").Length;

            var hql = $"select Id, {displayProperty} as Name, length({displayProperty}) - {termLength} as Ranking from {typeof(TEntity).FullName} ent ";

            var filterCriteria = new FilterCriteria(FilterCriteria.FilterMethod.Hql);
            AppendFilterCriteria<TEntity>(filterCriteria, filter);

            if (!string.IsNullOrWhiteSpace(term)) 
                filterCriteria.AddParameterisedCriterion($"lower(ent.{displayProperty}) like {{0}}", $"%{term.ToLower()}%");

            if (!string.IsNullOrWhiteSpace(entityConfig.DiscriminatorValue) && !allowInherited)
                filterCriteria.AddParameterisedCriterion($"ent.class={{0}}", entityConfig.DiscriminatorValue.Trim('\''));

            var sb = new StringBuilder();
            if (filterCriteria.FilterClauses.Any())
            {
                foreach (var filterClause in filterCriteria.FilterClauses)
                {
                    if (sb.Length > 0)
                        sb.Append(" and ");

                    sb.Append("(");
                    sb.Append(filterClause);
                    sb.Append(")");
                }
            }

            if (selectedValue != null)
            {
                if (sb.Length > 0)
                    sb.Append(" or ");

                sb.Append(" ent.Id = :id");
            }

            if (sb.Length > 0)
                hql += " where " + sb.ToString();

            hql += " order by Ranking, Name";

            var q = session.CreateQuery(hql);
            q.SetMaxResults(10);

            // transfer parameters
            Shesha.NHibernate.Session.SessionExtensions.TransferHqlParameters(q, filterCriteria);

            if (selectedValue != null)
                q.SetParameter("id", selectedValue);

            var data = await q.ListAsync<object[]>(cancellationToken);

            return data.Where(i => i[1] != null).Select(i => new AutocompleteItemDto
            {
                Value = i[0].ToString(), 
                DisplayText = i[1].ToString()
            }).ToList();
        }

        private void AppendFilterCriteria<TEntity>(FilterCriteria filterCriteria, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return;
            
            // JsonLogic is converted to HQL
            var jsonLogic = JObject.Parse(filter);

            // convert json logic to HQL
            var context = new JsonLogic2HqlConverterContext();

            var properties = _metadataProvider.GetProperties(typeof(TEntity));

            DataTableHelper.FillVariablesResolvers(properties, context);
            DataTableHelper.FillContextMetadata(properties, context);

            var hql = _jsonLogic2HqlConverter.Convert(jsonLogic, context);

            filterCriteria.FilterClauses.Add(hql);
            foreach (var parameter in context.FilterParameters)
            {
                filterCriteria.FilterParameters.Add(parameter.Key, parameter.Value);
            }
        }
    }
}