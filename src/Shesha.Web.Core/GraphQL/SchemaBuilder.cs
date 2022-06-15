using GraphQL.Types;
using System;

namespace Shesha.GraphQL
{
    /// <summary>
    /// GraphQL schema builder
    /// </summary>
    public class SchemaBuilder
    {
        /*
        public ObjectGraphType GenerateEntityQuery(Type entityType, Type idType)
        {
            throw new NotImplementedException();
        }
        */
        public FieldType GenerateEntityFieldType(ObjectGraphType graphType, Type entityType, Type idType)
        {
            /*
            graphType.Field<ListGraphType<PersonType>>("persons",
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
            */

            throw new NotImplementedException();
        }


        public ObjectGraphType<TEntity> GenerateEntityGraphType<TEntity>() 
        {
            /*
             public PersonType()
        {
            Field(x => x.Id);
            Field(x => x.FirstName).Description("The first day of the stay");
            Field(x => x.LastName).Description("The leaving day");
            Field<UserType>(nameof(Person.User));
        }
             */
            throw new NotImplementedException();
        }
        /*
         ObjectGraphType<Person>
         */
    }
}
