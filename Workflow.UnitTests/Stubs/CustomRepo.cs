using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.UnitTests.Stubs
{
    public class CustomRepo : ICustomRepo
    {
        public CustomRepo(string someInput)
        {

        }
        public string Basic1Operation()
        {
            return "I did something";
        }

        public List<string> Basic2Operation()
        {
            return new List<string>() { "Something 1","Something 2" };
        }
    }
}
