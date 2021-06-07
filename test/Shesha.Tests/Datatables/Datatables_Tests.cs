using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shesha.Domain;
using Shesha.JsonLogic;
using Shesha.Scheduler.Domain;
using Shesha.Web.DataTable;
using Shesha.Web.DataTable.Model;
using Xunit;

namespace Shesha.Tests.Datatables
{
    /// <summary>
    /// Datatable tests
    /// </summary>
    public class Datatables_Tests : SheshaNhTestBase
    {
        #region private declarations

        private readonly IDataTableConfigurationStore _tableConfigStore;
        private const string PersonsTableId = "InternalPersonsTest_Index";

        public Datatables_Tests()
        {
            _tableConfigStore = LocalIocManager.Resolve<IDataTableConfigurationStore>();
        }

        private HqlResult ConvertToHql(string expression, string tableConfigId = null, Action<JsonLogic2HqlConverterContext> prepareContextAction = null)
        {
            // Parse json into hierarchical structure
            var rule = JObject.Parse(expression);

            // Create an evaluator with default operators.
            var evaluator = new JsonLogic2HqlConverter();

            var context = new JsonLogic2HqlConverterContext();
            if (!string.IsNullOrWhiteSpace(tableConfigId))
            {
                WithUnitOfWork(() =>
                {
                    var tableConfig = _tableConfigStore.GetTableConfiguration(tableConfigId);
                    DataTableHelper.FillVariablesResolvers(tableConfig, context);
                    DataTableHelper.FillContextMetadata(tableConfig, context);
                });
            }

            prepareContextAction?.Invoke(context);

            // Apply the rule to the data.
            var result = evaluator.Convert(rule, context);

            return new HqlResult
            {
                Hql = result,
                Context = context
            };
        }

        private async Task<DataTableData> FetchData(string tableId, string expression)
        {
            var controller = LocalIocManager.Resolve<DataTableController>();

            var input = new DataTableGetDataInput
            {
                Id = tableId,
                CurrentPage = 1,
                PageSize = int.MaxValue,
                Filter = new List<ColumnFilterDto>(),
                SelectedFilters = new List<SelectedStoredFilterDto>
                {
                    new SelectedStoredFilterDto
                    {
                        Expression = expression, ExpressionType = "jsonlogic", Id = "test", Name = "Test"
                    }
                },
            };

            DataTableData data = null;
            await WithUnitOfWorkAsync(async () =>
            {
                data = await controller.GetTableDataAsync<Person, Guid>(input, CancellationToken.None);
            });

            return data;
        }

        #endregion

        #region string operations

        private string _stringField_Equals_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_Equals_expression);
            
