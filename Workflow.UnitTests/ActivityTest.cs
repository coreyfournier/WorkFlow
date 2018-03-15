using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Workflow.Core;
using Workflow.Core.Models;
using Workflow.Core.Persistance;
using System.Activities;
using System.Threading;

namespace Workflow.UnitTests
{
    [TestClass]
    public class ActivityTest
    {
        [TestInitialize]
        public void Setup()
        {       
            //Override the delay set in the workflow so they don't take forever to complete.
            Workflow.Core.Utilities.Retry.OverrideForUnitTests = new TimeSpan(0, 0, 2);
            //Set the in memory persistance store.
            PersistanceHelper.Store = new Workflow.Core.Persistance.InMemoryStore();
        }

        [TestMethod]
        public void ActivityExampleTest()
        {
            var application = CreateApplication();
            bool isCompleted = false;
            bool isGoingToBePersisted = false;

            application.ActivityCompletedEvent += (WorkflowApplicationCompletedEventArgs args) =>
            {
                isCompleted = true;
            };

            application.ActivityWillBePersistedEvent += (WorkflowApplicationIdleEventArgs args) =>
            {
                isGoingToBePersisted = true;
                Assert.AreEqual(1, FailOnceActivity.CallCount, "Call count is not as expected, it should be persisted after it failed once.");
            };

            //Reset it.
            FailOnceActivity.CallCount = 0;
            FireWorkflow(application);

            Assert.IsTrue(isCompleted);

            Assert.IsTrue(isGoingToBePersisted);
        }

        [TestMethod]
        public void ActivityWithLoopExampleTest()
        {
            var itemList = new int[] { 1, 2, 3, 4 };
            var eventArgs = new DataEventArgs<int[]>(itemList)
            {
                EventDate = DateTime.Now,
                SourceApiName = "",
                SourceType = new TypeWrapper(),
                SourceTransactionId = "Source-transactionId"
            };
            //Reset it.
            FailOnceActivity.CallCount = 0;
            var activity = new ActivityLoopWith1Error();
            var application = new ApplicationHelper(activity, activity.GetIdentity(), eventArgs);            
            bool isCompleted = false;
            bool isGoingToBePersisted = false;

            application.ActivityCompletedEvent += (WorkflowApplicationCompletedEventArgs args) =>
            {
                isCompleted = true;
            };

            application.ActivityWillBePersistedEvent += (WorkflowApplicationIdleEventArgs args) =>
            {
                isGoingToBePersisted = true;
                Assert.AreEqual(1, FailOnceActivity.CallCount, "Call count is not as expected, it should be persisted after it failed once.");
            };

            FireWorkflow(application);

            Assert.IsTrue(isCompleted);
            Assert.IsTrue(isGoingToBePersisted);

            Assert.AreEqual(itemList.Length + 1 /*+1 for the zero offset.*/, FailOnceActivity.CallCount,"The activity didn't loop the expected number of times.");
        }


        private ApplicationHelper CreateApplication()
        {
            var eventArgs = new DataEventArgs<ObjectItem>(new ObjectItem()
            {
                Option1= "This is option1",
                Option2 = "This is option1"
            })
            {
                EventDate = DateTime.Now,
                SourceApiName = "",
                SourceType = new TypeWrapper(),
                SourceTransactionId = "Source-transactionId"
            };

            var activity = new Activity1();
            
            return new ApplicationHelper(activity, activity.GetIdentity(), eventArgs);
        }


        /// <summary>
        /// Executes the workflow as an synchronous operation. It should only be done this way for unit testing.
        /// </summary>
        /// <param name="application"></param>
        private void FireWorkflow(ApplicationHelper application)
        {
            AutoResetEvent waitHandler = new AutoResetEvent(false);
            //Because the run is asynchronous i am using the wait handler to block until complete.
            application.ActivityCompletedEvent += (WorkflowApplicationCompletedEventArgs args) => {
                //Stop it from waiting
                waitHandler.Set();
            };

            application.ActivityAbortedEvent += (WorkflowApplicationAbortedEventArgs args) => {
                //Stop it from waiting
                waitHandler.Set();
            };

            application.ActivityUnhandledExceptionEvent += (WorkflowApplicationUnhandledExceptionEventArgs args) => {
                //Stop it from waiting
                //waitHandler.Set();
            };

            application.Run();

            //Wait no more than the time span for the activity to complete.
            waitHandler.WaitOne(new TimeSpan(0, 0, 60));
        }
    }
}
