using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Workflow.Core;

namespace Workflow.UnitTests
{
    [TestClass]
    public class TypeConversionTest
    {
        [TestMethod]
        public void ConvertType_Successful()
        {
            var args1 = new Workflow.Core.Models.DataEventArgs<CustomType[]>(
                new CustomType[] {
                    new CustomType()
                    {
                        FirstName = "first",
                        LastName = "last"
                    }});

            var result = args1.DataToType<CustomType[]>();

            Assert.AreEqual("first", result[0].FirstName);
        }

        /// <summary>
        /// This should fail with an exception as the type is different between the args and the convert type
        /// </summary>
        [TestMethod, ExpectedException(typeof(SeralizationFailedException))]
        public void ConvertType_Failure_XML()
        {
            var args1 = new Workflow.Core.Models.DataEventArgs<CustomType>(                
                    new CustomType()
                    {
                        FirstName = "first",
                        LastName = "last"
                    });

            //I should be trying to convert an object, and not an array
            var result = args1.DataToType<CustomType[]>();
        }

        /// <summary>
        /// This should fail with an exception as the type is different between the args and the convert type
        /// </summary>
        [TestMethod, ExpectedException(typeof(SeralizationFailedException))]
        public void ConvertType_Failure_JSON()
        {
            var args1 = new Workflow.Core.Models.DataEventArgs<CustomType>(
                    new CustomType()
                    {
                        FirstName = "first",
                        LastName = "last"
                    })
            {
                 Seralization = Core.Models.SeralizeAs.Json
            };

            //I should be trying to convert an object, and not an array
            var result = args1.DataToType<CustomType[]>();
        }

        public class CustomType
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