            Assert.Equal(@"(ent.FirstName = :par1)", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);
            
            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("Bob", param.Value);
        }

        [Fact]
        public async Task StringField_Equals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_Equals_expression);
            // todo: check data
        }


        private string _stringField_NotEquals_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_NotEquals_expression);

            Assert.Equal(@"(ent.FirstName <> :par1)", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);

            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("Bob", param.Value);
        }

        [Fact]
        public async Task StringField_NotEquals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_NotEquals_expression);
            // todo: check data
        }

        private string _stringField_Like_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_Like_expression);
            
            Assert.Equal(@"(ent.FirstName like '%' + :par1 + '%')", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);

            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("trick", param.Value);
        }

        [Fact]
        public async Task StringField_Like_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_Like_expression);
            // todo: check data
        }

        private string _stringField_NotLike_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_NotLike_expression);

            Assert.Equal(@"(not (ent.FirstName like '%' + :par1 + '%'))", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);

            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("trick", param.Value);
        }

        [Fact]
        public async Task StringField_NotLike_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_NotLike_expression);
            // todo: check data
        }

        private string _stringField_IsEmpty_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_IsEmpty_expression);
            
            Assert.Equal(@"(ent.FirstName is null)", hqlResult.Hql);
        }

        [Fact]
        public async Task StringField_IsEmpty_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_IsEmpty_expression);
            // todo: check data
        }

        private string _stringField_IsNotEmpty_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_IsNotEmpty_expression);

            Assert.Equal(@"(ent.FirstName is not null)", hqlResult.Hql);
        }

        [Fact]
        public async Task StringField_IsNotEmpty_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_IsNotEmpty_expression);
            // todo: check data
        }

        private string _stringField_StartsWith_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_StartsWith_expression);

            Assert.Equal(@"(ent.FirstName like :par1 + '%')", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);

            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("bo", param.Value);

        }

        [Fact]
        public async Task StringField_StartsWith_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_StartsWith_expression);
            // todo: check data
        }

        private string _stringField_EndsWith_expression = @"{
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
            var hqlResult = ConvertToHql(_stringField_EndsWith_expression);

            Assert.Equal(@"(ent.FirstName like '%' + :par1)", hqlResult.Hql);
            Assert.Single(hqlResult.Context.FilterParameters);

            var param = hqlResult.Context.FilterParameters.FirstOrDefault();
            Assert.Equal("par1", param.Key);
            Assert.Equal("ck", param.Value);
        }

        [Fact]
        public async Task StringField_EndsWith_Fetch()
        {
            var data = await FetchData(PersonsTableId, _stringField_EndsWith_expression);
            // todo: check data
        }

        #endregion

        #region bool operations

        private string _booleanField_Equals_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""IsLocked""
        },
        true
      ]
    }
  ]
}";
        [Fact]
        public void BooleanField_Equals_Convert()
        {
            var hqlResult = ConvertToHql(_booleanField_Equals_expression);
            
            Assert.Equal(@"(ent.IsLocked = 1)", hqlResult.Hql);
        }

        [Fact]
        public async Task BooleanField_Equals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _booleanField_Equals_expression);
            // todo: check data
        }

        private string _booleanField_NotEquals_expression = @"{
  ""and"": [
    {
      ""!="": [
        {
          ""var"": ""IsLocked""
        },
        false
      ]
    }
  ]
}";
        [Fact]
        public void BooleanField_NotEquals_Convert()
        {
            var hqlResult = ConvertToHql(_booleanField_NotEquals_expression);
            
            Assert.Equal(@"(ent.IsLocked <> 0)", hqlResult.Hql);
        }

        [Fact]
        public async Task BooleanField_NotEquals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _booleanField_NotEquals_expression);
            // todo: check data
        }

        #endregion

        #region nested columns resolving

        private string _nestedColumnResolver_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""User_UserName""
        },
        ""admin""
      ]
    }
  ]
}";

        [Fact]
        public void NestedColumnResolver_Convert()
        {
            var expression = _nestedColumnResolver_expression;

            // Parse json into hierarchical structure
            var rule = JObject.Parse(expression);

            // Create an evaluator with default operators.
            var evaluator = new JsonLogic2HqlConverter();

            var context = new JsonLogic2HqlConverterContext();
            context.VariablesResolvers.Add("User_UserName", "User.UserName");

            // Apply the rule to the data.
            var hql = evaluator.Convert(rule, context);


            // fill parameters using context
            Assert.Equal(@"(ent.User.UserName = :par1)", hql);
            Assert.Single(context.FilterParameters);
            Assert.Equal("admin", context.FilterParameters.FirstOrDefault().Value);
        }

        [Fact]
        public async Task NestedColumnResolver_Fetch()
        {
            var data = await FetchData(PersonsTableId, _nestedColumnResolver_expression);
            // todo: check data
        }

        #endregion

        #region datetime

        private string _datetimeField_NotEquals_Test_expression = @"{
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
            var hqlResult = ConvertToHql(_datetimeField_NotEquals_Test_expression);
            // fill parameters using context
            Assert.Equal(@"(ent.User.LastLoginDate > :par1)", hqlResult.Hql);
        }

        [Fact]
        public async Task DatetimeField_NotEquals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _datetimeField_NotEquals_Test_expression);
            // todo: check data
        }

        #endregion

        #region entity reference

        private string _entityReference_Equals_expression = @"{
  ""and"": [
    {
      ""=="": [
        {
          ""var"": ""AreaLevel1""
        },
        ""852c4011-4e94-463a-9e0d-b0054ab88f7d""
      ]
    }
  ]
}";

        [Fact]
        public void EntityReference_Equals_Convert()
        {
            var hqlResult = ConvertToHql(_entityReference_Equals_expression, PersonsTableId);
            // fill parameters using context
            Assert.Equal(@"(ent.AreaLevel1.Id = :par1)", hqlResult.Hql);
        }

        [Fact]
        public async Task EntityReference_Equals_Fetch()
        {
            var data = await FetchData(PersonsTableId, _entityReference_Equals_expression);
            // todo: check data
        }

        #endregion

        #region complex expression (with `or` and `and`)

        private string _complex_expression = @"{
  ""or"": [
    {
      ""=="": [
        {
          ""var"": ""AreaLevel1""
        },
        ""852c4011-4e94-463a-9e0d-b0054ab88f7d""
      ]
    },
    {
      ""and"": [
        {
          "">"": [
            {
              ""var"": ""User_LastLoginDate""
            },
            ""2021-04-25T08:13:55.000Z""
          ]
        },
        {
          ""=="": [
            {
              ""var"": ""IsLocked""
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
            var hqlResult = ConvertToHql(_complex_expression, PersonsTableId);
            // fill parameters using context
            Assert.Equal(@"(ent.AreaLevel1.Id = :par1) or ((ent.User.LastLoginDate > :par2) and (ent.IsLocked = 0))", hqlResult.Hql);
        }

        [Fact]
        public async Task ComplexExpression_Fetch()
        {
            var data = await FetchData(PersonsTableId, _entityReference_Equals_expression);
            // todo: check data
        }

        #endregion

        private class HqlResult
        {
            public string Hql { get; set; }
            public JsonLogic2HqlConverterContext Context { get; set; }
        }

        [Fact]
        private async Task TestConvert()
        {
            var controller = LocalIocManager.Resolve<DataTableController>();

            var input = new DataTableGetDataInput
            {
                Id = "Areas_Index",
                CurrentPage = 1,
                PageSize = int.MaxValue,
            };

            DataTableData data = null;
            await WithUnitOfWorkAsync(async () =>
            {
                data = await controller.GetTableDataAsync<Area, Guid>(input, CancellationToken.None);
            });
        }

        [Fact]
        private async Task ScheduledJob_Executions()
        {
            var controller = LocalIocManager.Resolve<DataTableController>();

            var input = new DataTableGetDataInput
            {
                Id = "ScheduledJob_Executions_test",
                CurrentPage = 1,
                PageSize = int.MaxValue,
            };

            DataTableData data = null;
            await WithUnitOfWorkAsync(async () =>
            {
                data = await controller.GetTableDataAsync<ScheduledJobExecution, Guid>(input, CancellationToken.None);
            });
        }

    }
}
