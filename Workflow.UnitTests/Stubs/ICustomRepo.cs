using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.UnitTests.Stubs
{
    public interface ICustomRepo
    {
        string Basic1Operation();

        List<string> Basic2Operation();
    }
}
