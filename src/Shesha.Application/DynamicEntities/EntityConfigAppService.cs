using Abp.Application.Services;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using Shesha.Web.DataTable;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.DynamicEntities
{
    /// inheritedDoc
    public class EntityConfigAppService : AsyncCrudAppService<EntityConfig, EntityConfigDto, Guid>, IEntityConfigAppService
    {
        public EntityConfigAppService(IRepository<EntityConfig, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<EntityConfig, Guid>("EntityConfigs_Index");

            table.AddProperty(e => e.Namespace);
            table.AddProperty(e => e.ClassName);
            table.AddProperty(e => e.FriendlyName);
            table.AddProperty(e => e.TypeShortAlias);
            table.AddProperty(e => e.TableName);
            table.AddProperty(e => e.DiscriminatorValue);
            table.AddProperty(e => e.Source);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            //table.OnRequestToFilterStatic = (criteria, input) =>
            //{
            //    criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            //};

            return table;
        }
    }
}
