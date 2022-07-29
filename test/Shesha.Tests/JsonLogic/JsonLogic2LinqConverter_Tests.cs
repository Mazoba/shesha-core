using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Shesha.Authorization.Users;
using Shesha.Domain;
using Shesha.Extensions;
using Shesha.JsonLogic;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.JsonLogic
{
    /// <summary>
    /// JsonLogic2LinqConverter tests
    /// </summary>
    public class JsonLogic2LinqConverter_Tests: SheshaNhTestBase
    {
        private Expression<Func<T, bool>> ConvertToExpression<T>(string jsonLogicExpression)
        {
            var converter = Resolve<IJsonLogic2LinqConverter>();

            // Parse json into hierarchical structure
            var jsonLogic = JObject.Parse(jsonLogicExpression);

            var expression = converter.ParseExpressionOf<T>(jsonLogic);
            return expression;
        }

        private async Task<List<T>> TryFetchData<T, TId>(string jsonLogicExpression, Func<IQueryable<T>, IQueryable<T>> prepareQueryable = null, Action<List<T>> assertions = null) where T: class, IEntity<TId>
        {
            var expression = ConvertToExpression<T>(jsonLogicExpression);

            var repository = LocalIocManager.Resolve<IRepository<T, TId>>();
            var asyncExecuter = LocalIocManager.Resolve<IAsyncQueryableExecuter>();

            List<T> data = null;
            
            await WithUnitOfWorkAsync(async () => {
                var query = repository.GetAll().Where(expression);

                if (prepareQueryable != null)
                    query = prepareQueryable.Invoke(query);

                data = await asyncExecuter.ToListAsync(query);

                assertions?.Invoke(data);
            });

            return data;
        }

        #region string operations

        private readonly string _stringField_Equals_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""FirstName""
        },
        ""Bob""
      ]
    }
  ]
}";

        [Fact]
        public void StringField_Equals_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_Equals_expression);
            Assert.Equal(@"ent => (ent.FirstName == ""Bob"")", expression.ToString());
        }

        [Fact]
        public async Task StringField_Equals_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_Equals_expression);
            Assert.NotNull(data);
        }


        private readonly string _stringField_NotEquals_expression = @"{
  ""and"": [
    {
      ""!="": [
        {
          ""var"": ""FirstName""
        },
        ""Bob""
      ]
    }
  ]
}";
        [Fact]
        public void StringField_NotEquals_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_NotEquals_expression);
            Assert.Equal(@"ent => (ent.FirstName != ""Bob"")", expression.ToString());
        }

        [Fact]
        public async Task StringField_NotEquals_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_NotEquals_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_Like_expression = @"{
  ""and"": [
    {
      ""in"": [
        ""trick"",
        {
          ""var"": ""FirstName""
        }
      ]
    }
  ]
}";
        [Fact]
        public void StringField_Like_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_Like_expression);
            Assert.Equal(@"ent => ent.FirstName.Contains(""trick"")", expression.ToString());
        }

        [Fact]
        public async Task StringField_Like_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_Like_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_NotLike_expression = @"{
  ""and"": [
    {
      ""!"": {
        ""in"": [
          ""trick"",
          {
            ""var"": ""FirstName""
          }
        ]
      }
    }
  ]
}";

        [Fact]
        public void StringField_NotLike_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_NotLike_expression);
            Assert.Equal(@"ent => Not(ent.FirstName.Contains(""trick""))", expression.ToString());
        }

        [Fact]
        public async Task StringField_NotLike_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_NotLike_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_IsEmpty_expression = @"{
  ""and"": [
    {
      ""!"": {
        ""var"": ""FirstName""
      }
    }
  ]
}";
        [Fact]
        public void StringField_IsEmpty_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_IsEmpty_expression);
            Assert.Equal(@"ent => (ent.FirstName == null)", expression.ToString());
        }

        [Fact]
        public async Task StringField_IsEmpty_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_IsEmpty_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_IsNotEmpty_expression = @"{
  ""and"": [
    {
      ""!!"": {
        ""var"": ""FirstName""
      }
    }
  ]
}";

        [Fact]
        public void StringField_IsNotEmpty_Test()
        {
            var expression = ConvertToExpression<Person>(_stringField_IsNotEmpty_expression);
            Assert.Equal(@"ent => (ent.FirstName != null)", expression.ToString());
        }

        [Fact]
        public async Task StringField_IsNotEmpty_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_IsNotEmpty_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_StartsWith_expression = @"{
  ""and"": [
    {
      ""startsWith"": [
        {
          ""var"": ""FirstName""
        },
        ""bo""
      ]
    }
  ]
}";

        [Fact]
        public void StringField_StartsWith_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_StartsWith_expression);
            Assert.Equal(@"ent => ent.FirstName.StartsWith(""bo"")", expression.ToString());
        }

        [Fact]
        public async Task StringField_StartsWith_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_StartsWith_expression);
            Assert.NotNull(data);
        }

        private readonly string _stringField_EndsWith_expression = @"{
  ""and"": [
    {
      ""endsWith"": [
        {
          ""var"": ""FirstName""
        },
        ""ck""
      ]
    }
  ]
}";

        [Fact]
        public void StringField_EndsWith_Convert()
        {
            var expression = ConvertToExpression<Person>(_stringField_EndsWith_expression);
            Assert.Equal(@"ent => ent.FirstName.EndsWith(""ck"")", expression.ToString());
        }

        [Fact]
        public async Task StringField_EndsWith_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_stringField_EndsWith_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region bool operations

        private readonly string _booleanField_Equals_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""OtpEnabled""
        },
        true
      ]
    }
  ]
}";
        [Fact]
        public void BooleanField_Equals_Convert()
        {
            var expression = ConvertToExpression<User>(_booleanField_Equals_expression);

            Assert.Equal($@"ent => (ent.{nameof(User.OtpEnabled)} == True)", expression.ToString());
        }

        [Fact]
        public async Task BooleanField_Equals_Fetch()
        {
            var data = await TryFetchData<User, Int64>(_booleanField_Equals_expression);
            Assert.NotNull(data);
        }

        private readonly string _booleanField_NotEquals_expression = @"{
  ""and"": [
    {
      ""!="": [
        {
          ""var"": ""OtpEnabled""
        },
        true
      ]
    }
  ]
}";
        [Fact]
        public void BooleanField_NotEquals_Convert()
        {
            var expression = ConvertToExpression<User>(_booleanField_NotEquals_expression);

            Assert.Equal($@"ent => (ent.{nameof(User.OtpEnabled)} != True)", expression.ToString());
        }

        [Fact]
        public async Task BooleanField_NotEquals_Fetch()
        {
            var data = await TryFetchData<User, Int64>(_booleanField_NotEquals_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region nested columns resolving

        private readonly string _nestedColumnResolver_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""User.UserName""
        },
        ""admin""
      ]
    }
  ]
}";

        [Fact]
        public void NestedColumnResolver_Convert()
        {
            var expression = ConvertToExpression<Person>(_nestedColumnResolver_expression);

            Assert.Equal(@"ent => (ent.User.UserName == ""admin"")", expression.ToString());
        }

        [Fact]
        public async Task NestedColumnResolver_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_nestedColumnResolver_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region datetime

        private readonly string _datetimeField_NotEquals_Test_expression = @"{
  ""and"": [
    {
      "">"": [
        {
          ""var"": ""User.LastLoginDate""
        },
        ""2021-04-25T06:13:50.000Z""
      ]
    }
  ]
}";

        [Fact]
        public void DatetimeField_NotEquals_Convert()
        {
            var expression = ConvertToExpression<Person>(_datetimeField_NotEquals_Test_expression);

            Assert.Equal(@"ent => (ent.User.LastLoginDate > Convert(25/04/2021 6:13:50 AM, Nullable`1))", expression.ToString());
        }

        [Fact]
        public async Task DatetimeField_NotEquals_Fetch()
        {
            var data = await TryFetchData<Person, Guid>(_datetimeField_NotEquals_Test_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region entity reference

        private readonly string _entityReference_Equals_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""ShaRole""
        },
        ""852c4011-4e94-463a-9e0d-b0054ab88f7d""
      ]
    }
  ]
}";

        [Fact]
        public void EntityReference_Equals_Convert()
        {
            var expression = ConvertToExpression<ShaRolePermission>(_entityReference_Equals_expression);

            Assert.Equal($@"ent => (ent.{nameof(ShaRolePermission.ShaRole)}.Id == ""852c4011-4e94-463a-9e0d-b0054ab88f7d"".ToGuid())", expression.ToString());            
        }

        [Fact]
        public async Task EntityReference_Equals_Fetch()
        {
            var data = await TryFetchData<ShaRolePermission, Guid>(_entityReference_Equals_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region complex expression (with `or` and `and`)

        private readonly string _complex_expression = @"{
  ""or"": [
    {
      ""=="": [
        {
          ""var"": ""ShaRole""
        },
        ""852c4011-4e94-463a-9e0d-b0054ab88f7d""
      ]
    },
    {
      ""and"": [
        {
          "">"": [
            {
              ""var"": ""ShaRole.LastModificationTime""
            },
            ""2021-04-25T08:13:55.000Z""
          ]
        },
        {
          ""=="": [
            {
              ""var"": ""IsGranted""
            },
            false
          ]
        }
      ]
    }
  ]
}";

        [Fact]
        public void ComplexExpression_Convert()
        {
            var expression = ConvertToExpression<ShaRolePermission>(_complex_expression);

            Assert.Equal(@"ent => ((ent.ShaRole.Id == ""852c4011-4e94-463a-9e0d-b0054ab88f7d"".ToGuid()) OrElse ((ent.ShaRole.LastModificationTime > Convert(25/04/2021 8:13:55 AM, Nullable`1)) AndAlso (ent.IsGranted == False)))", expression.ToString());
        }

        [Fact]
        public async Task ComplexExpression_Fetch()
        {
            var data = await TryFetchData<ShaRolePermission, Guid>(_entityReference_Equals_expression);
            Assert.NotNull(data);
        }

        #endregion

        #region sorting

        [Fact]
        public async Task ComplexExpression_Fetch_SortBy_Asc()
        {
            var data = await TryFetchData<ShaRolePermission, Guid>(_entityReference_Equals_expression, queryable => 
                queryable.OrderBy(nameof(ShaRolePermission.Permission))
            );

            Assert.NotNull(data);

            data.Should().BeInAscendingOrder(e => e.Permission);
        }

        [Fact]
        public async Task ComplexExpression_Fetch_SortBy_Desc()
        {
            var data = await TryFetchData<ShaRolePermission, Guid>(_entityReference_Equals_expression, queryable =>
                queryable.OrderByDescending(nameof(ShaRolePermission.Permission))
            );

            Assert.NotNull(data);

            data.Should().BeInDescendingOrder(e => e.Permission);
        }

        [Fact]
        public async Task ComplexExpression_Fetch_SortBy_NestedEntity_Property_Asc()
        {
            await TryFetchData<ShaRolePermission, Guid>(_entityReference_Equals_expression, 
                queryable => queryable.OrderBy($"{nameof(ShaRolePermission.ShaRole)}.{nameof(ShaRolePermission.ShaRole.Name)}"),
                data => {
                    Assert.NotNull(data);
                    
                    var roleNames = data.Select(e => e.ShaRole?.Name).ToList();
                    roleNames.Should().BeInAscendingOrder(e => e);
                }
            );
        }

        [Fact]
        public async Task ComplexExpression_Fetch_SortBy_Title_Asc()
        {
            await TryFetchData<User, Int64>(_booleanField_NotEquals_expression,
                queryable => queryable.OrderBy($"{nameof(User.TypeOfAccount)}"),
                data => {
                    Assert.NotNull(data);

                    var refListHelper = Resolve<IReferenceListHelper>();
                    
                    var titlesWithDisplayText = data.Select(e =>
                        {
                            var displayText = e.TypeOfAccount.HasValue
                                ? refListHelper.GetItemDisplayText("Shesha.Framework", "TypeOfAccount", (Int64)e.TypeOfAccount.Value)
                                : null;


                            return new { 
                                ItemValue = (Int64?)e.TypeOfAccount,
                                ItemText = displayText
                            };
                        })
                        .ToList();

                    titlesWithDisplayText.Should().BeInAscendingOrder(e => e.ItemText);
                }
            );
        }

        #endregion
    }
}
