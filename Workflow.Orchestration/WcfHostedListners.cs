using log4net;
using System;
using System.Linq;
using System.ServiceModel;
using Workflow.Core;
using Workflow.Core.Models;
using Workflow.Orchestration.Models;
using Workflow.Orchestration.Queues;

namespace Workflow.Orchestration
{
    /// <summary>
    /// Functions that listen for notifications from the queue. This is abstract so it allows the consumer to host the functions in their own code.
    /// To consume it create a class that inherits this and then host the new class (type) as a wcf service. See: https://msdn.microsoft.com/en-us/library/system.servicemodel.servicehost(v=vs.110).aspx
    /// </summary>
    public abstract class WcfHostedListners: IObservedChangeQueue, ISubscriberQueue
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WcfHostedListners));
        public enum Operation {
            ChangeObserved,
            SubscriberNotification
        }

        /// <summary>
        /// Gets all subscribers to the event specified. 
        /// </summary>
        /// <param name="eventApiName">Api name of the event</param>
        /// <returns></returns>
        protected abstract Subscriber[] GetSubscriberForEvent(string eventApiName);

        /// <summary>
        /// Notification when the operation started and by what transaction.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="transactionId"></param>
        /// <param name="data"></param>
        /// <param name="instanceId">Id of the activity if this is a subscriber notification event</param>
        public delegate void OperationStarted(Operation name, string transactionId, Object data, string instanceId);

        /// <summary>
        /// Notification when the operation ended and by what transaction.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="transactionId"></param>
        /// <param name="error">Exception when occured null otherwise</param>
        /// <param name="data"></param>
        /// <param name="instanceId">Id of the activity if this is a subscriber notification event</param>
        public delegate void OperationEnded(Operation name, string transactionId, Exception error, Object data, string instanceId);

        /// <summary>
        /// Allows external party to get notified when an operation has started.
        /// </summary>
        public event OperationStarted OperationStartedEvent;
        /// <summary>
        /// Allows an external party to get notified when an operation has ended.
        /// </summary>
        public event OperationEnded OperationEndedEvent;

        /// <summary>
        /// A new change occured. Find the subscribers and put the notification in the queue
        /// </summary>
        /// <param name="event"></param>
        [OperationBehavior(TransactionAutoComplete = true, TransactionScopeRequired = true)]
        public void ChangeObserved(DataEventArgs @event)
        {            
            log4net.LogicalThreadContext.Properties["TransactionId"] = @event.SourceTransactionId;
            _log.Debug("Change Observed Event=" + @event.SourceApiName);

            OperationStartedEvent?.Invoke(Operation.ChangeObserved, @event.SourceTransactionId, @event, null);

            //Find all subscribers and then put the notification in the queue
            using (var subscriberQueue = new SubscriberQueue())
            {
                var allSubscribers = GetSubscriberForEvent(@event.SourceApiName);

                if (allSubscribers == null)
                {
                    _log.Warn("No subscribers found for the SourceApiName=" + @event.SourceApiName + ". Maybe it is disabled? This event will now be considered as handled so it does not fill up the queue.");
                }
                else
                {
                    //For each subscriber referenced put the notification in the queue.
                    foreach (var subscriber in allSubscribers)
                    {
                        //_log.Debug()
                        subscriberQueue.SubscriberNotification(new SubscriberNotification()
                        {
                            Event = @event,
                            CreatedDate = DateTime.Now,
                            Subscriber = subscriber
                        });
                    }
                }
            }

            OperationEndedEvent?.Invoke(Operation.ChangeObserved, @event.SourceTransactionId, null, @event, null);
        }

        /// <summary>
        /// Notify the subscriber by running the workflow
        /// </summary>
        /// <param name="notification"></param>
        [OperationBehavior(TransactionAutoComplete = true, TransactionScopeRequired = true)]
        public void SubscriberNotification(SubscriberNotification notification)
        {            
            log4net.LogicalThreadContext.Properties["TransactionId"] = notification.Event.SourceTransactionId;

            _log.Info("Notifying subscriber Workflow=" + notification.Subscriber.WorkFlowType.FullName);
            
            using (ApplicationHelper application = new ApplicationHelper(notification.Subscriber.GetActivity(), notification.Subscriber.GetIdentity(), notification.Event))
            {
                if (OperationEndedEvent != null)
                {
                    application.ActivityStartedEvent += (Guid instanceId, System.Activities.Activity activity) =>
                    {
                        OperationStartedEvent?.Invoke(Operation.SubscriberNotification, notification.Event.SourceTransactionId, notification, instanceId.ToString());
                    };

                    application.ActivityUnhandledExceptionEvent += (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs args) =>
                    {
                        OperationEndedEvent?.Invoke(Operation.SubscriberNotification, notification.Event.SourceTransactionId, args.UnhandledException, notification, args.InstanceId.ToString());
                    };

                    application.ActivityCompletedEvent += (System.Activities.WorkflowApplicationCompletedEventArgs args) =>
                    {
                        OperationEndedEvent?.Invoke(Operation.SubscriberNotification, notification.Event.SourceTransactionId, null, notification, args.InstanceId.ToString());
                    };
                }
                
                application.Run();
            }            
        }

        public void Dispose()
        {
            //Don't need to do anything
        }
    }
}
