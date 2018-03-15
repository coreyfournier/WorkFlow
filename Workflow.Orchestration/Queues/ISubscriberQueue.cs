using System.ServiceModel;

namespace Workflow.Orchestration.Queues
{
    [ServiceContract]
    public interface ISubscriberQueue
    {
        /// <summary>
        /// Puts the notification in the queue and allows it to be removed.
        /// </summary>
        /// <param name="notification"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [OperationContract(IsOneWay = true)]
        void SubscriberNotification(Models.SubscriberNotification notification);
    }
}
