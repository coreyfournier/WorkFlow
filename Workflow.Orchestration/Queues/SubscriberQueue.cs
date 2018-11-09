using System.ServiceModel;
using System;
using log4net;

namespace Workflow.Orchestration.Queues
{
    public class SubscriberQueue : ClientBase<ISubscriberQueue>, ISubscriberQueue
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SubscriberQueue));

        public SubscriberQueue()
            : base("SubscriberEndPoint")
        {

        }

        /// <summary>
        /// Puts the notification in the queue. Properties on the input are validated
        /// </summary>
        /// <param name="notification"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [OperationBehavior(TransactionAutoComplete = true, TransactionScopeRequired = true)]
        public void SubscriberNotification(Models.SubscriberNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
            if(notification.Event == null)
                throw new ArgumentNullException(nameof(notification.Event));
            if(notification.Subscriber == null)
                throw new ArgumentNullException(nameof(notification.Subscriber));
            
            try
            {
                this.Channel.SubscriberNotification(notification);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw ex;
            }
        }
    }
}
