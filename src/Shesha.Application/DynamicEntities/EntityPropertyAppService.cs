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
    public class EntityPropertyAppService : AsyncCrudAppService<EntityProperty, EntityPropertyDto, Guid>, IEntityPropertyAppService
    {
        public EntityPropertyAppService(IRepository<EntityProperty, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Mobile Devices index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = ChildDataTableConfig<EntityConfig, EntityProperty, Guid>.OneToMany("EntityConfig_Properties", i => i.EntityConfig);

            table.AddProperty(e => e.Name, c => c.SortAscending());
            table.AddProperty(e => e.Label);
            table.AddProperty(e => e.Description);
            table.AddProperty(e => e.DataType);
            table.AddProperty(e => e.Source);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On"));

            return table;
        }
    }
}
