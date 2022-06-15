using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using GraphQL;
using GraphQL.Types;
using Shesha.Authorization.Users;
using Shesha.Domain;
using Shesha.GraphQL.Provider;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.GraphQL
{
    public class SheshaSchema : Schema
    {
        public SheshaSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = (PersonQuery)serviceProvider.GetService(typeof(PersonQuery)) ?? throw new InvalidOperationException();
        }
    }

    public class EmptySchema : Schema
    {
        public EmptySchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new EmptyQuery();
        }
    }

    public class PersonDto 
    { 
        public Guid Id { get; set; }
        public Guid FirstName { get; set; }
        public Guid LastName { get; set; }
    }

    public class PersonType : ObjectGraphType<Person>
    {
        public PersonType()
        {
            Field(x => x.Id);
            Field(x => x.FirstName).Description("The first day of the stay");
            Field(x => x.LastName).Description("The leaving day");
            Field<UserType>(nameof(Person.User));
        }
    }
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field(x => x.Id);
            Field(x => x.EmailAddress);
        }
    }

    public class PersonQuery : ObjectGraphType 
    {
        public PersonQuery(IRepository<Person, Guid> personRepository, IRepository<User, Int64> userRepository, IUnitOfWorkManager unitOfWorkManager) 
        {
            Field<ListGraphType<PersonType>>("persons",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<IdGraphType>
                    {
                        Name = "id"
                    },
                    new QueryArgument<StringGraphType>
                    {
                        Name = "firstName"
                    },
                }),
                resolve: context =>
                {
                    var query = personRepository.GetAll();
                    var persons = query.ToList();

                    return persons;
                }
            );
            Field<ListGraphType<UserType>>("users",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<IdGraphType>
                    {
                        Name = "id"
                    },
                    new QueryArgument<StringGraphType>
                    {
                        Name = "username"
                    },
                }),
                resolve: context =>
                {
                    var query = userRepository.GetAll();
                    var users = query.ToList();

                    return users;
                }
            );
        }
    }
}
