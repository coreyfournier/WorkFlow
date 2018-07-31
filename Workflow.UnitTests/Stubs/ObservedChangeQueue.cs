using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workflow.Core.Models;
using Workflow.Orchestration;
using Workflow.Orchestration.Queues;

namespace Workflow.UnitTests.Stubs
{
    public class ObservedChangeQueue : IObservedChangeQueue
    {
        WcfHostedListners _listner;
        public int ChangesObserved = 0;

        public ObservedChangeQueue(WcfHostedListners listner)
        {
            _listner = listner;
        }

        public void ChangeObserved(DataEventArgs @event)
        {
            _listner.ChangeObserved(@event);
            ChangesObserved++;
        }

        public void Dispose()
        {
            
        }
    }
}
