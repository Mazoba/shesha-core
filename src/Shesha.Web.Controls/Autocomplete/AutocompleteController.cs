using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;
using Shesha.Utilities;

namespace Shesha.Web.Autocomplete
{
    [AbpAuthorize()]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AutocompleteController: ControllerBase, ITransientDependency
    {
        private readonly IEntityConfigurationStore _configurationStore;
        private readonly IIocResolver _iocResolver;


        public AutocompleteController(IEntityConfigurationStore configurationStore, IIocResolver iocResolver)
        {
            _configurationStore = configurationStore;
            _iocResolver = iocResolver;
        }

        /// <summary>
        /// Get autocomplete list
        /// </summary>
        /// <param name="term"></param>
        /// <param name="typeShortAlias"></param>
        /// <param name="allowInherited"></param>
        /// <param name="selectedValue"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<AutocompleteItemDto>> ListAsync(string term, string typeShortAlias, bool allowInherited, Guid? selectedValue, CancellationToken cancellationToken)
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

                var task = (Task)genericMethod.Invoke(this, new object[] { term, allowInherited, selectedValue, cancellationToken });
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<AutocompleteItemDto>> GetList<TEntity, TPrimaryKey>(string term, bool allowInherited, Guid? selectedValue, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var sessionFactory = _iocResolver.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();

            var entityConfig = _configurationStore.Get(typeof(TEntity));
            var displayProperty = entityConfig.DisplayNamePropertyInfo.Name;

            var termLength = (term ?? "").Length;
            var hql = $"select Id, {displayProperty} as Name, length({displayProperty}) - {termLength} as Ranking from {typeof(TEntity).FullName} ent ";
            var conditions = new List<string>();
            if (!string.IsNullOrWhiteSpace(term))
                conditions.Add($"lower(ent.{displayProperty}) like '%{term.ToLower()}%'");

            var conditionsHql = conditions.Delimited(" and ");
            if (!string.IsNullOrWhiteSpace(conditionsHql))
                conditionsHql = "(" + conditionsHql + ")";
            
            if (selectedValue != null)
            {
                if (!string.IsNullOrWhiteSpace(conditionsHql))
                    conditionsHql += " or ";
                conditionsHql += " ent.Id = :id";
            }

            if (!string.IsNullOrWhiteSpace(conditionsHql))
                conditionsHql = "(" + conditionsHql + ")";

            if (!string.IsNullOrWhiteSpace(entityConfig.DiscriminatorValue) && !allowInherited)
            {
                if (!string.IsNullOrWhiteSpace(conditionsHql))
                    conditionsHql += " and ";

                conditionsHql += $"ent.class='{entityConfig.DiscriminatorValue.Trim('\'')}'";
            }

            if (!string.IsNullOrWhiteSpace(conditionsHql))
                hql += " where " + conditionsHql;

            hql += " order by Ranking, Name";

            var q = session.CreateQuery(hql);
            q.SetMaxResults(10);

            if (selectedValue != null)
                q.SetParameter("id", selectedValue);

            var data = await q.ListAsync<object[]>(cancellationToken);

            return data.Where(i => i[1] != null).Select(i => new AutocompleteItemDto
            {
                Value = i[0].ToString(), 
                DisplayText = i[1].ToString()
            }).ToList();
        }
    }
}