using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workflow.Orchestration;
using Workflow.Orchestration.Models;
using Workflow.Orchestration.Queues;

namespace Workflow.UnitTests.Stubs
{
    public class SubscriberQueueStub : ISubscriberQueue
    {
        WcfHostedListners _listner;
        public int SubscribersNotified = 0;

        public SubscriberQueueStub(WcfHostedListners listner)
        {
            _listner = listner;
        }

        public void Dispose()
        {
            
        }

        public void SubscriberNotification(SubscriberNotification notification)
        {
            _listner.SubscriberNotification(notification);
            SubscribersNotified++;
        }
    }
}
