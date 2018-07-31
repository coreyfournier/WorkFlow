using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Remoting.Messaging;
using Workflow.Core.Models;
using Workflow.Core.Persistance;
using Workflow.Orchestration;
using Workflow.Orchestration.Queues;

namespace Workflow.UnitTests
{
    [TestClass]
    public class OrchestrationTests
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(OrchestrationTests));

        public const string EventApiName = "one-event-api-name";
        
        /// <summary>
        /// All registered subscribers
        /// </summary>
        static Subscriber[] _eventSubscribers = new Subscriber[] {
            new Subscriber("Subscriber1",
                EventApiName,
                typeof(Activity1)) { }
        };

        /// <summary>
        /// Mimics a service or long running listner.
        /// </summary>
        Stubs.HostedListnerStub _hostedListner = new Stubs.HostedListnerStub(_eventSubscribers);

        [TestInitialize]
        public void Setup()
        {
            //Override the delay set in the workflow so they don't take forever to complete.
            Workflow.Core.Utilities.Retry.OverrideForUnitTests = new TimeSpan(0, 0, 2);
            //Set the in memory persistance store.
            PersistanceHelper.Store = new Workflow.Core.Persistance.InMemoryStore();            
        }

        /// <summary>
        /// Ensurs that an event was raised and the subscriber was notified.
        /// </summary>
        [TestMethod]
        public void EventRaisedTest()
        {
            var itemList = new int[] { 1, 2, 3, 4 };
            var eventArgs = new DataEventArgs<int[]>(itemList)
            {
                EventDate = DateTime.Now,
                SourceApiName = EventApiName,
                SourceType = new TypeWrapper(),
                SourceTransactionId = "Source-transactionId"
            };
            var observedChangeQueue = new Stubs.ObservedChangeQueue(_hostedListner);
            var subscriberQueue = new Stubs.SubscriberQueueStub(_hostedListner);

            //Register the queues
            DependencyContainer.Register<IObservedChangeQueue>(observedChangeQueue);
            DependencyContainer.Register<ISubscriberQueue>(subscriberQueue);

            using (IObservedChangeQueue e = DependencyContainer.Resolve<IObservedChangeQueue>())
            {
                e.ChangeObserved(eventArgs);
            }

            //Verify that there was the right count of events.
            Assert.AreEqual(1, observedChangeQueue.ChangesObserved);
            Assert.AreEqual(1, subscriberQueue.SubscribersNotified);
        }

        /// <summary>
        /// Fixes issues with the log4net and using logical thread context.
        /// https://stackoverflow.com/questions/23661372/log4net-logicalthreadcontext-and-unit-test-cases
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            CallContext.FreeNamedDataSlot("log4net.Util.LogicalThreadContextProperties");
        }
    }
}
