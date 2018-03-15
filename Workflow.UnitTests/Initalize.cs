using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.UnitTests
{
    [TestClass()]
    public class Initalize
    {
        /// <summary>
        /// This enables log4net in the unit tests. https://stackoverflow.com/questions/24692795/mstest-how-do-i-initialize-log4net-for-a-unittest-project
        /// </summary>
        /// <param name="tc"></param>
        [AssemblyInitialize()]
        public static void IntitalizeTests(TestContext tc)
        {
            //Causes the LogicalThreadContext to get loaded so the transactionId can be retreived.
            System.Configuration.ConfigurationManager.GetSection("adfasd");

            log4net.Config.XmlConfigurator.Configure();
        }
    }
}
