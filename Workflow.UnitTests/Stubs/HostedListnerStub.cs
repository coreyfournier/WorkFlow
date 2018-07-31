using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workflow.Core.Models;
using Workflow.Orchestration;

namespace Workflow.UnitTests.Stubs
{
    public class HostedListnerStub : WcfHostedListners
    {
        private Subscriber[] _subscribers;

        public HostedListnerStub(Subscriber[] subscribers)
        {
            _subscribers = subscribers;
        }

        /// <summary>
        /// Method to query the list of subscribers
        /// </summary>
        /// <param name="eventApiName"></param>
        /// <returns></returns>
        protected override Subscriber[] GetSubscriberForEvent(string eventApiName)
        {
            return _subscribers.Where(x => x.EventToListenToName == eventApiName).ToArray();
        }
    }
}
