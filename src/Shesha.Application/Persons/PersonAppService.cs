using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.Application.Services.Dto;
using Shesha.Authorization;
using Shesha.Authorization.Users;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.Web.DataTable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.Persons
{
    // http://localhost:21021/swagger/service:Person/swagger.json
    /// <summary>
    /// Person Application Service
    /// </summary>
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class PersonAppService : SheshaCrudServiceBase<Person, PersonAccountDto, Guid, FilteredPagedAndSortedResultRequestDto, CreatePersonAccountDto, PersonAccountDto>, IPersonAppService
    {
        private readonly UserManager _userManager;
        private readonly IRepository<ShaRoleAppointedPerson, Guid> _rolePersonRepository;

        public PersonAppService(IRepository<Person, Guid> repository, UserManager userManager, IRepository<ShaRoleAppointedPerson, Guid> rolePersonRepository) : base(repository)
        {
            _userManager = userManager;
            _rolePersonRepository = rolePersonRepository;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="repository"></param>
        public PersonAppService(IRepository<Person, Guid> repository) : base(repository)
        {

        }

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<Person, Guid>("Persons_Index");
            table.UseDtos = true;

            table.AddProperty(e => e.User.UserName, c => c.Caption("Username").SortAscending());
            table.AddProperty(e => e.FirstName);
            table.AddProperty(e => e.LastName);
            table.AddProperty(e => e.EmailAddress1);
            table.AddProperty(e => e.MobileNumber1);
            table.AddProperty(e => e.TypeOfAccount);
            table.AddProperty(e => e.PrimaryOrganisation, c => c.Caption("Service Provider"));
            table.AddProperty(e => e.User.LastLoginDate, c => c.Caption("Last log in"));
            table.AddProperty(e => e.IsLocked);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig TestTable()
        {
            var table = new DataTableConfig<Person, Guid>("PersonsTest_Index");

            table.AddProperty(e => e.User.UserName, c => c.Caption("Username").SortAscending());
            table.AddProperty(e => e.FirstName);
            table.AddProperty(e => e.LastName);
            table.AddProperty(e => e.EmailAddress1);
            table.AddProperty(e => e.MobileNumber1);
            table.AddProperty(e => e.TypeOfAccount);
            table.AddProperty(e => e.User.LastLoginDate, c => c.Caption("Last log in"));
            table.AddProperty(e => e.IsLocked);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }

        /// <summary>
        /// Table configuration for entity picker
        /// </summary>
        public static DataTableConfig PersonPickerTable()
        {
            var table = new DataTableConfig<Person, Guid>("Persons_Picker");

            table.AddProperty(e => e.FullName, c => c.SortAscending());
            table.AddProperty(e => e.EmailAddress1);

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }

        /// inheritedDoc
        [Obsolete(@"This override should be removed in future version as it creates a User account with each Person entity.
            this may not always be desired behaviour and it is not transparent in naming e.g. should rather create a spearate `CreateWithUesrAccount`
            method. Requirements are also quite application specific in that it requires both mobile and email to be mandatory and unique (may come form GDE).")]
        public override async Task<PersonAccountDto> CreateAsync(CreatePersonAccountDto input)
        {
            CheckCreatePermission();

            // Performing additional validations
            var validationResults = new List<ValidationResult>();

            if (input.TypeOfAccount == null)
                validationResults.Add(new ValidationResult("Type of account is mandatory"));

            if (string.IsNullOrWhiteSpace(input.FirstName))
                validationResults.Add(new ValidationResult("First Name is mandatory"));
            if (string.IsNullOrWhiteSpace(input.LastName))
                validationResults.Add(new ValidationResult("Last Name is mandatory"));

            // email and mobile number must be unique
            if (await MobileNoAlreadyInUse(input.MobileNumber, null))
                validationResults.Add(new ValidationResult("Specified mobile number already used by another person"));
            if (await EmailAlreadyInUse(input.EmailAddress, null))
                validationResults.Add(new ValidationResult("Specified email already used by another person"));

            if (validationResults.Any())
                throw new AbpValidationException("Please correct the errors and try again", validationResults);

            // Creating User Account to enable login into the application
            User user = await _userManager.CreateUser(
                input.UserName,
                input.TypeOfAccount?.ItemValue == (long)RefListTypeOfAccount.SQL,
                input.Password,
                input.PasswordConfirmation,
                input.FirstName,
                input.LastName,
                input.MobileNumber,
                input.EmailAddress);

            // Creating Person entity
            var person = ObjectMapper.Map<Person>(input);
            // manual map for now
            person.EmailAddress1 = input.EmailAddress;
            person.MobileNumber1 = input.MobileNumber;
            person.User = user;

            await Repository.InsertAsync(person);

            CurrentUnitOfWork.SaveChanges();

            return ObjectMapper.Map<PersonAccountDto>(person);
        }


        /// <summary>
        /// Checks is specified mobile number already used by another person
        /// </summary>
        /// <returns></returns>
        private async Task<bool> MobileNoAlreadyInUse(string mobileNo, Guid? id)
        {
            if (string.IsNullOrWhiteSpace(mobileNo))
                return false;

            return await Repository.GetAll().AnyAsync(e =>
                e.MobileNumber1.Trim().ToLower() == mobileNo.Trim().ToLower() && (id == null || e.Id != id));
        }

        /// <summary>
        /// Checks is specified email already used by another person
        /// </summary>
        /// <returns></returns>
        private async Task<bool> EmailAlreadyInUse(string email, Guid? id)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await Repository.GetAll().AnyAsync(e =>
                e.EmailAddress1.Trim().ToLower() == email.Trim().ToLower() && (id == null || e.Id != id));
        }

        [HttpGet]
        [AbpAuthorize()]
        public async Task<List<AutocompleteItemDto>> AutocompleteByName(string term)
        {
            term = (term ?? "").ToLower();

            var persons = await Repository.GetAll()
                .Where(p => (p.FullName ?? "").ToLower().Contains(term))
                .OrderBy(p => p.FirstName)
                .Take(10)
                .Select(p => new AutocompleteItemDto
                {
                    DisplayText = p.FullName.Trim(),
                    Value = p.Id.ToString()
                })
                .ToListAsync();

            return persons;
        }


        [HttpGet]
        [AbpAuthorize()]
        public async Task<List<AutocompleteItemDto>> AutocompleteByRole(string term, string role)
        {
            term = (term ?? "").ToLower();
            role = (role ?? "").ToLower();
            var persons = await _rolePersonRepository.GetAll()
                .Where(e => (e.Role.Name ?? "").ToLower() == role && (e.Person.FullName ?? "").ToLower().Contains(term))
                .Select(e => e.Person)
                .OrderBy(p => p.FirstName)
                .Take(10)
                .Select(p => new AutocompleteItemDto
                {
                    DisplayText = p.FullName.Trim(),
                    Value = p.Id.ToString()
                })
                .ToListAsync();

            return persons;
        } 
        

        /// inheritDocs
        public override async Task<PersonAccountDto> UpdateAsync(PersonAccountDto input)
        {
            var validationResults = new List<ValidationResult>();

            if (string.IsNullOrWhiteSpace(input.FirstName))
                validationResults.Add(new ValidationResult("First Name is mandatory"));
            if (string.IsNullOrWhiteSpace(input.LastName))
                validationResults.Add(new ValidationResult("Last Name is mandatory"));

            // email and mobile number must be unique
            if (await EmailAlreadyInUse(input.EmailAddress, input.Id))
                validationResults.Add(new ValidationResult("Specified email already used by another person"));

            if (await MobileNoAlreadyInUse(input.MobileNumber, input.Id))
                validationResults.Add(new ValidationResult("Specified mobile number already used by another person"));

            if (validationResults.Any())
                throw new AbpValidationException("Please correct the errors and try again", validationResults);

            return await base.UpdateAsync(input);
        }
    }
}