using System;
using Abp.Application.Services;
using Abp.Dependency;
using Shesha.Domain;
using Shesha.Web.DataTable;

namespace Shesha.Tests.Datatables
{
    public class DataTableTestAppService: ApplicationService, ITransientDependency
    {
        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig TestTable()
        {
            var table = new DataTableConfig<Person, Guid>("InternalPersonsTest_Index");

            table.AddProperty(e => e.User.UserName, c => c.Caption("Username").SortAscending());
            table.AddProperty(e => e.FirstName);
            table.AddProperty(e => e.LastName);
            table.AddProperty(e => e.EmailAddress1);
            table.AddProperty(e => e.MobileNumber1);
            table.AddProperty(e => e.TypeOfAccount);
            table.AddProperty(e => e.AreaLevel1, c => c.Caption("Area1"));
            table.AddProperty(e => e.User.LastLoginDate, c => c.Caption("Last log in"));
            table.AddProperty(e => e.IsLocked);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));
            table.AddProperty(e => e.IsContractor);

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }
    }
}
