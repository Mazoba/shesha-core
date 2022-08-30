using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Newtonsoft.Json.Linq;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.DynamicEntities;
using Shesha.Metadata;
using Shesha.NHibernate.UoW;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.DynamicEntities
{
    public class EntityModelBinder_Tests : SheshaNhTestBase
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IEntityModelBinder _entityModelBinder;
        private readonly IRepository<Person, Guid> _personRepo;

        public EntityModelBinder_Tests()
        {
            _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
            _entityModelBinder = Resolve<IEntityModelBinder>();
            _personRepo = Resolve<IRepository<Person, Guid>>();
        }

        [Fact]
        public async Task CascadeRuleEntityFinder_Test()
        {
            LoginAsHostAdmin();
            using (var uow = _unitOfWorkManager.Begin())
            {
                var nhuow = uow as NhUnitOfWork;

                var ent = new Person() { FirstName = "TestPerson", LastName = "TestPerson", DateOfBirth = new DateTime(1978, 09, 24, 13, 24, 17) };

                _personRepo.Insert(ent);
                await nhuow.SaveChangesAsync();

                var checkEnt = new Person() { FirstName = "TestPerson", LastName = "TestPerson", DateOfBirth = new DateTime(1978, 09, 24) };

                var f = new Finder();
                var newEnt = f.FindEntity(new CascadeRuleEntityFinderInfo(checkEnt) { _Repository = _personRepo });
                Assert.NotNull(newEnt);

                checkEnt.FirstName = "TestPersonNew";
                newEnt = f.FindEntity(new CascadeRuleEntityFinderInfo(checkEnt) { _Repository = _personRepo });
                Assert.Null(newEnt);

                _personRepo.HardDelete(ent);
                await nhuow.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CascadeCreateUpdateAndRules_Test()
        {
            LoginAsHostAdmin();

            var repoOrg = Resolve<IRepository<Organisation, Guid>>();
            var testOrgRepo = Resolve<IRepository<TestOrganisationAllowContactUpdate, Guid>>();

            using (var uow = _unitOfWorkManager.Begin())
            {
                var nhuow = uow as NhUnitOfWork;
                Person newTestPerson1 = null;
                Person newTestPerson2 = null;
                TestOrganisationAllowContactUpdate newTestOrg1 = null;
                TestOrganisationAllowContactUpdate newTestOrg2 = null;
                TestOrganisationAllowContactUpdate newTestOrg3 = null;
                TestOrganisationAllowContactUpdate newTestOrg4 = null;
                TestOrganisationAllowContactUpdate newTestOrg5 = null;
                TestOrganisationAllowContactUpdate newTestOrg6 = null;
                TestOrganisationAllowContactUpdate newTestOrg7 = null;
                TestOrganisationAllowContactUpdate newTestOrg8 = null;

                try
                {
                    // Child creation is not allowed
                    var errors = new List<ValidationResult>();
                    var newOrg = new Organisation();
                    var json1 = @"{ 'name': 'TestOrganisation', 'primaryContact': { 'firstName': 'TestPerson' } }";
                    var jObject1 = JObject.Parse(json1);
                    var result = _entityModelBinder.BindProperties(jObject1, newOrg, errors);
                    Assert.False(result);

                    // Child creation is allowed and success
                    var testErrors1 = new List<ValidationResult>();
                    newTestOrg1 = new TestOrganisationAllowContactUpdate();
                    var testResult1 = _entityModelBinder.BindProperties(jObject1, newTestOrg1, testErrors1);
                    Assert.True(testResult1);
                    testOrgRepo.Insert(newTestOrg1);
                    await nhuow.SaveChangesAsync();
                    newTestOrg1 = testOrgRepo.GetAll().FirstOrDefault(x => x.Name == "TestOrganisation");
                    newTestPerson1 = _personRepo.GetAll().FirstOrDefault(x => x.FirstName == "TestPerson");

                    // Child creation is allowed but fail due to empty FirstName
                    var testErrors2 = new List<ValidationResult>();
                    newTestOrg2 = new TestOrganisationAllowContactUpdate();
                    var json2 = @"{ 'name': 'TestOrganisation', 'primaryContact': { 'lastName': 'TestPerson' } }";
                    var jObject2 = JObject.Parse(json2);
                    var testResult2 = _entityModelBinder.BindProperties(jObject2, newTestOrg2, testErrors2);
                    Assert.False(testResult2);

                    // Child creation is allowed and choosing by FirstName
                    var testErrors3 = new List<ValidationResult>();
                    newTestOrg3 = new TestOrganisationAllowContactUpdate();
                    var json3 = @"{ 'name': 'TestOrganisation2', 'primaryContact': { 'firstName': 'TestPerson' } }";
                    var jObject3 = JObject.Parse(json3);
                    var testResult3 = _entityModelBinder.BindProperties(jObject3, newTestOrg3, testErrors3);
                    Assert.True(testResult3);
                    Assert.True(newTestPerson1?.Id == newTestOrg1.PrimaryContact.Id);

                    // Child creation is allowed and choosing by FirstName with updating child LastName
                    var lastName = _personRepo.GetAll().FirstOrDefault(x => x.FirstName == "TestPerson")?.LastName;
                    var testErrors4 = new List<ValidationResult>();
                    newTestOrg4 = new TestOrganisationAllowContactUpdate();
                    var json4 = @"{ 'name': 'TestOrganisation3', 'primaryContact': { 'firstName': 'TestPerson', 'lastName': 'TestLastName' } }";
                    var jObject4 = JObject.Parse(json4);
                    var testResult4 = _entityModelBinder.BindProperties(jObject4, newTestOrg4, testErrors4);
                    Assert.True(testResult4);
                    Assert.True(newTestOrg4.PrimaryContact.LastName == "TestLastName");
                    Assert.True(newTestOrg4.PrimaryContact.LastName != lastName);
                    await nhuow.SaveChangesAsync();
                    newTestPerson1 = _personRepo.GetAll().FirstOrDefault(x => x.FirstName == "TestPerson");

                    // Create test person 2
                    newTestPerson2 = new Person() { FirstName = "TestPerson2" };
                    _personRepo.Insert(newTestPerson2);
                    nhuow.SaveChanges();
                    newTestPerson2 = _personRepo.GetAll().FirstOrDefault(x => x.FirstName == "TestPerson2");

                    // Change child by Id
                    var testErrors5 = new List<ValidationResult>();
                    var json5 = @$"{{ 'id': '{newTestOrg1.Id}', 'name': 'TestOrganisation1', 'primaryContact': {{ 'id': '{newTestPerson2.Id}' }} }}";
                    var jObject5 = JObject.Parse(json5);
                    newTestOrg5 = testOrgRepo.Get(newTestOrg1.Id);
                    var testResult5 = _entityModelBinder.BindProperties(jObject5, newTestOrg5, testErrors5);
                    Assert.True(testResult5);
                    await nhuow.SaveChangesAsync();
                    newTestOrg5 = testOrgRepo.Get(newTestOrg1.Id);
                    Assert.True(newTestOrg5.Name == "TestOrganisation1");
                    Assert.True(newTestOrg5.PrimaryContact?.FirstName == "TestPerson2");

                    // Edit child with Id
                    var testErrors6 = new List<ValidationResult>();
                    var json6 = @$"{{ 'id': '{newTestOrg1.Id}', 'primaryContact': {{ 'id': '{newTestPerson2.Id}', 'lastName': 'TestLastName2' }} }}";
                    var jObject6 = JObject.Parse(json6);
                    newTestOrg6 = testOrgRepo.Get(newTestOrg1.Id);
                    var testResult6 = _entityModelBinder.BindProperties(jObject6, newTestOrg6, testErrors6);
                    Assert.True(testResult6);
                    await nhuow.SaveChangesAsync();
                    newTestOrg6 = testOrgRepo.Get(newTestOrg1.Id);
                    Assert.True(newTestOrg6.Name == "TestOrganisation1");
                    Assert.True(newTestOrg6.PrimaryContact?.Id == newTestPerson2.Id && newTestOrg6.PrimaryContact?.LastName == "TestLastName2");

                    // Change child by Id and edit
                    var testErrors7 = new List<ValidationResult>();
                    var json7 = @$"{{ 'id': '{newTestOrg1.Id}', 'primaryContact': {{ 'id': '{newTestPerson1.Id}', 'firstName': 'TestPerson1', 'lastName': 'TestLastName1' }} }}";
                    var jObject7 = JObject.Parse(json7);
                    newTestOrg7 = testOrgRepo.Get(newTestOrg1.Id);
                    var testResult7 = _entityModelBinder.BindProperties(jObject7, newTestOrg7, testErrors7);
                    Assert.True(testResult7);
                    await nhuow.SaveChangesAsync();
                    newTestOrg7 = testOrgRepo.Get(newTestOrg1.Id);
                    Assert.True(newTestOrg7.Name == "TestOrganisation1");
                    Assert.True(newTestOrg7.PrimaryContact?.Id == newTestPerson1.Id && newTestOrg7.PrimaryContact?.FirstName == "TestPerson1" && newTestOrg7.PrimaryContact?.LastName == "TestLastName1");

                    // Change child by Id short notation
                    var testErrors8 = new List<ValidationResult>();
                    var json8 = @$"{{ 'id': '{newTestOrg1.Id}', 'primaryContactId': '{newTestPerson2.Id}' }}";
                    var jObject8 = JObject.Parse(json8);
                    newTestOrg8 = testOrgRepo.Get(newTestOrg1.Id);
                    var testResult8 = _entityModelBinder.BindProperties(jObject5, newTestOrg5, testErrors5);
                    Assert.True(testResult8);
                    await nhuow.SaveChangesAsync();
                    newTestOrg8 = testOrgRepo.Get(newTestOrg1.Id);
                    Assert.True(newTestOrg8.Name == "TestOrganisation1");
                    Assert.True(newTestOrg8.PrimaryContact?.Id == newTestPerson2.Id && newTestOrg8.PrimaryContact?.LastName == "TestLastName2");

                }
                finally
                {
                    if (newTestOrg1 != null) testOrgRepo.HardDelete(newTestOrg1);
                    if (newTestOrg2 != null) testOrgRepo.HardDelete(newTestOrg2);
                    if (newTestOrg3 != null) testOrgRepo.HardDelete(newTestOrg3);
                    if (newTestOrg4 != null) testOrgRepo.HardDelete(newTestOrg4);
                    if (newTestOrg5 != null) testOrgRepo.HardDelete(newTestOrg5);
                    if (newTestOrg6 != null) testOrgRepo.HardDelete(newTestOrg6);
                    if (newTestOrg7 != null) testOrgRepo.HardDelete(newTestOrg7);
                    if (newTestOrg8 != null) testOrgRepo.HardDelete(newTestOrg8);
                    if (newTestPerson1 != null) _personRepo.HardDelete(newTestPerson1);
                    if (newTestPerson2 != null) _personRepo.HardDelete(newTestPerson2);
                    await nhuow.SaveChangesAsync();
                }
            }
        }
    }

    public class Finder : CascadeRuleEntityFinderBase<Person, Guid>
    {
        public override Person FindEntity(CascadeRuleEntityFinderInfo<Person, Guid> info)
        {
            var p = info.NewObject;

            if (string.IsNullOrEmpty(p.FirstName)) throw new Exception($"`{nameof(Person.FirstName)}` is mandatory");
            if (string.IsNullOrEmpty(p.LastName)) throw new Exception($"`{nameof(Person.LastName)}` is mandatory");
            if (p.DateOfBirth == null) throw new Exception($"`{nameof(Person.DateOfBirth)}` is mandatory");
            var sd = p.DateOfBirth?.Date;
            var ed = sd?.AddDays(1);
            return info.Repository.GetAll().FirstOrDefault(x => x.FirstName == p.FirstName && x.LastName == p.LastName && x.DateOfBirth > sd && x.DateOfBirth < ed);
        }
    }

    public class SimplyFinder : CascadeRuleEntityFinderBase<Person, Guid>
    {
        
        public override Person FindEntity(CascadeRuleEntityFinderInfo<Person, Guid> info)
        {
            var p = info.NewObject;

            if (string.IsNullOrEmpty(p.FirstName)) throw new CascadeUpdateRuleException($"`{nameof(Person.FirstName)}` is mandatory");
            return info.Repository.GetAll().FirstOrDefault(x => x.FirstName == p.FirstName);
        }
    }

    [DiscriminatorValue("Test.Organisaion")]
    public class TestOrganisationAllowContactUpdate : Organisation
    {
        [CascadeUpdateRules(true, true, true, typeof(SimplyFinder))]
        public override Person PrimaryContact { get; set; }
    }
}
