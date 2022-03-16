using AutoMapper;
using Moq;
using Shesha.Domain.Enums;
using Shesha.DynamicEntities.Mapper;
using System;
using Xunit;

namespace Shesha.Tests.DynamicEntities
{
    public class NumericToEnumTypeConverter_Test: SheshaNhTestBase
    {
        [Fact]
        public void ConvertInt64ToPersonTitle_Test()
        {
            RefListPersonTitle destination = 0;
            Int64 source = 1;

            var converter = new NumericToEnumTypeConverter<Int64, RefListPersonTitle>();

            destination = converter.Convert(source, destination, GetMockResolutionContext());

            Assert.Equal(RefListPersonTitle.Mr, destination);
        }

        [Fact]
        public void ConvertInt64ToIntEnum_Test()
        {
            IntItems destination = 0;
            Int64 source = 1;

            var converter = new NumericToEnumTypeConverter<Int64, IntItems>();

            destination = converter.Convert(source, destination, GetMockResolutionContext());

            Assert.Equal(IntItems.Value1, destination);
        }

        [Fact]
        public void ConvertInt64ToInt64Enum_Test()
        {
            Int64Items destination = 0;
            Int64 source = 1;

            var converter = new NumericToEnumTypeConverter<Int64, Int64Items>();

            destination = converter.Convert(source, destination, GetMockResolutionContext());

            Assert.Equal(Int64Items.Value1, destination);
        }

        [Fact]
        public void ConvertInt64ToByteEnum_Test()
        {
            ByteItems destination = 0;
            Int64 source = 1;

            var converter = new NumericToEnumTypeConverter<Int64, ByteItems>();

            destination = converter.Convert(source, destination, GetMockResolutionContext());

            Assert.Equal(ByteItems.Value1, destination);
        }

        private ResolutionContext GetMockResolutionContext() 
        {
            var options = new Mock<IMappingOperationOptions>();
            var mapper = new Mock<IRuntimeMapper>();

            // ToDo: ABP662
            return null;//new ResolutionContext(options.Object, mapper.Object);
        }

        public enum IntItems : int 
        { 
            Value1 = 1,
            Value2 = 2,
        }

        public enum ByteItems : byte
        {
            Value1 = 1,
            Value2 = 2,
        }

        public enum Int64Items : Int64
        {
            Value1 = 1,
            Value2 = 2,
        }
    }
}
