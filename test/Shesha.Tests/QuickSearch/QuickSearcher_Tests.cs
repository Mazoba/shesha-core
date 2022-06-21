using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;
using Shesha.QuickSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.QuickSearch
{
    /// <summary>
    /// Tests for <see cref="QuickSearcher"/>
    /// </summary>
    public class QuickSearcher_Tests : SheshaNhTestBase
    {
        [Fact]
        public async Task SearchPerson_TextFields_Convert_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<Person>("test", new List<string> {
                nameof(Person.FirstName),
                nameof(Person.LastName),
                $"{nameof(Person.User)}.{nameof(Person.User.UserName)}"
            });

            Assert.Equal(@"ent => ((ent.FirstName.Contains(""test"") OrElse ent.LastName.Contains(""test"")) OrElse ent.User.UserName.Contains(""test""))", expression.ToString());
        }

        [Fact]
        public async Task SearchPerson_TextFields_Fetch_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<Person>("test", new List<string> {
                nameof(Person.FirstName),
                nameof(Person.LastName),
                $"{nameof(Person.User)}.{nameof(Person.User.UserName)}"
            });

            await TryFetchData<Person, Guid>(query => query.Where(expression), data => {
                // check data
            });
        }

        [Fact]
        public async Task SearchTestPerson_TextFields_Convert_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<TestPerson>("test", new List<string> {
                nameof(TestPerson.FirstName),
                nameof(TestPerson.LastName),
                $"{nameof(TestPerson.Organisation)}.{nameof(TestPerson.Organisation.Name)}"
            });

            Assert.Equal(@"ent => ((ent.FirstName.Contains(""test"") OrElse ent.LastName.Contains(""test"")) OrElse ent.Organisation.Name.Contains(""test""))", expression.ToString());
        }

        [Fact]
        public async Task SearchTestPerson_TextFields_NestedEntity_Convert_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<TestPerson>("test", new List<string> {
                nameof(TestPerson.FirstName),
                nameof(TestPerson.LastName),
                nameof(TestPerson.Organisation)
            });

            Assert.Equal(@"ent => ((ent.FirstName.Contains(""test"") OrElse ent.LastName.Contains(""test"")) OrElse ent.Organisation.Name.Contains(""test""))", expression.ToString());
        }

        [Fact]
        public async Task SearchTestPerson_RefList_Convert_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<TestPerson>("mrs", new List<string> {
                nameof(TestPerson.Title)
            });

            Assert.Equal(@"ent => value(NHibernate.Linq.NhQueryable`1[Shesha.Domain.ReferenceListItem]).Any(entTitle => ((((entTitle.ReferenceList.Namespace == ""Shesha.Core"") AndAlso (entTitle.ReferenceList.Name == ""PersonTitles"")) AndAlso (Convert(ent.Title, Nullable`1) == Convert(entTitle.ItemValue, Nullable`1))) AndAlso entTitle.Item.Contains(""mrs"")))", expression.ToString());
        }

        [Fact]
        public async Task SearchTestPerson_RefList_Fetch_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<Person>("mrs", new List<string> {
                nameof(TestPerson.Title)
            });

            await TryFetchData<Person, Guid>(query => query.Where(expression), data => {
                // check data
            });
        }

        [Fact]
        public async Task SearchTest_Organisation_MultiValueRefList_Convert_Test()
        {
            var quickSearcher = Resolve<QuickSearcher>();

            var expression = quickSearcher.GetQuickSearchExpression<TestOrganisation>("email", new List<string> {
                nameof(TestOrganisation.ContactMethods)
            });

            Assert.Equal(@"ent => value(NHibernate.Linq.NhQueryable`1[Shesha.Domain.ReferenceListItem]).Any(entContactMethods => ((((entContactMethods.ReferenceList.Namespace == ""Shesha.Core"") AndAlso (entContactMethods.ReferenceList.Name == ""PreferredContactMethod"")) AndAlso ((Convert(ent.ContactMethods, Nullable`1) & Convert(entContactMethods.ItemValue, Nullable`1)) > Convert(0, Nullable`1))) AndAlso entContactMethods.Item.Contains(""email"")))", expression.ToString());
        }

        #region private methods

        private async Task<List<T>> TryFetchData<T, TId>(Func<IQueryable<T>, IQueryable<T>> prepareQueryable = null, Action<List<T>> assertions = null) where T : class, IEntity<TId>
        {
            var repository = LocalIocManager.Resolve<IRepository<T, TId>>();
            var asyncExecuter = LocalIocManager.Resolve<IAsyncQueryableExecuter>();

            List<T> data = null;

            await WithUnitOfWorkAsync(async () => {
                var query = repository.GetAll();

                if (prepareQueryable != null)
                    query = prepareQueryable.Invoke(query);

                data = await asyncExecuter.ToListAsync(query);

                assertions?.Invoke(data);
            });

            return data;
        }

        #endregion
    }

    public class TestPerson: Entity<Guid>
    {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual RefListPersonTitle? Title { get; set; }
        public virtual TestOrganisation Organisation { get; set; }
        
        [ReferenceList("Shesha.Core", "PreferredContactMethod")]
        public virtual Int64? PreferredContactMethod { get; set; }
    }

    public class TestOrganisation : Entity<Guid>
    {
        //[EntityDisplayName]
        public virtual string Name { get; set; }
        public virtual string Address { get; set; }
        
        [MultiValueReferenceList("Shesha.Core", "PreferredContactMethod")]
        public virtual Int64? ContactMethods { get; set; }
    }
}
